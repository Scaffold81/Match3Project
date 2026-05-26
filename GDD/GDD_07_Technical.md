# GDD 07 — Техническое резюме

---

## 🏗️ Архитектура

**Паттерн:** MVP (Model-View-Presenter)  
**DI:** Zenject (ProjectContext + SceneContext)  
**Реактивность:** R3 (ReactiveProperty, Subject, CompositeDisposable)  
**Async:** UniTask + CancellationToken  
**Анимации:** DOTween + SetLink  
**Данные:** ScriptableObject + PlayerPrefs + Newtonsoft.Json  

---

## 📂 Структура проекта

```
Assets/Match3/
  ├── Scripts/
  │   ├── Core/           — BoardService, SwapService, MatchService,
  │   │                     GravityService, SpawnService
  │   ├── Obstacles/      — ObstacleService, ObstacleHitService
  │   ├── SuperGems/      — SuperGemService, SuperGemActivator
  │   ├── Boosts/         — BoostService, BoostApplicator
  │   ├── GameLoop/       — GameLoopController, GameStateService
  │   ├── Objectives/     — ObjectiveService, ObjectiveTracker
  │   ├── Progress/       — ProgressService, StarCalculator
  │   ├── Wallet/         — WalletService
  │   ├── Inventory/      — InventoryService
  │   ├── Rewards/        — RewardService
  │   ├── Map/            — StageMapPresenter, StageNodePresenter
  │   ├── UI/             — Все View и Presenter для экранов/попапов
  │   ├── Configs/        — ScriptableObject конфиги
  │   └── Installers/     — Zenject Installers
  ├── Prefabs/
  ├── Sprites/
  └── ScriptableObjects/
```

---

## ⚙️ Ключевые сервисы

### Core (игровое поле)

| Сервис | Ответственность |
|--------|----------------|
| `BoardService` | Хранит `BoardCell[,]`, инициализация доски |
| `SwapService` | Валидация + выполнение свопа, ReactiveProperty выбора |
| `MatchService` | Поиск матчей (линии 3+, T/L форма), определение суперфишки |
| `GravityService` | Применение гравитации — сдвиг фишек вниз |
| `SpawnService` | Спаун новых фишек сверху после гравитации |
| `CascadeService` | Цикл: Match → Destroy → Gravity → Spawn → repeat |

### Gameplay

| Сервис | Ответственность |
|--------|----------------|
| `ObstacleService` | Хранение состояния препятствий, логика ударов |
| `SuperGemService` | Создание и активация суперфишек |
| `BoostService` | Выбор и применение бустов |
| `GameLoopController` | Оркестрация хода: Swap → Match → Cascade → Objectives → Win/Lose |
| `GameStateService` | Состояние сцены: Playing / Paused / Won / Lost |
| `ObjectiveService` | Отслеживание целей, событие OnAllCompleted |

### Мета

| Сервис | Ответственность |
|--------|----------------|
| `ProgressService` | Звёзды, CurrentAddress, разблокировка этапов |
| `WalletService` | Монеты + жизни, события изменения |
| `InventoryService` | Количество бустов, добавление/расход |
| `RewardService` | Выдача наград (coins/lives/boost/superprize) |

---

## 🔌 Zenject — контексты

### ProjectContext (глобальные, живут всё время)
- `WalletService`
- `InventoryService`
- `ProgressService`
- `RewardService`

### SceneContext — StageMap
- `StageMapPresenter`
- `StageNodePresenter[]` (Pool или dynamic bind)
- `LevelSelectPopupPresenter`

### SceneContext — GamePlay
- `BoardService`
- `SwapService`
- `MatchService`
- `GravityService`
- `SpawnService`
- `CascadeService`
- `ObstacleService`
- `SuperGemService`
- `BoostService`
- `ObjectiveService`
- `GameLoopController`
- `GameStateService`
- все Presenter и View сцены

---

## 📡 Потоки данных (R3)

```
WalletService.Coins          → WalletPresenter → WalletView
WalletService.Lives          → WalletPresenter → WalletView
ObjectiveService.OnProgress  → ObjectivePresenter → ObjectiveHUDView
ObjectiveService.OnCompleted → GameLoopController → Win sequence
GameStateService.State       → PausePresenter / HUD visibility
ProgressService.CurrentAddr  → StageMapPresenter → скролл к позиции
InventoryService.BoostCount  → BackpackPresenter → BackpackView (кнопки)
SwapService.SelectedCell     → SwapPresenter → BoardView (подсветка)
```

---

## 📦 ScriptableObject конфиги

| Config | Данные |
|--------|--------|
| `WorldMapConfig` | Массив CountryConfig |
| `CountryConfig` | Название, тема, StageConfig[] |
| `StageConfig` | IsBonusStage, LevelConfig[3], StageRewards[] |
| `LevelConfig` | Rows, Cols, MoveLimit, AllowedNodeTypes, Grid[], Objectives[] |
| `GemConfig` | Спрайты и цвета для каждого NodeType (per-country) |
| `ObstacleConfig` | MaxHp, спрайт, анимация per ObstacleType |
| `BoostConfig` | Иконки, стоимость per BoostType |

---

## ✅ Статус реализации (детальный)

| Система | Статус | Фаза |
|---------|--------|------|
| BoardService | ✅ Готово | 1 |
| SwapService | ✅ Готово | 1 |
| MatchService (3-в-ряд) | ✅ Готово | 1 |
| GravityService | ✅ Готово | 1 |
| SpawnService | ✅ Готово | 1 |
| CascadeService | ✅ Готово | 1 |
| ObstacleService (Ice/Box/Chain/Rock) | ✅ Готово | 1 |
| SuperGemService (5 типов) | ✅ Готово | 1 |
| BoostService (7 типов) | ✅ Готово | 1 |
| GameLoopController | ✅ Готово | 1 |
| ObjectiveService (Collect + ClearLayer) | ✅ Готово | 1 |
| ProgressService (звёзды, адрес) | ✅ Готово | 1 |
| WalletService (монеты + жизни) | ✅ Готово | 1 |
| InventoryService | ✅ Готово | 1 |
| RewardService | ✅ Готово | 1 |
| StageMap UI | ✅ Готово | 1 |
| GamePlay UI (HUD + попапы) | ✅ Готово | 1 |
| WorldMapConfig (данные) | ✅ Готово | 1 |
| DOTween анимации (все View) | 🟡 Написано, не проверено | 1 |
| Object Pooling (GemPool) | ⬜ Не начато | 2 |
| Аудио (AudioService) | ⬜ Не начато | 2 |
| Ежедневный бонус | ⬜ Не начато | 3 |
| Таймер жизней | ⬜ Не начато | 3 |
| Rewarded Ads | ⬜ Не начато | 3 |
| Second Chance (+5 ходов) | ⬜ Не начато | 3 |
| IAP / Магазин | ⬜ Не начато | 4 |
| Дневник исследователя | ⬜ Не начато | 4 |
| Analytics | ⬜ Не начато | 4 |
| Push-уведомления | ⬜ Не начато | 4 |

---

## 🗓️ Фазы разработки

| Фаза | Содержание | Цель |
|------|-----------|------|
| **Фаза 1** | Core gameplay + полный контент (MVP) | Playtest-ready build |
| **Фаза 2** | Object Pooling, Audio, полишинг анимаций | Performance build |
| **Фаза 3** | Мета: жизни/таймер, Rewarded Ads, Second Chance | Монетизация v1 |
| **Фаза 4** | IAP, Магазин, Analytics, Push | Soft Launch |
| **Фаза 5** | Контент: Страны 3–5, LiveOps | Scale |

---

## ⚠️ Известные риски

| Риск | Вероятность | Митигация |
|------|------------|-----------|
| Производительность на слабых устройствах (каскад + анимации) | Средняя | Object Pooling в Фазе 2 |
| Баланс сложности не соответствует retention | Высокая | A/B тест ходов ±2 с Фазы 3 |
| DOTween утечки (не всегда SetLink) | Низкая | Code review перед Фазой 2 |
| PlayerPrefs переполнение (много ключей) | Низкая | Миграция на JSON-сохранение в Фазе 3 |
