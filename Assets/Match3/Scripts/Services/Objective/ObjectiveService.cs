#nullable enable

using System;
using System.Collections.Generic;
using Match3.Configs;
using Match3.Core.Enums;
using Match3.Core.Models;
using R3;
using UnityEngine;

namespace Match3.Services.Objective
{
    public sealed class ObjectiveService : IDisposable
    {
        private readonly ReactiveProperty<ObjectiveProgress[]> _progress =
            new(Array.Empty<ObjectiveProgress>());

        private readonly Subject<NodeType> _onObjectiveCompleted = new();
        private readonly Subject<Unit> _onAllObjectivesCompleted = new();

        public ReadOnlyReactiveProperty<ObjectiveProgress[]> Progress => _progress;
        public Observable<NodeType> OnObjectiveCompleted => _onObjectiveCompleted;
        public Observable<Unit> OnAllObjectivesCompleted => _onAllObjectivesCompleted;

        public bool IsAllCompleted { get; private set; }

        public void Initialize(LevelConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            IsAllCompleted = false;

            var progressList = new ObjectiveProgress[config.Objectives.Length];
            for (var i = 0; i < config.Objectives.Length; i++)
            {
                var obj = config.Objectives[i];
                progressList[i] = new ObjectiveProgress(obj.nodeType, obj.count);
            }

            _progress.Value = progressList;
        }

        public void RegisterMatch(List<Vector2Int> matchedCells, NodeType[,] boardSnapshot)
        {
            var countByType = new Dictionary<NodeType, int>();

            foreach (var cell in matchedCells)
            {
                var nodeType = boardSnapshot[cell.x, cell.y];
                if (nodeType == NodeType.None) continue;

                if (!countByType.ContainsKey(nodeType))
                    countByType[nodeType] = 0;
                countByType[nodeType]++;
            }

            foreach (var (nodeType, count) in countByType)
                RegisterCollected(nodeType, count);
        }

        private void RegisterCollected(NodeType nodeType, int count)
        {
            var progress = _progress.Value;
            var changed = false;

            for (var i = 0; i < progress.Length; i++)
            {
                if (progress[i].NodeType != nodeType) continue;
                if (progress[i].IsCompleted) continue;

                progress[i].AddCollected(count);
                changed = true;

                if (progress[i].IsCompleted)
                    _onObjectiveCompleted.OnNext(nodeType);
            }

            if (!changed) return;

            _progress.ForceNotify();
            CheckAllCompleted();
        }

        private void CheckAllCompleted()
        {
            if (IsAllCompleted) return;

            foreach (var p in _progress.Value)
                if (!p.IsCompleted) return;

            IsAllCompleted = true;
            _onAllObjectivesCompleted.OnNext(Unit.Default);
        }

        public void Dispose()
        {
            _progress.Dispose();
            _onObjectiveCompleted.Dispose();
            _onAllObjectivesCompleted.Dispose();
        }
    }

    public sealed class ObjectiveProgress
    {
        public NodeType NodeType { get; }
        public int Required { get; }
        public int Collected { get; private set; }
        public bool IsCompleted => Collected >= Required;

        public ObjectiveProgress(NodeType nodeType, int required)
        {
            NodeType = nodeType;
            Required = required;
            Collected = 0;
        }

        public void AddCollected(int count) =>
            Collected = Math.Min(Collected + count, Required);
    }
}
