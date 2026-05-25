#nullable enable

using System;
using Match3.Configs;
using Match3.Views;
using UnityEngine.Pool;
using Zenject;

namespace Match3.Services.Factories
{
    public sealed class GemPool : IDisposable
    {
        private readonly GemConfig   _gemConfig;
        private readonly DiContainer _container;

        private ObjectPool<GemView>?       _pool;
        private UnityEngine.Transform?     _poolContainer;

        private const int DefaultCapacity = 81;
        private const int MaxSize         = 200;

        [Inject]
        public GemPool(GemConfig gemConfig, DiContainer container)
        {
            _gemConfig = gemConfig;
            _container = container;
        }

        public void Initialize(UnityEngine.Transform poolContainer)
        {
            _poolContainer = poolContainer;

            _pool = new ObjectPool<GemView>(
                createFunc:      CreateGem,
                actionOnGet:     gem => gem.gameObject.SetActive(true),
                actionOnRelease: ReturnToPool,
                actionOnDestroy: gem => UnityEngine.Object.Destroy(gem.gameObject),
                defaultCapacity: DefaultCapacity,
                maxSize:         MaxSize
            );
        }

        public GemView Get(UnityEngine.Transform parent)
        {
            var gem = _pool!.Get();
            gem.transform.SetParent(parent, false);
            return gem;
        }

        public void Return(GemView gem)
        {
            _pool!.Release(gem);
        }

        private GemView CreateGem()
        {
            return _container.InstantiatePrefabForComponent<GemView>(
                _gemConfig.GemViewPrefab, _poolContainer);
        }

        private void ReturnToPool(GemView gem)
        {
            gem.SetEmpty();
            gem.ResetScale();
            gem.transform.SetParent(_poolContainer, false);
            gem.gameObject.SetActive(false);
        }

        public void Dispose() => _pool?.Clear();
    }
}
