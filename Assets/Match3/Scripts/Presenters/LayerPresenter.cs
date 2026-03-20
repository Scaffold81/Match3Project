#nullable enable

using System;
using Match3.Services.Layer;
using Match3.Views;
using R3;
using UnityEngine;
using Zenject;

namespace Match3.Presenters
{
    public sealed class LayerPresenter : IInitializable, IDisposable
    {
        private readonly LayerService _layerService;
        private readonly LayerView _layerView;
        private readonly BoardView _boardView;

        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public LayerPresenter(
            LayerService layerService,
            LayerView layerView,
            BoardView boardView)
        {
            _layerService = layerService;
            _layerView = layerView;
            _boardView = boardView;
        }

        public void Initialize()
        {
            _layerService.OnLayerCleared
                .Subscribe(cell => _layerView.ClearLayerCell(cell))
                .AddTo(_disposables);
        }

        public void RenderLayers(int rows, int cols)
        {
            _layerView.ClearAll();

            for (var row = 0; row < rows; row++)
            for (var col = 0; col < cols; col++)
            {
                if (!_layerService.HasLayer(row, col)) continue;

                var anchoredPos = _boardView.GetAnchoredPosition(row, col);
                _layerView.SpawnLayerCell(new Vector2Int(row, col), anchoredPos);
            }
        }

        public void Dispose() => _disposables.Dispose();
    }
}
