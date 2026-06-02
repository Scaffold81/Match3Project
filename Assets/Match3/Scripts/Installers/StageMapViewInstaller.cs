#nullable enable

using Match3.Views;
using Zenject;

namespace Match3.Installers
{
    public sealed class StageMapViewInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<StageMapView>().FromComponentInHierarchy().AsSingle();
            Container.Bind<LevelSelectPopupView>().FromComponentInHierarchy().AsSingle();
            Container.Bind<CountryCompletePopupView>().FromComponentInHierarchy().AsSingle();
        }
    }
}
