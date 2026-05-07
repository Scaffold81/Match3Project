#nullable enable

using System;
using System.Collections.Generic;
using Match3.Configs;
using Match3.Core.Enums;
using Match3.Core.Models;
using Match3.Services;
using Match3.Services.SceneManagement;
using Match3.Views;
using R3;
using UnityEngine;
using Zenject;

namespace Match3.Presenters
{
    public sealed class StageMapPresenter : IInitializable, IDisposable
    {
        private readonly WorldMapConfig       _worldMapConfig;
        private readonly ProgressService      _progressService;
        private readonly ISceneManagerService _sceneManagerService;
        private readonly StageMapView         _stageMapView;
        private readonly LevelSelectPopupView _levelSelectPopupView;
        private readonly GemConfig            _gemConfig;
        private readonly RewardIconConfig     _rewardIconConfig;

        private readonly CompositeDisposable _disposables = new();

        private int _popupCountryIndex;
        private int _popupStageIndex;
        private int _popupLevelIndex;

        [Inject]
        public StageMapPresenter(
            WorldMapConfig        worldMapConfig,
            ProgressService       progressService,
            ISceneManagerService  sceneManagerService,
            StageMapView          stageMapView,
            LevelSelectPopupView  levelSelectPopupView,
            GemConfig             gemConfig,
            RewardIconConfig      rewardIconConfig)
        {
            _worldMapConfig       = worldMapConfig;
            _progressService      = progressService;
            _sceneManagerService  = sceneManagerService;
            _stageMapView         = stageMapView;
            _levelSelectPopupView = levelSelectPopupView;
            _gemConfig            = gemConfig;
            _rewardIconConfig     = rewardIconConfig;
        }

        public void Initialize()
        {
            RefreshMap();
            SubscribeNodes();
            SubscribePopup();
            ScrollToCurrentProgress();
        }

        // ── Refresh ──────────────────────────────────────────────────────────

        private void RefreshMap()
        {
            _stageMapView.RefreshStages(
                getStageStars:   (c, s) => _progressService.GetStageStars(c, s),
                isStageUnlocked: (c, s) => _progressService.IsStageUnlocked(c, s));

            _stageMapView.RefreshCountries(
                getIcon:    c => _worldMapConfig.GetCountry(c)?.CountryIcon!,
                getName:    c => _worldMapConfig.GetCountry(c)?.CountryName ?? string.Empty,
                getColor:   c => _worldMapConfig.GetCountry(c)?.SectionColor ?? Color.white,
                isUnlocked: c => _progressService.IsCountryUnlocked(c));
        }

        // ── Подписки ─────────────────────────────────────────────────────────

        private void SubscribeNodes()
        {
            foreach (var node in _stageMapView.StageNodes)
            {
                node.OnClicked
                    .Subscribe(OnStageClicked)
                    .AddTo(_disposables);
            }
        }

        private void SubscribePopup()
        {
            _levelSelectPopupView.OnPlayClicked
                .Subscribe(_ => OnPlayClicked())
                .AddTo(_disposables);

            _levelSelectPopupView.OnCloseClicked
                .Subscribe(_ => _levelSelectPopupView.Hide())
                .AddTo(_disposables);
        }

        // ── Логика ───────────────────────────────────────────────────────────

        private void OnStageClicked(StageNodeView node)
        {
            var stage = _worldMapConfig.GetStage(node.countryIndex, node.stageIndex);
            if (stage == null)
            {
                Debug.LogError($"[StageMapPresenter] Stage null [{node.countryIndex},{node.stageIndex}]");
                return;
            }

            _popupCountryIndex = node.countryIndex;
            _popupStageIndex   = node.stageIndex;
            _popupLevelIndex   = 0;

            // Цели = сумма по всем уровням этапа сгруппированная по NodeType
            var objectives     = AggregateStageObjectives(stage);
            var objectiveIcons = new Sprite?[objectives.Length];
            for (var i = 0; i < objectives.Length; i++)
                objectiveIcons[i] = _gemConfig.GetVisual(objectives[i].nodeType)?.Sprite;

            // Награды = из StageConfig.StageRewards
            var rewards     = stage.StageRewards;
            var rewardIcons = new Sprite?[rewards.Length];
            for (var i = 0; i < rewards.Length; i++)
                rewardIcons[i] = _rewardIconConfig.GetIcon(rewards[i].Type, rewards[i].Boost);

            _levelSelectPopupView.Show(
                levelTitle:      stage.StageName,
                characterSprite: stage.CharacterSprite,
                objectives:      objectives,
                objectiveIcons:  objectiveIcons,
                stageRewards:    rewards,
                rewardIcons:     rewardIcons);
        }

        private void OnPlayClicked()
        {
            var address = new LevelAddress(_popupCountryIndex, _popupStageIndex, _popupLevelIndex);
            _progressService.SetCurrentAddress(address);
            _levelSelectPopupView.Hide();
            _sceneManagerService.LoadSceneAsync(SceneId.Game);
        }

        // ── Агрегация целей этапа ─────────────────────────────────────────────

        /// <summary>
        /// Суммирует цели всех 3 уровней этапа по NodeType.
        /// Например: Level1(Red×15) + Level2(Red×10, Blue×20) = Red×25, Blue×20.
        /// </summary>
        private static ObjectiveData[] AggregateStageObjectives(StageConfig stage)
        {
            var totals = new Dictionary<NodeType, int>();

            for (var l = 0; l < stage.LevelCount; l++)
            {
                var level = stage.GetLevel(l);
                if (level == null) continue;

                foreach (var obj in level.Objectives)
                {
                    if (obj.nodeType == NodeType.None) continue;
                    totals.TryGetValue(obj.nodeType, out var current);
                    totals[obj.nodeType] = current + obj.count;
                }
            }

            var result = new ObjectiveData[totals.Count];
            var index  = 0;
            foreach (var kvp in totals)
                result[index++] = new ObjectiveData { nodeType = kvp.Key, count = kvp.Value };

            return result;
        }

        private void ScrollToCurrentProgress()
        {
            StageNodeView? target = null;

            foreach (var node in _stageMapView.StageNodes)
            {
                if (!node.IsUnlocked) break;
                target = node;
            }

            if (target != null)
                _stageMapView.ScrollToNode(target);
        }

        public void Dispose() => _disposables.Dispose();
    }
}
