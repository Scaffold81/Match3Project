#nullable enable

using System;
using Match3.Core.Enums;
using R3;
using UnityEngine;

namespace Match3.Views
{
    /// <summary>
    /// Нижняя панель бустов в игровой сцене.
    /// Слоты — BoostSlotView, pre-placed в иерархии.
    /// Подписывается на OnClicked каждого слота и публикует общий OnBoostClicked.
    /// </summary>
    public sealed class BackpackView : MonoBehaviour
    {
        [SerializeField] private BoostSlotView[] _slots = Array.Empty<BoostSlotView>();

        private readonly Subject<BoostType>      _onBoostClicked = new();
        private readonly CompositeDisposable     _disposables    = new();

        public Observable<BoostType> OnBoostClicked => _onBoostClicked;

        private void Awake()
        {
            foreach (var slot in _slots)
            {
                slot.OnClicked
                    .Subscribe(boost => _onBoostClicked.OnNext(boost))
                    .AddTo(_disposables);
            }
        }

        private void OnDestroy()
        {
            _onBoostClicked.Dispose();
            _disposables.Dispose();
        }

        public void UpdateCount(BoostType boost, int count)
        {
            foreach (var slot in _slots)
            {
                if (slot.BoostType != boost) continue;
                slot.UpdateCount(count);
                break;
            }
        }

        public void SetAllInteractable(bool interactable)
        {
            foreach (var slot in _slots)
                slot.SetInteractable(interactable);
        }

        public Vector3 GetIconWorldPosition(BoostType boost)
        {
            foreach (var slot in _slots)
                if (slot.BoostType == boost)
                    return slot.IconTransform.position;
            return Vector3.zero;
        }
    }
}
