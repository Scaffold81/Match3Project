#nullable enable

using Match3.Core.Enums;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.Views
{
    /// <summary>
    /// Универсальный слот буста — используется в BackpackView (игра) и BackpackPopupView (карта).
    /// Публикует OnClicked — подписчик сам решает что делать с кликом.
    /// </summary>
    public sealed class BoostSlotView : MonoBehaviour
    {
        [field: SerializeField] public BoostType BoostType { get; private set; }

        [SerializeField] private Image         _icon          = null!;
        [SerializeField] private Button        _button        = null!;
        [SerializeField] private TMP_Text      _countLabel    = null!;
        [SerializeField] private CanvasGroup   _canvasGroup   = null!;
        [SerializeField] private RectTransform _iconTransform = null!;

        private readonly Subject<BoostType> _onClicked = new();
        public Observable<BoostType> OnClicked => _onClicked;

        public RectTransform IconTransform => _iconTransform;

        private void Awake()
        {
            _button.onClick.AddListener(() => _onClicked.OnNext(BoostType));
        }

        private void OnDestroy() => _onClicked.Dispose();

        public void SetIcon(Sprite? icon)
        {
            _icon.sprite  = icon;
            _icon.enabled = icon != null;
        }

        public void UpdateCount(int count)
        {
            _countLabel.text     = count.ToString();
            _button.interactable = count > 0;
            _canvasGroup.alpha   = count > 0 ? 1f : 0.45f;
        }

        public void SetInteractable(bool interactable)
        {
            _button.interactable = interactable;
            _canvasGroup.alpha   = interactable ? 1f : 0.45f;
        }
    }
}
