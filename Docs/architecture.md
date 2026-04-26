# Match3 — Архитектура

## Общее описание
Мобильная игра-три в ряд (Match-3) с реактивным программированием на R3, Zenject и UniTask.

## Структура проекта
```
Assets/
├── Match3/
│   ├── Scripts/
│   │   ├── Configs/       — ScriptableObject конфиги (Board, Gem, Animation)
│   │   ├── Core/           — Модели данных (CellData, NodeType, Vector2Int)
│   │   ├── Services/       — Бизнес-логика (Board, Match, Layer, Objective)
│   │   ├── Controllers/    — Игровой цикл (GameLoopController)
│   │   ├── Presenters/     — Связь UI с сервисами
│   │   ├── Views/          — UI компоненты (BoardView, GemView)
│   │   └── Installers/     — Zenject биндинги
├── Plugins/                — Внешние библиотеки (Zenject, DOTween)
└── Packages/               — NuGet пакеты

Docs/
├── architecture.md         — Архитектура проекта
├── context.md              — Текущие задачи и прогресс
└── decisions.md            — Принятые архитектурные решения
```

## Ключевые системы

### 1. BoardService
- **Назначение:** Управление логикой игрового поля (сетка, узлы)
- **Состояние:** `ReactiveProperty<NodeType[,]>` для реактивных обновлений
- **Методы:** `Initialize()`, `SetNode()`, `RemoveNode()`, `SwapNodes()`

### 2. MatchService
- **Назначение:** Поиск и обработка совпадений (3+ в ряд)
- **Вход:** Массив доски, размеры сетки
- **Выход:** Список групп совпадений `List<Vector2Int>[]`

### 3. GravityService
- **Назначение:** Применение гравитации (падение фишек вниз)
- **Вход:** Текущее состояние доски
- **Выход:** Список падений `(Vector2Int from, Vector2Int to)`

### 4. LayerService
- **Назначение:** Система наложения слоев (покрытие ячеек)
- **Состояние:** `TotalLayerCells` — количество покрытых клеток
- **Методы:** `Initialize()`, `ProcessMatches()`, `OnAllLayersCleared`

### 5. ObjectiveService
- **Назначение:** Система целей/достижений
- **Состояние:** `ReactiveProperty<ObjectiveProgress[]>`
- **Прогресс:** Отслеживание собранных типов фишек

### 6. MoveCounterService
- **Назначение:** Подсчет ходов (лимит или неограниченно)
- **Состояние:** `ReactiveProperty<int> MovesLeft`
- **Событие:** `OnMovesExhausted` — когда ходы закончились

### 7. SpawnService
- **Назначение:** Спавн скрытых и отсутствующих фишек
- **Логика:** Вычисление рядов спавна, заполнение пустых клеток

### 8. GameLoopController
- **Назначение:** Координация игрового цикла
- **Ответственность:** Запуск уровня, обработка свопов, каскадные матчи
- **Асинхронность:** UniTask для анимаций и ожидания

## DI (Zenject)
Основные биндинги:
```csharp
Container.Bind<BoardService>().AsSingle();
Container.Bind<MatchService>().AsSingle();
Container.Bind<LayerService>().AsSingle();
Container.Bind<ObjectiveService>().AsSingle();
Container.Bind<MoveCounterService>().AsSingle();
Container.Bind<SpawnService>().AsSingle();
Container.Bind<GameLoopController>().AsTransient();
```

## Особенности и нюансы
- **Реактивность:** R3 для всех состояний (ReactiveProperty, Observable)
- **Асинхронность:** UniTask вместо корутин
- **Управление ресурсами:** CompositeDisposable для подписок
- **UI архитектура:** Model-View-Presenter паттерн
- **Типы узлов:** NodeType (Red, Blue, Green, Yellow, Purple, Orange) + Hidden/None
- **Типы клеток:** CellType (Normal, Hidden)
