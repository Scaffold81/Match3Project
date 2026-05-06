#nullable enable

using Match3.Controllers;
using Match3.Services.Board;
using Match3.Services.Boost;
using Match3.Services.Factories;
using Match3.Services.Hint;
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
            Container.Bind<BoardService>().AsSingle().NonLazy();
            Container.Bind<SwapService>().AsSingle().NonLazy();
            Container.Bind<LayerService>().AsSingle().NonLazy();
            Container.Bind<LevelService>().AsSingle().NonLazy();
            Container.Bind<HintService>().AsSingle().NonLazy();
            Container.Bind<BoostService>().AsSingle().NonLazy();
            Container.Bind<GemFactory>().AsSingle().NonLazy();

            Container
                .BindInterfacesAndSelfTo<GameLoopController>()
                .AsSingle()
                .NonLazy();
        }
    }
}
