#nullable enable

using System;
using Match3.Configs;
using Match3.Core.Enums;
using Match3.Services.Board;
using Match3.Services.Spawn;
using Match3.Views;
using R3;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Match3.Presenters
{
    public sealed class BoardPresenter : IInitializable, IDisposable
    {
        private readonly BoardService _boardService;
        private readonly BoardView _boardView;
        private readonly GemConfig _gemConfig;
        private readonly BoardConfig _boardConfig;
        private readonly SpawnService _spawnService;

        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public BoardPresenter(
            BoardService boardService,
            BoardView boardView,
            GemConfig gemConfig,
            BoardConfig boardConfig,
            SpawnService spawnService)
        {
            _boardService = boardService;
            _boardView = boardView;
            _gemConfig = gemConfig;
            _boardConfig = boardConfig;
            _spawnService = spawnService;
        }

        public void Initialize()
        {
            _boardView.Initialize(_boardConfig);
        }

        public void RenderBoard()
        {
            _boardView.ClearAll();

            for (var row = 0; row < _boardService.Rows; row++)
            for (var col = 0; col < _boardService.Columns; col++)
            {
                if (!_boardService.IsNormalCell(row, col)) continue;

                var nodeType = _boardService.GetNode(row, col);
                if (nodeType == NodeType.None) continue;

                SpawnGemView(new Vector2Int(row, col), nodeType);
            }
        }

        public void SpawnGemView(Vector2Int cell, NodeType nodeType)
        {
            var visual = _gemConfig.GetVisual(nodeType);
            if (visual == null)
            {
                Debug.LogWarning($"BoardPresenter: no visual for NodeType {nodeType}");
                return;
            }

            var go = new GameObject($"Gem_{nodeType}_{cell.x}_{cell.y}");
            go.AddComponent<Image>();
            var gemView = go.AddComponent<GemView>();
            gemView.Setup(nodeType, visual);

            _boardView.PlaceGem(cell, gemView);
        }

        public void Dispose() => _disposables.Dispose();
    }
}
