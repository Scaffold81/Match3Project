# Match-3 — Архитектурный план

> ⚠️ Читай этот файл перед любой задачей. Держи актуальным.

---

## 🗂️ Enums

```csharp
public enum NodeType     { None, Red, Blue, Green, Yellow, Purple, Orange }
public enum SuperGemType { None, HorizontalArrow, VerticalArrow, ColorBomb, Bomb, MegaBomb }
public enum BoostType    { None, HorizontalArrow, VerticalArrow, ColorBomb, Bomb, MegaBomb, Hint, Shuffle }
public enum CellType     { Normal, Hidden }
public enum LevelState   { Idle, Playing, Won, Lost }
```

---

## 💎 Супер-фишки

| Тип | Триггер матча | Эффект |
|-----|--------------|--------|
| `HorizontalArrow` | 4 горизонталь | Вся строка |
| `VerticalArrow` | 4 вертикаль | Весь столбец |
| `ColorBomb` | 5 прямая | Все фишки цвета |
| `Bomb` | T/L форма (5 кл.) | 3×3 |
| `MegaBomb` | 6+ | 5×5 |

---

## 🎒 Инвентарь и бусты

### InventoryService (ProjectContext — живёт между сессиями)
```
PlayerPrefs хранилище: "inventory_boost_{BoostType}"
GetCount(boost) : ReadOnlyReactiveProperty<int>
HasAny(boost)   : bool
Add(boost, n)   : void
TrySpend(boost) : bool
AddDebugStarterPack() — ⚠️ временно, +1000 каждого буста при старте уровня
```

### BoostService (SceneContext)
```
SelectBoost(boost):
  Hint/Shuffle → применяются сразу (TrySpend → событие)
  SuperGem     → ActiveBoost = boost, ждём клик на поле

TryApplyBoostAt(pos) → TrySpend + OnBoostApplied
CancelBoost()        → ActiveBoost = None

ActiveBoost        : ReadOnlyReactiveProperty<BoostType>
OnBoostSelected    : Observable<BoostType>
OnBoostCancelled   : Observable<BoostType>
OnBoostApplied     : Observable<(BoostType, Vector2Int)>
OnHintApplied      : Observable<(from, to)>
OnShuffleApplied   : Observable<Unit>
```

### HintService (SceneContext)
```
GetPossibleSwaps()  → List<(from, to)>
TryRequestHint()    → случайный своп → OnHintRequested
Shuffle()           → Fisher-Yates без матчей, гарантия хода → OnShuffleRequested
```

---

## 🖼️ UI — Бусты

```
BackpackView   — нижняя панель, кнопки бустов (BoostButtonEntry[])
               - OnBoostClicked : Observable<BoostType>
               - UpdateCount(boost, count)
               - GetIconWorldPosition(boost) → для анимации вылета

ActiveBoostView — шапка, активный буст
               - ShowBoost(icon, fromWorldPos) → анимация вылета DOTween
               - HideBoost()
               - OnCancelClicked : Observable<Unit>
```

### BoostPresenter
```
BackpackView.OnBoostClicked  → BoostService.SelectBoost
BoostService.OnBoostSelected → ActiveBoostView.ShowBoost (иконка вылетает из рюкзака в шапку)
BoostService.OnBoostCancelled/Applied → ActiveBoostView.HideBoost
ActiveBoostView.OnCancelClicked → BoostService.CancelBoost
InventoryService.GetCount × each boost → BackpackView.UpdateCount
ActiveBoost != None → BackpackView.SetAllInteractable(false)
```

---

## 🏗️ Services

### ProjectContext
```
InventoryService  — бусты между сессиями (PlayerPrefs)
ISceneManagerService → SceneManagerService
Bootstrapper
```

### SceneContext
```
BoardService    — сетка + матчи + гравитация + спавн
SwapService     — выбор двумя кликами + OnSwapRequested
LayerService    — слои
LevelService    — старт + цели + ходы + победа/поражение
HintService     — подсказка + shuffle алгоритм
BoostService    — активный буст + применение
GameLoopController (IInitializable)
```

---

## 🎮 GameLoopController — поток с бустами

```
OnCellClicked(pos):
  BoostService.HasActiveBoost?
    → YES: TryApplyBoostAt(pos) → ApplyBoostAtAsync
    → NO:  ClearHint + SwapService.TrySelect

ApplyBoostAtAsync(boost, pos):
  GetExplosionCells → AnimateDestroyCellsAsync
  ComputeAndApplyFalls + AnimateFallsAsync
  GetSpawnList + AnimateSpawnAsync
  FindAndCreateMatches → ResolveAsync если есть

ShuffleBoardAsync:
  HintService.Shuffle() → BoardPresenter.AnimateShuffleAsync
  (Fold все фишки → SetGemType → Spawn с новым визуалом)

ShowHint(from, to):
  GemView.PlayHint() на обеих фишках (бесконечный пульс)
  ClearHint() при следующем клике
```

---

## 📦 Configs

```
GemConfig      — Gems[], SuperGemIcons[], GemViewPrefab
BoardConfig    — BoardPadding, CellSpacing, GemPadding (всё в px)
AnimationConfig — SwapDuration, SwapReturnDuration, FallDuration,
                  MatchDestroyDuration, SelectDuration, SelectScale,
                  ShuffleFoldDuration
LevelConfig    — MoveLimit, AllowedNodeTypes[], Objectives[], Grid[]
LevelConfigRepository
```

---

## 📁 Структура папок (актуальная)

```
Assets/Match3/Scripts/
├── Core/
│   ├── Enums/   NodeType, SuperGemType, BoostType, CellType, LevelState
│   ├── Models/  BoardCell, CellData, ObjectiveData
│   ├── GemMatch.cs, GemState.cs, IGemView.cs
├── Configs/     GemConfig, BoardConfig, AnimationConfig, LevelConfig*
├── Controllers/ Bootstrapper, GameLoopController
├── Services/
│   ├── Board/         BoardService
│   ├── Swap/          SwapService
│   ├── Layer/         LayerService
│   ├── Level/         LevelService
│   ├── HintService.cs
│   ├── BoostService.cs
│   ├── InventoryService.cs   ← ProjectContext
│   └── [Старые — удалить]: Match, Gravity, Spawn, Objective, MoveCounter, Factories
├── Views/
│   ├── BoardView, BoardInputHandler, GemView
│   ├── LayerView, ObjectiveView, MoveCounterView, LevelResultView
│   ├── BackpackView    ← рюкзак с кнопками бустов
│   └── ActiveBoostView ← шапка с активным бустом
├── Presenters/
│   ├── BoardPresenter, SwapPresenter, LayerPresenter
│   ├── ObjectivePresenter, LevelPresenter
│   └── BoostPresenter
└── Installers/
    ├── ProjectConfigInstaller
    ├── ProjectServiceInstaller  ← InventoryService
    ├── SceneServiceInstaller    ← 6 сервисов + GameLoopController
    ├── SceneViewInstaller       ← 8 View
    └── ScenePresenterInstaller  ← 6 Presenter
```

---

## 🔮 Второй этап (не реализован)
- Комбо-свопы двух супер-фишек
- Магазин бустов
- Реклама (AdMob/IronSource) вместо заглушки
- Визуальные эффекты взрывов (частицы)
