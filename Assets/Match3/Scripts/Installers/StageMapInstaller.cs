#nullable enable

using Match3.Presenters;
using Match3.Views;
using Zenject;

namespace Match3.Installers
{
    public sealed class StageMapInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<StageMapPresenter>()
                .AsSingle()
                .NonLazy();

            Container.Bind<ShopView>()
                .FromComponentInHierarchy()
                .AsSingle();

            Container.BindInterfacesAndSelfTo<ShopPresenter>()
                .AsSingle()
                .NonLazy();
        }
    }
}
