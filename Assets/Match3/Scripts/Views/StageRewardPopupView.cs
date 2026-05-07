#nullable enable

using System.Collections.Generic;
using DG.Tweening;
using Match3.Core.Models;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.Views
{
    /// <summary>
    /// Попап с наградой за завершение этапа (все уровни пройдены).
    /// Спрайты наград передаются снаружи (GameFlowService резолвит через RewardIconConfig).
    /// </summary>
    public sealed class StageRewardPopupView : MonoBehaviour
    {
        [Header("Корень попапа")]
        [SerializeField] private GameObject  _root        = null!;
        [SerializeField] private CanvasGroup _canvasGroup = null!;

        [Header("Шапка")]
        [SerializeField] private TMP_Text _titleText = null!;

        [Header("Награды")]
        [SerializeField] private Transform      _rewardContainer = null!;
        [SerializeField] private RewardItemView _rewardPrefab    = null!;

        [Header("Кнопка")]
        [SerializeField] private Button _claimButton = null!;

        private readonly Subject<Unit> _onClaimClicked = new();
        public Observable<Unit> OnClaimClicked => _onClaimClicked;

        private readonly List<RewardItemView> _spawnedRewards = new();

        private Tween? _tween;

        private void Awake()
        {
            _claimButton.onClick.AddListener(() => _onClaimClicked.OnNext(Unit.Default));
            _root.SetActive(false);
        }

        private void OnDestroy()
        {
            _tween?.Kill();
            _onClaimClicked.Dispose();
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void Show(string stageName, RewardData[] rewards, Sprite?[] rewardIcons)
        {
            _titleText.text = $"{stageName} — Этап завершён!";

            SpawnRewards(rewards, rewardIcons);

            _root.SetActive(true);
            _canvasGroup.alpha = 0f;

            _tween?.Kill();
            _tween = _canvasGroup
                .DOFade(1f, 0.25f)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject);
        }

        public void Hide()
        {
            _tween?.Kill();
            _tween = _canvasGroup
                .DOFade(0f, 0.2f)
                .SetEase(Ease.InQuad)
                .SetLink(gameObject)
                .OnComplete(() => _root.SetActive(false));
        }

        // ── Spawn ─────────────────────────────────────────────────────────────

        private void SpawnRewards(RewardData[] rewards, Sprite?[] icons)
        {
            ClearSpawned(_spawnedRewards);

            for (var i = 0; i < rewards.Length; i++)
            {
                var item = Instantiate(_rewardPrefab, _rewardContainer);
                var icon = i < icons.Length ? icons[i] : null;
                item.Setup(rewards[i], icon);
                _spawnedRewards.Add(item);
            }
        }

        private static void ClearSpawned<T>(List<T> list) where T : MonoBehaviour
        {
            foreach (var item in list)
                if (item != null) Destroy(item.gameObject);
            list.Clear();
        }
    }
}
