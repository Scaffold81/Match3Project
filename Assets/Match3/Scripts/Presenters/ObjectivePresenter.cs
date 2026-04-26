#nullable enable

using System;
using Match3.Configs;
using Match3.Core.Enums;
using Match3.Services.Level;
using Match3.Views;
using R3;
using UnityEngine;
using Zenject;

namespace Match3.Presenters
{
    public sealed class ObjectivePresenter : IInitializable, IDisposable
    {
        private readonly LevelService  _levelService;
        private readonly ObjectiveView _objectiveView;
        private readonly GemConfig     _gemConfig;

        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public ObjectivePresenter(
            LevelService  levelService,
            ObjectiveView objectiveView,
            GemConfig     gemConfig)
        {
            _levelService  = levelService;
            _objectiveView = objectiveView;
            _gemConfig     = gemConfig;
        }

        public void Initialize()
        {
            _levelService.Progress
                .Subscribe(progress =>
                {
                    for (var i = 0; i < progress.Length; i++)
                    {
                        _objectiveView.UpdateProgress(i, progress[i].Collected, progress[i].Required);
                        if (progress[i].IsCompleted)
                            _objectiveView.MarkCompleted(i);
                    }
                })
                .AddTo(_disposables);
        }

        public void RenderObjectives(ObjectiveProgress[] progress)
        {
            var nodeTypes = new NodeType[progress.Length];
            var totals    = new int[progress.Length];
            var icons     = new Sprite?[progress.Length];

            for (var i = 0; i < progress.Length; i++)
            {
                nodeTypes[i] = progress[i].NodeType;
                totals[i]    = progress[i].Required;
                icons[i]     = _gemConfig.GetVisual(progress[i].NodeType)?.Sprite;
            }

            _objectiveView.SetupObjectives(nodeTypes, totals, icons);
        }

        public void Dispose() => _disposables.Dispose();
    }
}
