#nullable enable

using Match3.Controllers;
using Match3.Services.Board;
using Match3.Services.Layer;
using Match3.Services.Level;
using Match3.Services.Swap;
using Zenject;

namespace Match3.Installers
{
    public sealed class SceneServiceInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            // 4 сервиса
            Container.Bind<BoardService>().AsSingle().NonLazy();
            Container.Bind<SwapService>().AsSingle().NonLazy();
            Container.Bind<LayerService>().AsSingle().NonLazy();
            Container.Bind<LevelService>().AsSingle().NonLazy();

            Container
                .BindInterfacesAndSelfTo<GameLoopController>()
                .AsSingle()
                .NonLazy();
        }
    }
}
