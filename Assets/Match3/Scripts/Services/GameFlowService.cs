#nullable enable

using System;
using Match3.Configs;
using Match3.Controllers;
using Match3.Core.Enums;
using Match3.Core.Models;
using Match3.Services.Level;
using Match3.Services.SceneManagement;
using Match3.Views;
using R3;
using UnityEngine;
using Zenject;

namespace Match3.Services
{
    public sealed class GameFlowService : IInitializable, IDisposable
    {
        private readonly LevelService         _levelService;
        private readonly ProgressService      _progressService;
        private readonly RewardService        _rewardService;
        private readonly WorldMapConfig       _worldMapConfig;
        private readonly GemConfig            _gemConfig;
        private readonly RewardIconConfig     _rewardIconConfig;
        private readonly ISceneManagerService _sceneManagerService;
        private readonly GameLoopController   _gameLoopController;
        private readonly LevelTaskPopupView   _levelTaskPopupView;
        private readonly StageRewardPopupView _stageRewardPopupView;

        private readonly CompositeDisposable _disposables = new();

        private readonly Subject<Sprite?> _onLevelLost = new();
        public Observable<Sprite?> OnLevelLost => _onLevelLost;

        [Inject]
        public GameFlowService(
            LevelService         levelService,
            ProgressService      progressService,
            RewardService        rewardService,
            WorldMapConfig       worldMapConfig,
            GemConfig            gemConfig,
            RewardIconConfig     rewardIconConfig,
            ISceneManagerService sceneManagerService,
            GameLoopController   gameLoopController,
            LevelTaskPopupView   levelTaskPopupView,
            StageRewardPopupView stageRewardPopupView)
        {
            _levelService         = levelService;
            _progressService      = progressService;
            _rewardService        = rewardService;
            _worldMapConfig       = worldMapConfig;
            _gemConfig            = gemConfig;
            _rewardIconConfig     = rewardIconConfig;
            _sceneManagerService  = sceneManagerService;
            _gameLoopController   = gameLoopController;
            _levelTaskPopupView   = levelTaskPopupView;
            _stageRewardPopupView = stageRewardPopupView;
        }

        // ── IInitializable ────────────────────────────────────────────────────

        public void Initialize()
        {
            _stageRewardPopupView.Hide();

            _levelService.OnLevelWon
                .Take(1)
                .Subscribe(_ => HandleWin())
                .AddTo(_disposables);

            _levelService.OnLevelLost
                .Take(1)
                .Subscribe(_ => HandleLose())
                .AddTo(_disposables);

            _stageRewardPopupView.OnClaimClicked
                .Subscribe(_ => GoToMap())
                .AddTo(_disposables);

            _levelTaskPopupView.OnPlayClicked
                .Take(1)
                .Subscribe(_ => StartPlay())
                .AddTo(_disposables);

            ShowCurrentLevelTask();
        }

        // ── Попап задания ─────────────────────────────────────────────────────

        private void ShowCurrentLevelTask()
        {
            var address = _progressService.CurrentAddress.CurrentValue;
            var stage   = _worldMapConfig.GetStage(address.CountryIndex, address.StageIndex);
            var config  = stage?.GetLevel(address.LevelIndex);

            if (stage == null || config == null)
            {
                Debug.LogError($"[GameFlowService] Stage/LevelConfig not found for {address}");
                return;
            }

            var objectives = config.Objectives;
            var icons      = new UnityEngine.Sprite?[objectives.Length];
            for (var i = 0; i < objectives.Length; i++)
                icons[i] = _gemConfig.GetVisual(objectives[i].nodeType)?.Sprite;

            _levelTaskPopupView.Show(
                levelTitle:      $"{stage.StageName} — Уровень {address.LevelIndex + 1}",
                characterSprite: stage.CharacterSprite,
                objectives:      objectives,
                objectiveIcons:  icons);
        }

        private void StartPlay()
        {
            _levelTaskPopupView.Hide();
            _gameLoopController.EnableInput();
        }

        // ── Победа ────────────────────────────────────────────────────────────

        private void HandleWin()
        {
            var address = _progressService.CurrentAddress.CurrentValue;
            var stage   = _worldMapConfig.GetStage(address.CountryIndex, address.StageIndex);

            if (stage == null) { GoToMap(); return; }

            SaveProgress();

            var isLastLevel = address.LevelIndex == stage.LevelCount - 1;

            if (isLastLevel)
                HandleStageComplete(stage);
            else
                HandleNextLevel(address);
        }

        private void HandleStageComplete(StageConfig stage)
        {
            if (stage.StageRewards.Length > 0)
                _rewardService.GrantAll(stage.StageRewards);

            var rewards     = stage.StageRewards;
            var rewardIcons = new UnityEngine.Sprite?[rewards.Length];
            for (var i = 0; i < rewards.Length; i++)
                rewardIcons[i] = _rewardIconConfig.GetIcon(rewards[i].Type, rewards[i].Boost);

            _stageRewardPopupView.Show(stage.StageName, rewards, rewardIcons);
        }

        private void HandleNextLevel(LevelAddress current)
        {
            var next = new LevelAddress(current.CountryIndex, current.StageIndex, current.LevelIndex + 1);
            _progressService.SetCurrentAddress(next);
            _sceneManagerService.LoadSceneAsync(SceneId.Game);
        }

        // ── Поражение ─────────────────────────────────────────────────────────

        private void HandleLose()
        {
            var address = _progressService.CurrentAddress.CurrentValue;
            var stage   = _worldMapConfig.GetStage(address.CountryIndex, address.StageIndex);

            // Явно фиксируем адрес — гарантируем что рестарт вернётся на этот же уровень
            _progressService.SetCurrentAddress(address);

            _onLevelLost.OnNext(stage?.SadCharacterSprite);
        }

        // ── Прогресс ─────────────────────────────────────────────────────────

        private void SaveProgress()
        {
            var config = _levelService.CurrentConfig;
            if (config == null) return;

            var address = _progressService.CurrentAddress.CurrentValue;
            var stars   = StarCalculator.Calculate(
                _levelService.MovesLeft.CurrentValue,
                config.MoveLimit);

            _progressService.SetStars(address, stars);
        }

        // ── Навигация ─────────────────────────────────────────────────────────

        private void GoToMap() =>
            _sceneManagerService.LoadSceneAsync(SceneId.StageMap);

        // ── IDisposable ───────────────────────────────────────────────────────

        public void Dispose()
        {
            _onLevelLost.Dispose();
            _disposables.Dispose();
        }
    }
}
