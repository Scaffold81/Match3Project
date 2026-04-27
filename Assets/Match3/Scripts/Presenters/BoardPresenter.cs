#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Match3.Configs;
using Match3.Core;
using Match3.Core.Enums;
using Match3.Services.Board;
using Match3.Views;
using UnityEngine;
using Zenject;

namespace Match3.Presenters
{
    public sealed class BoardPresenter : IInitializable, IDisposable
    {
        private readonly BoardService    _boardService;
        private readonly BoardView       _boardView;
        private readonly BoardConfig     _boardConfig;
        private readonly GemConfig       _gemConfig;
        private readonly AnimationConfig _animConfig;

        [Inject]
        public BoardPresenter(
            BoardService    boardService,
            BoardView       boardView,
            BoardConfig     boardConfig,
            GemConfig       gemConfig,
            AnimationConfig animConfig)
        {
            _boardService = boardService;
            _boardView    = boardView;
            _boardConfig  = boardConfig;
            _gemConfig    = gemConfig;
            _animConfig   = animConfig;

            _boardView.Initialize(_boardConfig, _gemConfig.GemViewPrefab);
        }

        public void Initialize() { }

        // ── Setup ─────────────────────────────────────────────────────────────

        public void InitializeLayout() =>
            _boardView.InitializeLayout(_boardService.Rows, _boardService.Columns);

        public void CreateGems(IEnumerable<(Vector2Int pos, NodeType type)> spawnList)
        {
            foreach (var (pos, type) in spawnList)
                CreateGemAt(pos, type);
        }

        public GemView CreateGemAt(Vector2Int pos, NodeType type)
        {
            var visual = _gemConfig.GetVisual(type)
                ?? throw new InvalidOperationException($"No visual for {type}");

            var view = _boardView.InstantiateGem(pos.x, pos.y);
            view.Init(pos, type);
            view.SetVisual(type, visual);

            _boardService.PlaceGem(pos, view);
            return view;
        }

        /// <summary>
        /// Создаёт супер-фишку: цвет + иконка типа.
        /// Анимирует особым спавном (пульс).
        /// </summary>
        public GemView CreateSuperGemAt(Vector2Int pos, NodeType nodeType, SuperGemType superGemType)
        {
            var view = CreateGemAt(pos, nodeType);

            var iconData = _gemConfig.GetSuperGemIcon(superGemType);
            if (iconData != null)
                view.SetSuperIcon(iconData);
            else
                view.SetSuperGemType(superGemType);

            view.PlaySuperSpawn(_animConfig.FallDuration);

            Debug.LogWarning($"[BoardPresenter] Супер-фишка создана: {superGemType} ({nodeType}) в {pos}");
            return view;
        }

        // ── Swap ──────────────────────────────────────────────────────────────

        public async UniTask AnimateSwapAsync(
            Vector2Int from, Vector2Int to,
            IGemView gemFrom, IGemView gemTo,
            CancellationToken ct)
        {
            var viewFrom = gemFrom as GemView;
            var viewTo   = gemTo   as GemView;
            if (viewFrom == null || viewTo == null) return;

            var worldTo   = _boardView.GetSlotWorldPosition(to);
            var worldFrom = _boardView.GetSlotWorldPosition(from);

            _boardView.ReparentToOverlay(viewFrom);
            _boardView.ReparentToOverlay(viewTo);

            var tcsA = new UniTaskCompletionSource();
            var tcsB = new UniTaskCompletionSource();

            viewFrom.PlayMoveToWorldPos(worldTo, _animConfig.SwapDuration, () =>
            {
                _boardView.ReparentToContainer(viewFrom, to);
                tcsA.TrySetResult();
            });

            viewTo.PlayMoveToWorldPos(worldFrom, _animConfig.SwapDuration, () =>
            {
                _boardView.ReparentToContainer(viewTo, from);
                tcsB.TrySetResult();
            });

            await UniTask.WhenAll(tcsA.Task, tcsB.Task).AttachExternalCancellation(ct);
            Debug.LogWarning($"[BoardPresenter] AnimateSwapAsync завершён: viewFrom→{to}, viewTo→{from}");
        }

        public async UniTask AnimateReturnSwapAsync(
            Vector2Int from, Vector2Int to,
            IGemView gemFrom, IGemView gemTo,
            CancellationToken ct)
        {
            var viewFrom = gemFrom as GemView;
            var viewTo   = gemTo   as GemView;
            if (viewFrom == null || viewTo == null) return;

            var worldTo   = _boardView.GetSlotWorldPosition(to);
            var worldFrom = _boardView.GetSlotWorldPosition(from);

            _boardView.ReparentToOverlay(viewFrom);
            _boardView.ReparentToOverlay(viewTo);

            var tcsA = new UniTaskCompletionSource();
            var tcsB = new UniTaskCompletionSource();

            viewFrom.PlayMoveToWorldPos(worldTo, _animConfig.SwapReturnDuration, () =>
            {
                _boardView.ReparentToContainer(viewFrom, to);
                tcsA.TrySetResult();
            });

            viewTo.PlayMoveToWorldPos(worldFrom, _animConfig.SwapReturnDuration, () =>
            {
                _boardView.ReparentToContainer(viewTo, from);
                tcsB.TrySetResult();
            });

            await UniTask.WhenAll(tcsA.Task, tcsB.Task).AttachExternalCancellation(ct);
            Debug.LogWarning($"[BoardPresenter] AnimateReturnSwapAsync завершён: viewFrom→{to}, viewTo→{from}");
        }

        // ── Falls ─────────────────────────────────────────────────────────────

        public async UniTask AnimateFallsAsync(
            IEnumerable<(Vector2Int from, Vector2Int to)> moves,
            CancellationToken ct)
        {
            var hasAny = false;

            foreach (var (_, to) in moves)
            {
                var gem = _boardService.GetGem(to) as GemView;
                if (gem == null) continue;

                var targetWorldPos = _boardView.GetSlotWorldPosition(to);
                var slot           = to;

                _boardView.ReparentToOverlay(gem);
                gem.PlayFallToWorldPos(targetWorldPos, _animConfig.FallDuration, () =>
                {
                    _boardView.ReparentToContainer(gem, slot);
                });

                hasAny = true;
            }

            if (hasAny)
                await UniTask.Delay(
                    TimeSpan.FromSeconds(_animConfig.FallDuration),
                    cancellationToken: ct);
        }

        // ── Destroy ───────────────────────────────────────────────────────────

        public async UniTask AnimateDestroyMatchAsync(GemMatch match, CancellationToken ct)
        {
            var tasks = new List<UniTask>(match.MatchedGems.Count);

            foreach (var gem in match.MatchedGems)
            {
                var gemView = gem as GemView;
                if (gemView == null) continue;

                var pos = gem.CurrentIndex;
                var tcs = new UniTaskCompletionSource();

                gemView.PlayDestroy(_animConfig.MatchDestroyDuration, () =>
                {
                    _boardService.RemoveGem(pos);
                    _boardView.DestroyGem(gemView);
                    tcs.TrySetResult();
                });

                tasks.Add(tcs.Task);
            }

            if (tasks.Count > 0)
                await UniTask.WhenAll(tasks).AttachExternalCancellation(ct);
        }

        /// <summary>
        /// Уничтожение произвольного набора позиций (взрывы супер-фишек).
        /// </summary>
        public async UniTask AnimateDestroyCellsAsync(
            IEnumerable<Vector2Int> cells,
            CancellationToken ct)
        {
            var tasks = new List<UniTask>();

            foreach (var pos in cells)
            {
                var gem = _boardService.GetGem(pos);
                if (gem == null) continue;

                var gemView = gem as GemView;
                if (gemView == null) continue;

                var capturedPos = pos;
                var tcs         = new UniTaskCompletionSource();

                gemView.PlayDestroy(_animConfig.MatchDestroyDuration, () =>
                {
                    _boardService.RemoveGem(capturedPos);
                    _boardView.DestroyGem(gemView);
                    tcs.TrySetResult();
                });

                tasks.Add(tcs.Task);
            }

            if (tasks.Count > 0)
                await UniTask.WhenAll(tasks).AttachExternalCancellation(ct);
        }

        // ── Spawn ─────────────────────────────────────────────────────────────

        public async UniTask AnimateSpawnAsync(
            IEnumerable<(Vector2Int pos, NodeType type)> spawnList,
            CancellationToken ct)
        {
            var hasAny = false;

            foreach (var (pos, type) in spawnList)
            {
                var gem = CreateGemAt(pos, type);
                gem.PlaySpawn(_animConfig.FallDuration);
                hasAny = true;
            }

            if (hasAny)
                await UniTask.Delay(
                    TimeSpan.FromSeconds(_animConfig.FallDuration),
                    cancellationToken: ct);
        }

        public void Dispose() { }
    }
}
