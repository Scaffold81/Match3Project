#nullable enable

using System.Collections.Generic;
using UnityEngine;

namespace Match3.Core
{
    public sealed class GemMatch
    {
        /// <summary>Индексы ячеек, входящих в матч.</summary>
        public readonly List<Vector2Int> MatchingCells = new();

        /// <summary>Гемы, входящие в матч — для ObjectiveService и эффектов.</summary>
        public readonly List<IGemView> MatchedGems = new();

        public Vector2Int OriginPoint;

        public void AddGem(IGemView gem)
        {
            if (gem.CurrentMatch != null) return;

            MatchingCells.Add(gem.CurrentIndex);
            MatchedGems.Add(gem);
            gem.CurrentMatch = this;
        }
    }
}
