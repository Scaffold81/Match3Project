#nullable enable

using Match3.Configs;
using Match3.Presenters;
using UnityEngine;
using Zenject;

namespace Match3.Installers
{
    public sealed class ScenePresenterInstaller : MonoInstaller
    {
        [SerializeField] private LevelConfig _levelConfig = null!;

        public override void InstallBindings()
        {
            if (_levelConfig == null)
                Debug.LogError("ScenePresenterInstaller: LevelConfig is not assigned");

            Container.BindInstance(_levelConfig).AsSingle();

            Container.BindInterfacesAndSelfTo<BoardPresenter>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<SwapPresenter>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<LayerPresenter>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<ObjectivePresenter>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<LevelPresenter>().AsSingle().NonLazy();
        }
    }
}
