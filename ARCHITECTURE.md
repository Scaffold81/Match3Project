# Match-3 — Архитектурный план

> ⚠️ Читай этот файл перед любой задачей. Держи актуальным.

---

## 🗂️ Enums

```csharp
public enum NodeType     { None, Red, Blue, Green, Yellow, Purple, Orange }
public enum SuperGemType { None, HorizontalArrow, VerticalArrow, ColorBomb, Bomb, MegaBomb }
public enum CellType     { Normal, Hidden }
public enum SceneId      { Bootstrap, Game }
public enum LevelState   { Idle, Playing, Won, Lost }
```

---

## 💎 Супер-фишки

| Тип | Триггер | Эффект |
|-----|---------|--------|
| `HorizontalArrow` | 4 в ряд горизонталь | Сносит всю строку |
| `VerticalArrow` | 4 в ряд вертикаль | Сносит весь столбец |
| `ColorBomb` | 5 подряд (прямая линия) | Сносит все фишки своего цвета |
| `Bomb` | T или L-форма (5 клеток) | Взрыв 3×3 |
| `MegaBomb` | 6+ клеток в матче | Взрыв 5×5 |

**Правила спавна:**
- Спавнится в `OriginPoint` матча (позиция свопнутого гема, или центроид)
- Хранит `NodeType` (цвет) + `SuperGemType` (способность)
- Визуал: цветная фишка + иконка-оверлей из `GemConfig.SuperGemIcons`

**Активация:** супер-фишка попадает в матч → `CollectExplosionCells` добавляет её область
к уничтожению. Комбо-свопы — второй этап.

---

## 📦 Data Models

```
CellData          : cellType, nodeType, hasLayer
ObjectiveData     : nodeType, count
ObjectiveProgress : NodeType, Required, Collected, IsCompleted  ← в LevelService

GemMatch:
  MatchingCells[]    : List<Vector2Int>
  MatchedGems[]      : List<IGemView>
  OriginPoint        : Vector2Int
  MatchNodeType      : NodeType
  SuperGemToSpawn    : SuperGemType   ← вычисляется ComputeSuperGem()
  SuperGemSpawnPos   : Vector2Int
  HasSuperGemSpawn   : bool
```

---

## 📦 Configs (ScriptableObjects)

```
LevelConfigRepository         ← ProjectConfigInstaller
LevelConfig
├── MoveLimit, AllowedNodeTypes[], Objectives[], Grid[]

GemConfig
├── GemViewPrefab             — базовый префаб
├── Gems[]: GemVisualData     — NodeType + Sprite + Color
└── SuperGemIcons[]: SuperGemIconData — SuperGemType + Icon + Tint

BoardConfig                   — CellSize, CellSpacing, Padding
AnimationConfig               — SwapDuration, FallDuration, MatchDestroyDuration...
```

---

## 🏗️ Инфраструктура — Zenject

### ProjectContext
```
ProjectConfigInstaller  → GemConfig, BoardConfig, AnimationConfig, LevelConfigRepository
ProjectServiceInstaller → ISceneManagerService, Bootstrapper
```

### SceneContext (Game)
```
SceneServiceInstaller   → 4 сервиса + GameLoopController
SceneViewInstaller      → Views + BoardInputHandler
ScenePresenterInstaller → Presenters (NonLazy)
```

---

## 🏗️ Services — 4 сервиса (Scene scope)

| Сервис | Ответственность |
|--------|----------------|
| **BoardService** | Состояние сетки + матчи + гравитация + спавн |
| **SwapService** | Выбор фишек (два клика) + OnSwapRequested |
| **LayerService** | Слои под фишками |
| **LevelService** | Старт + цели + ходы + победа/поражение |

### BoardService API
```
Initialize / GenerateInitialGems / FindAndCreateMatches
HasMatchAfterSwap / ComputeAndApplyFalls / GetSpawnList
PlaceGem / RemoveGem / ExchangeGems / LockCell
IsNormalCell / IsNormalCell / AreNeighbors / GetGem
Rows / Columns
```

### SwapService
```
TrySelect(pos) → 1й клик: _firstCell / 2й клик: OnSwapRequested или переназначение
Lock() / Unlock()
```

### LevelService
```
StartLevel(config) → BoardService.Initialize + LayerService.Initialize + цели + ходы
RegisterMatch(match) / UseMove() / ProcessTurnResult()
State / Progress / MovesLeft / OnLevelWon / OnLevelLost
```

---

## 🎮 GameLoopController — поток одного хода

```
OnCellClicked(pos)
  → SwapService.TrySelect(pos)
      └── OnSwapRequested → HandleSwapAsync(from, to)
            ├── ExchangeGems + AnimateSwapAsync
            ├── FindAndCreateMatches([from, to])
            ├── нет матча → ExchangeGems back + AnimateReturnSwapAsync
            └── есть матч → ResolveAsync(matches)

ResolveAsync(matches):
  while matches.Count > 0:
    1. match.ComputeSuperGem()          — определяем форму
    2. RegisterMatch / ProcessMatches   — сервисы
    3. CollectExplosionCells(matches)   — взрывы супер-фишек
    4. AnimateDestroyMatchAsync × N     — уничтожение
       + AnimateDestroyCellsAsync       — взрывы параллельно
    5. SpawnSuperGems(matches)          — спавн супер-фишек
    6. ComputeAndApplyFalls             — гравитация
    7. AnimateFallsAsync
    8. GetSpawnList + AnimateSpawnAsync — новые фишки
    9. FindAndCreateMatches(allCells)   — каскад → повтор
```

---

## 🖼️ Views

| View | Ответственность |
|------|----------------|
| BoardView | Сетка, слоты, InstantiateGem, ReparentToOverlay/Container, DragLayer |
| BoardInputHandler | IPointerClickHandler → OnCellClicked(Vector2Int) |
| GemView | Image (цвет) + Image _superIcon (иконка типа) + анимации DOTween |
| LayerView / ObjectiveView / MoveCounterView / LevelResultView | UI |

**GemView.SetSuperIcon(iconData)** — устанавливает оверлей иконки на фишку.

---

## 🔗 Presenters

| Presenter | Связывает |
|-----------|----------|
| BoardPresenter | BoardService ↔ BoardView. CreateGemAt / CreateSuperGemAt / Animate* |
| SwapPresenter | SwapService ↔ визуальный фидбек |
| LayerPresenter | LayerService ↔ LayerView |
| ObjectivePresenter | LevelService.Progress ↔ ObjectiveView |
| LevelPresenter | LevelService ↔ MoveCounterView + LevelResultView |

---

## 📁 Структура папок

```
Assets/Match3/Scripts/
├── Core/
│   ├── Enums/        NodeType, SuperGemType, CellType, SceneId, LevelState
│   ├── Models/       BoardCell, CellData, ObjectiveData
│   ├── GemMatch.cs   + ComputeSuperGem()
│   ├── GemState.cs
│   └── IGemView.cs   + SuperGemType, SetSuperGemType()
├── Configs/
│   GemConfig (+ SuperGemIcons[]), BoardConfig, AnimationConfig, LevelConfig*
├── Controllers/
│   Bootstrapper, GameLoopController (+ взрывы + спавн супер-фишек)
├── Services/
│   Board / Swap / Layer / Level   ✅ 4 активных
│   Match / Gravity / Spawn / Objective / MoveCounter / Factories ❌ удалить
├── Views/
│   GemView (+ _superIcon Image, SetSuperIcon, PlaySuperSpawn)
│   BoardView (+ _dragLayer, ReparentToOverlay/Container)
│   BoardInputHandler, LayerView, ObjectiveView, MoveCounterView, LevelResultView
├── Presenters/
│   BoardPresenter (+ CreateSuperGemAt, AnimateDestroyCellsAsync)
│   SwapPresenter, LayerPresenter, ObjectivePresenter, LevelPresenter
└── Installers/
    Project*Installer, Scene*Installer
```

---

## 🔮 Второй этап (не реализован)
- Комбо-свопы двух супер-фишек
- Визуальные эффекты взрывов (частицы)
- Подсветка выбранной фишки (SwapPresenter)
