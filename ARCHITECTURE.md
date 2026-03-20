# Match-3 — Архитектурный план

## Статус: 🟢 Этапы 1-6 завершены

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

## 📦 LevelConfig (ScriptableObject)

```
LevelConfig
├── MoveLimit          : int             — 0 = без ограничений
├── AllowedNodeTypes[] : NodeType[]      — пусто = все типы
├── Objectives[]       : ObjectiveData[]
└── Grid[]             : CellRow[]
    └── CellRow.Cells[]: CellData[]
```

**Логика ячейки:**
- `Hidden`              → пустая, фишек нет
- `Normal` + `None`     → случайная фишка
- `Normal` + `Red` etc. → конкретная фишка

**Логика спауна:**
```
[Hidden][Hidden][Normal][Normal][Normal]
                 ↑ спаун над первой Normal
```

---

## 🏗️ Инфраструктура — Zenject

### ProjectContext
```
ProjectConfigInstaller   → GemConfig, BoardConfig, AnimationConfig (BindInstance)
ProjectServiceInstaller  → ISceneManagerService, Bootstrapper (IInitializable)
```

### SceneContext (Game)
```
SceneServiceInstaller    → сервисы + GameLoopController (IInitializable)
SceneViewInstaller       → Views + InputController (FromComponentInHierarchy)
ScenePresenterInstaller  → LevelConfig (BindInstance) + Presenters
```
> ⚠️ Порядок: Service → View → Presenter

---

## 🏗️ Services

| Сервис | Scope | Ответственность |
|--------|-------|----------------|
| SceneManagerService | Project | Загрузка сцен |
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
| GameLoopController | Scene | Оркестратор игрового цикла (IInitializable) |
| InputController | Scene | Тапы → SwapPresenter |

---

## 🖼️ Views

| View | Ответственность |
|------|----------------|
| BoardView | Сетка, позиции, словарь GemView |
| GemView | Фишка + анимации DOTween |
| LayerView | Слои под полем |
| ObjectiveView | UI целей |
| MoveCounterView | UI счётчика ходов |
| LevelResultView | UI победы/поражения |

---

## 🔗 Presenters

| Presenter | Связывает |
|-----------|----------|
| BoardPresenter | BoardService ↔ BoardView |
| SwapPresenter | SwapService ↔ GemView (анимации) |
| LayerPresenter | LayerService ↔ LayerView |
| ObjectivePresenter | ObjectiveService ↔ ObjectiveView |
| LevelPresenter | LevelService + MoveCounterService ↔ UI |

---

## ⚙️ Configs

| Конфиг | Содержимое |
|--------|-----------|
| LevelConfig | Поле, цели, лимит, типы |
| GemConfig | Sprite + Color per NodeType |
| BoardConfig | CellSize, CellSpacing |
| AnimationConfig | SwapDuration, SwapReturnDuration, FallDuration, MatchDestroyDuration |

---

## 📁 Структура папок

```
Assets/Match3/
├── Configs/
│   ├── Levels/              — LevelConfig assets
│   ├── GemConfig.asset
│   ├── BoardConfig.asset
│   └── AnimationConfig.asset
├── Scripts/
│   ├── Core/Enums/          — NodeType, CellType, SceneId
│   ├── Core/Models/         — CellData, ObjectiveData
│   ├── Configs/             — SO классы
│   ├── Controllers/         — Bootstrapper, GameLoopController, InputController
│   ├── Services/
│   │   ├── SceneManagement/
│   │   ├── Board/
│   │   ├── Match/
│   │   ├── Swap/
│   │   ├── Gravity/
│   │   ├── Spawn/
│   │   ├── Layer/
│   │   ├── Objective/
│   │   ├── MoveCounter/
│   │   └── Level/
│   ├── Views/
│   ├── Presenters/
│   └── Installers/
├── Prefabs/Gem/
└── Scenes/Bootstrap, Game
```

---

## 🔄 Поток запуска

```
Bootstrap → ProjectContext → Bootstrapper.Initialize()
  → LoadSceneAsync(Game)
    → SceneContext → Installers
      → GameLoopController.Initialize()
        → LevelService.StartLevel(config)
          → RenderBoard / RenderLayers / RenderObjectives
```

## 🔄 Поток игрового цикла

```
InputController.Update()
  → SwapPresenter.OnCellTapped(cell)
    → SwapService.TrySwap(from, to)
      ├── нет матча → OnSwapFailed → анимация возврата
      └── есть матч → OnSwapSuccess
            → GameLoopController.OnSwapSucceeded()
              ├── await AnimateDestroy (DOTween)
              ├── BoardService.RemoveNodes()
              ├── GravityService.ApplyGravity()
              ├── await AnimateFalls (DOTween)
              ├── SpawnService.SpawnMissing()
              ├── await AnimateSpawn (DOTween)
              ├── MatchService — каскадные матчи (цикл while)
              ├── MoveCounterService.UseMove()
              └── LevelService.ProcessTurnResult()
```

---

## 📋 Прогресс разработки

### Этап 1 — Core Data & Enums ✅
- [x] NodeType, CellType, SceneId
- [x] CellData, ObjectiveData

### Этап 2 — Configs ✅
- [x] LevelConfig, GemConfig, BoardConfig, AnimationConfig

### Этап 3 — Infrastructure ✅
- [x] ISceneManagerService + SceneManagerService
- [x] Bootstrapper
- [x] ProjectConfigInstaller, ProjectServiceInstaller
- [x] SceneServiceInstaller, SceneViewInstaller, ScenePresenterInstaller

### Этап 4 — Services ✅
- [x] BoardService, MatchService, SwapService
- [x] GravityService, SpawnService, LayerService
- [x] ObjectiveService, MoveCounterService, LevelService

### Этап 5 — Views & Presenters ✅
- [x] BoardView + BoardPresenter
- [x] GemView + SwapPresenter
- [x] LayerView + LayerPresenter
- [x] ObjectiveView + ObjectivePresenter
- [x] LevelResultView + MoveCounterView + LevelPresenter

### Этап 6 — GameLoop & Полировка ✅
- [x] GameLoopController (каскады, анимации, оркестрация)
- [x] InputController

### Следующие шаги — настройка в Unity
- [ ] Создать сцены Bootstrap и Game
- [ ] Настроить ProjectContext (ProjectConfigInstaller + ProjectServiceInstaller)
- [ ] Настроить SceneContext (3 инсталлера, порядок важен)
- [ ] Создать SO-ассеты (GemConfig, BoardConfig, AnimationConfig)
- [ ] Создать первый LevelConfig
- [ ] Настроить GameObject иерархию в Game сцене
- [ ] Создать префаб GemView
