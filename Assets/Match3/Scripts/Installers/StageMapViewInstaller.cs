#nullable enable

using Match3.Configs;
using Match3.Views;
using UnityEngine;
using Zenject;

namespace Match3.Installers
{
    /// <summary>
    /// View-биндинги для сцены StageMapScene.
    /// GemConfig биндится в ProjectConfigInstaller (ProjectContext).
    /// </summary>
    public sealed class StageMapViewInstaller : MonoInstaller
    {
        [SerializeField] private WorldMapConfig _worldMapConfig = null!;

        public override void InstallBindings()
        {
            Container.BindInstance(_worldMapConfig).AsSingle();
            Container.Bind<StageMapView>().FromComponentInHierarchy().AsSingle();
            Container.Bind<LevelSelectPopupView>().FromComponentInHierarchy().AsSingle();
        }
    }
}
