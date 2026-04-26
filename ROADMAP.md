# Match-3 — Roadmap разработки

> Текущий статус и план по фазам.
> Архитектура → `ARCHITECTURE.md` | Механики → `GDD.md`

---

## 📊 Текущий статус

| Фаза | Название | Статус |
|------|----------|--------|
| 1 | Core — данные и инфраструктура | ✅ Готово |
| 2 | Gameplay — базовый игровой цикл | 🟡 В процессе (настройка Unity) |
| 3 | Polish — анимации, бонусные фишки, звёзды | ⬜ Не начато |
| 4 | Meta — прогрессия, карта уровней, бустеры | ⬜ Не начато |
| 5 | Release — оптимизация, монетизация, магазин | ⬜ Не начато |

---

## ✅ Фаза 1 — Core (ГОТОВО)

### 1.1 Data & Enums
- [x] `NodeType`, `CellType`, `SceneId`
- [x] `CellData`, `ObjectiveData`

### 1.2 Configs (ScriptableObjects)
- [x] `LevelConfig`
- [x] `LevelConfigRepository`
- [x] `GemConfig`
- [x] `BoardConfig`
- [x] `AnimationConfig`

### 1.3 Инфраструктура (Zenject)
- [x] `ProjectConfigInstaller`
- [x] `ProjectServiceInstaller`
- [x] `SceneServiceInstaller`
- [x] `SceneViewInstaller`
- [x] `ScenePresenterInstaller`
- [x] `ISceneManagerService` + `SceneManagerService`
- [x] `Bootstrapper`

### 1.4 Services
- [x] `BoardService`
- [x] `MatchService`
- [x] `SwapService`
- [x] `GravityService`
- [x] `SpawnService`
- [x] `LayerService`
- [x] `ObjectiveService`
- [x] `MoveCounterService`
- [x] `LevelService`
- [x] `GemFactory`

### 1.5 Views & Presenters
- [x] `BoardView` + `BoardPresenter`
- [x] `GemView` + `SwapPresenter`
- [x] `LayerView` + `LayerPresenter`
- [x] `ObjectiveView` + `ObjectivePresenter`
- [x] `MoveCounterView`
- [x] `LevelResultView` + `LevelPresenter`
- [x] `InputController`
- [x] `GameLoopController`

---

## 🟡 Фаза 2 — Настройка Unity (В ПРОЦЕССЕ)

### 2.1 Сцены и контексты
- [ ] Создать сцену `Bootstrap` с `ProjectContext`
- [ ] Создать сцену `Game` с `SceneContext`
- [ ] Назначить инсталлеры в контексты

### 2.2 Assets
- [ ] Создать `LevelConfigRepository.asset` → `Assets/Match3/Configs/`
- [ ] Добавить тестовые уровни в `LevelConfigRepository.Levels[]`
- [ ] Назначить `LevelConfigRepository` в `ProjectConfigInstaller`
- [ ] Создать `GemBase.prefab` → `Assets/Match3/Prefabs/Gems/`
- [ ] Заполнить `GemConfig` (Sprite + Color + Prefab для каждого NodeType)
- [ ] Настроить `BoardConfig` (CellSize, Spacing)
- [ ] Настроить `AnimationConfig` (длительности анимаций)

### 2.3 UI сцены
- [ ] Разметить `BoardView` на канвасе
- [ ] Настроить `ObjectiveView` (иконки целей)
- [ ] Настроить `MoveCounterView`
- [ ] Настроить `LevelResultView` (экраны победы / поражения)

### 2.4 Тестирование
- [ ] Проверить своп → матч → гравитацию → спаун
- [ ] Проверить каскадные матчи
- [ ] Проверить слои (Layer)
- [ ] Проверить условия победы и поражения
- [ ] Проверить сцену Bootstrap → Game (переход)

---

## ⬜ Фаза 3 — Polish

### 3.1 Анимации (DOTween)
- [ ] Анимация свопа (плавный обмен)
- [ ] Анимация отказа (тряска при неверном свопе)
- [ ] Анимация уничтожения (вспышка + scale = 0)
- [ ] Анимация падения (bounce при приземлении)
- [ ] Анимация появления (влёт сверху)
- [ ] Анимация каскада (нарастающий эффект)

### 3.2 Бонусные фишки
- [ ] Добавить `BonusNodeType` в Enum (Striped, Bomb, ColorBomb)
- [ ] `BonusMatchService` — определение создания бонусной фишки
- [ ] `BonusActivationService` — логика взрыва при своппинге
- [ ] Визуал и анимации бонусных фишек

### 3.3 Система звёзд
- [ ] Добавить в `LevelConfig` пороги: `TwoStarMoves`, `ThreeStarMoves`
- [ ] `LevelService` возвращает кол-во звёзд по остатку ходов
- [ ] `LevelResultView` — отображение заработанных звёзд

### 3.4 Аудио
- [ ] `AudioService` (ProjectContext)
- [ ] Звуки: матч, своп-отказ, каскад, победа, поражение
- [ ] Фоновая музыка

### 3.5 Визуальные эффекты
- [ ] Частицы при уничтожении фишек
- [ ] Свечение при выборе фишки
- [ ] Эффект каскада (нарастающая интенсивность)

---

## ⬜ Фаза 4 — Meta

### 4.1 Карта уровней
- [ ] `MapView` — скролл-карта с узлами уровней
- [ ] `MapService` — прогресс и разблокировка
- [ ] `SaveService` — сохранение прогресса (PlayerPrefs / JSON)
- [ ] Переход Map → Game уровень

### 4.2 Система бустеров
- [ ] `BoosterConfig` — типы и эффекты
- [ ] `BoosterService` — инвентарь, применение
- [ ] UI выбора бустеров перед уровнем
- [ ] UI бустеров во время уровня

### 4.3 Валюта и награды
- [ ] `CurrencyService` — монеты
- [ ] Начисление монет за звёзды
- [ ] Покупка бустеров за монеты

### 4.4 Система жизней
- [ ] `LivesService` — 5 жизней, восстановление по таймеру
- [ ] UI жизней в главном меню и на карте

---

## ⬜ Фаза 5 — Release

### 5.1 Оптимизация
- [ ] Object Pooling для GemView
- [ ] Профилирование: CPU / Memory / GC
- [ ] Оптимизация анимаций (батчинг DOTween)

### 5.2 Монетизация
- [ ] IAP — покупка монет, бустеров, жизней
- [ ] Rewarded Ads — за монеты / продолжение
- [ ] `ShopService`, `IAPService`

### 5.3 Аналитика
- [ ] `AnalyticsService` — события: level_start, level_complete, level_fail
- [ ] Интеграция с Firebase или аналогом

### 5.4 Контент
- [ ] Минимум 50 уровней к релизу
- [ ] Level Editor (Editor-инструмент для быстрого создания уровней)
- [ ] Балансировка сложности

---

## 🐛 Известные проблемы

| # | Проблема | Приоритет | Статус |
|---|---------|-----------|--------|
| 1 | Анимации DOTween не проверены в рантайме | High | Открыт |
| 2 | Каскадные матчи не протестированы | High | Открыт |
| 3 | LevelConfigRepository не создан в Unity | High | Открыт |
| 4 | GemBase.prefab не создан (работает через код) | Medium | Открыт |

---

## 📝 Журнал изменений

### [0.2.0] — Текущая
- Исправлена индексация `BoardView.GetGemView` (row*cols+col)
- Убран двойной вызов `SpawnMissing`
- `LevelConfig.CreateForTest()` вместо запрещённого `new LevelConfig()`
- Убраны все `Debug.Log` нарушения (~10 файлов)
- `GameLoopController` создаёт тестовый уровень автоматически при пустом репозитории

### [0.1.0]
- Core архитектура: все сервисы, вьюхи, презентеры
- Базовый игровой цикл: своп → матч → гравитация → спаун → каскад
- Zenject DI: ProjectContext + SceneContext
