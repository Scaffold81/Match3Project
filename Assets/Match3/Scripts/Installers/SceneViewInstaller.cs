#nullable enable

using Match3.Views;
using Zenject;

namespace Match3.Installers
{
    public sealed class SceneViewInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<BoardView>().FromComponentInHierarchy().AsSingle();
            Container.Bind<BoardInputHandler>().FromComponentInHierarchy().AsSingle();
            Container.Bind<LayerView>().FromComponentInHierarchy().AsSingle();
            Container.Bind<ObjectiveView>().FromComponentInHierarchy().AsSingle();
            Container.Bind<MoveCounterView>().FromComponentInHierarchy().AsSingle();
            Container.Bind<BackpackView>().FromComponentInHierarchy().AsSingle();
            Container.Bind<ActiveBoostView>().FromComponentInHierarchy().AsSingle();

            Container.Bind<LevelTaskPopupView>().FromComponentInHierarchy().AsSingle();
            Container.Bind<StageRewardPopupView>().FromComponentInHierarchy().AsSingle();
            Container.Bind<GameBackgroundView>().FromComponentInHierarchy().AsSingle();
        }
    }
}
