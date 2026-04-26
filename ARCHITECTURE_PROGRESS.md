## 📋 Прогресс разработки

### Этап 1 — Core Data & Enums
- [x] Enums: NodeType, CellType, SceneId
- [x] Models: CellData, ObjectiveData

### Этап 2 — Configs (ScriptableObjects)
- [x] LevelConfig
- [x] GemConfig
- [x] BoardConfig
- [x] AnimationConfig

### Этап 3 — Infrastructure
- [x] ISceneManagerService + SceneManagerService
- [x] Bootstrapper
- [x] ProjectConfigInstaller
- [x] ProjectServiceInstaller
- [x] SceneServiceInstaller
- [x] SceneViewInstaller
- [x] ScenePresenterInstaller

### Этап 4 — Services
- [x] BoardService
- [x] MatchService
- [x] SwapService
- [x] GravityService
- [x] SpawnService
- [x] LayerService
- [x] ObjectiveService
- [x] MoveCounterService
- [x] LevelService

### Этап 5 — Views & Presenters
- [x] BoardView + BoardPresenter
- [x] GemView + SwapPresenter
- [x] LayerView + LayerPresenter
- [x] ObjectiveView + ObjectivePresenter
- [x] LevelResultView + LevelPresenter
- [x] MoveCounterView

### Этап 6 — Полировка ✅
- [x] Исправлена компиляция GemFactory (режим без префабов работает автоматически)
- [x] GameLoopController создаёт тестовый уровень автоматически
- [x] BoardView создаёт ячейки через код (без префабов, опционально можно назначить)
  - Добавлен `using UnityEngine.UI` для Image
  - Упрощено создание GemView через код
- [ ] Анимации (DOTween) — проверка и настройка
- [ ] Каскадные матчи — тестирование cascade loop
- [ ] Тестирование уровней — заполнение LevelConfigRepository

### Компиляция
- 🟢 **Статус:** Исправлены все баги после ревью кода (Qwen fix)
