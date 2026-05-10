#nullable enable

using System;
using Match3.Core.Enums;

namespace Match3.Core.Models
{
    [Serializable]
    public sealed class CellData
    {
        public CellType    cellType;
        public NodeType    nodeType;
        public ObstacleType obstacleType;

        /// <summary>
        /// Количество ударов необходимых чтобы уничтожить препятствие.
        /// 0 означает «использовать значение по умолчанию для данного типа».
        /// </summary>
        public int obstacleHp;

        public CellData() { }

        public CellData(CellType type, NodeType node,
                        ObstacleType obstacle = ObstacleType.None, int hp = 0)
        {
            cellType    = type;
            nodeType    = node;
            obstacleType = obstacle;
            obstacleHp  = hp;
        }
    }
}
