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
public enum AdPlacementId { ContinueGame, RewardedLives, RewardedCoins, RewardedBoost, Interstitial }
public enum AdFailReason  { None, NoFill, NetworkError, Timeout, Unknown }
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
              ├── StoryConfig?   — опциональная история этапа (StageStoryConfig)
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

LevelSelectPopupView   — попап выбора уровня
  Show(stageName, characterSprite, objectives, objectiveIcons,
       stageRewards, rewardIcons, storySlide?)   ← storySlide опциональный
  Hide()
  OnPlayClicked  : Observable<Unit>
  OnCloseClicked : Observable<Unit>
```

### StageMapPresenter (SceneContext)
```
Initialize():
  RefreshStages + RefreshCountries
  Подписка на StageNodeView.OnClicked → открыть попап
    → передаёт stage.StoryConfig?.StageSelectStory в LevelSelectPopupView.Show()
  Подписка на LevelSelectPopupView.OnPlayClicked → SetCurrentAddress + LoadScene(Game)
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
  → Показать LevelTaskPopupView (задание уровня + StartStory если есть)
  → Игрок нажимает Play
  → GameLoopController.EnableInput() → игра началась

Победа (LevelService.OnLevelWon):
  → GameFlowService.HandleWin()
  → SaveProgress()
  → AdService.RegisterLevelCompleted() + TryShowInterstitialAsync()
  → Последний уровень этапа?
      ДА  → HandleStageComplete → StageRewardPopupView.Show(+ WinStory) → Claim → LoadScene(StageMap)
      НЕТ → HandleNextLevel → SetCurrentAddress(next) → LoadScene(Game)

Поражение (LevelService.OnLevelLost):
  → GameFlowService.HandleLose()
  → _onLevelLost.OnNext((SadCharacterSprite, LoseStory))
  → LevelResultView показывает панель поражения + LoseStory
  → "Продолжить" → AdService.ShowRewarded(ContinueGame) → +5 ходов
  → Restart → LoadScene(Game)
  → Back to Map → LoadScene(StageMap)
```

### GameFlowService (SceneContext, IInitializable, IDisposable)
```
Оркестрирует весь игровой цикл внутри Game-сцены.

OnLevelLost : Observable<(Sprite? CharacterSprite, StorySlide? Story)>

ShowCurrentLevelTask():
  → читает stage.StoryConfig?.GetLevelStory(levelIndex)?.StartStory
  → передаёт в LevelTaskPopupView.Show()

HandleStageComplete(stage, address):
  → читает stage.StoryConfig?.GetLevelStory(levelIndex)?.WinStory
  → передаёт в StageRewardPopupView.Show()

HandleLose():
  → читает stage.StoryConfig?.GetLevelStory(levelIndex)?.LoseStory
  → публикует через _onLevelLost
```

### LevelTaskPopupView (View, MonoBehaviour)
```
Show(levelTitle, characterSprite, objectives, objectiveIcons, storySlide?)
Hide()
OnPlayClicked : Observable<Unit>

Story-блок скрыт если storySlide == null или !HasContent.
```

### StageRewardPopupView (View, MonoBehaviour)
```
Show(stageName, rewards, rewardIcons, storySlide?)
Hide()
OnClaimClicked : Observable<Unit>
```

### LevelResultView (View, MonoBehaviour) — только поражение
```
Подписывается на GameFlowService.OnLevelLost : Observable<(Sprite?, StorySlide?)>
Показывает характер + story-блок (если есть)
OnRestartClicked      : Observable<Unit>
OnBackToMapClicked    : Observable<Unit>
OnContinueClicked     : Observable<Unit>  ← кнопка "Продолжить за рекламу"
```

---

## 📖 Система историй (Story) ✅ РЕАЛИЗОВАНО

### Модели
```
StorySlide (Core/Models, Serializable)
  Image?           : Sprite       — картинка слайда
  LocalizationId?  : string       — ID для системы локализации
  FallbackText?    : string       — текст до подключения локализации
  HasContent       : bool         — true если хоть одно поле заполнено

LevelStoryData (Core/Models, Serializable)
  StartStory?  : StorySlide  — перед стартом уровня (LevelTaskPopupView)
  WinStory?    : StorySlide  — победа на последнем уровне (StageRewardPopupView)
  LoseStory?   : StorySlide  — поражение (LevelResultView)
```

### Конфиг
```
StageStoryConfig (Configs, ScriptableObject)
  menuName: "Match3/Story/Stage Story"

  StageSelectStory? : StorySlide      — экран выбора этапа (LevelSelectPopupView)
  LevelStories[3]   : LevelStoryData  — по индексу уровня

  GetLevelStory(levelIndex) → LevelStoryData?
```

### Подключение к StageConfig
```
StageConfig.StoryConfig? : StageStoryConfig   — null = этап без истории
```

### Story-блок во View
```
Каждый из 4 View содержит:
  [SerializeField] GameObject _storyPanel   — контейнер (скрыт по умолчанию)
  [SerializeField] Image      _storyImage   — картинка
  [SerializeField] TMP_Text   _storyText    — текст

ApplyStory(StorySlide? slide):
  null || !HasContent → _storyPanel.SetActive(false)
  иначе              → заполняет Image + Text, SetActive(true)
  // TODO: при подключении локализации читать по LocalizationId вместо FallbackText
```

### Флоу данных
```
StageMapPresenter → LevelSelectPopupView.Show(..., stage.StoryConfig?.StageSelectStory)
GameFlowService   → LevelTaskPopupView.Show(..., storyConfig?.GetLevelStory(idx)?.StartStory)
GameFlowService   → StageRewardPopupView.Show(..., storyConfig?.GetLevelStory(idx)?.WinStory)
GameFlowService   → _onLevelLost.OnNext((sadSprite, storyConfig?.GetLevelStory(idx)?.LoseStory))
LevelResultView   ← подписывается на OnLevelLost, применяет LoseStory
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

### RewardService (ProjectContext, IDisposable)
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
  "wallet_lives"           → int
  "wallet_lives_timestamp" → string — Unix-секунды; "0" = полные

Lives             : ReadOnlyReactiveProperty<int>
TimeUntilNextLife : ReadOnlyReactiveProperty<TimeSpan>   — Zero когда жизни полные
MaxLives          : int

TrySpendLife()     → bool
AddLives(amount)

Таймер: UniTask-цикл (тик каждую секунду).
Офлайн-восстановление на старте.
```

### WalletView (MonoBehaviour, SceneContext)
```
Живёт в Canvas каждой сцены (StageMap, Game). Presenter не нужен.
Construct(CoinService, LivesService) — подписывается напрямую.
OnDestroy() → _disposables.Dispose()
```

---

## 📺 Реклама (Ads) ✅ РЕАЛИЗОВАНО

### AdConfig (ProjectContext, ScriptableObject)
```
Путь: Match3/Configs/Ad

AppIdAndroid, AppIdIos
InterstitialCooldownSeconds   : int  = 30
MinLevelsBetweenInterstitials : int  = 3
Placements                    : AdPlacementEntry[]

AdPlacementEntry:
  PlacementId   : AdPlacementId
  UnitIdAndroid : string
  UnitIdIos     : string
  Rewards       : RewardData[]
```

### AdResult (Core/Models, readonly struct)
```
IsRewarded : bool
FailReason : AdFailReason

AdResult.Success() / AdResult.Skip() / AdResult.Fail(reason)
```

### IAdProvider (интерфейс)
```
Реализации:
  MockAdProvider   — для разработки (имитирует показ с задержкой 500мс)
  // AdMobProvider — подключить при интеграции реального SDK
```

### AdService (ProjectContext, IInitializable, IDisposable)
```
ShowRewardedAsync(placementId, ct) → UniTask<AdResult>
TryShowInterstitialAsync(ct)       → UniTask<bool>
RegisterLevelCompleted()           — вызывать из GameFlowService после победы
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

## 🎒 Система рюкзака и бустов ✅ РЕАЛИЗОВАНО

### Общая схема
```
Рюкзак — это два независимых места использования бустов:

  1. Game-сцена (игровой процесс):
       BackpackView + BoostPresenter → применение буста на доске

  2. StageMap-сцена (карта уровней):
       BackpackPopupView → просмотр инвентаря + получение бустов за рекламу/монеты
```

### InventoryService (ProjectContext, IDisposable)
```
PlayerPrefs: "inventory_boost_{BoostType}"

AllBoosts : BoostType[] — все 7 типов (HorizontalArrow, VerticalArrow, ColorBomb,
                          Bomb, MegaBomb, Hint, Shuffle)

GetCount(boost) : ReadOnlyReactiveProperty<int>
HasAny(boost)   : bool
Add(boost, n)   : void   — throws если n <= 0
TrySpend(boost) : bool   — false если 0 в инвентаре

AddDebugStarterPack() — ⚠️ временно, удалить после реализации реальных наград
```

### ItemConfig (ProjectContext, ScriptableObject)
```
Путь: Match3/Configs/Item
Единый источник правды для визуала и стоимости предметов.

BoostItems[]  : BoostItemEntry   — { BoostType, Icon (Sprite), CoinPrice (int) }
RewardIcons[] : RewardIconEntry  — { RewardType, Icon (Sprite) }

GetBoostIcon(boost)       → Sprite?
GetBoostCoinPrice(boost)  → int
GetIcon(RewardType, BoostType) → Sprite?
```

### BoostSlotView (MonoBehaviour)
```
Универсальный слот — используется в BackpackView (игра) и BackpackPopupView (карта).

BoostType       : BoostType  — назначается в инспекторе
IconTransform   : RectTransform
OnClicked       : Observable<BoostType>

SetIcon(Sprite?)
UpdateCount(int)    — обновляет счётчик + alpha (0.45f если 0)
SetInteractable(bool)
```

### BackpackView (MonoBehaviour) — Game-сцена
```
Нижняя панель бустов в игре. Слоты — pre-placed BoostSlotView[].

OnBoostClicked : Observable<BoostType>  — агрегирует клики всех слотов

UpdateCount(boost, count)
SetAllInteractable(bool)                — блокируется когда буст уже активен
GetIconWorldPosition(boost) → Vector3   — для анимации вылета в ActiveBoostView
```

### BackpackPopupView (MonoBehaviour) — StageMap-сцена ✅ НОВЫЙ
```
Рюкзак-попап на карте. Работает в двух режимах:
  _startVisible = false  — скрытый попап с кнопкой открытия (_showButton)
  _startVisible = true   — встроенная панель (например, в LevelSelectPopupView)

Слоты подключаются к InventoryService через Construct().
Клик по слоту → ResourcePopupService.Request() → появляется ResourcePopupView.

Construct(InventoryService, ResourcePopupService, AdConfig, ItemConfig):
  Для каждого слота:
    — SetIcon из ItemConfig
    — Подписка на InventoryService.GetCount → UpdateCount
    — Подписка на OnClicked → OnBoostSlotClicked(boostType)

OnBoostSlotClicked(boostType):
  — Берёт Rewards из AdConfig.GetPlacement(RewardedBoost)
  — Берёт CoinPrice из ItemConfig.GetBoostCoinPrice(boost)
  — Собирает ResourcePopupRequest { Title, Rewards, RewardIcons, AdPlacementId, CoinPrice }
  — Вызывает ResourcePopupService.Request(request)

Show() / Hide() — анимация CanvasGroup.DOFade (0.25f / 0.2f)
```

### ActiveBoostView (MonoBehaviour) — Game-сцена
```
Шапка — показывает активный буст во время выбора цели на доске.

OnCancelClicked : Observable<Unit>

ShowBoost(icon, fromWorldPos):
  — Иконка вылетает из позиции слота рюкзака (DOTween.Sequence)
  — DOMove (0.35f, OutBack) + DOFade (0.25f)

HideBoost():
  — DOFade исчезновение (0.2f)
```

### BoostPresenter (SceneContext, IInitializable, IDisposable)
```
Связывает BackpackView + ActiveBoostView + BoostService + InventoryService.

Initialize():
  Подписка InventoryService.GetCount → BackpackView.UpdateCount (для каждого из AllBoosts)
  BackpackView.OnBoostClicked → BoostService.SelectBoost
  BoostService.OnBoostSelected → ActiveBoostView.ShowBoost (иконка из GemConfig)
  BoostService.OnBoostCancelled → ActiveBoostView.HideBoost
  BoostService.OnBoostApplied  → ActiveBoostView.HideBoost
  ActiveBoostView.OnCancelClicked → BoostService.CancelBoost
  BoostService.ActiveBoost → BackpackView.SetAllInteractable(boost == None)

Примечание: иконки Hint/Shuffle пока не настроены в GemConfig — LogWarning.
```

### ResourcePopupService (ProjectContext, IDisposable)
```
Медиатор для открытия ResourcePopupView из любого места.

OnRequest : Observable<ResourcePopupRequest>
Request(ResourcePopupRequest) — публикует запрос

ResourcePopupRequest (Model):
  Title, CharacterDialog, DialogLocaleId
  CharacterSprite?   : Sprite
  Rewards            : RewardData[]
  RewardIcons        : Sprite?[]
  AdPlacementId      : AdPlacementId
  AdButtonLabel      : string
  CoinPrice?         : int
  CoinButtonLabel    : string
  NotifySuccess()    — callback после успешного получения награды
```

### ResourcePopupView (MonoBehaviour) — универсальный попап
```
Автономен — подписывается на ResourcePopupService.OnRequest сам (через Construct).

Структура:
  — Персонаж (иконка + диалог)
  — Список наград (RewardItemView, динамически)
  — Кнопка "Смотреть рекламу" → AdService.ShowRewardedAsync
  — Кнопка "Купить за монеты" (скрыта если CoinPrice == null) → CoinService.TrySpend
  — Кнопка "Закрыть"

При успехе рекламы: RewardService.GrantAll не нужен — AdService делает это сам.
При покупке за монеты: RewardService.GrantAll вызывается напрямую.
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
Lock() / Unlock()
ClearSelection() — вызывается при OnBoostSelected/OnBoostCancelled
                   предотвращает случайный своп после буста
```

---

## 🏗️ Services

### ProjectContext
```
InventoryService      — бусты (PlayerPrefs)
ProgressService       — прогресс карты (PlayerPrefs)
CoinService           — монеты (PlayerPrefs)
LivesService          — жизни + таймер (PlayerPrefs)
RewardService         — выдача наград (IDisposable)
AdService             — реклама (IInitializable, IDisposable)
ResourcePopupService  — медиатор попапа ресурсов (IDisposable)
WalletView            — HUD кошелька, подписка через Construct()
ISceneManagerService  → SceneManagerService
Bootstrapper          — стартует с SceneId.StageMap
```

### SceneContext (Game)
```
BoardService, SwapService, LayerService, LevelService
HintService, BoostService
GemFactory
GameLoopController  (IInitializable #1, IDisposable)
GameFlowService     (IInitializable #2, IDisposable)
```

### SceneContext (StageMap)
```
BackpackPopupView   — через MonoBehaviours To Inject или FromComponentInHierarchy
```

### LevelService (SceneContext)
```
RegisterMatch(match)
RegisterDestroyedCells(gems)
ProcessTurnResult()
```

### GameLoopController (SceneContext)
```
Баги исправлены (2025):
  - ClearSelection() при OnBoostSelected/OnBoostCancelled
  - RegisterDestroyedCells + LayerService.ProcessMatches в ApplyBoostAtAsync
  - ProcessTurnResult() всегда после буста
  - LockCell(false) до null-чека в HandleSwapAsync
```

### GemFactory (SceneContext)
```
Create(nodeType, parent, name) → GemView
  — использует GemConfig.GemViewPrefab
  — назначает SetConfig + SetVisual
  — позиционирование — ответственность BoardView.PositionGem()
```

---

## 📦 Configs

```
LevelConfig       — MoveLimit, AllowedNodeTypes[], Objectives[], Grid[], Rewards[]
StageConfig       — StageName, StageIcon, IsBonusStage, SuperPrize, StageRewards[],
                    StoryConfig?, Levels[3]
StageStoryConfig  — StageSelectStory?, LevelStories[3]
CountryConfig     — CountryName, CountryIcon, SectionColor, Stages[10]
WorldMapConfig    — Countries[5]
EconomyConfig     — MaxLives, LifeRegenSeconds, LivesPurchasePrice,
                    LivesPurchaseAmount, InitialCoins
AdConfig          — AppIds, Cooldowns, Placements[] (PlacementId + UnitIds + Rewards)
ItemConfig        — BoostItems[] (BoostType + Icon + CoinPrice), RewardIcons[]
```

---

## 📁 Структура папок (актуальная)

```
Assets/Match3/Scripts/
├── Core/
│   ├── Enums/          NodeType, SuperGemType, BoostType, CellType, SceneId,
│   │                   RewardType, AdPlacementId, AdFailReason
│   ├── Models/         BoardCell, CellData, ObjectiveData, LevelAddress, RewardData
│   │                   StorySlide, LevelStoryData, AdResult, ResourcePopupRequest
│   ├── BoostTypeExtensions.cs
│   └── GemMatch.cs, GemState.cs, IGemView.cs
├── Configs/
│   ├── GemConfig, BoardConfig, AnimationConfig
│   ├── LevelConfig, LevelConfigRepository
│   ├── StageConfig, WorldMapConfig, CountryConfig
│   ├── StageStoryConfig, EconomyConfig
│   ├── AdConfig
│   └── ItemConfig
├── Controllers/
│   ├── Bootstrapper
│   └── GameLoopController
├── Services/
│   ├── Board/          BoardService
│   ├── Swap/           SwapService
│   ├── Layer/          LayerService
│   ├── Level/          LevelService
│   ├── Factories/      GemFactory
│   ├── Ads/            IAdProvider, MockAdProvider, AdService
│   ├── HintService, BoostService
│   ├── GameFlowService
│   ├── InventoryService, ProgressService, RewardService
│   ├── CoinService, LivesService
│   ├── ResourcePopupService
│   └── StarCalculator
├── Views/
│   ├── StageMapView, StageNodeView, CountryNodeView, LevelSelectPopupView
│   ├── BoardView, GemView, LayerView
│   ├── ObjectiveView, ObjectiveItemView, MoveCounterView
│   ├── LevelResultView, LevelTaskPopupView, StageRewardPopupView
│   ├── BackpackView       — панель бустов в Game-сцене
│   ├── BackpackPopupView  — рюкзак-попап на карте (StageMap)
│   ├── BoostSlotView      — универсальный слот буста
│   ├── ActiveBoostView    — шапка с активным бустом (Game)
│   ├── ResourcePopupView  — универсальный попап получения ресурса
│   ├── RewardItemView     — одна строка награды в ResourcePopupView
│   ├── WalletView
│   └── BoardInputHandler
├── Presenters/
│   ├── StageMapPresenter
│   ├── BoardPresenter, SwapPresenter, LayerPresenter
│   ├── ObjectivePresenter
│   ├── LevelPresenter
│   ├── BoostPresenter
│   └── WalletPresenter
├── Editor/
│   ├── WorldMapConfigGenerator, StageMapUISetup, LevelEditorWindow
│   └── CellDataDrawer, UISetupEditor, StageMapUISetupEditor
└── Installers/
    ├── ProjectConfigInstaller    ← AdConfig, ItemConfig
    ├── ProjectServiceInstaller   ← IAdProvider→MockAdProvider, AdService,
    │                                ResourcePopupService
    ├── StageMapInstaller, StageMapViewInstaller
    ├── SceneServiceInstaller
    ├── SceneViewInstaller
    └── ScenePresenterInstaller
```

---

## 🛡️ Блокирующие препятствия ✅ РЕАЛИЗОВАНО (Variant B)

### Типы
| Тип | Поведение | Триггер удара | HP |
|------|----------|----------------|----|
| **Ice** | Гем заморожен, не движется, не матчится | Смежный матч | 1–2 |
| **Box** | Нет гема, блокирует падение | Смежный матч | 1–3 |
| **Chain** | Гем виден. HP=1 участвует в матче; HP>1 смежный удар | HP=1: прямой матч; HP>1: смежный | 1–2 |
| **Rock** | Нет гема, как Box но прочнее | Смежный матч | 2–4 |

### Архитектура (Variant B)
```
Препятствия хранятся в BoardCell (ObstacleType + ObstacleHp + MaxObstacleHp).

BoardService:
  ProcessObstaclesFromMatch(matchedCells)
  HitObstaclesDirectly(cells)
  GetObstacles()
  OnObstacleHit, OnObstacleCleared, OnAllObstaclesCleared

LayerPresenter переписан на события BoardService
LayerView: SpawnObstacleCell / UpdateCellHp / ClearCell
LevelService.CheckWinCondition: _boardService.IsAllObstaclesCleared
```

### TODO по препятствиям
- Заменить dev-цвета в LayerView на спрайты (ObstacleConfig ScriptableObject)
- Обновить Level Editor (выбор типа + HP препятствия)
- Удалить файл LayerService.cs

---

## 📐 План генерации уровней (135 уровней)

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
ROUNDED 7×7:   H.N.N.N.N.N.H / N.N.N.N.N.N.N (×5) / H.N.N.N.N.N.H
CROSS 7×7:     H.H.N.N.N.H.H (×2) / N.N.N.N.N.N.N (×3) / H.H.N.N.N.H.H (×2)
DIAMOND 7×7:   симметричный ромб, центральная строка полная
HOURGLASS 7×7: песочные часы — широкий верх/низ, узкий центр
STAIRS 8×7:    ступени с нарастающим Hidden слева
T-SHAPE 8×8:   2 полных строки сверху + 5 узких строк (стебель)
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
- LevelConfig.Rewards[] — выдаются через RewardService при первом прохождении уровня
- Комбо-свопы двух супер-фишек
- Визуальные эффекты взрывов (частицы)
- BoostPresenter: добавить иконки Hint/Shuffle в GemConfig или отдельный BoostConfig
- WalletView — добавить в Canvas StageMap и Game сцен, назначить TMP-слоты в инспекторе
- EconomyConfig — создать ассет через Match3/Configs/Economy и назначить в ProjectConfigInstaller
- ItemConfig — создать ассет через Match3/Configs/Item, заполнить иконки и цены
- AdConfig — создать ассет через Match3/Configs/Ad, заполнить UnitId
- SceneViewInstaller + StageMapViewInstaller — добавить биндинг WalletView.FromComponentInHierarchy
- StageStoryConfig — назначить в нужные StageConfig через инспектор
- Story: при локализации заменить FallbackText на чтение по LocalizationId
- При подключении реального SDK — создать AdMobProvider : IAdProvider, сменить биндинг
- Заменить dev-цвета в LayerView на спрайты (ObstacleConfig)
- Обновить Level Editor (выбор типа + HP препятствия)
- Удалить файл LayerService.cs
- AddDebugStarterPack() — удалить после реализации реальных наград
