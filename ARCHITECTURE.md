# Match-3 — Архитектурный план

> ⚠️ Читай этот файл перед любой задачей. Держи актуальным.

---

## 🗂️ Enums

```csharp
public enum NodeType     { None, Red, Blue, Green, Yellow, Purple, Orange }
public enum SuperGemType { None, HorizontalArrow, VerticalArrow, ColorBomb, Bomb, MegaBomb }
public enum BoostType    { None, HorizontalArrow, VerticalArrow, ColorBomb, Bomb, MegaBomb, Hint, Shuffle }
public enum CellType     { Normal, Hidden }
public enum LevelState   { Idle, Playing, Won, Lost }  // определён внутри LevelService.cs
public enum SceneId      { Bootstrap, StageMap, Game }
public enum RewardType   { Boost, Coins, Lives }
```

### Утилиты
```csharp
// Core/BoostTypeExtensions.cs
static class BoostTypeExtensions
{
    public static SuperGemType ToSuperGemType(this BoostType boost)
}
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

## 🎮 Игровой цикл (Game Scene) ✅ РЕАЛИЗОВАНО

### Поток внутри Game-сцены
```
GameLoopController.Initialize()
  → Подготовка доски (без включения ввода)

GameFlowService.Initialize()
  → Показать LevelTaskPopupView (задание уровня)
  → Игрок нажимает Play
  → GameLoopController.EnableInput() → игра началась

Победа (LevelService.OnLevelWon):
  → GameFlowService.HandleWin()
  → SaveProgress()
  → Последний уровень этапа?
      ДА  → HandleStageComplete → StageRewardPopupView.Show() → Claim → LoadScene(StageMap)
      НЕТ → HandleNextLevel → SetCurrentAddress(next) → LoadScene(Game)

Поражение (LevelService.OnLevelLost):
  → GameFlowService.HandleLose()
  → LevelResultView.ShowLose()
  → Restart → LoadScene(Game)
  → Back to Map → LoadScene(StageMap)
```

### GameFlowService (SceneContext, IInitializable, IDisposable)
```
Оркестрирует весь игровой цикл внутри Game-сцены.

Initialize():
  Подписки на: OnLevelWon, OnLevelLost, OnRestartClicked,
               OnBackToMapClicked, OnClaimClicked, OnPlayClicked
  ShowCurrentLevelTask()

HandleWin():
  1. Проверяет wasStageCompleted ДО SaveProgress
  2. SaveProgress() → StarCalculator.Calculate(movesLeft, moveLimit)
  3. Последний уровень? → HandleStageComplete | HandleNextLevel

HandleStageComplete(stage, grantRewards):
  if grantRewards → RewardService.GrantAll(stage.StageRewards)
  StageRewardPopupView.Show()

HandleNextLevel(address):
  ProgressService.SetCurrentAddress(next) → LoadScene(Game)
```

### LevelTaskPopupView (View, MonoBehaviour)
```
Show(stageName, levelIndex, objectives[])  — показывает задание уровня
Hide()
OnPlayClicked : Observable<Unit>

Форматирует ObjectiveData[] в читаемый текст (визуализация — ответственность View).
```

### StageRewardPopupView (View, MonoBehaviour)
```
Show(stageName, rewards[])  — показывает награды за этап
Hide()
OnClaimClicked : Observable<Unit>
```

### LevelResultView (View, MonoBehaviour) — только поражение
```
ShowLose()   — показывает панель поражения
Hide()
OnRestartClicked   : Observable<Unit>
OnBackToMapClicked : Observable<Unit>
```

---

## 🎁 Система наград ✅ РЕАЛИЗОВАНО

### RewardData (Models)
```csharp
struct RewardData { RewardType Type; BoostType Boost; int Amount; }
```

### LevelConfig.Rewards[] и StageConfig.StageRewards[]
```
LevelConfig.Rewards[]      — награды за отдельный уровень (TODO: пока не выдаются)
StageConfig.StageRewards[] — выдаются через RewardService при первом завершении этапа
Бонусный этап → ставь более ценные награды (редкие бусты, много монет).
```

### RewardService (ProjectContext, IDisposable) ✅ ОБНОВЛЕНО
```
GrantAll(RewardData[])    → выдаёт все награды из массива
OnRewardGranted           : Observable<RewardData>  ← Presenter слушает для анимации

Поддерживаемые типы:
  Boost → InventoryService.Add(boost, amount)
  Coins → CoinService.Add(amount)
  Lives → LivesService.AddLives(amount)
```

---

## 💰 Кошелёк (Wallet) ✅ РЕАЛИЗОВАНО

### EconomyConfig (ProjectContext, ScriptableObject)
```
Путь: Match3/Configs/Economy
Значения (все редактируются в инспекторе):

  MaxLives            = 5       — максимум жизней
  LifeRegenSeconds    = 1800    — 30 мин на одну жизнь
  LivesPurchasePrice  = 300     — монет за покупку жизней
  LivesPurchaseAmount = 5       — кол-во жизней при покупке
  InitialCoins        = 500     — монеты при первом запуске
```

### CoinService (ProjectContext, IDisposable)
```
PlayerPrefs: "wallet_coins"

Coins : ReadOnlyReactiveProperty<int>
Add(amount)           — добавляет монеты (throws если amount <= 0)
TrySpend(amount)      — тратит монеты; false если недостаточно
```

### LivesService (ProjectContext, IDisposable)
```
PlayerPrefs:
  "wallet_lives"           → int    — текущее кол-во жизней
  "wallet_lives_timestamp" → string — Unix-секунды прихода следующей жизни; "0" = полные

Lives             : ReadOnlyReactiveProperty<int>
TimeUntilNextLife : ReadOnlyReactiveProperty<TimeSpan>   — Zero когда жизни полные
MaxLives          : int

TrySpendLife()     → bool   — тратит жизнь; false если 0
AddLives(amount)            — добавляет жизни; молча игнорирует если уже MaxLives

Таймер: UniTask-цикл (тик каждую секунду).
  При потере с максимума → nextLifeAt = now + RegenSeconds.
  При достижении максимума → nextLifeAt сбрасывается в 0.
  Офлайн-восстановление → на старте Tick() досчитывает пропущенное время.
```

### WalletView (MonoBehaviour, DontDestroyOnLoad)
```
Спавнится из ProjectContext через FromComponentInNewPrefab.
Canvas: Screen Space – Overlay, Sort Order = 10.

SetCoins(int)
SetLives(int current, int max)
ShowTimer(TimeSpan)
HideTimer()
```

### WalletView (MonoBehaviour, SceneContext)
```
Живёт в Canvas каждой сцены (StageMap, Game). Presenter не нужен.
Подписывается напрямую на сервисы в Construct().

Construct(CoinService, LivesService):
  Coins             → _coinsText
  Lives             → _livesText ("current/max")
  TimeUntilNextLife → _timerContainer + _timerText (Zero = скрыть)

OnDestroy() → _disposables.Dispose()
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
AddDebugStarterPack() — ⚠️ временно, удалить после реализации реальных наград
```

### BoostService (SceneContext)
```
SelectBoost / TryApplyBoostAt / CancelBoost
ActiveBoost      : ReadOnlyReactiveProperty<BoostType>
OnBoostSelected  : Observable<BoostType>
OnBoostCancelled : Observable<BoostType>
OnBoostApplied   : Observable<(BoostType, Vector2Int)>
OnHintApplied    : Observable<(from, to)>
OnShuffleApplied : Observable<Unit>
```

### SwapService (SceneContext)
```
Lock() / Unlock()        — глобальный лок ввода
ClearSelection()         — сброс _firstCell без лока
                           вызывается при OnBoostSelected / OnBoostCancelled
                           предотвращает случайный своп после буста
```

### GemFactory (SceneContext)
```
Create(nodeType, parent, name) → GemView
  — использует GemConfig.GemViewPrefab (базовый префаб)
  — назначает SetConfig + SetVisual
  — позиционирование — ответственность BoardView.PositionGem()
```

---

## 🏗️ Services

### ProjectContext
```
InventoryService   — бусты (PlayerPrefs)
ProgressService    — прогресс карты (PlayerPrefs)
CoinService        — монеты (PlayerPrefs) ✅ НОВЫЙ
LivesService       — жизни + таймер (PlayerPrefs) ✅ НОВЫЙ
RewardService      — выдача наград за уровни (IDisposable)
WalletView         — HUD в Canvas сцены, подписка на сервисы через Construct() ✅ НОВЫЙ
ISceneManagerService → SceneManagerService
Bootstrapper       — стартует с SceneId.StageMap
```

### SceneContext (Game)
```
BoardService, SwapService, LayerService, LevelService
HintService, BoostService
GemFactory              — создание GemView
GameLoopController      — подготовка доски (IInitializable #1, IDisposable)
GameFlowService         — игровой цикл: попапы, прогресс, навигация (IInitializable #2, IDisposable)
```

### LevelService (SceneContext)
```
RegisterMatch(match)                 — регистрация матча через GemMatch
RegisterDestroyedCells(gems)         — регистрация без GemMatch (для бустов)
ProcessTurnResult()                  — всегда вызывается после свопа И после буста
```

### GameLoopController (SceneContext)
```
Баги исправлены (2025):
  - ClearSelection() при OnBoostSelected/OnBoostCancelled
  - RegisterDestroyedCells + LayerService.ProcessMatches в ApplyBoostAtAsync
  - ProcessTurnResult() всегда после буста
  - LockCell(false) до null-чека в HandleSwapAsync
```

---

## 📦 Configs

```
LevelConfig    — MoveLimit, AllowedNodeTypes[], Objectives[], Grid[], Rewards[]
StageConfig    — StageName, StageIcon, IsBonusStage, SuperPrize, StageRewards[], Levels[3]
CountryConfig  — CountryName, CountryIcon, SectionColor, Stages[10]
WorldMapConfig — Countries[5]
EconomyConfig  — MaxLives, LifeRegenSeconds, LivesPurchasePrice, LivesPurchaseAmount, InitialCoins ✅ НОВЫЙ
```

---

## 📁 Структура папок (актуальная)

```
Assets/Match3/Scripts/
├── Core/
│   ├── Enums/          NodeType, SuperGemType, BoostType, CellType, SceneId, RewardType
│   ├── Models/         BoardCell, CellData, ObjectiveData, LevelAddress, RewardData
│   ├── BoostTypeExtensions.cs  ← extension ToSuperGemType()
│   └── GemMatch.cs, GemState.cs, IGemView.cs
├── Configs/
│   ├── GemConfig, BoardConfig, AnimationConfig, RewardIconConfig
│   ├── LevelConfig, LevelConfigRepository
│   ├── StageConfig, WorldMapConfig, CountryConfig
│   └── EconomyConfig  ✅ НОВЫЙ
├── Controllers/
│   ├── Bootstrapper
│   └── GameLoopController    ← EnableInput() вызывается GameFlowService
├── Services/
│   ├── Board/          BoardService
│   ├── Swap/           SwapService
│   ├── Layer/          LayerService
│   ├── Level/          LevelService  (содержит LevelState enum)
│   ├── Factories/      GemFactory
│   ├── HintService, BoostService
│   ├── GameFlowService          ← оркестратор игрового цикла (SceneContext)
│   ├── InventoryService, ProgressService, RewardService  ← ProjectContext
│   ├── CoinService              ← ProjectContext ✅ НОВЫЙ
│   ├── LivesService             ← ProjectContext ✅ НОВЫЙ
│   └── StarCalculator
├── Views/
│   ├── StageMapView, StageNodeView, CountryNodeView, LevelSelectPopupView
│   ├── BoardView, GemView, LayerView
│   ├── ObjectiveView, MoveCounterView
│   ├── LevelResultView, LevelTaskPopupView, StageRewardPopupView
│   ├── BackpackView, ActiveBoostView
│   ├── WalletView               ← DontDestroyOnLoad, спавн из ProjectContext ✅ НОВЫЙ
│   └── BoardInputHandler
├── Presenters/
│   ├── StageMapPresenter
│   ├── BoardPresenter, SwapPresenter, LayerPresenter
│   ├── ObjectivePresenter
│   ├── LevelPresenter           ← только HUD: ходы + цели
│   ├── BoostPresenter
│   └── WalletPresenter          ← ProjectContext, один на всю игру ✅ НОВЫЙ
├── Editor/
│   ├── WorldMapConfigGenerator, StageMapUISetup, LevelEditorWindow
│   └── CellDataDrawer, UISetupEditor, StageMapUISetupEditor
└── Installers/
    ├── ProjectConfigInstaller    ← + EconomyConfig ✅
    ├── ProjectServiceInstaller   ← + CoinService, LivesService, WalletPresenter, WalletView ✅
    ├── StageMapInstaller, StageMapViewInstaller
    ├── SceneServiceInstaller    ← + GameFlowService (после GameLoopController)
    ├── SceneViewInstaller       ← + LevelTaskPopupView, StageRewardPopupView
    └── ScenePresenterInstaller
```

## 🛡️ Блокирующие препятствия ✅ РЕАЛИЗОВАНО (Variant B)

### Типы
| Тип | Поведение | Триггер удара | HP |
|------|----------|----------------|----|
| **Ice** | Гем заморожен, не движется, не матчится | Смежный матч | 1–2 |
| **Box** | Нет гема, блокирует падение | Смежный матч | 1–3 |
| **Chain** | Гем виден. HP=1 участвует в матче; HP>1 смежный удар | HP=1: прямой матч; HP>1: смежный | 1–2 |
| **Rock** | Нет гема, как Box но прочнее. Визуальное разнообразие | Смежный матч | 2–4 |

### Архитектура (Variant B)
```
Препятствия хранятся в BoardCell (ObstacleType + ObstacleHp + MaxObstacleHp).

BoardCell автоматически обновляет поведение:
  CanBeMoved     = !Locked && !HasObstacle && gem?.CanMove
  CanFall        = !Locked && !HasObstacle && ...
  CanMatch()     = gem != null && !Ice && !(Chain && hp > 1)
  BlockFall      = Locked || HasObstacle || ...
  IsEmpty()      = !HasObstacle && gem == null && incoming == null

CellData: obstacleType + obstacleHp (0 = дефолт для типа)

BoardService владеет логикой:
  ProcessObstaclesFromMatch(matchedCells)  — удары по правилам типа
  HitObstaclesDirectly(cells)             — прямой удар (бустеры, супер-фишки)
  GetObstacles()                          — для рендеринга
  OnObstacleHit, OnObstacleCleared, OnAllObstaclesCleared

LayerService — удалён (stub-файл, удалить вручную)
LayerPresenter переписан на события BoardService
LayerView: SpawnObstacleCell / UpdateCellHp / ClearCell

LevelService.CheckWinCondition: _boardService.IsAllObstaclesCleared
```

### TODO по препятствиям
- Заменить dev-цвета в LayerView на спрайты (ObstacleConfig ScriptableObject)
- Обновить Level Editor (выбор типа + HP препятствия)
- Удалить файл LayerService.cs

## 📐 План генерации уровней (135 уровней, B+C)

### Прогрессия сложности
| Страна | Уровни | Ходы | Цвета | Размеры досок |
|--------|--------|------|-------|---------------|
| Egypt 0 | 1–27 | 32→24 | 3→4 | 7×7 → 8×8 |
| Greece 1 | 28–54 | 28→24 | 4→5 | 8×8 → 9×9 |
| China 2 | 55–81 | 26→22 | 5 | 9×9 → 10×10 |
| Maya 3 | 82–108 | 24→20 | 5→6 | 10×10 → 11×11 |
| India 4 | 109–135 | 22→18 | 6 | 11×11 → 12×12 |

### Egypt — детальный план этапов
| Этап | Уровни | Размер | Форма | Препятствия | Ходы |
|------|--------|--------|-------|-------------|------|
| 01 | 1–3 | 7×7 | FULL | — | 32,30,30 |
| 02 | 4–6 | 7×7 | ROUNDED | — | 30,28,28 |
| 03 | 7–9 | 7×7 | CROSS | Ice×2 | 30,28,28 |
| 04 | 10–12 | 7×7 | DIAMOND | Ice×4 | 28,26,26 |
| 05 | 13–15 | 7×7 | HOURGLASS | Ice×4 Box×2 | 28,26,26 |
| 06 | 16–18 | 8×7 | FULL | Box×4 | 28,26,26 |
| 07 | 19–21 | 8×7 | STAIRS | Box×4 Chain×2 | 26,24,24 |
| 08 | 22–24 | 8×8 | FULL | Chain×4 | 26,24,24 |
| 09 | 25–27 | 8×8 | T-SHAPE | Ice×4 Chain×2 | 26,24,24 |

### Формы досок (Hidden-ячейки)
```
ROUNDED 7×7:   H.N.N.N.N.N.H  (углы срезаны)
               N.N.N.N.N.N.N  (×5 rows)
               H.N.N.N.N.N.H

CROSS 7×7:     H.H.N.N.N.H.H  (2×2 углы скрыты)
               H.H.N.N.N.H.H
               N.N.N.N.N.N.N  (×3 rows)
               H.H.N.N.N.H.H
               H.H.N.N.N.H.H

DIAMOND 7×7:   H.H.H.N.H.H.H
               H.H.N.N.N.H.H
               H.N.N.N.N.N.H
               N.N.N.N.N.N.N
               H.N.N.N.N.N.H
               H.H.N.N.N.H.H
               H.H.H.N.H.H.H

HOURGLASS 7×7: N.N.N.N.N.N.N
               H.N.N.N.N.N.H
               H.H.N.N.N.H.H
               H.H.H.N.H.H.H
               H.H.N.N.N.H.H
               H.N.N.N.N.N.H
               N.N.N.N.N.N.N

STAIRS 8×7:    N×7 / H.N×6 / H.N×6 / HH.N×5 / HH.N×5
               HHH.N×4 / HHH.N×4 / HHHH.N×3

T-SHAPE 8×8:   N×8 (×2 top rows)
               HH.N×4.HH  / HHH.NN.HHH (×5 stem rows)
```

### obstacleType enum (в YAML)
```
0=None  1=Ice  2=Box  3=Chain  4=Rock
cellType: 0=Normal  1=Hidden
```

### AllowedNodeTypes (hex в YAML)
```
3 цвета (R,B,G): 010000000200000003000000
4 цвета +Y:      01000000020000000300000004000000
5 цветов +P:     0100000002000000030000000400000005000000
6 цветов +O:     010000000200000003000000040000000500000006000000
```

---

## 📝 TODO

- **#7** LevelState enum вынести из LevelService.cs в Core/Enums/
- UI анимации попапов (DOTween — вылет/исчезновение)
- Анимация вылета иконок наград в StageRewardPopupView
- LevelConfig.Rewards[] — выдаются через RewardService при первом прохождении уровня ✅
- Комбо-свопы двух супер-фишек
- Визуальные эффекты взрывов (частицы)
- WalletView — добавить в Canvas StageMap и Game сцен, назначить TMP-слоты в инспекторе
- EconomyConfig — создать ассет через Match3/Configs/Economy и назначить в ProjectConfigInstaller
- SceneViewInstaller + StageMapViewInstaller — добавить биндинг WalletView.FromComponentInHierarchy
