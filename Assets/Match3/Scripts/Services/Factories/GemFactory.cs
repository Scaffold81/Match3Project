#nullable enable

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

        public GemView? Create(NodeType nodeType, string name)
        {
            var visual = _gemConfig.GetVisual(nodeType);
            if (visual == null)
            {
                Debug.LogWarning($"GemFactory: no visual data for NodeType {nodeType}");
                return null;
            }

            if (visual.Prefab == null)
            {
                Debug.LogWarning($"GemFactory: prefab not assigned for NodeType {nodeType}");
                return null;
            }

            var gemView = _container.InstantiatePrefabForComponent<GemView>(visual.Prefab);
            gemView.name = name;
            gemView.Setup(nodeType, visual);

            return gemView;
        }
    }
}
