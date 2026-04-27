#nullable enable

using System.Collections.Generic;
using Match3.Core.Enums;
using UnityEngine;

namespace Match3.Core
{
    public sealed class GemMatch
    {
        public readonly List<Vector2Int> MatchingCells = new();
        public readonly List<IGemView>   MatchedGems   = new();

        public Vector2Int  OriginPoint  { get; set; }
        public NodeType    MatchNodeType { get; private set; } = NodeType.None;

        // ── Супер-фишка ───────────────────────────────────────────────────────
        public SuperGemType SuperGemToSpawn  { get; private set; } = SuperGemType.None;
        public Vector2Int   SuperGemSpawnPos { get; private set; }
        public bool         HasSuperGemSpawn => SuperGemToSpawn != SuperGemType.None;

        public void AddGem(IGemView gem)
        {
            if (gem.CurrentMatch != null) return;

            if (MatchNodeType == NodeType.None)
                MatchNodeType = gem.GemType;

            MatchingCells.Add(gem.CurrentIndex);
            MatchedGems.Add(gem);
            gem.CurrentMatch = this;
        }

        /// <summary>
        /// Вызывать после того как все гемы добавлены.
        /// Анализирует форму матча и записывает SuperGemToSpawn + SuperGemSpawnPos.
        /// </summary>
        public void ComputeSuperGem()
        {
            var count = MatchingCells.Count;
            if (count < 4)
            {
                SuperGemToSpawn = SuperGemType.None;
                return;
            }

            SuperGemToSpawn  = DetectShape(count);
            SuperGemSpawnPos = ComputeCenter();

            Debug.LogWarning(
                $"[GemMatch] Форма: {count} клеток → {SuperGemToSpawn} " +
                $"в позиции {SuperGemSpawnPos}");
        }

        // ── Определение формы ────────────────────────────────────────────────

        private SuperGemType DetectShape(int count)
        {
            if (count >= 6) return SuperGemType.MegaBomb;

            var allSameRow = IsAllSameRow();
            var allSameCol = IsAllSameCol();

            if (count == 4)
            {
                if (allSameRow) return SuperGemType.HorizontalArrow;
                if (allSameCol) return SuperGemType.VerticalArrow;
                // 4 в L-форме → бомба
                return SuperGemType.Bomb;
            }

            if (count == 5)
            {
                if (allSameRow || allSameCol) return SuperGemType.ColorBomb;
                // T или L форма
                return SuperGemType.Bomb;
            }

            return SuperGemType.None;
        }

        private bool IsAllSameRow()
        {
            var row = MatchingCells[0].x;
            for (var i = 1; i < MatchingCells.Count; i++)
                if (MatchingCells[i].x != row) return false;
            return true;
        }

        private bool IsAllSameCol()
        {
            var col = MatchingCells[0].y;
            for (var i = 1; i < MatchingCells.Count; i++)
                if (MatchingCells[i].y != col) return false;
            return true;
        }

        /// <summary>
        /// Центр матча — используем OriginPoint (позиция свопнутого гема).
        /// Он всегда входит в матч и является наиболее интуитивной точкой спавна.
        /// Fallback — центроид всех клеток.
        /// </summary>
        private Vector2Int ComputeCenter()
        {
            if (MatchingCells.Contains(OriginPoint))
                return OriginPoint;

            var sumRow = 0;
            var sumCol = 0;
            foreach (var c in MatchingCells)
            {
                sumRow += c.x;
                sumCol += c.y;
            }
            return new Vector2Int(
                Mathf.RoundToInt((float)sumRow / MatchingCells.Count),
                Mathf.RoundToInt((float)sumCol / MatchingCells.Count));
        }
    }
}
