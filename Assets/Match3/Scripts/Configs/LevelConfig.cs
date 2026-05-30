#nullable enable

using System;
using Match3.Core.Enums;
using Match3.Core.Models;
using UnityEngine;

namespace Match3.Configs
{
    [CreateAssetMenu(fileName = "LevelConfig", menuName = "Match3/Configs/Level")]
    public sealed class LevelConfig : ScriptableObject
    {
        [field: SerializeField] public int MoveLimit { get; private set; } = 0;
        [field: SerializeField] public NodeType[] AllowedNodeTypes { get; private set; } = Array.Empty<NodeType>();
        [field: SerializeField] public ObjectiveData[] Objectives { get; private set; } = Array.Empty<ObjectiveData>();
        [field: SerializeField] public CellRow[] Grid { get; private set; } = Array.Empty<CellRow>();


        public int Rows    => Grid.Length;
        public int Columns => Grid.Length > 0 ? Grid[0].Cells.Length : 0;

        public CellData GetCell(int row, int col) => Grid[row].Cells[col];

        public static LevelConfig CreateForTest(
            CellRow[]       grid,
            int             moveLimit,
            ObjectiveData[] objectives)
        {
            var config       = CreateInstance<LevelConfig>();
            config.Grid      = grid;
            config.MoveLimit = moveLimit;
            config.Objectives = objectives;
            return config;
        }
    }

    [Serializable]
    public sealed class CellRow
    {
        [field: SerializeField] public CellData[] Cells { get; set; } = Array.Empty<CellData>();

        public CellRow() { }

        public CellRow(CellData[] cells)
        {
            Cells = cells;
        }
    }
}
