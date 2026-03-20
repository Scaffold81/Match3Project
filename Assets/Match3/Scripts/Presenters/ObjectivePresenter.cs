#nullable enable

using System;
using Match3.Configs;
using Match3.Services.Objective;
using Match3.Views;
using R3;
using UnityEngine;
using Zenject;

namespace Match3.Presenters
{
    public sealed class ObjectivePresenter : IInitializable, IDisposable
    {
        private readonly ObjectiveService _objectiveService;
        private readonly ObjectiveView _objectiveView;
        private readonly GemConfig _gemConfig;

        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public ObjectivePresenter(
            ObjectiveService objectiveService,
            ObjectiveView objectiveView,
            GemConfig gemConfig)
        {
            _objectiveService = objectiveService;
            _objectiveView = objectiveView;
            _gemConfig = gemConfig;
        }

        public void Initialize()
        {
            _objectiveService.Progress
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
            var nodeTypes = new Core.Enums.NodeType[progress.Length];
            var totals = new int[progress.Length];
            var icons = new Sprite?[progress.Length];

            for (var i = 0; i < progress.Length; i++)
            {
                nodeTypes[i] = progress[i].NodeType;
                totals[i] = progress[i].Required;
                icons[i] = _gemConfig.GetVisual(progress[i].NodeType)?.Sprite;
            }

            _objectiveView.SetupObjectives(nodeTypes, totals, icons);
        }

        public void Dispose() => _disposables.Dispose();
    }
}
