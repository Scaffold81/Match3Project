#nullable enable

using System;
using System.Collections.Generic;
using Match3.Configs;
using Match3.Core.Enums;
using Match3.Core.Models;
using Match3.Services;
using Match3.Services.Ads;
using Match3.Services.SceneManagement;
using Match3.Views;
using R3;
using UnityEngine;
using Zenject;

namespace Match3.Presenters
{
    public sealed class StageMapPresenter : IInitializable, IDisposable
    {
        private readonly WorldMapConfig           _worldMapConfig;
        private readonly ProgressService          _progressService;
        private readonly RewardService            _rewardService;
        private readonly LivesService             _livesService;
        private readonly ResourcePopupService     _resourcePopupService;
        private readonly EconomyConfig            _economyConfig;
        private readonly AdConfig                 _adConfig;
        private readonly ISceneManagerService     _sceneManagerService;
        private readonly StageMapView             _stageMapView;
        private readonly LevelSelectPopupView     _levelSelectPopupView;
        private readonly CountryCompletePopupView _countryCompletePopupView;
        private readonly GemConfig                _gemConfig;
        private readonly RewardIconConfig         _rewardIconConfig;

        private readonly CompositeDisposable _disposables = new();

        private int _popupCountryIndex;
        private int _popupStageIndex;
        private int _popupLevelIndex;

        [Inject]
        public StageMapPresenter(
            WorldMapConfig            worldMapConfig,
            ProgressService           progressService,
            RewardService             rewardService,
            LivesService              livesService,
            ResourcePopupService      resourcePopupService,
            EconomyConfig             economyConfig,
            AdConfig                  adConfig,
            ISceneManagerService      sceneManagerService,
            StageMapView              stageMapView,
            LevelSelectPopupView      levelSelectPopupView,
            CountryCompletePopupView  countryCompletePopupView,
            GemConfig                 gemConfig,
            RewardIconConfig          rewardIconConfig)
        {
            _worldMapConfig           = worldMapConfig;
            _progressService          = progressService;
            _rewardService            = rewardService;
            _livesService             = livesService;
            _resourcePopupService     = resourcePopupService;
            _economyConfig            = economyConfig;
            _adConfig                 = adConfig;
            _sceneManagerService      = sceneManagerService;
            _stageMapView             = stageMapView;
            _levelSelectPopupView     = levelSelectPopupView;
            _countryCompletePopupView = countryCompletePopupView;
            _gemConfig                = gemConfig;
            _rewardIconConfig         = rewardIconConfig;
        }

        public void Initialize()
        {
            RefreshMap();
            SubscribeNodes();
            SubscribePopup();
            ScrollToCurrentProgress();
            CheckPendingCountryReward();
        }

        // ── Refresh ───────────────────────────────────────────────────────────

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

        // ── Подписки ──────────────────────────────────────────────────────────

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

            _countryCompletePopupView.OnClaimClicked
                .Take(1)
                .Subscribe(_ => _countryCompletePopupView.Hide())
                .AddTo(_disposables);
        }

        // ── Логика ────────────────────────────────────────────────────────────

        private void OnStageClicked(StageNodeView node)
        {
            var stage = _worldMapConfig.GetStage(node.countryIndex, node.stageIndex);
            if (stage == null)
            {
                Debug.LogError($"[StageMapPresenter] Stage null [{node.countryIndex},{node.stageIndex}]");
                return;
            }

            if (_livesService.Lives.CurrentValue <= 0)
            {
                RequestLives(node, stage);
                return;
            }

            OpenLevelSelect(node, stage);
        }

        private void RequestLives(StageNodeView node, StageConfig stage)
        {
            var entry   = _adConfig.GetPlacement(AdPlacementId.RewardedLives);
            var rewards = entry?.Rewards ?? Array.Empty<RewardData>();

            var rewardIcons = new Sprite?[rewards.Length];
            for (var i = 0; i < rewards.Length; i++)
                rewardIcons[i] = _rewardIconConfig.GetIcon(rewards[i].Type, rewards[i].Boost);

            var request = new ResourcePopupRequest
            {
                Title            = "Жизни закончились!",
                CharacterSprite  = stage.CharacterSprite,
                CharacterDialog  = "Сначала пополни жизни!",
                DialogLocaleId   = "popup.no_lives.dialog",
                Rewards          = rewards,
                RewardIcons      = rewardIcons,
                AdPlacementId    = AdPlacementId.RewardedLives,
                AdButtonLabel    = "👁 Получить жизнь",
                CoinPrice        = _economyConfig.LivesPurchasePrice,
                CoinButtonLabel  = $"💰 {_economyConfig.LivesPurchasePrice} — получить жизни",
            };

            request.OnSuccess
                .Take(1)
                .Subscribe(_ => OpenLevelSelect(node, stage))
                .AddTo(_disposables);

            _resourcePopupService.Request(request);
        }

        private void OpenLevelSelect(StageNodeView node, StageConfig stage)
        {
            _popupCountryIndex = node.countryIndex;
            _popupStageIndex   = node.stageIndex;
            _popupLevelIndex   = 0;

            var objectives     = AggregateStageObjectives(stage);
            var objectiveIcons = new Sprite?[objectives.Length];
            for (var i = 0; i < objectives.Length; i++)
                objectiveIcons[i] = _gemConfig.GetVisual(objectives[i].nodeType)?.Sprite;

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
                rewardIcons:     rewardIcons,
                storySlide:      stage.StoryConfig?.StageSelectStory);
        }

        private void OnPlayClicked()
        {
            var address = new LevelAddress(_popupCountryIndex, _popupStageIndex, _popupLevelIndex);
            _progressService.SetCurrentAddress(address);
            _levelSelectPopupView.Hide();
            _sceneManagerService.LoadSceneAsync(SceneId.Game);
        }

        // ── Награда за страну ─────────────────────────────────────────────────

        private void CheckPendingCountryReward()
        {
            var countryIndex = _progressService.GetPendingCountryReward();
            if (countryIndex < 0) return;

            var country = _worldMapConfig.GetCountry(countryIndex);
            if (country == null) return;

            _progressService.ClearPendingCountryReward();

            if (country.CountryRewards.Length > 0)
                _rewardService.GrantAll(country.CountryRewards);

            var rewardIcons = new Sprite?[country.CountryRewards.Length];
            for (var i = 0; i < country.CountryRewards.Length; i++)
                rewardIcons[i] = _rewardIconConfig.GetIcon(
                    country.CountryRewards[i].Type,
                    country.CountryRewards[i].Boost);

            _countryCompletePopupView.Show(
                country.CountryName,
                country.CharacterSprite,
                country.CountryRewards,
                rewardIcons);
        }

        // ── Агрегация целей этапа ─────────────────────────────────────────────

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
