#nullable enable

using Match3.Controllers;
using Match3.Services.Board;
using Match3.Services.Gravity;
using Match3.Services.Layer;
using Match3.Services.Level;
using Match3.Services.Match;
using Match3.Services.MoveCounter;
using Match3.Services.Objective;
using Match3.Services.Spawn;
using Match3.Services.Swap;
using Zenject;

namespace Match3.Installers
{
    public sealed class SceneServiceInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<BoardService>().AsSingle().NonLazy();
            Container.Bind<MatchService>().AsSingle().NonLazy();
            Container.Bind<SwapService>().AsSingle().NonLazy();
            Container.Bind<GravityService>().AsSingle().NonLazy();
            Container.Bind<SpawnService>().AsSingle().NonLazy();
            Container.Bind<LayerService>().AsSingle().NonLazy();
            Container.Bind<ObjectiveService>().AsSingle().NonLazy();
            Container.Bind<MoveCounterService>().AsSingle().NonLazy();
            Container.Bind<LevelService>().AsSingle().NonLazy();

            Container
                .BindInterfacesAndSelfTo<GameLoopController>()
                .AsSingle()
                .NonLazy();
        }
    }
}
