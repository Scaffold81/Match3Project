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
public enum SceneId      { Bootstrap, StageMap, Game }
public enum RewardType   { Boost, Coins, Lives }
```

---

## 🗺️ Карта уровней (StageMap) ✅ РЕАЛИЗОВАНО

### Структура данных
```
WorldMapConfig
  └── CountryConfig[5]           — 5 стран (Egypt, Greece, China, Maya, India)
        └── StageConfig[10]      — 9 обычных + 1 бонусный этап на страну
              ├── IsBonusStage   — последний (индекс 9) — бонусный, даёт SuperPrize
              └── LevelConfig[3] — 3 уровня на этап

LevelAddress { CountryIndex, StageIndex, LevelIndex }
```

### Логика разблокировки этапов
```
StageIndex 0-8 (обычные):
  stageIdx == 0 && countryIdx == 0 → всегда открыт
  stageIdx > 0                     → открывается когда Stage[stageIdx-1] завершён
  stageIdx == 0 && countryIdx > 0  → открывается когда бонусный этап предыдущей страны завершён

StageIndex 9 (бонусный):
  Открывается когда ВСЕ 9 обычных этапов страны завершены
  Выдаёт SuperPrize через RewardService при победе
```

### Поток сцен
```
Bootstrap → StageMap → Game → StageMap (после победы/поражения)
```

### ProgressService (ProjectContext)
```
PlayerPrefs ключи: "progress_stars_{c}_{s}_{l}", "progress_current_{c/s/l}"

GetStars(address)                → int 0-3
SetStars(address, stars)         → сохраняет если лучше предыдущего
GetStageStars(c, s)              → сумма 0-9
GetCountryStars(c)               → сумма по 9 обычным этапам (без бонусного)
IsStageUnlocked(c, s)            → bool
IsStageCompleted(c, s)           → bool (все 3 уровня ≥ 1 звезда)
AreAllRegularStagesCompleted(c)  → bool (все 9 обычных, для разблокировки бонусного)
IsCountryCompleted(c)            → bool (бонусный этап пройден)
IsCountryUnlocked(c)             → bool
IsLevelUnlocked(c, s, l)         → bool
GetNextLevel(address)            → LevelAddress? (null = конец игры)
SetCurrentAddress(address)       → сохраняет в PlayerPrefs
CurrentAddress                   : ReadOnlyReactiveProperty<LevelAddress>
```

### StageMapScene — Views
```
StageMapView           — ScrollRect + зигзаг-карта
  RefreshStages(getStageStars, isStageUnlocked)
  RefreshCountries(getIcon, getName, getColor, isUnlocked)
  ScrollToNode(node)
  StageNodes   : List<StageNodeView>
  CountryNodes : List<CountryNodeView>
  _placeNodes  : bool → OnValidate() → PlaceNodesInEditor() (50 объектов в Content)
  _clearNodes  : bool → OnValidate() → ClearNodesInEditor()

StageNodeView          — кнопка этапа (90×80px)
  countryIndex, stageIndex — назначаются PlaceNodesInEditor
  IsBonus   : bool         — визуальный режим бонусного этапа (золотой цвет)
  IsUnlocked: bool
  Refresh(totalStars, isUnlocked, isBonus)
  OnClicked : Observable<StageNodeView>

CountryNodeView        — заголовок страны (300×72px)
  countryIndex
  Refresh(icon, countryName, sectionColor, isUnlocked)

LevelSelectPopupView   — попап выбора уровня (3 кнопки)
  Show(stageName, starsPerLevel[], isUnlocked[])
  Hide()
  OnLevelClicked : Observable<int>
  OnCloseClicked : Observable<Unit>
```

### StageMapPresenter (SceneContext)
```
Initialize():
  RefreshStages + RefreshCountries
  Подписка на StageNodeView.OnClicked → открыть попап
  Подписка на LevelSelectPopupView.OnLevelClicked → SetCurrentAddress + LoadScene(Game)
  Подписка на OnCloseClicked → Hide
  ScrollToCurrentProgress()
```

### Editor-инструменты
```
Match3/Generate World Map Configs  → WorldMapConfigGenerator.cs
Match3/Setup StageMap Scene        → StageMapUISetup.cs
Match3/Level Editor                → LevelEditorWindow.cs

StageMapView Inspector:
  _placeNodes = true → расставляет 50 объектов (5×CountryNode + 45×StageNode)
  _clearNodes = true → очищает Content
```

---

## 🎁 Система наград ✅ РЕАЛИЗОВАНО

### RewardData (Models)
```csharp
struct RewardData { RewardType Type; BoostType Boost; int Amount; }
```

### LevelConfig.Rewards[]
```
Указываются прямо в конфиге уровня.
Бонусный этап → ставь более ценные награды (редкие бусты, много монет).
```

### RewardService (ProjectContext)
```
GrantAll(RewardData[])    → выдаёт все награды из массива
OnRewardGranted           : Observable<RewardData>  ← Presenter слушает для анимации

Поддерживаемые типы:
  Boost → InventoryService.Add(boost, amount)
  Coins → заглушка (TODO: CoinService)
  Lives → заглушка (TODO: LivesService)
```

### Как вызывать
```csharp
// В LevelPresenter при победе:
_rewardService.GrantAll(_levelConfig.Rewards);
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

### InventoryService (ProjectContext)
```
PlayerPrefs: "inventory_boost_{BoostType}"
GetCount(boost) : ReadOnlyReactiveProperty<int>
HasAny(boost)   : bool
Add(boost, n)   : void
TrySpend(boost) : bool
AddDebugStarterPack() — ⚠️ временно
```

### BoostService (SceneContext)
```
SelectBoost / TryApplyBoostAt / CancelBoost
ActiveBoost     : ReadOnlyReactiveProperty<BoostType>
OnBoostApplied  : Observable<(BoostType, Vector2Int)>
OnHintApplied   : Observable<(from, to)>
OnShuffleApplied: Observable<Unit>
```

---

## 🏗️ Services

### ProjectContext
```
InventoryService   — бусты (PlayerPrefs)
ProgressService    — прогресс карты (PlayerPrefs)
RewardService      — выдача наград за уровни
ISceneManagerService → SceneManagerService
Bootstrapper       — стартует с SceneId.StageMap
```

### SceneContext (Game)
```
BoardService, SwapService, LayerService, LevelService
HintService, BoostService
GameLoopController (IInitializable)
```

---

## 📦 Configs

```
LevelConfig    — MoveLimit, AllowedNodeTypes[], Objectives[], Grid[], Rewards[]
StageConfig    — StageName, StageIcon, IsBonusStage, SuperPrize, Levels[3]
CountryConfig  — CountryName, CountryIcon, SectionColor, Stages[10]
WorldMapConfig — Countries[5]
```

---

## 📁 Структура папок (актуальная)

```
Assets/Match3/Scripts/
├── Core/
│   ├── Enums/   NodeType, SuperGemType, BoostType, CellType, LevelState, SceneId, RewardType
│   ├── Models/  BoardCell, CellData, ObjectiveData, LevelAddress, RewardData
│   └── GemMatch.cs, GemState.cs, IGemView.cs
├── Configs/
│   ├── GemConfig, BoardConfig, AnimationConfig
│   ├── LevelConfig          ← + Rewards[]
│   ├── LevelConfigRepository
│   ├── StageConfig          ← + IsBonusStage, SuperPrize
│   ├── WorldMapConfig, CountryConfig
├── Controllers/
│   ├── Bootstrapper
│   └── GameLoopController
├── Services/
│   ├── Board/, Swap/, Layer/, Level/
│   ├── HintService, BoostService
│   ├── InventoryService   ← ProjectContext
│   ├── ProgressService    ← ProjectContext
│   ├── RewardService      ← ProjectContext ✅ NEW
│   └── StarCalculator
├── Views/
│   ├── StageMapView, StageNodeView, CountryNodeView, LevelSelectPopupView
│   └── BoardView, GemView, LayerView, ObjectiveView, MoveCounterView...
├── Presenters/
│   ├── StageMapPresenter
│   └── BoardPresenter, LevelPresenter, BoostPresenter...
├── Editor/
│   ├── WorldMapConfigGenerator, StageMapUISetup, LevelEditorWindow
│   └── CellDataDrawer, UISetupEditor, StageMapUISetupEditor
└── Installers/
    ├── ProjectConfigInstaller
    ├── ProjectServiceInstaller  ← + RewardService ✅
    ├── StageMapInstaller, StageMapViewInstaller
    ├── SceneServiceInstaller, SceneViewInstaller, ScenePresenterInstaller
```

---

## 🔮 Не реализовано (следующие этапы)
- Препятствия: Ice, Box, Chain, HedgehogFish
- Вызов RewardService.GrantAll в LevelPresenter при победе
- UI попап наград (анимация вылета иконок)
- CoinService + LivesService (заглушки в RewardService)
- Комбо-свопы двух супер-фишек
- Визуальные эффекты взрывов (частицы)
