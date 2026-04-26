# Логирование флоу игры Match3 — Улучшенная версия

## Цель
Отслеживание всех этапов от загрузки конфигов до завершения генерации уровня с детализированными логами.

---

## 📦 Этап 1: Загрузка конфигов (ProjectConfigInstaller)

```csharp
// ✅ Логи добавлены в ProjectConfigInstaller.InstallBindings()
private void ValidateConfigs()
{
    Debug.Log($"[CONFIG] Loading GemConfig...");
    if (_gemConfig == null)
        Debug.LogError("[CONFIG] ❌ GemConfig is not assigned");
    else
        Debug.Log($"[CONFIG] ✅ GemConfig loaded: {_gemConfig.Gems.Length} types");

    Debug.Log($"[CONFIG] Loading BoardConfig...");
    if (_boardConfig == null)
        Debug.LogError("[CONFIG] ❌ BoardConfig is not assigned");
    else
        Debug.Log($"[CONFIG] ✅ BoardConfig loaded: CellSize={_boardConfig.CellSize}, Spacing={_boardConfig.CellSpacing}");

    Debug.Log($"[CONFIG] Loading AnimationConfig...");
    if (_animationConfig == null)
        Debug.LogError("[CONFIG] ❌ AnimationConfig is not assigned");
    else
        Debug.Log($"[CONFIG] ✅ AnimationConfig loaded: Swap={_animationConfig.SwapDuration}s, Fall={_animationConfig.FallDuration}s");

    Debug.Log($"[CONFIG] Loading LevelConfigRepository...");
    if (_levelConfigRepository == null)
        Debug.LogError("[CONFIG] ❌ LevelConfigRepository is not assigned");
    else
        Debug.Log($"[CONFIG] ✅ LevelConfigRepository loaded: {_levelConfigRepository.Count} levels");
}
```

---

## 🎮 Этап 2: Инициализация (GameLoopController.Initialize)

```csharp
public void Initialize()
{
    Debug.Log($"[GAMELOOP] Checking LevelConfigRepository...");
    var levelConfigs = _levelRepository.Levels;
    
    if (levelConfigs.Length == 0)
    {
        Debug.LogWarning($"[GAMELOOP] ⚠️ LevelConfigRepository.Levels is empty, using fallback test level");
        levelConfigs = new[] { CreateTestLevel() };
        Debug.Log($"[GAMELOOP] ✅ Created fallback test level");
    }

    var levelConfig = _levelRepository.First ?? levelConfigs[0];
    Debug.Log($"[GAMELOOP] ✅ Selected level: index={levelConfigs.IndexOf(levelConfig)}, moveLimit={levelConfig.MoveLimit}");
    
    // Вывод структуры уровня
    Debug.Log($"[GAMELOOP] Level grid: {levelConfig.Rows}x{levelConfig.Columns} cells");
    for (var r = 0; r < levelConfig.Rows; r++)
    {
        var rowCells = string.Join(", ", Enumerable.Range(0, levelConfig.Columns).Select(c => levelConfig.GetCell(r, c).cellType.ToString()));
        Debug.Log($"[GAMELOOP]   Row {r}: {rowCells}");
    }

    _swapService.OnSwapSuccess
        .Subscribe(data => OnSwapSucceeded().Forget())
        .AddTo(_disposables);

    _levelService.State
        .Where(state => state == LevelState.Playing)
        .Take(1)
        .Subscribe(_ => OnLevelStarted())
        .AddTo(_disposables);

    Debug.Log("[GAMELOOP] Initializing BoardService...");
    _levelService.StartLevel(levelConfig);
    Debug.Log($"[GAMELOOP] ✅ Board initialized: {_boardService.Rows}x{_boardService.Columns}");

    Debug.Log("[GAMELOOP] Initializing LayerService...");
    Debug.Log($"[GAMELOOP] ✅ LayerService initialized: {levelConfig.Grid.Sum(c => c.hasLayer)} layer cells");

    Debug.Log("[GAMELOOP] Initializing ObjectiveService...");
    Debug.Log($"[GAMELOOP] ✅ Objectives: {_objectiveService.Progress.Length} objectives");
    for (var i = 0; i < _objectiveService.Progress.Length; i++)
    {
        var prog = _objectiveService.Progress.CurrentValue[i];
        Debug.Log($"[GAMELOOP]   Objective {i}: {prog.NodeType} -> collect {prog.Collected}/{prog.Required}");
    }

    Debug.Log("[GAMELOOP] Initializing MoveCounterService...");
    Debug.Log($"[GAMELOOP] ✅ MoveCounter: {_moveCounterService.IsLimited ? "Limited" : "Unlimited"}, Moves={_moveCounterService.MovesLeft.CurrentValue}");

    Debug.Log("[GAMELOOP] Initializing SpawnService...");
    var hiddenCells = levelConfig.Grid.Sum(c => c.cellType == CellType.Hidden ? 1 : 0);
    Debug.Log($"[GAMELOOP] ✅ SpawnService initialized with {hiddenCells} hidden cells");

    Debug.Log("[GAMELOOP] Spawning missing gems...");
    _spawnService.SpawnMissing();
    var filledCells = _boardService.Board.CurrentValue.Sum(c => c != NodeType.None ? 1 : 0);
    Debug.Log($"[GAMELOOP] ✅ Spawn complete. Board state: {filledCells} filled cells");
}
```

---

## 🎨 Этап 3: Рендеринг (OnLevelStarted)

```csharp
private void OnLevelStarted()
{
    Debug.Log("[BOARD] Rendering board: {_boardService.Rows}x{_boardService.Columns}");
    _boardPresenter.RenderBoard();
    Debug.Log("[BOARD] ✅ Board render complete");

    Debug.Log("[LAYER] Rendering layers: {_boardService.Rows}x{_boardService.Columns}");
    _layerPresenter.RenderLayers(_boardService.Rows, _boardService.Columns);
    Debug.Log("[LAYER] ✅ Layers render complete");

    Debug.Log("[OBJECTIVE] Rendering objectives...");
    _objectivePresenter.RenderObjectives(_objectiveService.Progress.CurrentValue);
    Debug.Log("[OBJECTIVE] ✅ Objectives setup complete");

    Debug.Log("[LEVEL] Setup move counter...");
    _levelPresenter.SetupMoveCounter();
    Debug.Log("[LEVEL] ✅ Move counter setup complete");
}
```

---

## 🔄 Этап 4: Свап (SwapPresenter + GameLoopController.OnSwapSucceeded)

### Выбор ячейки:
```csharp
private void OnGemTapped(Vector2Int cell)
{
    if (_selectedCell == null)
    {
        _selectedCell = cell;
        Debug.Log($"[SWAP] ✅ Selected cell: {cell}");
        return;
    }

    var from = _selectedCell.Value;
    _selectedCell = null;
    
    Debug.Log($"[SWAP] Attempting swap: {from} ↔ {cell}");
    _swapService.TrySwap(from, cell);
}
```

### Успешный свап:
```csharp
_swapService.OnSwapSuccess
    .Subscribe(data =>
    {
        Debug.Log($"[SWAP] ✅ Swap succeeded: {data.from} ↔ {data.to}");
        _boardView.SwapVisualsAt(data.from, data.to);
        _boardView.GetGemView(data.from)?.PlaySwapPulse(_animationConfig.SwapDuration);
        _boardView.GetGemView(data.to)?.PlaySwapPulse(_animationConfig.SwapDuration);
        _selectedCell = null;
    })
```

### Неуспешный свап:
```csharp
_swapService.OnSwapFailed
    .Subscribe(data =>
    {
        Debug.Log($"[SWAP] ❌ Swap failed: {data.from} ↔ {data.to}");
        // Меняем визуал туда-обратно с паузой
        _boardView.SwapVisualsAt(data.from, data.to);
        _boardView.GetGemView(data.from)?.PlaySwapPulse(_animationConfig.SwapDuration, () =>
            _boardView.SwapVisualsAt(data.from, data.to));
        _selectedCell = null;
    })
```

---

## 🎯 Этап 5: Поиск матчей (ProcessMatchesAsync)

```csharp
private async UniTask ProcessMatchesAsync(CancellationToken ct)
{
    var board   = _boardService.Board.CurrentValue;
    var matches = _matchService.FindMatches(board, _boardService.Rows, _boardService.Columns);

    Debug.Log($"[MATCH] Found {matches.Count} match groups");
    
    while (matches.Count > 0)
    {
        var matchedCells  = _matchService.GetAllMatchedCells(matches);
        var boardSnapshot = CopyBoard(board);

        Debug.Log($"[OBJECTIVE] Registered match: {matchedCells.Count} cells");
        _objectiveService.RegisterMatch(matchedCells, boardSnapshot);

        Debug.Log($"[LAYER] Processed matches in layers: {_layerService.TotalLayerCells} remaining layer cells");
        _layerService.ProcessMatches(matchedCells);

        // Анимируем уничтожение — PlayDestroy внутри уже делает SetEmpty
        Debug.Log($"[MATCH] Animating destruction of {matchedCells.Count} cells...");
        await AnimateDestroyAsync(matchedCells, ct);
        Debug.Log("[MATCH] ✅ Destruction animation complete");

        // Обновляем логику
        foreach (var cell in matchedCells)
            _boardService.RemoveNode(cell.x, cell.y);
        
        Debug.Log($"[BOARD] Removed {matchedCells.Count} cells. Board now has {_boardService.Board.CurrentValue.Sum(c => c != NodeType.None ? 1 : 0)} filled cells");

        await UniTask.Yield(ct);

        // Гравитация — сдвигаем визуал вниз
        var falls = _gravityService.ApplyGravity();
        ApplyFallsVisual(falls);
        Debug.Log($"[GRAVITY] Applied gravity: {falls.Count} falls");

        await UniTask.Delay(
            TimeSpan.FromSeconds(_animationConfig.FallDuration),
            cancellationToken: ct);

        // Спаун — ставим визуал + анимация появления
        var spawned = _spawnService.SpawnMissing();
        await AnimateSpawnAsync(spawned, ct);
        Debug.Log($"[SPAWN] Spawned {spawned.Count} new gems");
    }
    
    Debug.Log("[MATCH] ✅ All matches processed");
}
```

---

## 🏆 Этап 6: Проверка условий (LevelService)

### Старт уровня:
```csharp
public void StartLevel(LevelConfig config)
{
    // ... инициализация служб ...

    Debug.Log("[LEVEL] ✅ Spawn complete. Board state: {filledCells} filled cells");
    SubscribeToEvents();

    _state.Value = LevelState.Playing;
    Debug.Log("[LEVEL] ✅ Level started");
}
```

### Проверка победы:
```csharp
private void CheckWinCondition()
{
    var objectivesComplete = _objectiveService.IsAllCompleted;
    var layersComplete     = _layerService.TotalLayerCells == 0 || _layerService.IsAllCleared;
    
    Debug.Log($"[LEVEL] Win check: objectives={objectivesComplete}, layers={layersComplete}");
    
    if (objectivesComplete && layersComplete) 
    {
        Debug.Log("[LEVEL] 🎉 WIN!");
        Win();
    }
}
```

### Проверка поражения:
```csharp
private void CheckLoseCondition()
{
    if (_moveCounterService.IsLimited && _moveCounterService.IsExhausted)
    {
        Debug.Log("[LEVEL] 💀 LOSE! Moves exhausted");
        Lose();
    }
}
```

---

## 📊 Полная последовательность логов

```
[CONFIG] Loading GemConfig...
[CONFIG] ✅ GemConfig loaded: 7 types
[CONFIG] Loading BoardConfig...
[CONFIG] ✅ BoardConfig loaded: CellSize=100, Spacing=4
[CONFIG] Loading AnimationConfig...
[CONFIG] ✅ AnimationConfig loaded: Swap=0.2s, Fall=0.3s
[CONFIG] Loading LevelConfigRepository...
[CONFIG] ✅ LevelConfigRepository loaded: 1 levels

[GAMELOOP] Checking LevelConfigRepository...
[GAMELOOP] ✅ Selected level: index=0, moveLimit=1000
[GAMELOOP] Level grid: 5x7 cells
[GAMELOOP]   Row 0: Normal,Normal,Normal,Normal,Normal
[GAMELOOP]   ... (и т.д.)
[GAMELOOP] Initializing BoardService...
[GAMELOOP] ✅ Board initialized: 5x7
[GAMELOOP] Initializing LayerService...
[GAMELOOP] ✅ LayerService initialized: 4 layer cells
[GAMELOOP] Initializing ObjectiveService...
[GAMELOOP] ✅ Objectives: 2 objectives
[GAMELOOP]   Objective 0: Red -> collect 10
[GAMELOOP]   Objective 1: Green -> collect 10
[GAMELOOP] Initializing MoveCounterService...
[GAMELOOP] ✅ MoveCounter: Limited, Moves=1000
[GAMELOOP] Initializing SpawnService...
[GAMELOOP] ✅ SpawnService initialized with 0 hidden cells
[GAMELOOP] Spawning missing gems...
[GAMELOOP] ✅ Spawn complete. Board state: 12 filled cells

[BOARD] Rendering board: 5x7
[BOARD]   Cell (0,0): Red -> Color.red
[BOARD]   ... (и т.д.)
[BOARD] ✅ Board render complete

[LAYER] Rendering layers: 5x7
[LAYER]   Layer at (2,2) pos=(..., ...)
[LAYER]   ... (и т.д.)
[LAYER] ✅ Layers render complete

[OBJECTIVE] Rendering objectives...
[OBJECTIVE]   Goal 0: Red -> collect 10 (sprite=...)
[OBJECTIVE]   Goal 1: Green -> collect 10 (sprite=...)
[OBJECTIVE] ✅ Objectives setup complete

[SWAP] Selected cell: (0,0)
[SWAP] Attempting swap: (0,0) ↔ (0,1)
[SWAP] ✅ Swap succeeded: (0,0) ↔ (0,1)

[MATCH] Found 1 match groups
[OBJECTIVE] Registered match: 3 cells
[LAYER] Processed matches in layers: 4 remaining layer cells
[MATCH] Animating destruction of 3 cells...
[MATCH] ✅ Destruction animation complete
[BOARD] Removed 3 cells. Board now has 9 filled cells
[GRAVITY] Applied gravity: 5 falls
[SPAWN] Spawned 5 new gems

[OBJECTIVE] Collected 3 of Red: 3/10
[OBJECTIVE] 🎉 All objectives completed!

[LEVEL] Checking win condition...
[LEVEL] Win check: objectives=true, layers=false
[LEVEL] Checking lose condition...

// ... (цикл продолжается)

[LEVEL] 🎉 WIN!   // или 💀 LOSE! Moves exhausted
```

---

## ✅ Результат

После добавления логов вы сможете отследить каждый шаг:
1. ✅ Загрузка конфигов и их валидность
2. ✅ Создание уровня из репозитория
3. ✅ Инициализация всех служб
4. ✅ Рендеринг доски, целей, слоёв
5. ✅ Свапы и их валидность
6. ✅ Поиск матчей и уничтожение
7. ✅ Гравитацию и спавн новых фишек
8. ✅ Проверку условий победы/поражения
