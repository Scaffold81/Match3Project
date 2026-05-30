#nullable enable

using Match3.Configs;
using Match3.Controllers;
using Match3.Services;
using Match3.Services.Ads;
using Match3.Services.Inventory;
using Match3.Services.SceneManagement;
using Zenject;

namespace Match3.Installers
{
    public sealed class ProjectServiceInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container
                .Bind<ISceneManagerService>()
                .To<SceneManagerService>()
                .AsSingle()
                .NonLazy();

            Container
                .BindInterfacesTo<Bootstrapper>()
                .AsSingle()
                .NonLazy();

            Container.Bind<InventoryService>()                 .AsSingle().NonLazy();
            Container.Bind<ProgressService>()                  .AsSingle().NonLazy();
            Container.Bind<CoinService>()                      .AsSingle().NonLazy();
            Container.Bind<LivesService>()                     .AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<RewardService>() .AsSingle().NonLazy();
            Container.Bind<ResourcePopupService>()             .AsSingle().NonLazy();

            Container.Bind<IAdProvider>().To<MockAdProvider>() .AsSingle();
            Container.BindInterfacesAndSelfTo<AdService>()     .AsSingle().NonLazy();
        }
    }
}
