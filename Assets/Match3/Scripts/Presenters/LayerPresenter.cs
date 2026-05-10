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
    public sealed class LayerPresenter : IInitializable, IDisposable
    {
        private readonly BoardService _boardService;
        private readonly LayerView    _layerView;
        private readonly BoardView    _boardView;
        private readonly GemConfig    _gemConfig;

        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public LayerPresenter(
            BoardService boardService,
            LayerView    layerView,
            BoardView    boardView,
            GemConfig    gemConfig)
        {
            _boardService = boardService;
            _layerView    = layerView;
            _boardView    = boardView;
            _gemConfig    = gemConfig;
        }

        public void Initialize()
        {
            _boardService.OnObstacleHit
                .Subscribe(data =>
                {
                    var visual = GetVisual(data.pos);
                    if (visual != null)
                        _layerView.UpdateCellHp(data.pos, data.newHp, data.maxHp, visual);
                })
                .AddTo(_disposables);

            _boardService.OnObstacleCleared
                .Subscribe(pos => _layerView.ClearCell(pos))
                .AddTo(_disposables);
        }

        public void RenderLayers(int rows, int cols)
        {
            _layerView.ClearAll();

            foreach (var (pos, type, hp, maxHp) in _boardService.GetObstacles())
            {
                var visual = _gemConfig.GetObstacleVisual(type);
                if (visual == null)
                {
                    Debug.LogWarning($"[LayerPresenter] Нет ObstacleVisualData для {type} в GemConfig");
                    continue;
                }

                var anchoredPos = _boardView.GetAnchoredPosition(pos.x, pos.y);
                _layerView.SpawnObstacleCell(pos, hp, maxHp, visual, anchoredPos, _boardView.CellSize);
            }
        }

        public void Dispose() => _disposables.Dispose();

        private ObstacleVisualData? GetVisual(Vector2Int pos)
        {
            if (!_boardService.Cells.TryGetValue(pos, out var cell)) return null;
            return _gemConfig.GetObstacleVisual(cell.ObstacleType);
        }
    }
}
