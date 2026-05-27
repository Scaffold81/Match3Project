#nullable enable
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Match3.Configs;
using UnityEngine;
using Zenject;

namespace Match3.Services.Ads
{
    public sealed class AdService : IInitializable, IDisposable
    {
        private readonly IAdProvider              _provider;
        private readonly AdConfig                _config;
        private readonly RewardService           _rewardService;
        private readonly CancellationTokenSource _cts = new();

        private int   _levelsSinceLastInterstitial;
        private float _lastInterstitialTime = float.MinValue;

        [Inject]
        public AdService(IAdProvider provider, AdConfig config, RewardService rewardService)
        {
            _provider      = provider;
            _config        = config;
            _rewardService = rewardService;
        }

        public void Initialize() => InitializeAsync(_cts.Token).Forget();

        private async UniTaskVoid InitializeAsync(CancellationToken ct)
        {
            try
            {
                var appId = Application.platform == RuntimePlatform.IPhonePlayer
                    ? _config.AppIdIos
                    : _config.AppIdAndroid;

                await _provider.InitializeAsync(appId, ct);
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                Debug.LogError($"[AdService] Init failed: {e.Message}");
            }
        }

        public async UniTask<AdResult> ShowRewardedAsync(AdPlacementId placementId, CancellationToken ct)
        {
            var entry = _config.GetPlacement(placementId);
            if (entry == null)
                return AdResult.Fail(AdFailReason.Unknown);

            var unitId = Application.platform == RuntimePlatform.IPhonePlayer
                ? entry.UnitIdIos
                : entry.UnitIdAndroid;

            if (!_provider.IsRewardedReady(unitId))
                return AdResult.Fail(AdFailReason.NoFill);

            var result = await _provider.ShowRewardedAsync(unitId, ct);

            if (result.IsRewarded)
                _rewardService.GrantAll(entry.Rewards);

            return result;
        }

        public async UniTask<bool> TryShowInterstitialAsync(CancellationToken ct)
        {
            var entry = _config.GetPlacement(AdPlacementId.Interstitial);
            if (entry == null) return false;

            var cooldownPassed = Time.time - _lastInterstitialTime >= _config.InterstitialCooldownSeconds;
            var levelsOk       = _levelsSinceLastInterstitial >= _config.MinLevelsBetweenInterstitials;

            if (!cooldownPassed || !levelsOk) return false;

            var unitId = Application.platform == RuntimePlatform.IPhonePlayer
                ? entry.UnitIdIos
                : entry.UnitIdAndroid;

            if (!_provider.IsInterstitialReady(unitId)) return false;

            var shown = await _provider.ShowInterstitialAsync(unitId, ct);

            if (shown)
            {
                _lastInterstitialTime        = Time.time;
                _levelsSinceLastInterstitial = 0;
            }

            return shown;
        }

        public void RegisterLevelCompleted() => _levelsSinceLastInterstitial++;

        public void Dispose() => _cts.Cancel();
    }
}
