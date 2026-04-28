#nullable enable

using System;
using System.Collections.Generic;
using Match3.Core.Enums;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.Views
{
    /// <summary>
    /// Нижняя панель — рюкзак с кнопками бустов.
    /// Каждая кнопка показывает иконку буста и его количество.
    /// </summary>
    public sealed class BackpackView : MonoBehaviour
    {
        [SerializeField] private BoostButtonEntry[] _entries = Array.Empty<BoostButtonEntry>();

        private readonly Subject<BoostType> _onBoostClicked = new();
        public Observable<BoostType> OnBoostClicked => _onBoostClicked;

        private void Awake()
        {
            foreach (var entry in _entries)
            {
                var boostType = entry.BoostType;
                entry.Button.onClick.AddListener(() =>
                    _onBoostClicked.OnNext(boostType));
            }
        }

        private void OnDestroy() => _onBoostClicked.Dispose();

        public void UpdateCount(BoostType boost, int count)
        {
            foreach (var entry in _entries)
            {
                if (entry.BoostType != boost) continue;
                entry.CountLabel.text = count.ToString();
                entry.Button.interactable = count > 0;
                entry.CanvasGroup.alpha = count > 0 ? 1f : 0.45f;
                break;
            }
        }

        public void SetAllInteractable(bool interactable)
        {
            foreach (var entry in _entries)
            {
                entry.Button.interactable = interactable;
                entry.CanvasGroup.alpha = interactable ? 1f : 0.45f;
            }
        }

        /// <summary>
        /// Возвращает мировую позицию иконки для анимации вылета в шапку.
        /// </summary>
        public Vector3 GetIconWorldPosition(BoostType boost)
        {
            foreach (var entry in _entries)
                if (entry.BoostType == boost)
                    return entry.IconTransform.position;
            return Vector3.zero;
        }
    }

    [Serializable]
    public sealed class BoostButtonEntry
    {
        [field: SerializeField] public BoostType BoostType { get; private set; }
        [field: SerializeField] public Button Button { get; private set; } = null!;
        [field: SerializeField] public TMP_Text CountLabel { get; private set; } = null!;
        [field: SerializeField] public CanvasGroup CanvasGroup { get; private set; } = null!;
        [field: SerializeField] public RectTransform IconTransform { get; private set; } = null!;
    }
}
