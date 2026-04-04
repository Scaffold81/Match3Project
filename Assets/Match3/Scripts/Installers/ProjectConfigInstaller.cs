#nullable enable

using Match3.Configs;
using UnityEngine;
using Zenject;

namespace Match3.Installers
{
    public sealed class ProjectConfigInstaller : MonoInstaller
    {
        [SerializeField] private GemConfig              _gemConfig              = null!;
        [SerializeField] private BoardConfig            _boardConfig            = null!;
        [SerializeField] private AnimationConfig        _animationConfig        = null!;
        [SerializeField] private LevelConfigRepository  _levelConfigRepository  = null!;

        public override void InstallBindings()
        {
            ValidateConfigs();

            Container.BindInstance(_gemConfig).AsSingle();
            Container.BindInstance(_boardConfig).AsSingle();
            Container.BindInstance(_animationConfig).AsSingle();
            Container.BindInstance(_levelConfigRepository).AsSingle();
        }

        private void ValidateConfigs()
        {
            if (_gemConfig == null)
                Debug.LogError("ProjectConfigInstaller: GemConfig is not assigned");

            if (_boardConfig == null)
                Debug.LogError("ProjectConfigInstaller: BoardConfig is not assigned");

            if (_animationConfig == null)
                Debug.LogError("ProjectConfigInstaller: AnimationConfig is not assigned");

            if (_levelConfigRepository == null)
                Debug.LogError("ProjectConfigInstaller: LevelConfigRepository is not assigned");
        }
    }
}
