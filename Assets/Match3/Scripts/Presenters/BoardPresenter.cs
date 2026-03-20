#nullable enable

using System;
using Match3.Configs;
using Match3.Core.Enums;
using Match3.Services.Board;
using Match3.Services.Factories;
using Match3.Services.Spawn;
using Match3.Views;
using R3;
using UnityEngine;
using Zenject;

namespace Match3.Presenters
{
    public sealed class BoardPresenter : IInitializable, IDisposable
    {
        private readonly BoardService _boardService;
        private readonly BoardView    _boardView;
        private readonly BoardConfig  _boardConfig;
        private readonly GemFactory   _gemFactory;

        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public BoardPresenter(
            BoardService boardService,
            BoardView    boardView,
            BoardConfig  boardConfig,
            GemFactory   gemFactory)
        {
            _boardService = boardService;
            _boardView    = boardView;
            _boardConfig  = boardConfig;
            _gemFactory   = gemFactory;
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
            var gemView = _gemFactory.Create(nodeType, $"Gem_{nodeType}_{cell.x}_{cell.y}");
            if (gemView == null) return;

            _boardView.PlaceGem(cell, gemView);
        }

        public void Dispose() => _disposables.Dispose();
    }
}
