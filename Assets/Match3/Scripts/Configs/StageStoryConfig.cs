#nullable enable

using Match3.Core.Models;
using UnityEngine;

namespace Match3.Configs
{
    [CreateAssetMenu(fileName = "StageStoryConfig", menuName = "Match3/Story/Stage Story")]
    public sealed class StageStoryConfig : ScriptableObject
    {
        [field: SerializeField]
        [field: Tooltip("История на экране выбора этапа (LevelSelectPopupView)")]
        public StorySlide? StageSelectStory { get; private set; }

        [field: SerializeField]
        [field: Tooltip("Истории для каждого из 3 уровней этапа. Индекс = LevelIndex.")]
        public LevelStoryData[] LevelStories { get; private set; } = new LevelStoryData[3];

        public LevelStoryData? GetLevelStory(int levelIndex)
        {
            if (levelIndex < 0 || levelIndex >= LevelStories.Length) return null;
            return LevelStories[levelIndex];
        }
    }
}
