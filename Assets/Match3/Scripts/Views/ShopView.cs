#nullable enable
using System.Collections.Generic;
using Match3.Configs;
using R3;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Match3.Views
{
    public sealed class ShopView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup = null!;
        [SerializeField] private Button      _openButton  = null!;
        [SerializeField] private Button      _closeButton = null!;

        private readonly Subject<string>        _onBuyClicked = new();
        private readonly List<ShopItemCardView> _cards        = new();

        public Observable<string> OnBuyClicked => _onBuyClicked;

        private void Awake()
        {
            _canvasGroup.alpha          = 0f;
            _canvasGroup.interactable   = false;
            _canvasGroup.blocksRaycasts = false;

            _openButton.onClick.AddListener(Show);
            _closeButton.onClick.AddListener(Hide);

            GetComponentsInChildren(true, _cards);
            Debug.LogWarning($"ShopView.Awake: found {_cards.Count} cards in children");
        }

        public void Bind(ShopConfig shopConfig, ItemConfig itemConfig)
        {
            Debug.LogWarning($"ShopView.Bind: shopConfig.Items={shopConfig.Items.Length}, cards={_cards.Count}");

            foreach (var card in _cards)
            {
                var data = shopConfig.FindById(card.PurchaseId);

                if (data == null)
                {
                    Debug.LogWarning($"ShopView.Bind: no config item for purchaseId='{card.PurchaseId}' — hiding card");
                    card.gameObject.SetActive(false);
                    continue;
                }

                Debug.LogWarning($"ShopView.Bind: binding card '{card.PurchaseId}' → '{data.Title}'");
                card.Setup(data, itemConfig);
                card.OnBuyClicked
                    .Subscribe(id => _onBuyClicked.OnNext(id));
            }
        }

        public void Show()
        {
            _canvasGroup.interactable   = true;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.DOFade(1f, 0.25f)
                .SetEase(Ease.OutCubic)
                .SetLink(gameObject);
        }

        public void Hide()
        {
            _canvasGroup.interactable   = false;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.DOFade(0f, 0.2f)
                .SetEase(Ease.InCubic)
                .SetLink(gameObject);
        }

        public void SetAllCardsInteractable(bool interactable)
        {
            foreach (var card in _cards)
                card.SetInteractable(interactable);
        }

        private void OnDestroy() => _onBuyClicked.Dispose();
    }
}
