# Match-3 — Архитектурный план

## Статус: 🟢 В разработке

---

## 🎮 Механики

| Механика | Поведение |
|----------|-----------|
| **Swap** | Игрок меняет две соседние фишки. Нет матча → анимация возврата |
| **Матч** | 3+ фишек одного типа по горизонтали или вертикали |
| **Гравитация** | Фишки падают вниз после исчезновения |
| **Спаун** | Новые фишки спавнятся над первой Normal ячейкой столбца сверху |
| **Сбор фишек** | Любой матч с нужным типом → +1 к цели |
| **Слой** | Матч над ячейкой с слоем → слой снимается. Вся зона очищена → засчитана |

---

## 🗂️ Enums

```csharp
public enum NodeType { None, Red, Blue, Green, Yellow, Purple, Orange }
public enum CellType { Normal, Hidden }
public enum SceneId  { Bootstrap, Game }
```

---

## 📦 Data Models

```
CellData       : cellType, nodeType, hasLayer
ObjectiveData  : nodeType, count
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
SceneServiceInstaller    → сервисы + GameLoopController + GemFactory
SceneViewInstaller       → Views + InputController (FromComponentInHierarchy)
ScenePresenterInstaller  → Presenters (BindInterfacesAndSelfTo, NonLazy)
```
> ⚠️ Порядок: Service → View → Presenter

---

## 🏗️ Services

| Сервис | Scope | Ответственность |
|--------|-------|----------------|
| SceneManagerService | Project | Загрузка сцен |
| GemFactory | Scene | Инстанцирование GemView через Zenject |
| BoardService | Scene | Состояние сетки |
| MatchService | Scene | Поиск матчей |
| SwapService | Scene | Логика свопа |
| GravityService | Scene | Падение фишек |
| SpawnService | Scene | Спаун фишек |
| LayerService | Scene | Состояние слоёв |
| ObjectiveService | Scene | Прогресс целей |
| MoveCounterService | Scene | Счётчик ходов |
| LevelService | Scene | Старт/конец уровня |

---

## 🎮 Controllers

| Контроллер | Scope | Ответственность |
|------------|-------|----------------|
| Bootstrapper | Project | Точка входа, переключение сцены |
| GameLoopController | Scene | Оркестратор. Берёт `LevelRepository.First` для теста |
| InputController | Scene | Тапы → SwapPresenter |

---

## 🖼️ Views

| View | Ответственность |
|------|----------------|
| BoardView | Сетка, позиции, словарь GemView |
| GemView | Фишка + анимации DOTween (UI/Image) |
| LayerView | Слои под полем |
| ObjectiveView | UI целей |
| MoveCounterView | UI счётчика ходов |
| LevelResultView | UI победы/поражения |

---

## 🔗 Presenters

| Presenter | Связывает |
|-----------|----------|
| BoardPresenter | GemFactory + BoardService ↔ BoardView |
| SwapPresenter | SwapService ↔ GemView (анимации) |
| LayerPresenter | LayerService ↔ LayerView |
| ObjectivePresenter | ObjectiveService ↔ ObjectiveView |
| LevelPresenter | LevelService + MoveCounterService ↔ UI |

---

## 📁 Структура папок

```
Assets/Match3/
├── Configs/
│   ├── Levels/                    — LevelConfig assets
│   ├── LevelConfigRepository.asset
│   ├── GemConfig.asset
│   ├── BoardConfig.asset
│   └── AnimationConfig.asset
├── Scripts/
│   ├── Core/Enums/
│   ├── Core/Models/
│   ├── Configs/                   — LevelConfig, LevelConfigRepository, GemConfig, ...
│   ├── Controllers/               — Bootstrapper, GameLoopController, InputController
│   ├── Services/
│   │   ├── Factories/             — GemFactory
│   │   ├── SceneManagement/
│   │   ├── Board/ Match/ Swap/
│   │   ├── Gravity/ Spawn/ Layer/
│   │   ├── Objective/ MoveCounter/ Level/
│   ├── Views/
│   ├── Presenters/
│   └── Installers/
├── Prefabs/
│   ├── Gems/                      — GemBase.prefab
│   └── UI/                        — LayerCell.prefab
└── Scenes/Bootstrap, Game
```

---

## 🔄 Поток запуска

```
Bootstrap → ProjectContext → Bootstrapper.Initialize()
  → LoadSceneAsync(Game)
    → SceneContext → Installers
      → GameLoopController.Initialize()
        → LevelRepository.First → LevelService.StartLevel(config)
          → RenderBoard / RenderLayers / RenderObjectives
```

## 🔄 Поток игрового цикла

```
InputController → SwapPresenter.OnCellTapped()
  → SwapService.TrySwap()
    ├── нет матча → OnSwapFailed → анимация возврата
    └── есть матч → OnSwapSuccess
          → GameLoopController
            ├── AnimateDestroy → BoardService.RemoveNodes()
            ├── GravityService.ApplyGravity() → AnimateFalls
            ├── SpawnService.SpawnMissing() → AnimateSpawn
            ├── cascade loop (while matches)
            ├── MoveCounterService.UseMove()
            └── LevelService.ProcessTurnResult()
```

---

## 📋 Прогресс разработки

### Этапы 1-6 ✅ — весь код написан

### Настройка в Unity ✅
- [x] Исправлена компиляция GemFactory (режим без префабов работает автоматически)
- [x] BoardView создаёт ячейки через код (без префабов, опционально можно назначить)
- [x] GameLoopController создаёт тестовый уровень автоматически, если `LevelConfigRepository` пустой
- [ ] Создать `LevelConfigRepository.asset` → `Assets/Match3/Configs/` (для сохранения разработки)
- [ ] Добавить уровни в `LevelConfigRepository.Levels[]`
- [ ] Назначить `LevelConfigRepository` в `ProjectConfigInstaller`
- [ ] **Вариант А (рекомендуемый):** Создать `GemBase.prefab` → `Assets/Match3/Prefabs/Gems/` и назначить в GemConfig
  **или Вариант Б:** Оставить Prefab null — GemFactory создаст ячейку через код автоматически
- [ ] Заполнить `GemConfig` (Sprite + Color + (опционально) Prefab на каждый NodeType)
- [ ] Настроить UI Scene
