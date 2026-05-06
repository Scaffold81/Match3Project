#nullable enable

using Match3.Presenters;
using Zenject;

namespace Match3.Installers
{
    /// <summary>
    /// Инсталлер для сцены StageMapScene.
    /// Подключить к SceneContext на сцене карты.
    /// </summary>
    public sealed class StageMapInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<StageMapPresenter>()
                .AsSingle()
                .NonLazy();
        }
    }
}
