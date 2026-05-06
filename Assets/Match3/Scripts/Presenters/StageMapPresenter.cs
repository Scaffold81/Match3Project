#nullable enable

using System;
using Match3.Configs;
using Match3.Core.Enums;
using Match3.Core.Models;
using Match3.Services;
using Match3.Services.SceneManagement;
using Match3.Views;
using R3;
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

        private readonly CompositeDisposable _disposables = new();

        private int _popupCountryIndex;
        private int _popupStageIndex;

        [Inject]
        public StageMapPresenter(
            WorldMapConfig        worldMapConfig,
            ProgressService       progressService,
            ISceneManagerService  sceneManagerService,
            StageMapView          stageMapView,
            LevelSelectPopupView  levelSelectPopupView)
        {
            _worldMapConfig       = worldMapConfig;
            _progressService      = progressService;
            _sceneManagerService  = sceneManagerService;
            _stageMapView         = stageMapView;
            _levelSelectPopupView = levelSelectPopupView;
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
                getIcon:     c => _worldMapConfig.GetCountry(c)?.CountryIcon!,
                getName:     c => _worldMapConfig.GetCountry(c)?.CountryName ?? string.Empty,
                getColor:    c => _worldMapConfig.GetCountry(c)?.SectionColor ?? UnityEngine.Color.white,
                isUnlocked:  c => _progressService.IsCountryUnlocked(c));
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
            _levelSelectPopupView.OnLevelClicked
                .Subscribe(OnLevelSelected)
                .AddTo(_disposables);

            _levelSelectPopupView.OnCloseClicked
                .Subscribe(_ => _levelSelectPopupView.Hide())
                .AddTo(_disposables);
        }

        // ── Логика ───────────────────────────────────────────────────────────

        private void OnStageClicked(StageNodeView node)
        {
            _popupCountryIndex = node.countryIndex;
            _popupStageIndex   = node.stageIndex;

            var stage = _worldMapConfig.GetStage(node.countryIndex, node.stageIndex);
            if (stage == null) return;

            var starsPerLevel = new int[3];
            var isUnlocked    = new bool[3];

            for (var l = 0; l < 3; l++)
            {
                starsPerLevel[l] = _progressService.GetStars(node.countryIndex, node.stageIndex, l);
                isUnlocked[l]    = _progressService.IsLevelUnlocked(node.countryIndex, node.stageIndex, l);
            }

            _levelSelectPopupView.Show(stage.StageName, starsPerLevel, isUnlocked);
        }

        private void OnLevelSelected(int levelIndex)
        {
            var address = new LevelAddress(_popupCountryIndex, _popupStageIndex, levelIndex);
            _progressService.SetCurrentAddress(address);
            _levelSelectPopupView.Hide();
            _sceneManagerService.LoadSceneAsync(SceneId.Game);
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
