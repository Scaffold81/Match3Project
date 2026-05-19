#nullable enable

using System;
using Match3.Configs;
using Match3.Core.Enums;
using Match3.Views;
using UnityEngine;
using Zenject;

namespace Match3.Services.Factories
{
    public sealed class GemFactory
    {
        private readonly DiContainer _container;
        private readonly GemConfig   _gemConfig;

        [Inject]
        public GemFactory(DiContainer container, GemConfig gemConfig)
        {
            _container = container;
            _gemConfig = gemConfig;
        }

        /// <summary>
        /// Создаёт GemView из базового GemViewPrefab, назначает визуал и конфиг.
        /// Позиционирование — ответственность BoardView.
        /// </summary>
        public GemView Create(NodeType nodeType, Transform parent, string name)
        {
            var visual = _gemConfig.GetVisual(nodeType)
                ?? throw new InvalidOperationException($"[GemFactory] No visual for {nodeType}");

            var view = _container.InstantiatePrefabForComponent<GemView>(
                _gemConfig.GemViewPrefab, parent);

            view.name = name;
            view.SetConfig(_gemConfig);
            view.SetVisual(nodeType, visual);
            return view;
        }
    }
}
