#nullable enable

using Match3.Controllers;
using Match3.Views;
using Zenject;

namespace Match3.Installers
{
    public sealed class SceneViewInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<BoardView>().FromComponentInHierarchy().AsSingle();
            Container.Bind<LayerView>().FromComponentInHierarchy().AsSingle();
            Container.Bind<ObjectiveView>().FromComponentInHierarchy().AsSingle();
            Container.Bind<MoveCounterView>().FromComponentInHierarchy().AsSingle();
            Container.Bind<LevelResultView>().FromComponentInHierarchy().AsSingle();
            Container.Bind<InputController>().FromComponentInHierarchy().AsSingle();
        }
    }
}
