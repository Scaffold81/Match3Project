#nullable enable
using Match3.Configs;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.Views
{
    public sealed class ShopItemCardView : MonoBehaviour
    {
        [SerializeField] private string    _purchaseId  = string.Empty;

        [SerializeField] private Image     _icon        = null!;
        [SerializeField] private TMP_Text  _titleText   = null!;
        [SerializeField] private TMP_Text  _costText    = null!;
        [SerializeField] private Button    _buyButton   = null!;
        [SerializeField] private Transform _rewardRoot  = null!;
        [SerializeField] private RewardItemView _rewardItemPrefab = null!;

        private readonly Subject<string> _onBuyClicked = new();
        public Observable<string> OnBuyClicked => _onBuyClicked;

        public string PurchaseId => _purchaseId;

        public void Setup(ShopItemData data, ItemConfig itemConfig)
        {
            _icon.sprite  = data.Icon;
            _icon.enabled = data.Icon != null;

            _titleText.text = data.Title;
            _costText.text  = data.CoinCost > 0 ? $"{data.CoinCost}" : "IAP";

            foreach (Transform child in _rewardRoot)
                Destroy(child.gameObject);

            foreach (var reward in data.Rewards)
            {
                var rewardItem = Instantiate(_rewardItemPrefab, _rewardRoot);
                rewardItem.Setup(reward, itemConfig.GetIcon(reward.Type, reward.Boost));
            }

            _buyButton.onClick.RemoveAllListeners();
            _buyButton.onClick.AddListener(() => _onBuyClicked.OnNext(_purchaseId));
        }

        public void SetInteractable(bool interactable) => _buyButton.interactable = interactable;

        private void OnDestroy() => _onBuyClicked.Dispose();
    }
}
