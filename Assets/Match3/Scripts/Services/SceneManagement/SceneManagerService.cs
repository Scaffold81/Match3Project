#nullable enable

using Match3.Core.Enums;
using UnityEngine.SceneManagement;

namespace Match3.Services.SceneManagement
{
    public sealed class SceneManagerService : ISceneManagerService
    {
        public SceneId TargetSceneId { get; private set; } = SceneId.Game;

        public void LoadSceneAsync(SceneId scene)
        {
            TargetSceneId = scene;
            SceneManager.LoadSceneAsync(scene.ToString());
        }
    }
}
