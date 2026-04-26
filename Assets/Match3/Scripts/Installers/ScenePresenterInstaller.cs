#nullable enable

using Match3.Presenters;
using Zenject;

namespace Match3.Installers
{
    public sealed class ScenePresenterInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            // BindInterfacesAndSelfTo — Zenject автоматически вызывает IInitializable.Initialize()
            Container.BindInterfacesAndSelfTo<BoardPresenter>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<ObjectivePresenter>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<SwapPresenter>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<LayerPresenter>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<LevelPresenter>().AsSingle().NonLazy();
        }
    }
}
