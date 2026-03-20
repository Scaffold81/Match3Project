#nullable enable

using System;
using System.Collections.Generic;
using Match3.Configs;
using Match3.Core.Enums;
using Match3.Services.Board;
using UnityEngine;
using Zenject;

namespace Match3.Services.Spawn
{
    public sealed class SpawnService
    {
        private readonly BoardService _boardService;
        private readonly System.Random _random = new();

        private NodeType[] _allowedTypes = Array.Empty<NodeType>();
        private int[] _spawnRows = Array.Empty<int>();

        [Inject]
        public SpawnService(BoardService boardService)
        {
            _boardService = boardService;
        }

        public void Initialize(LevelConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            _allowedTypes = config.AllowedNodeTypes.Length > 0
                ? config.AllowedNodeTypes
                : GetAllNodeTypes();

            _spawnRows = CalculateSpawnRows(config);
        }

        public List<(Vector2Int position, NodeType nodeType)> SpawnMissing()
        {
            var spawned = new List<(Vector2Int, NodeType)>();

            for (var col = 0; col < _boardService.Columns; col++)
            {
                for (var row = 0; row < _boardService.Rows; row++)
                {
                    if (!_boardService.IsNormalCell(row, col)) continue;
                    if (!_boardService.IsEmpty(row, col)) continue;

                    var nodeType = GetRandomAllowedType();
                    _boardService.SetNode(row, col, nodeType);
                    spawned.Add((new Vector2Int(row, col), nodeType));
                }
            }

            return spawned;
        }

        public int GetSpawnRow(int col) =>
            col >= 0 && col < _spawnRows.Length ? _spawnRows[col] : 0;

        private int[] CalculateSpawnRows(LevelConfig config)
        {
            var rows = new int[config.Columns];

            for (var col = 0; col < config.Columns; col++)
            {
                rows[col] = 0;
                for (var row = 0; row < config.Rows; row++)
                {
                    var cell = config.GetCell(row, col);
                    if (cell.cellType == CellType.Normal)
                    {
                        rows[col] = row;
                        break;
                    }
                }
            }

            return rows;
        }

        private NodeType GetRandomAllowedType() =>
            _allowedTypes[_random.Next(_allowedTypes.Length)];

        private NodeType[] GetAllNodeTypes()
        {
            var values = Enum.GetValues(typeof(NodeType));
            var result = new List<NodeType>();

            foreach (NodeType value in values)
                if (value != NodeType.None)
                    result.Add(value);

            return result.ToArray();
        }
    }
}
