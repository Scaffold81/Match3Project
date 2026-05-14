#nullable enable

using Match3.Controllers;
using Match3.Presenters;
using Match3.Services;
using Match3.Services.Inventory;
using Match3.Services.SceneManagement;
using Match3.Views;
using UnityEngine;
using Zenject;

namespace Match3.Installers
{
    public sealed class ProjectServiceInstaller : MonoInstaller
    {
        [SerializeField] private WalletView _walletViewPrefab = null!;

        public override void InstallBindings()
        {
            ValidateRefs();

            Container
                .Bind<ISceneManagerService>()
                .To<SceneManagerService>()
                .AsSingle()
                .NonLazy();

            Container
                .BindInterfacesTo<Bootstrapper>()
                .AsSingle()
                .NonLazy();

            // ── Живут между сессиями ──────────────────────────────────────────

            Container.Bind<InventoryService>()                 .AsSingle().NonLazy();
            Container.Bind<ProgressService>()                  .AsSingle().NonLazy();
            Container.Bind<CoinService>()                      .AsSingle().NonLazy();
            Container.Bind<LivesService>()                     .AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<RewardService>() .AsSingle().NonLazy();

            // ── Wallet UI — спавнится один раз, живёт всю игру ───────────────

            Container
                .Bind<WalletView>()
                .FromComponentInNewPrefab(_walletViewPrefab)
                .AsSingle()
                .NonLazy();

            Container
                .BindInterfacesAndSelfTo<WalletPresenter>()
                .AsSingle()
                .NonLazy();
        }

        private void ValidateRefs()
        {
            if (_walletViewPrefab == null)
                Debug.LogError("ProjectServiceInstaller: WalletView prefab is not assigned");
        }
    }
}
