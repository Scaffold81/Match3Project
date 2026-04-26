# Match-3 — Архитектурный план

> ⚠️ Читай этот файл перед любой задачей. Держи актуальным.
> 📋 Прогресс разработки → `ROADMAP.md`
> 🎮 Механики и геймдизайн → `GDD.md`

---

## 🗂️ Enums

```csharp
public enum NodeType { None, Red, Blue, Green, Yellow, Purple, Orange }
public enum CellType { Normal, Hidden }
public enum SceneId  { Bootstrap, Game }
public enum LevelState { Idle, Playing, Won, Lost }
```

---

## 📦 Data Models

```
CellData       : cellType, nodeType, hasLayer
ObjectiveData  : nodeType, count
ObjectiveProgress : NodeType, Required, Collected, IsCompleted  ← в LevelService
```

---

## 📦 Configs (ScriptableObjects)

```
LevelConfigRepository         ← ProjectConfigInstaller
└── Levels[] : LevelConfig[]  — все уровни игры

LevelConfig
├── MoveLimit          : int             — 0 = без ограничений
├── AllowedNodeTypes[] : NodeType[]      — пусто = все типы
├── Objectives[]       : ObjectiveData[]
└── Grid[]             : CellRow[]
    └── CellRow.Cells[]: CellData[]

GemConfig
└── Gems[]: GemVisualData      — NodeType + Sprite + Color + Prefab

BoardConfig                   — CellSize, CellSpacing
AnimationConfig               — SwapDuration, FallDuration, etc.
```

**Логика ячейки:**
- `Hidden`              → пустая, фишек нет
- `Normal` + `None`     → случайная фишка
- `Normal` + `Red` etc. → конкретная фишка

---

## 🏗️ Инфраструктура — Zenject

### ProjectContext
```
ProjectConfigInstaller
  → GemConfig, BoardConfig, AnimationConfig, LevelConfigRepository (BindInstance)

ProjectServiceInstaller
  → ISceneManagerService → SceneManagerService
  → Bootstrapper (IInitializable)
```

### SceneContext (Game)
```
SceneServiceInstaller    → 4 сервиса + GameLoopController
SceneViewInstaller       → Views + BoardInputHandler (FromComponentInHierarchy)
ScenePresenterInstaller  → Presenters (BindInterfacesAndSelfTo, NonLazy)
```
> ⚠️ Порядок биндинга: Service → View → Presenter

---

## 🏗️ Services — 4 сервиса (Scene scope)

| Сервис | Ответственность |
|--------|----------------|
| **BoardService** | Состояние сетки + поиск матчей + гравитация + спавн |
| **SwapService** | Выбор фишек (два клика) + валидация свопа + событие OnSwapRequested |
| **LayerService** | Состояние слоёв под фишками |
| **LevelService** | Старт уровня + цели (objective) + счётчик ходов + победа/поражение |

### BoardService — публичный API

```
Initialize(config)
GenerateInitialGems(allowedTypes)  — начальная расстановка без матчей
FindAndCreateMatches(seedCells)    — поиск матчей
HasMatchAfterSwap(a, b)            — проверка свопа
ComputeAndApplyFalls()             — гравитация
GetSpawnList()                     — список пустых ячеек для спавна
FindAllPossibleSwaps()             — подсказки
PlaceGem / RemoveGem / ExchangeGems / LockCell
```

### SwapService — логика выбора (два клика)

```
TrySelect(pos):
  1й клик  → запоминаем _firstCell, логируем
  2й клик  → если тот же → сброс
           → если не сосед → переназначаем _firstCell
           → если сосед → LockCell + OnSwapRequested.OnNext((first, pos))

OnSwapRequested : Observable<(from, to)>
Lock() / Unlock()                  — блокировка во время анимации
FindAllPossibleSwaps()             → делегирует BoardService
```

### LevelService — объединяет Level + Objective + MoveCounter

```
StartLevel(config)           — инициализирует BoardService, LayerService, цели, ходы
RegisterMatch(match)         — учёт целей
UseMove()                    — счётчик ходов
ProcessTurnResult()          — проверка победы/поражения

// Reactive
State : ReactiveProperty<LevelState>
Progress : ReactiveProperty<ObjectiveProgress[]>
MovesLeft / MovesUsed : ReactiveProperty<int>
OnLevelWon / OnLevelLost / OnMovesExhausted / OnObjectiveCompleted
```

---

## 🎮 Controllers

| Контроллер | Ответственность |
|------------|----------------|
| Bootstrapper | Точка входа, переключение сцены |
| GameLoopController | Оркестратор: input → swap → match → gravity → spawn → cascade |

---

## 🖼️ Views

| View | Ответственность |
|------|----------------|
| BoardView | Сетка, позиции ячеек, инстанцирование GemView |
| BoardInputHandler | IPointerClickHandler → OnCellClicked(Vector2Int) |
| GemView | Фишка + анимации DOTween |
| LayerView | Отображение слоёв под полем |
| ObjectiveView | UI целей уровня |
| MoveCounterView | UI счётчика ходов |
| LevelResultView | UI победы / поражения |

---

## 🔗 Presenters

| Presenter | Связывает |
|-----------|----------|
| BoardPresenter | BoardService ↔ BoardView (создание, анимации) |
| SwapPresenter | SwapService ↔ визуальный фидбек |
| LayerPresenter | LayerService ↔ LayerView |
| ObjectivePresenter | LevelService.Progress ↔ ObjectiveView |
| LevelPresenter | LevelService ↔ MoveCounterView + LevelResultView |

---

## 📁 Структура папок

```
Assets/Match3/
├── Configs/
│   ├── Levels/
│   ├── LevelConfigRepository.asset
│   ├── GemConfig.asset
│   ├── BoardConfig.asset
│   └── AnimationConfig.asset
├── Scripts/
│   ├── Core/
│   │   ├── Enums/
│   │   └── Models/
│   ├── Configs/
│   ├── Controllers/               — Bootstrapper, GameLoopController
│   ├── Services/
│   │   ├── SceneManagement/       — ISceneManagerService, SceneManagerService
│   │   ├── Board/                 — BoardService  ✅ активный
│   │   ├── Swap/                  — SwapService   ✅ активный
│   │   ├── Layer/                 — LayerService  ✅ активный
│   │   ├── Level/                 — LevelService  ✅ активный (+ ObjectiveProgress)
│   │   ├── Match/                 — MatchService  ❌ удалить
│   │   ├── Gravity/               — GravityService ❌ удалить
│   │   ├── Spawn/                 — SpawnService  ❌ удалить
│   │   ├── Objective/             — ObjectiveService ❌ удалить
│   │   ├── MoveCounter/           — MoveCounterService ❌ удалить
│   │   └── Factories/             — GemFactory ❌ удалить (не используется)
│   ├── Views/
│   ├── Presenters/
│   └── Installers/
│       ├── ProjectConfigInstaller.cs
│       ├── ProjectServiceInstaller.cs
│       ├── SceneServiceInstaller.cs
│       ├── SceneViewInstaller.cs
│       └── ScenePresenterInstaller.cs
├── Prefabs/
│   ├── Gems/
│   └── UI/
└── Scenes/
    ├── Bootstrap
    └── Game
```

---

## 🔄 Поток запуска

```
Bootstrap сцена
  → ProjectContext
    → Bootstrapper.Initialize()
      → SceneManagerService.LoadSceneAsync(Game)
        → Game сцена
          → SceneContext
            → GameLoopController.Initialize()
              → LevelService.StartLevel(config)
                  → BoardService.Initialize()
                  → LayerService.Initialize()
                  → InitializeObjectives()
                  → InitializeMoveCounter()
              → BoardPresenter.InitializeLayout()
              → BoardService.GenerateInitialGems()
              → BoardPresenter.CreateGems()
              → LayerPresenter.RenderLayers()
              → подписка: BoardInputHandler.OnCellClicked → SwapService.TrySelect
              → подписка: SwapService.OnSwapRequested → HandleSwapAsync
```

## 🔄 Поток игрового цикла (клик-выбор)

```
BoardInputHandler.OnPointerClick(pos)
  → OnCellClicked(pos)
    → GameLoopController.OnCellClicked(pos)
      → SwapService.TrySelect(pos)
          ├── 1й клик → _firstCell = pos  (лог: "Первая фишка выбрана")
          ├── 2й клик, не сосед → _firstCell = pos  (лог: "Переназначаем первую")
          ├── 2й клик, тот же → _firstCell = null  (лог: "Отменяем выбор")
          └── 2й клик, сосед → OnSwapRequested.OnNext((first, pos))
                → GameLoopController.HandleSwapAsync(from, to)
                    ├── нет матча → AnimateReturnSwap → разблокировать
                    └── есть матч → ResolveAsync()
                          ├── LevelService.RegisterMatch()
                          ├── LayerService.ProcessMatches()
                          ├── BoardPresenter.AnimateDestroyMatchAsync()
                          ├── BoardService.ComputeAndApplyFalls()
                          ├── BoardPresenter.AnimateFallsAsync()
                          ├── BoardService.GetSpawnList()
                          ├── BoardPresenter.AnimateSpawnAsync()
                          ├── [cascade] BoardService.FindAndCreateMatches() → повтор
                          └── LevelService.ProcessTurnResult() → Won / Lost
```
