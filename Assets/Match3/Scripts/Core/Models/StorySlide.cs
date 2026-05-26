#nullable enable

using System;
using UnityEngine;

namespace Match3.Core.Models
{
    [Serializable]
    public sealed class StorySlide
    {
        [field: SerializeField]
        [field: Tooltip("Картинка слайда")]
        public Sprite? Image { get; private set; }

        [field: SerializeField]
        [field: Tooltip("ID строки в системе локализации")]
        public string? LocalizationId { get; private set; }

        [field: SerializeField]
        [field: Tooltip("Текст до подключения локализации")]
        public string? FallbackText { get; private set; }

        public bool HasContent =>
            Image != null ||
            !string.IsNullOrEmpty(LocalizationId) ||
            !string.IsNullOrEmpty(FallbackText);
    }
}
