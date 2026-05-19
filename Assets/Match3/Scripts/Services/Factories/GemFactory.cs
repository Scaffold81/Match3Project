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
        private readonly GemConfig _gemConfig;
        private readonly GemPool   _gemPool;

        [Inject]
        public GemFactory(GemConfig gemConfig, GemPool gemPool)
        {
            _gemConfig = gemConfig;
            _gemPool   = gemPool;
        }

        public GemView Create(NodeType nodeType, Transform parent, string name)
        {
            var visual = _gemConfig.GetVisual(nodeType)
                ?? throw new InvalidOperationException(
                    $"[GemFactory] No visual for {nameof(nodeType)}: {nodeType}");

            var view = _gemPool.Get(parent);
            view.name = name;
            view.SetConfig(_gemConfig);
            view.SetVisual(nodeType, visual);
            return view;
        }

        public void Return(GemView gem) => _gemPool.Return(gem);
    }
}
