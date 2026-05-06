## 📋 Прогресс разработки

### Этап 1 — Core Data & Enums
- [x] Enums: NodeType, CellType, SceneId, SuperGemType, BoostType, RewardType
- [x] Models: CellData, ObjectiveData, LevelAddress, RewardData, BoardCell
- [x] Core: GemMatch, GemState, IGemView
- [x] Утилиты: BoostTypeExtensions (ToSuperGemType)

### Этап 2 — Configs (ScriptableObjects)
- [x] LevelConfig (+ Rewards[])
- [x] LevelConfigRepository
- [x] GemConfig
- [x] BoardConfig
- [x] AnimationConfig
- [x] StageConfig, CountryConfig, WorldMapConfig

### Этап 3 — Infrastructure
- [x] ISceneManagerService + SceneManagerService
- [x] Bootstrapper
- [x] ProjectConfigInstaller
- [x] ProjectServiceInstaller
- [x] SceneServiceInstaller (+ GemFactory)
- [x] SceneViewInstaller
- [x] ScenePresenterInstaller
- [x] StageMapInstaller, StageMapViewInstaller

### Этап 4 — Services
- [x] BoardService
- [x] SwapService
- [x] GravityService (внутри BoardService.ComputeAndApplyFalls)
- [x] SpawnService (внутри BoardService.GetSpawnList)
- [x] MatchService (внутри BoardService.FindAndCreateMatches)
- [x] LayerService
- [x] LevelService (содержит ObjectiveProgress, LevelState, MoveCounter логику)
- [x] HintService
- [x] BoostService
- [x] InventoryService
- [x] ProgressService
- [x] RewardService (IDisposable, BindInterfacesAndSelfTo)
- [x] StarCalculator
- [x] GemFactory

### Этап 5 — Views & Presenters
- [x] BoardView (PositionGem вместо InstantiateGem)
- [x] GemView + SwapPresenter
- [x] LayerView + LayerPresenter
- [x] ObjectiveView + ObjectivePresenter
- [x] MoveCounterView
- [x] LevelResultView (Observable<Unit> вместо Action events) + LevelPresenter
- [x] BackpackView + ActiveBoostView + BoostPresenter
- [x] BoardInputHandler
- [x] StageMapView + StageMapPresenter
- [x] StageNodeView, CountryNodeView, LevelSelectPopupView

### Этап 6 — Рефакторинг и чистота кода ✅
- [x] Удалены мёртвые файлы: MoveCounterService, ObjectiveService, HintView, HintPresenter
- [x] LevelResultView: Action events → R3 Observable
- [x] GemFactory подключена в BoardPresenter (вместо прямого Instantiate)
- [x] RewardService реализует IDisposable, биндится через BindInterfacesAndSelfTo
- [x] BoostTypeExtensions: убрано дублирование BoostTypeToSuperGemType

### Открытые задачи
- [ ] **#6** Кнопки Рестарт/Следующий уровень подключить в LevelPresenter
- [ ] **#7** LevelState enum вынести в Core/Enums/
- [ ] Анимации (DOTween) — проверка в рантайме
- [ ] Каскадные матчи — тестирование
- [ ] Настройка Unity: сцены, ассеты, UI (Фаза 2 Roadmap)

### Компиляция
- 🟢 **Статус:** Код чистый, все рефакторинги применены
