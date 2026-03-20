#nullable enable

using System;
using Match3.Core.Enums;

namespace Match3.Core.Models
{
    [Serializable]
    public sealed class CellData
    {
        public CellType cellType;
        public NodeType nodeType;
        public bool hasLayer;
    }
}
