#nullable enable

#if UNITY_EDITOR

using Match3.Installers;
using Match3.Views;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Match3.Editor
{
    /// <summary>
    /// Match3 → Setup StageMap Scene
    ///
    /// Полная автонастройка сцены StageMap:
    ///   1. Canvas + Background + ScrollRect(StageMapView) + LevelSelectPopup
    ///   2. Префабы CountryNode / StageNode (только если не существуют)
    ///   3. SceneContext + StageMapInstaller + StageMapViewInstaller
    ///   4. WorldMapConfig → StageMapViewInstaller
    ///   5. Build Settings (Bootstrap=0, StageMap=1, Game=2)
    /// </summary>
    public static class StageMapUISetup
    {
        // ── Цвета ────────────────────────────────────────────────────────────
        private static readonly Color ColorBg         = Hex("0F0F1E");
        private static readonly Color ColorCard       = Hex("1E1E35");
        private static readonly Color ColorBorder     = Hex("3A3A5A");
        private static readonly Color ColorAccent     = Hex("C8973A");
        private static readonly Color ColorText       = Hex("EAEAEA");
        private static readonly Color ColorTextMuted  = Hex("888888");
        private static readonly Color ColorUnlocked   = Hex("2A2A4A");
        private static readonly Color ColorStar       = Hex("FFD700");
        private static readonly Color ColorStarEmpty  = Hex("333344");
        private static readonly Color ColorOverlay    = new(0f, 0f, 0f, 0.75f);

        private const float RW = 390f;
        private const float RH = 844f;
        private const float CountryBtnW = 300f;
        private const float CountryBtnH = 72f;
        private const float StageBtnW   = 90f;
        private const float StageBtnH   = 80f;

        private const string PrefabDir         = "Assets/Match3/Prefabs/UI/Map";
        private const string PrefabCountryPath = PrefabDir + "/CountryNode.prefab";
        private const string PrefabStagePath   = PrefabDir + "/StageNode.prefab";
        private const string WorldMapConfigPath = "Assets/Match3/Configs/WorldMap/WorldMapConfig.asset";

        // ─────────────────────────────────────────────────────────────────────
        [MenuItem("Match3/Setup StageMap Scene")]
        public static void Setup()
        {
            if (!ConfirmIfExists("StageMapCanvas")) return;

            System.IO.Directory.CreateDirectory(PrefabDir);

            // 1. UI
            var canvas = BuildCanvas();
            BuildBackground(canvas);
            var scrollGo = BuildScrollRect(canvas);
            BuildLevelSelectPopup(canvas);

            // 2. Префабы
            BuildCountryNodePrefab();
            BuildStageNodePrefab();

            // 3. StageMapView — назначаем поля
            var mapView = scrollGo.AddComponent<StageMapView>();
            var mapSO   = new SerializedObject(mapView);

            mapSO.FindProperty("_scrollRect")
                 .objectReferenceValue = scrollGo.GetComponent<ScrollRect>();
            mapSO.FindProperty("_content")
                 .objectReferenceValue = scrollGo.transform.Find("Viewport/Content")
                                                 .GetComponent<RectTransform>();
            // public поля (без underscore)
            mapSO.FindProperty("zigzagOffsetX").floatValue = 120f;
            mapSO.FindProperty("itemSpacingY") .floatValue = 160f;

            var cn = AssetDatabase.LoadAssetAtPath<CountryNodeView>(PrefabCountryPath);
            var sn = AssetDatabase.LoadAssetAtPath<StageNodeView>(PrefabStagePath);
            if (cn != null) mapSO.FindProperty("countryPrefab").objectReferenceValue = cn;
            if (sn != null) mapSO.FindProperty("stagePrefab")  .objectReferenceValue = sn;
            mapSO.ApplyModifiedPropertiesWithoutUndo();

            // 4. SceneContext + Installers
            BuildSceneContext(canvas);

            // 5. Build Settings
            ApplyBuildSettings();

            UnityEditor.SceneManagement.EditorSceneManager
                .MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            Selection.activeGameObject = canvas;
            Debug.LogWarning("[StageMapUI] ✅ Сцена настроена. Назначь WorldMapConfig в StageMapViewInstaller, затем нажми Place Nodes на StageMapView.");
        }

        // ── Canvas ────────────────────────────────────────────────────────────
        private static GameObject BuildCanvas()
        {
            var go     = new GameObject("StageMapCanvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(RW, RH);
            scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight  = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return go;
        }

        // ── Background ────────────────────────────────────────────────────────
        private static void BuildBackground(GameObject parent)
        {
            var go        = UI("Background", parent);
            Stretch(go);
            var img       = go.AddComponent<Image>();
            img.color     = ColorBg;
            img.raycastTarget = false;
        }

        // ── ScrollRect ────────────────────────────────────────────────────────
        private static GameObject BuildScrollRect(GameObject parent)
        {
            var go = UI("MapScrollRect", parent);
            Stretch(go);

            var sr               = go.AddComponent<ScrollRect>();
            sr.horizontal        = false;
            sr.vertical          = true;
            sr.scrollSensitivity = 30f;
            sr.movementType      = ScrollRect.MovementType.Elastic;
            sr.elasticity        = 0.1f;
            sr.decelerationRate  = 0.135f;

            var vp    = UI("Viewport", go);
            Stretch(vp);
            var vpImg = vp.AddComponent<Image>();
            vpImg.color = Color.clear;
            vpImg.raycastTarget = false;
            var mask  = vp.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            var content = UI("Content", vp);
            var crt     = content.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0f, 0f);
            crt.anchorMax = new Vector2(1f, 0f);
            crt.pivot     = new Vector2(0.5f, 0f);
            crt.sizeDelta = new Vector2(0f, 4000f);
            crt.anchoredPosition = Vector2.zero;

            sr.viewport = vp.GetComponent<RectTransform>();
            sr.content  = crt;

            return go;
        }

        // ── LevelSelectPopup ──────────────────────────────────────────────────
        private static void BuildLevelSelectPopup(GameObject canvas)
        {
            var overlay    = UI("LevelSelectPopup", canvas);
            Stretch(overlay);
            var ovImg      = overlay.AddComponent<Image>();
            ovImg.color    = ColorOverlay;
            var cg         = overlay.AddComponent<CanvasGroup>();

            // клик на фон = закрыть
            overlay.AddComponent<Button>().transition = Selectable.Transition.None;

            // Панель
            var panel  = UI("PopupPanel", overlay);
            var prt    = panel.GetComponent<RectTransform>();
            prt.anchorMin        = new Vector2(0f, 0f);
            prt.anchorMax        = new Vector2(1f, 0f);
            prt.pivot            = new Vector2(0.5f, 0f);
            prt.sizeDelta        = new Vector2(0f, 340f);
            var pImg   = panel.AddComponent<Image>();
            pImg.color = ColorCard;

            // Заголовок
            var titleGo  = UI("StageNameLabel", panel);
            var trt      = titleGo.GetComponent<RectTransform>();
            trt.anchorMin        = new Vector2(0f, 1f);
            trt.anchorMax        = new Vector2(1f, 1f);
            trt.pivot            = new Vector2(0.5f, 1f);
            trt.anchoredPosition = new Vector2(0f, -22f);
            trt.sizeDelta        = new Vector2(-40f, 32f);
            var titleTmp      = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.text      = "Stage Name";
            titleTmp.fontSize  = 22f;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color     = ColorText;
            titleTmp.alignment = TextAlignmentOptions.Center;

            // 3 кнопки уровней
            var levelBtns  = new GameObject[3];
            var levelImgs  = new Image[3][];
            for (var i = 0; i < 3; i++)
                levelBtns[i] = BuildLevelButton(panel, i, out levelImgs[i]);

            // Кнопка закрыть
            var closeGo  = UI("CloseButton", panel);
            var closeRT  = closeGo.GetComponent<RectTransform>();
            closeRT.anchorMin        = new Vector2(1f, 1f);
            closeRT.anchorMax        = new Vector2(1f, 1f);
            closeRT.pivot            = new Vector2(1f, 1f);
            closeRT.anchoredPosition = new Vector2(-8f, -8f);
            closeRT.sizeDelta        = new Vector2(36f, 36f);
            var closeImg  = closeGo.AddComponent<Image>();
            closeImg.color = ColorBorder;
            var closeBtn  = closeGo.AddComponent<Button>();
            var xGo       = UI("X", closeGo);
            Stretch(xGo);
            var xTmp      = xGo.AddComponent<TextMeshProUGUI>();
            xTmp.text      = "×";
            xTmp.fontSize  = 22f;
            xTmp.color     = ColorText;
            xTmp.alignment = TextAlignmentOptions.Center;
            xTmp.raycastTarget = false;

            overlay.SetActive(false);

            // LevelSelectPopupView
            var popupView = overlay.AddComponent<LevelSelectPopupView>();
            var pso       = new SerializedObject(popupView);
            pso.FindProperty("_root")          .objectReferenceValue = overlay;
            pso.FindProperty("_canvasGroup")   .objectReferenceValue = cg;
            pso.FindProperty("_stageNameLabel").objectReferenceValue = titleTmp;
            pso.FindProperty("_closeButton")   .objectReferenceValue = closeBtn;

            var ep = pso.FindProperty("_levelEntries");
            ep.arraySize = 3;
            for (var i = 0; i < 3; i++)
            {
                var entry = ep.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("Button")
                     .objectReferenceValue = levelBtns[i].GetComponent<Button>();
                entry.FindPropertyRelative("LevelLabel")
                     .objectReferenceValue = levelBtns[i].transform.Find("Label")
                                                         ?.GetComponent<TextMeshProUGUI>();
                entry.FindPropertyRelative("LockOverlay")
                     .objectReferenceValue = levelBtns[i].transform.Find("Lock")?.gameObject;

                var sp = entry.FindPropertyRelative("Stars");
                sp.arraySize = 3;
                for (var s = 0; s < 3; s++)
                    sp.GetArrayElementAtIndex(s).objectReferenceValue = levelImgs[i][s];
            }
            pso.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject BuildLevelButton(
            GameObject parent, int idx, out Image[] starImages)
        {
            var go  = UI($"LevelButton_{idx}", parent);
            var rt  = go.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.05f, 1f);
            rt.anchorMax        = new Vector2(0.95f, 1f);
            rt.pivot            = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -88f - idx * 68f);
            rt.sizeDelta        = new Vector2(0f, 60f);
            go.AddComponent<Image>().color = ColorUnlocked;
            go.AddComponent<Button>();

            var lbl  = UI("Label", go);
            var lrt  = lbl.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0f, 0f);
            lrt.anchorMax = new Vector2(0.6f, 1f);
            lrt.offsetMin = new Vector2(16f, 0f);
            var lTmp      = lbl.AddComponent<TextMeshProUGUI>();
            lTmp.text      = $"Level {idx + 1}";
            lTmp.fontSize  = 16f;
            lTmp.color     = ColorText;
            lTmp.alignment = TextAlignmentOptions.MidlineLeft;
            lTmp.raycastTarget = false;

            var starsGo  = UI("Stars", go);
            var srt      = starsGo.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.62f, 0.15f);
            srt.anchorMax = new Vector2(0.98f, 0.85f);
            var hl = starsGo.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 4f; hl.childAlignment = TextAnchor.MiddleCenter;
            hl.childForceExpandWidth = true; hl.childControlWidth = true;
            hl.childControlHeight = true;

            starImages = new Image[3];
            for (var s = 0; s < 3; s++)
            {
                var sg = UI($"Star_{s}", starsGo);
                var si = sg.AddComponent<Image>();
                si.color = ColorStarEmpty;
                si.raycastTarget = false;
                sg.AddComponent<LayoutElement>().preferredWidth = 16f;
                starImages[s] = si;
            }

            var lockGo  = UI("Lock", go);
            Stretch(lockGo);
            lockGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
            var lkTmp   = UI("Icon", lockGo).AddComponent<TextMeshProUGUI>();
            Stretch(lkTmp.gameObject);
            lkTmp.text = "🔒"; lkTmp.fontSize = 20f;
            lkTmp.color = ColorTextMuted;
            lkTmp.alignment = TextAlignmentOptions.Center;
            lkTmp.raycastTarget = false;
            lockGo.SetActive(idx > 0);

            return go;
        }

        // ── CountryNode Prefab ────────────────────────────────────────────────
        private static void BuildCountryNodePrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<CountryNodeView>(PrefabCountryPath) != null) return;

            var go  = new GameObject("CountryNode");
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(CountryBtnW, CountryBtnH);

            var bg    = go.AddComponent<Image>();
            bg.color  = ColorAccent;

            var iconGo  = UI("Icon", go);
            var irt     = iconGo.GetComponent<RectTransform>();
            irt.anchorMin        = new Vector2(0f, 0.1f);
            irt.anchorMax        = new Vector2(0f, 0.9f);
            irt.pivot            = new Vector2(0f, 0.5f);
            irt.anchoredPosition = new Vector2(14f, 0f);
            irt.sizeDelta        = new Vector2(44f, 0f);
            var iconImg           = iconGo.AddComponent<Image>();
            iconImg.raycastTarget = false;

            var nameGo  = UI("NameLabel", go);
            var nrt     = nameGo.GetComponent<RectTransform>();
            nrt.anchorMin = Vector2.zero;
            nrt.anchorMax = Vector2.one;
            nrt.offsetMin = new Vector2(68f, 0f);
            nrt.offsetMax = new Vector2(-12f, 0f);
            var nameTmp      = nameGo.AddComponent<TextMeshProUGUI>();
            nameTmp.text      = "Country";
            nameTmp.fontSize  = 20f;
            nameTmp.fontStyle = FontStyles.Bold;
            nameTmp.color     = Color.white;
            nameTmp.alignment = TextAlignmentOptions.MidlineLeft;
            nameTmp.raycastTarget = false;

            var lockGo  = UI("LockOverlay", go);
            Stretch(lockGo);
            lockGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);
            lockGo.SetActive(false);

            // CountryNodeView — поля: _icon, _nameLabel, _lockOverlay, _background
            var view = go.AddComponent<CountryNodeView>();
            var vso  = new SerializedObject(view);
            vso.FindProperty("_icon")       .objectReferenceValue = iconImg;
            vso.FindProperty("_nameLabel")  .objectReferenceValue = nameTmp;
            vso.FindProperty("_lockOverlay").objectReferenceValue = lockGo;
            vso.FindProperty("_background") .objectReferenceValue = bg;
            vso.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(go, PrefabCountryPath);
            Object.DestroyImmediate(go);
            Debug.LogWarning($"[StageMapUI] CountryNode prefab → {PrefabCountryPath}");
        }

        // ── StageNode Prefab ──────────────────────────────────────────────────
        private static void BuildStageNodePrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<StageNodeView>(PrefabStagePath) != null) return;

            var go  = new GameObject("StageNode");
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(StageBtnW, StageBtnH);
            go.AddComponent<Image>().color = ColorUnlocked;
            go.AddComponent<Button>();

            var iconGo  = UI("Icon", go);
            var irt     = iconGo.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0.15f, 0.35f);
            irt.anchorMax = new Vector2(0.85f, 0.90f);
            var iconImg   = iconGo.AddComponent<Image>();
            iconImg.raycastTarget = false;

            var numGo  = UI("NumLabel", go);
            var nrt    = numGo.GetComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0f, 0.05f);
            nrt.anchorMax = new Vector2(1f, 0.38f);
            var numTmp    = numGo.AddComponent<TextMeshProUGUI>();
            numTmp.text = "1"; numTmp.fontSize = 11f;
            numTmp.color = ColorTextMuted;
            numTmp.alignment = TextAlignmentOptions.Center;
            numTmp.raycastTarget = false;

            var starsGo  = UI("Stars", go);
            var srt      = starsGo.GetComponent<RectTransform>();
            srt.anchorMin        = new Vector2(0.05f, 0f);
            srt.anchorMax        = new Vector2(0.95f, 0f);
            srt.pivot            = new Vector2(0.5f, 0f);
            srt.anchoredPosition = new Vector2(0f, 4f);
            srt.sizeDelta        = new Vector2(0f, 13f);
            var hl = starsGo.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 3f; hl.childAlignment = TextAnchor.MiddleCenter;
            hl.childForceExpandWidth = true; hl.childControlWidth = true;
            hl.childControlHeight = true;

            var starImages = new Image[3];
            for (var s = 0; s < 3; s++)
            {
                var sg = UI($"Star_{s}", starsGo);
                var si = sg.AddComponent<Image>();
                si.color = ColorStarEmpty; si.raycastTarget = false;
                sg.AddComponent<LayoutElement>().preferredWidth = 13f;
                starImages[s] = si;
            }

            var lockGo  = UI("LockOverlay", go);
            Stretch(lockGo);
            lockGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
            lockGo.SetActive(false);

            // StageNodeView — поля: _button, _icon, _lockOverlay, _stars
            var view = go.AddComponent<StageNodeView>();
            var vso  = new SerializedObject(view);
            vso.FindProperty("_button")     .objectReferenceValue = go.GetComponent<Button>();
            vso.FindProperty("_icon")       .objectReferenceValue = iconImg;
            vso.FindProperty("_lockOverlay").objectReferenceValue = lockGo;
            var sp = vso.FindProperty("_stars");
            sp.arraySize = 3;
            for (var s = 0; s < 3; s++)
                sp.GetArrayElementAtIndex(s).objectReferenceValue = starImages[s];
            vso.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(go, PrefabStagePath);
            Object.DestroyImmediate(go);
            Debug.LogWarning($"[StageMapUI] StageNode prefab → {PrefabStagePath}");
        }

        // ── SceneContext + Installers ─────────────────────────────────────────
        private static void BuildSceneContext(GameObject canvas)
        {
            // Удаляем старый если есть
            var old = GameObject.Find("SceneContext");
            if (old != null) Object.DestroyImmediate(old);

            var go = new GameObject("SceneContext");
            var sc = go.AddComponent<SceneContext>();

            var mapInstaller     = go.AddComponent<StageMapInstaller>();
            var mapViewInstaller = go.AddComponent<StageMapViewInstaller>();

            // Назначаем WorldMapConfig в StageMapViewInstaller
            var worldMapConfig = AssetDatabase.LoadAssetAtPath<Match3.Configs.WorldMapConfig>(WorldMapConfigPath);
            if (worldMapConfig != null)
            {
                var vso = new SerializedObject(mapViewInstaller);
                var p   = vso.FindProperty("_worldMapConfig");
                if (p != null)
                {
                    p.objectReferenceValue = worldMapConfig;
                    vso.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            else
            {
                Debug.LogWarning($"[StageMapUI] WorldMapConfig не найден по пути: {WorldMapConfigPath}. Назначь вручную в StageMapViewInstaller.");
            }

            // Добавляем инсталлеры в SceneContext.Installers
            var scSO        = new SerializedObject(sc);
            var installersProp = scSO.FindProperty("Installers");
            if (installersProp != null)
            {
                installersProp.arraySize = 2;
                installersProp.GetArrayElementAtIndex(0).objectReferenceValue = mapInstaller;
                installersProp.GetArrayElementAtIndex(1).objectReferenceValue = mapViewInstaller;
                scSO.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(go);
            Debug.LogWarning("[StageMapUI] SceneContext создан с StageMapInstaller + StageMapViewInstaller.");
        }

        // ── Build Settings ────────────────────────────────────────────────────
        private static void ApplyBuildSettings()
        {
            var scenes = new[]
            {
                "Assets/Match3/Scenes/Bootstrap.unity",
                "Assets/Match3/Scenes/StageMap.unity",
                "Assets/Match3/Scenes/Game.unity",
            };

            var entries = new EditorBuildSettingsScene[scenes.Length];
            for (var i = 0; i < scenes.Length; i++)
                entries[i] = new EditorBuildSettingsScene(scenes[i], true);

            EditorBuildSettings.scenes = entries;
            Debug.LogWarning("[StageMapUI] Build Settings: Bootstrap(0) StageMap(1) Game(2)");
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static GameObject UI(string name, GameObject parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private static void Stretch(GameObject go)
        {
            var rt      = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static bool ConfirmIfExists(string name)
        {
            var old = GameObject.Find(name);
            if (old == null) return true;
            if (!EditorUtility.DisplayDialog("Match3 StageMap UI",
                $"{name} уже существует. Пересоздать?", "Да", "Отмена"))
                return false;
            Object.DestroyImmediate(old);
            return true;
        }

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }
    }
}

#endif
