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

        public CellData() { }
        
        // Конструктор для быстрого создания (для тестов)
        public CellData(CellType type, NodeType node, bool layer = false)
        {
            cellType   = type;
            nodeType   = node;
            hasLayer   = layer;
        }
    }
}
