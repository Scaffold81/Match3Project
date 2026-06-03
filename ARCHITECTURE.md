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
public enum PurchaseResult { Success, Cancelled, Failed, NotEnoughCoins }
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

## 🗺️ Карта уровней (StageMap) ✅ РЕАЛИЗОВАНО / 📋 РАСШИРЯЕТСЯ

> Подробные промпты по графике и архитектуре расширения: Docs/GlobalMap_Prompts.md

### Концепция карты (обновлённая)
Вертикальный ScrollRect. Content собирается из чередующихся секций:

```
[CountrySectionView  — Египет          ]  egypt_map.png        390×1400px
[CountryTransitionView — Египет→Греция ]  transition_eg_gr.png 390×300px
[CountrySectionView  — Греция          ]  greece_map.png       390×1400px
[CountryTransitionView — Греция→Китай  ]  transition_gr_ch.png 390×300px
...и так далее
```

**Добавить новую страну** = добавить `CountryConfig` + 2 спрайта (секция + переход).

### Структура данных
```
WorldMapConfig (обновить)
  └── Countries[]   : CountryConfig[]            — секции стран
  └── Transitions[] : CountryTransitionConfig[]  — переходы между странами (N-1 штук)

CountryConfig (расширить)
  SectionSprite  : Sprite   — фон секции (390×1400px, fade верх/низ ~150px)
  SectionHeight  : float    — высота секции (default: 1400)
  └── StageConfig[10]       — 9 обычных + 1 бонусный этап

CountryTransitionConfig (ScriptableObject, новый)
  menuName: "Match3/Configs/CountryTransition"
  FromCountryIndex  : int
  ToCountryIndex    : int
  TransitionSprite  : Sprite  — спрайт перехода (390×300px)
  TransitionHeight  : float   — высота (default: 300)

LevelAddress { CountryIndex, StageIndex, LevelIndex }
```

### Новые View
```
CountrySectionView (MonoBehaviour, Prefab)
  — Image SectionSprite, высота = SectionHeight из CountryConfig
  — содержит StageNodeView[] и CountryNodeView — как сейчас
  Setup(CountryConfig)

CountryTransitionView (MonoBehaviour, Prefab)
  — Image TransitionSprite, высота = TransitionHeight
  — не содержит игровых узлов
  Setup(CountryTransitionConfig)
```

### StageMapView (обновить)
```
BuildContent(WorldMapConfig):
  for i in 0..Countries.Length:
    Instantiate(CountrySectionPrefab).Setup(Countries[i])
    if i < Countries.Length - 1:
      Instantiate(CountryTransitionPrefab).Setup(Transitions[i])

Content.sizeDelta.y = сумма всех SectionHeight + TransitionHeight
```

### Логика разблокировки этапов (не меняется)
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
StageMapView           — ScrollRect + динамический Content
  BuildContent(WorldMapConfig)   ← новый метод
  ScrollToNode(node)
  StageNodes   : List<StageNodeView>    ← собираются после BuildContent
  CountryNodes : List<CountryNodeView>  ← собираются после BuildContent

StageNodeView          — кнопка этапа (90×80px)
  countryIndex, stageIndex
  IsBonus   : bool
  IsUnlocked: bool
  Refresh(totalStars, isUnlocked, isBonus)
  OnClicked : Observable<StageNodeView>

CountryNodeView        — заголовок страны (300×72px)
  countryIndex
  Refresh(icon, countryName, sectionColor, isUnlocked)

CountrySectionView     — секция страны (новый)
  Setup(CountryConfig)

CountryTransitionView  — переход между странами (новый)
  Setup(CountryTransitionConfig)

LevelSelectPopupView   — попап выбора уровня (не меняется)
  Show(stageName, characterSprite, objectives, objectiveIcons,
       stageRewards, rewardIcons, storySlide?)
  Hide()
  OnPlayClicked  : Observable<Unit>
  OnCloseClicked : Observable<Unit>
```

### StageMapPresenter (SceneContext, не меняется)
```
Initialize():
  _view.BuildContent(_worldMapConfig)   ← вызвать первым
  RefreshStages + RefreshCountries
  Подписка на StageNodeView.OnClicked → открыть попап
  Подписка на LevelSelectPopupView.OnPlayClicked → SetCurrentAddress + LoadScene(Game)
  Подписка на OnCloseClicked → Hide
  ScrollToCurrentProgress()
```

### Editor-инструменты (обновить)
```
Match3/Generate World Map Configs  → WorldMapConfigGenerator.cs
  — добавить генерацию CountryTransitionConfig ассетов

Match3/Setup StageMap Scene        → StageMapUISetup.cs
  — заменить статическую расстановку узлов на BuildContent

Match3/Level Editor                → LevelEditorWindow.cs (не меняется)
```

### Графические ассеты — TODO
```
Секции стран (390×1400px, fade верх/низ 150px):
  egypt_map.png, greece_map.png, china_map.png, maya_map.png, india_map.png, russia_map.png

Переходы между странами (390×300px):
  transition_egypt_greece.png   — Средиземное море, корабль
  transition_greece_china.png   — Шёлковый путь, верблюды
  transition_china_maya.png     — Тихий океан, корабль
  transition_maya_india.png     — Атлантика, мыс Доброй Надежды
  transition_india_russia.png   — Северный путь, тройка/поезд

Пины стран (256×256px, прозрачный фон):
  egypt_pin.png, greece_pin.png, china_pin.png, maya_pin.png, india_pin.png, russia_pin.png

Промпты для всех ассетов → Docs/GlobalMap_Prompts.md
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
```

### StageRewardPopupView (View, MonoBehaviour)
```
Show(stageName, rewards, rewardIcons, storySlide?)
Hide()
OnClaimClicked : Observable<Unit>
```

### LevelResultView (View, MonoBehaviour)
```
OnRestartClicked      : Observable<Unit>
OnBackToMapClicked    : Observable<Unit>
OnContinueClicked     : Observable<Unit>
```

---

## 📖 Система историй (Story) ✅ РЕАЛИЗОВАНО

### Модели
```
StorySlide { Image?, LocalizationId?, FallbackText?, HasContent }
LevelStoryData { StartStory?, WinStory?, LoseStory? }
```

### Конфиг
```
StageStoryConfig (ScriptableObject)
  StageSelectStory? : StorySlide
  LevelStories[3]   : LevelStoryData
  GetLevelStory(levelIndex) → LevelStoryData?
```

### Флоу данных
```
StageMapPresenter → LevelSelectPopupView.Show(..., stage.StoryConfig?.StageSelectStory)
GameFlowService   → LevelTaskPopupView / StageRewardPopupView / _onLevelLost
```

---

## 🎁 Система наград ✅ РЕАЛИЗОВАНО

```
RewardData { RewardType, BoostType, Amount }

RewardService (ProjectContext):
  GrantAll(RewardData[])
  OnRewardGranted : Observable<RewardData>
```

---

## 💰 Кошелёк (Wallet) ✅ РЕАЛИЗОВАНО

```
EconomyConfig: MaxLives=5, LifeRegenSeconds=1800, LivesPurchasePrice=300,
               LivesPurchaseAmount=5, InitialCoins=500

CoinService:  Coins : ReadOnlyReactiveProperty<int>, Add, TrySpend
LivesService: Lives, TimeUntilNextLife, TrySpendLife, AddLives

WalletView — живёт в Canvas каждой сцены (StageMap, Game)
```

---

## 📺 Реклама (Ads) ✅ РЕАЛИЗОВАНО

```
AdConfig: AppIds, InterstitialCooldownSeconds=30, MinLevelsBetweenInterstitials=3,
          Placements[] (PlacementId + UnitIds + Rewards)

AdService: ShowRewardedAsync, TryShowInterstitialAsync, RegisterLevelCompleted
IAdProvider → MockAdProvider (реальный: AdMobProvider — TODO)
```

---

## 💎 Супер-фишки

| Тип | Триггер | Эффект |
|-----|---------|--------|
| HorizontalArrow | 4 горизонталь | Вся строка |
| VerticalArrow | 4 вертикаль | Весь столбец |
| ColorBomb | 5 прямая | Все фишки цвета |
| Bomb | T/L (5 кл.) | 3×3 |
| MegaBomb | 6+ | 5×5 |

---

## 🎒 Система рюкзака и бустов ✅ РЕАЛИЗОВАНО

```
InventoryService (ProjectContext): GetCount, HasAny, Add, TrySpend
ItemConfig: BoostItems[] (BoostType + Icon + CoinPrice), RewardIcons[]

BackpackView        — нижняя панель бустов в Game-сцене
BackpackPopupView   — рюкзак-попап на StageMap
BoostSlotView       — универсальный слот
ActiveBoostView     — шапка с активным бустом (Game)
BoostPresenter      — связывает всё вместе (SceneContext Game)
BoostService        — SelectBoost, TryApplyBoostAt, CancelBoost
ResourcePopupService / ResourcePopupView — получить ресурс за рекламу/монеты
```

---

## 🛒 Магазин (Shop) ✅ РЕАЛИЗОВАНО

```
ShopConfig: Items[] (PurchaseId + CoinCost + Icon + Title + Rewards[])
ShopService: BuyWithCoinsAsync, BuyWithIAPAsync, OnPurchaseSuccess
IIAPProvider → MockIAPProvider (реальный: UnityIAPProvider — TODO)
ShopView / ShopItemCardView / ShopPresenter / ShopInstaller
```

---

## 🏗️ Services

### ProjectContext
```
InventoryService, ProgressService, CoinService, LivesService
RewardService, AdService, ResourcePopupService, ShopService
ISceneManagerService → SceneManagerService
Bootstrapper — стартует с SceneId.StageMap
```

### SceneContext (StageMap)
```
StageMapPresenter
BackpackPopupView
ShopPresenter
```

### SceneContext (Game)
```
BoardService, SwapService, LayerService, LevelService
HintService, BoostService, GemFactory
GameLoopController (IInitializable #1)
GameFlowService    (IInitializable #2)
```

---

## 📦 Configs

```
LevelConfig            — MoveLimit, AllowedNodeTypes[], Objectives[], Grid[], Rewards[]
StageConfig            — StageName, StageIcon, BackgroundOverride?,
                         IsBonusStage, SuperPrize, StageRewards[], StoryConfig?, Levels[3]
StageStoryConfig       — StageSelectStory?, LevelStories[3]
CountryConfig          — CountryName, CountryIcon, SectionColor,
                         GameBackgroundSprite?,                              ← добавлено
                         SectionSprite, SectionHeight, Stages[10]   ← добавить спрайт
CountryTransitionConfig — FromCountryIndex, ToCountryIndex,          ← новый
                          TransitionSprite, TransitionHeight
WorldMapConfig         — Countries[], Transitions[]                  ← добавить Transitions
EconomyConfig          — MaxLives, LifeRegenSeconds, LivesPurchasePrice,
                         LivesPurchaseAmount, InitialCoins
AdConfig               — AppIds, Cooldowns, Placements[]
ItemConfig             — BoostItems[], RewardIcons[]
ShopConfig             — Items[]
```

---

## 📁 Структура папок (актуальная)

```
Assets/Match3/Scripts/
├── Core/
│   ├── Enums/          NodeType, SuperGemType, BoostType, CellType, SceneId,
│   │                   RewardType, AdPlacementId, AdFailReason, PurchaseResult
│   ├── Models/         BoardCell, CellData, ObjectiveData, LevelAddress, RewardData
│   │                   StorySlide, LevelStoryData, AdResult, ResourcePopupRequest
│   ├── BoostTypeExtensions.cs
│   └── GemMatch.cs, GemState.cs, IGemView.cs
├── Configs/
│   ├── GemConfig, BoardConfig, AnimationConfig
│   ├── LevelConfig, LevelConfigRepository
│   ├── StageConfig, CountryConfig (+ SectionSprite)
│   ├── CountryTransitionConfig                    ← новый
│   ├── WorldMapConfig (+ Transitions[])           ← обновить
│   ├── StageStoryConfig, EconomyConfig
│   ├── AdConfig, ItemConfig, ShopConfig
├── Controllers/
│   ├── Bootstrapper, GameLoopController
├── Services/
│   ├── Board/, Swap/, Layer/, Level/, Factories/
│   ├── Ads/, Shop/
│   ├── HintService, BoostService, GameFlowService
│   ├── InventoryService, ProgressService, RewardService
│   ├── CoinService, LivesService, ResourcePopupService
├── Views/
│   ├── StageMap/                                  ← новая папка
│   │   ├── StageMapView (обновить — BuildContent)
│   │   ├── StageNodeView, CountryNodeView
│   │   ├── CountrySectionView                     ← новый
│   │   ├── CountryTransitionView                  ← новый
│   │   └── LevelSelectPopupView
│   ├── BoardView, GemView, LayerView
│   ├── ObjectiveView, ObjectiveItemView, MoveCounterView
│   ├── LevelResultView, LevelTaskPopupView, StageRewardPopupView
│   ├── BackpackView, BackpackPopupView, BoostSlotView
│   ├── ActiveBoostView, ResourcePopupView, RewardItemView
│   ├── ShopView, ShopItemCardView, WalletView
│   └── BoardInputHandler
├── Presenters/
│   ├── StageMapPresenter (обновить — вызов BuildContent)
│   ├── BoardPresenter, SwapPresenter, LayerPresenter
│   ├── ObjectivePresenter, LevelPresenter
│   ├── BoostPresenter, ShopPresenter, WalletPresenter
└── Installers/
    ├── ProjectConfigInstaller, ProjectServiceInstaller
    ├── StageMapInstaller (обновить — CountryTransitionConfig)
    ├── StageMapViewInstaller
    ├── ShopInstaller
    ├── SceneServiceInstaller, SceneViewInstaller, ScenePresenterInstaller
```

---

## 🛡️ Блокирующие препятствия ✅ РЕАЛИЗОВАНО (Variant B)

| Тип | Поведение | Триггер | HP |
|-----|-----------|---------|-----|
| Ice | Гем заморожен | Смежный матч | 1–2 |
| Box | Нет гема, блокирует падение | Смежный матч | 1–3 |
| Chain | Гем виден | HP=1: прямой; HP>1: смежный | 1–2 |
| Rock | Нет гема, прочнее Box | Смежный матч | 2–4 |

```
BoardService: ProcessObstaclesFromMatch, HitObstaclesDirectly,
              OnObstacleHit, OnObstacleCleared, OnAllObstaclesCleared
```

---

## 📐 План генерации уровней (135 уровней)

| Страна | Уровни | Ходы | Цвета | Доски |
|--------|--------|------|-------|-------|
| Egypt 0 | 1–27 | 32→24 | 3→4 | 7×7→8×8 |
| Greece 1 | 28–54 | 28→24 | 4→5 | 8×8→9×9 |
| China 2 | 55–81 | 26→22 | 5 | 9×9→10×10 |
| Maya 3 | 82–108 | 24→20 | 5→6 | 10×10→11×11 |
| India 4 | 109–135 | 22→18 | 6 | 11×11→12×12 |

### Egypt — этапы
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

---

## 📝 TODO

### StageMap — расширение карты (приоритет)
- [ ] Добавить `SectionSprite`, `SectionHeight` в `CountryConfig`
- [ ] Создать `CountryTransitionConfig` ScriptableObject
- [ ] Добавить `Transitions[]` в `WorldMapConfig`
- [ ] Создать Prefab `CountrySectionView`
- [ ] Создать Prefab `CountryTransitionView`
- [ ] Обновить `StageMapView` — метод `BuildContent(WorldMapConfig)`
- [ ] Обновить `StageMapPresenter` — вызов `BuildContent` первым в `Initialize()`
- [ ] Обновить `StageMapUISetup` (Editor)
- [ ] Обновить `WorldMapConfigGenerator` (Editor) — генерировать `CountryTransitionConfig`
- [ ] Создать `CountryTransitionConfig` ассеты (5 штук: eg→gr, gr→ch, ch→ma, ma→in, in→ru)
- [ ] Заказать/сгенерировать графику (промпты: Docs/GlobalMap_Prompts.md)

### Прочее
- [ ] LevelState enum → Core/Enums/
- [ ] UI анимации попапов (DOTween)
- [ ] LevelConfig.Rewards[] — выдавать через RewardService
- [ ] Комбо-свопы двух супер-фишек
- [ ] Визуальные эффекты взрывов (частицы)
- [ ] BoostPresenter: иконки Hint/Shuffle
- [ ] WalletView — Canvas StageMap и Game сцен
- [ ] EconomyConfig, ItemConfig, AdConfig, ShopConfig — создать ассеты
- [ ] ShopView, ShopItemCardView — создать GameObject/Prefab
- [ ] ShopInstaller — добавить в SceneContext StageMap
- [ ] UnityIAPProvider, AdMobProvider — при подключении реальных SDK
- [ ] Заменить dev-цвета LayerView на спрайты (ObstacleConfig)
- [ ] Обновить Level Editor (тип + HP препятствия)
- [ ] Удалить LayerService.cs
- [x] Удалить AddDebugStarterPack()
