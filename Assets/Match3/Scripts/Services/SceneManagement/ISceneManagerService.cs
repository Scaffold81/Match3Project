#nullable enable

using Match3.Core.Enums;

namespace Match3.Services.SceneManagement
{
    public interface ISceneManagerService
    {
        SceneId TargetSceneId { get; }
        void LoadSceneAsync(SceneId scene);
    }
}
