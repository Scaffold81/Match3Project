#nullable enable

using Match3.Core.Enums;
using Match3.Services.SceneManagement;
using Zenject;

namespace Match3.Controllers
{
    public sealed class Bootstrapper : IInitializable
    {
        private readonly ISceneManagerService _sceneManagerService;

        [Inject]
        public Bootstrapper(ISceneManagerService sceneManagerService)
        {
            _sceneManagerService = sceneManagerService;
        }

        public void Initialize()
        {
            _sceneManagerService.LoadSceneAsync(SceneId.StageMap);
        }
    }
}
