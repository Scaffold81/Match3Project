#nullable enable

using System;
using Match3.Core.Enums;
using Match3.Services;
using Match3.Services.Debugging;
using Match3.Services.SceneManagement;
using Match3.Views;
using R3;
using UnityEngine.InputSystem;
using Zenject;

namespace Match3.Presenters
{
    /// <summary>
    /// Связывает DebugService и DebugPanelView.
    /// Тильда (~) переключает видимость панели — пока только для Editor/PC (клавиатура).
    /// Позже сюда же будет заведён UI-триггер для Android.
    /// </summary>
    public sealed class DebugPresenter : IInitializable, ITickable, IDisposable
    {
        private readonly DebugService         _debugService;
        private readonly DebugPanelView       _view;
        private readonly CheatService         _cheatService;
        private readonly ISceneManagerService _sceneManagerService;

        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public DebugPresenter(
            DebugService         debugService,
            DebugPanelView       view,
            CheatService         cheatService,
            ISceneManagerService sceneManagerService)
        {
            _debugService         = debugService;
            _view                 = view;
            _cheatService         = cheatService;
            _sceneManagerService  = sceneManagerService;
        }

        public void Initialize()
        {
            // Project-level синглтон — Initialize вызывается один раз за сессию,
            // Clear() тут просто защита на случай будущих реинициализаций.
            _debugService.Clear();
            RegisterActions();

            _debugService.IsVisible
                .Subscribe(_view.SetVisible)
                .AddTo(_disposables);

            _view.OnActionClicked
                .Subscribe(index => _debugService.Actions[index].Execute())
                .AddTo(_disposables);
        }

        public void Tick()
        {
            if (Keyboard.current != null && Keyboard.current.backquoteKey.wasPressedThisFrame)
                _debugService.Toggle();
        }

        private void RegisterActions()
        {
            _debugService.Register("Progress", "Unlock All Levels", () =>
            {
                _cheatService.UnlockAll();
                _sceneManagerService.LoadSceneAsync(SceneId.StageMap);
            });

            _debugService.Register("Progress", "Lock All Levels", () =>
            {
                _cheatService.LockAll();
                _sceneManagerService.LoadSceneAsync(SceneId.StageMap);
            });

            _view.SetActions(_debugService.Actions);
        }

        public void Dispose() => _disposables.Dispose();
    }
}
