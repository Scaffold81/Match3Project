#nullable enable

using System;
using UnityEngine;

namespace Match3.Core.Models
{
    [Serializable]
    public sealed class LevelStoryData
    {
        [field: SerializeField]
        [field: Tooltip("История перед стартом уровня — показывается в LevelTaskPopupView")]
        public StorySlide? StartStory { get; private set; }

        [field: SerializeField]
        [field: Tooltip("История победы — показывается в StageRewardPopupView (только на последнем уровне этапа)")]
        public StorySlide? WinStory { get; private set; }

        [field: SerializeField]
        [field: Tooltip("История поражения — показывается в LevelResultView")]
        public StorySlide? LoseStory { get; private set; }
    }
}
