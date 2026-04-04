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

            Debug.LogWarning($"[GemFactory] Constructed | GemConfig gems count={gemConfig.Gems.Length}");
        }

        public GemView? Create(NodeType nodeType, string name)
        {
            var visual = _gemConfig.GetVisual(nodeType);
            if (visual == null)
            {
                Debug.LogWarning($"[GemFactory] No visual data for NodeType={nodeType} — проверь GemConfig, все ли NodeType заполнены");
                return null;
            }

            Debug.LogWarning($"[GemFactory] Instantiating GemView for NodeType={nodeType}");

            // Если префаб не назначен — создаём пустую ячейку через код
            if (visual.Prefab == null)
            {
                Debug.LogWarning($"[GemFactory] No Prefab assigned for NodeType={nodeType}, creating empty GemView");
                var gemObj = new GameObject(name, typeof(GemView));
                var script = gemObj.GetComponent<GemView>();
                script.SetVisual(nodeType, visual); // NodeType устанавливается внутри SetVisual
                gemObj.name = name;
                return script as GemView; // Каст к GemView
            }

            var gemView = _container.InstantiatePrefabForComponent<GemView>(visual.Prefab);
            gemView.name = name;
            gemView.SetVisual(nodeType, visual);

            Debug.LogWarning($"[GemFactory] Created {name} | active={gemView.gameObject.activeSelf}");

            return gemView;
        }
    }
}
