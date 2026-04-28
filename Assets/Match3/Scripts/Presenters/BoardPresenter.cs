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
            view.SetConfig(_gemConfig);
            view.Init(pos, type);
            view.SetVisual(type, visual);

            _boardService.PlaceGem(pos, view);
            return view;
        }

        public GemView CreateSuperGemAt(Vector2Int pos, NodeType nodeType, SuperGemType superGemType)
        {
            var view     = CreateGemAt(pos, nodeType);
            var iconData = _gemConfig.GetSuperGemIcon(superGemType);
            if (iconData != null) view.SetSuperIcon(iconData);
            else                  view.SetSuperGemType(superGemType);
            view.PlaySuperSpawn(_animConfig.FallDuration);
            Debug.LogWarning($"[BoardPresenter] Супер-фишка: {superGemType} ({nodeType}) в {pos}");
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
            Debug.LogWarning($"[BoardPresenter] Swap завершён: {from}↔{to}");
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
        }

        // ── Shuffle ───────────────────────────────────────────────────────────

        /// <summary>
        /// Анимация перемешивания:
        ///   Фаза 1 — PlayFold: все фишки сжимаются (НЕ уничтожаются, НЕ вызывает SetEmpty)
        ///   Фаза 2 — SetGemType: меняем спрайт/цвет в сжатом состоянии
        ///   Фаза 3 — PlaySpawn: разворачиваются с новым визуалом
        ///
        /// HintService.Shuffle() уже обновил данные в BoardService до вызова этого метода.
        /// </summary>
        public async UniTask AnimateShuffleAsync(
            IEnumerable<(Vector2Int pos, NodeType type)> newLayout,
            CancellationToken ct)
        {
            // Собираем все гемы с их новыми типами
            var allGems = new List<(GemView view, NodeType newType)>();
            foreach (var (pos, type) in newLayout)
            {
                var gem = _boardService.GetGem(pos) as GemView;
                if (gem == null) continue;
                allGems.Add((gem, type));
            }

            if (allGems.Count == 0) return;

            // Фаза 1 — сжатие через PlayFold (не SetEmpty, не MarkDestroyed)
            var foldTasks = new List<UniTask>(allGems.Count);
            foreach (var (view, _) in allGems)
            {
                var tcs = new UniTaskCompletionSource();
                view.PlayFold(_animConfig.ShuffleFoldDuration, () => tcs.TrySetResult());
                foldTasks.Add(tcs.Task);
            }
            await UniTask.WhenAll(foldTasks).AttachExternalCancellation(ct);

            // Фаза 2 — меняем визуал пока фишки сжаты (scale = 0, невидимы)
            foreach (var (view, newType) in allGems)
                view.SetGemType(newType);

            // Фаза 3 — разворачиваемся с новым цветом
            foreach (var (view, _) in allGems)
                view.PlaySpawn(_animConfig.ShuffleFoldDuration);

            await UniTask.Delay(
                TimeSpan.FromSeconds(_animConfig.ShuffleFoldDuration),
                cancellationToken: ct);

            Debug.LogWarning("[BoardPresenter] AnimateShuffleAsync завершён");
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
                    _boardView.ReparentToContainer(gem, slot));

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

        public async UniTask AnimateDestroyCellsAsync(
            IEnumerable<Vector2Int> cells,
            CancellationToken ct)
        {
            var tasks = new List<UniTask>();

            foreach (var pos in cells)
            {
                var gemView = _boardService.GetGem(pos) as GemView;
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
