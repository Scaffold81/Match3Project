#nullable enable

using System;
using Match3.Configs;
using Match3.Core.Enums;
using Match3.Services.Board;
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
        private readonly GemConfig    _gemConfig;

        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public BoardPresenter(
            BoardService boardService,
            BoardView    boardView,
            BoardConfig  boardConfig,
            GemConfig    gemConfig)
        {
            _boardService = boardService;
            _boardView    = boardView;
            _boardConfig  = boardConfig;
            _gemConfig    = gemConfig;

            _boardView.Initialize(_boardConfig);
        }

        public void Initialize()
        {
            // Создаём фиксированную сетку после инициализации сервиса
            var rows = _boardService.Rows;
            var cols = _boardService.Columns;
            _boardView.InitializeGrid(rows, cols);

            // Сразу рендерим начальное состояние
            RenderBoard();

            // Подписываемся на изменения (каскадные матчи)
            _disposables.Add(_boardService.Board.Subscribe(visual => RenderBoard()));
        }

        public void RenderBoard()
        {
            // Заполняем существующую сетку визуалами по данным сервиса
            for (var row = 0; row < _boardService.Rows; row++)
            for (var col = 0; col < _boardService.Columns; col++)
            {
                var cell     = new Vector2Int(row, col);
                var nodeType = _boardService.GetNode(row, col);

                if (!_boardService.IsNormalCell(row, col) || nodeType == NodeType.None)
                {
                    _boardView.GetGemView(cell)?.SetEmpty();
                    continue;
                }

                SetCellVisual(cell, nodeType);
            }
        }

        // Обновляет визуал одной ячейки
        public void SetCellVisual(Vector2Int cell, NodeType nodeType)
        {
            var visual = _gemConfig.GetVisual(nodeType);
            if (visual == null)
            {
                Debug.LogWarning($"BoardPresenter: no visual for {nodeType}");
                return;
            }

            _boardView.GetGemView(cell)?.SetVisual(nodeType, visual);
        }

        public void SetCellEmpty(Vector2Int cell)
        {
            _boardView.GetGemView(cell)?.SetEmpty();
        }

        public void Dispose() => _disposables.Dispose();
    }
}
