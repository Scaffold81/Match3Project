#nullable enable

using Match3.Views;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.Editor
{
    public static class UISetupEditor
    {
        private const float ScreenWidth  = 390f;
        private const float ScreenHeight = 844f;
        private const float HeaderHeight = 120f;
        private const float BoardPadding = 20f;
        private const float BoardSize    = ScreenWidth - BoardPadding * 2f;

        private static readonly Color ColorBg           = HexColor("1A1A2E");
        private static readonly Color ColorHeader       = HexColor("16213E");
        private static readonly Color ColorBoard        = HexColor("0F3460");
        private static readonly Color ColorBoardBorder  = HexColor("E94560");
        private static readonly Color ColorOverlay      = new(0f, 0f, 0f, 0.85f);
        private static readonly Color ColorWin          = HexColor("0F3460");
        private static readonly Color ColorLose         = HexColor("2D1B33");
        private static readonly Color ColorBtnPrimary   = HexColor("E94560");
        private static readonly Color ColorBtnSecondary = HexColor("533483");
        private static readonly Color ColorTextWhite    = HexColor("EAEAEA");
        private static readonly Color ColorTextAccent   = HexColor("E94560");
        private static readonly Color ColorLayerCell    = new(1f, 0.84f, 0f, 0.35f);

        // internal — чтобы SerializedObjectHelper мог использовать
        internal struct ObjectiveEntryViewData
        {
            public GameObject      Root;
            public Image           Icon;
            public TextMeshProUGUI CountText;
            public GameObject      CompletedMark;
        }

        [MenuItem("Match3/Setup UI Scene")]
        public static void SetupUI()
        {
            var old = GameObject.Find("Canvas");
            if (old != null)
            {
                if (!EditorUtility.DisplayDialog("Match3 UI Setup",
                    "Canvas уже существует. Пересоздать?", "Да", "Отмена"))
                    return;
                Object.DestroyImmediate(old);
            }

            var canvas = CreateCanvas();
            CreateBackground(canvas);

            var header = CreateHeader(canvas);
            CreateMoveCounter(header);
            CreateObjectivePanel(header);

            CreateBoardArea(canvas);
            CreateResultPanel(canvas);
            CreateInputHandler(canvas);
            EnsureLayerCellPrefab();

            Selection.activeGameObject = canvas;
            EditorUtility.SetDirty(canvas);

            Debug.LogWarning("Match3 UI: иерархия создана. Назначь LayerView.LayerCellPrefab вручную если не назначился.");
        }

        // ── Canvas ───────────────────────────────────────────────────────
        private static GameObject CreateCanvas()
        {
            var go     = new GameObject("Canvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode          = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution  = new Vector2(ScreenWidth, ScreenHeight);
            scaler.screenMatchMode      = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight   = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return go;
        }

        // ── Background ───────────────────────────────────────────────────
        private static void CreateBackground(GameObject parent)
        {
            var go  = CreateUIObject("Background", parent);
            StretchFull(go);
            var img = go.AddComponent<Image>();
            img.color         = ColorBg;
            img.raycastTarget = false;
        }

        // ── Header ───────────────────────────────────────────────────────
        private static GameObject CreateHeader(GameObject parent)
        {
            var go = CreateUIObject("Header", parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(0f, -HeaderHeight);
            rt.offsetMax = Vector2.zero;

            var img = go.AddComponent<Image>();
            img.color         = ColorHeader;
            img.raycastTarget = false;

            var line     = CreateUIObject("AccentLine", go);
            var lrt      = line.GetComponent<RectTransform>();
            lrt.anchorMin        = new Vector2(0f, 0f);
            lrt.anchorMax        = new Vector2(1f, 0f);
            lrt.pivot            = new Vector2(0.5f, 0f);
            lrt.sizeDelta        = new Vector2(0f, 2f);
            lrt.anchoredPosition = Vector2.zero;
            var limg             = line.AddComponent<Image>();
            limg.color           = ColorBoardBorder;
            limg.raycastTarget   = false;

            return go;
        }

        // ── Move Counter ─────────────────────────────────────────────────
        private static void CreateMoveCounter(GameObject header)
        {
            var go = CreateUIObject("MoveCounterPanel", header);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0.35f, 1f);
            rt.offsetMin = new Vector2(12f, 8f);
            rt.offsetMax = new Vector2(-4f, -8f);

            // Иконка
            var icon    = CreateUIObject("Icon", go);
            var irt     = icon.GetComponent<RectTransform>();
            irt.anchorMin        = new Vector2(0f, 0.5f);
            irt.anchorMax        = new Vector2(0f, 0.5f);
            irt.pivot            = new Vector2(0f, 0.5f);
            irt.anchoredPosition = Vector2.zero;
            irt.sizeDelta        = new Vector2(32f, 32f);
            var iconImg          = icon.AddComponent<Image>();
            iconImg.color        = ColorTextAccent;
            iconImg.raycastTarget = false;

            // Число ходов
            var numGo    = CreateUIObject("MovesLeftText", go);
            var nrt      = numGo.GetComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0f, 0f);
            nrt.anchorMax = new Vector2(1f, 1f);
            nrt.offsetMin = new Vector2(40f, 0f);
            nrt.offsetMax = Vector2.zero;
            var tmp       = numGo.AddComponent<TextMeshProUGUI>();
            tmp.text      = "30";
            tmp.fontSize  = 36f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color     = ColorTextWhite;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;

            // Бесконечность
            var infGo    = CreateUIObject("UnlimitedIndicator", go);
            var irt2     = infGo.GetComponent<RectTransform>();
            irt2.anchorMin = new Vector2(0f, 0f);
            irt2.anchorMax = new Vector2(1f, 1f);
            irt2.offsetMin = new Vector2(40f, 0f);
            irt2.offsetMax = Vector2.zero;
            var infTmp    = infGo.AddComponent<TextMeshProUGUI>();
            infTmp.text      = "inf";
            infTmp.fontSize  = 36f;
            infTmp.fontStyle = FontStyles.Bold;
            infTmp.color     = ColorTextAccent;
            infTmp.alignment = TextAlignmentOptions.MidlineLeft;

            var view = go.AddComponent<MoveCounterView>();
            SerializedObjectHelper.SetField(view, "_movesLeftText",      numGo.GetComponent<TextMeshProUGUI>());
            SerializedObjectHelper.SetField(view, "_unlimitedIndicator", infGo);
        }

        // ── Objective Panel ──────────────────────────────────────────────
        private static void CreateObjectivePanel(GameObject header)
        {
            var go = CreateUIObject("ObjectivePanel", header);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.35f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(4f, 8f);
            rt.offsetMax = new Vector2(-12f, -8f);

            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing                = 8f;
            layout.childAlignment         = TextAnchor.MiddleRight;
            layout.childForceExpandWidth  = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth      = false;
            layout.childControlHeight     = false;

            const int maxObjectives = 4;
            var entries = new ObjectiveEntryViewData[maxObjectives];
            for (var i = 0; i < maxObjectives; i++)
                entries[i] = CreateObjectiveEntry(go, i);

            var view = go.AddComponent<ObjectiveView>();
            SerializedObjectHelper.SetObjectiveEntries(view, entries);
        }

        private static ObjectiveEntryViewData CreateObjectiveEntry(GameObject parent, int index)
        {
            var root = CreateUIObject($"ObjectiveEntry_{index}", parent);
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(72f, 72f);

            var card          = root.AddComponent<Image>();
            card.color        = new Color(1f, 1f, 1f, 0.08f);
            card.raycastTarget = false;

            // Иконка фишки
            var iconGo  = CreateUIObject("Icon", root);
            var irt     = iconGo.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0.1f, 0.3f);
            irt.anchorMax = new Vector2(0.9f, 0.9f);
            irt.offsetMin = irt.offsetMax = Vector2.zero;
            var iconImg          = iconGo.AddComponent<Image>();
            iconImg.color        = Color.white;
            iconImg.raycastTarget = false;

            // Счётчик
            var countGo  = CreateUIObject("CountText", root);
            var crt      = countGo.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0f, 0f);
            crt.anchorMax = new Vector2(1f, 0.35f);
            crt.offsetMin = crt.offsetMax = Vector2.zero;
            var countTmp      = countGo.AddComponent<TextMeshProUGUI>();
            countTmp.text      = "0";
            countTmp.fontSize  = 18f;
            countTmp.fontStyle = FontStyles.Bold;
            countTmp.color     = ColorTextWhite;
            countTmp.alignment = TextAlignmentOptions.Center;

            // Галочка
            var checkGo  = CreateUIObject("CompletedMark", root);
            var chrt     = checkGo.GetComponent<RectTransform>();
            chrt.anchorMin = Vector2.zero;
            chrt.anchorMax = Vector2.one;
            chrt.offsetMin = chrt.offsetMax = Vector2.zero;
            var checkImg          = checkGo.AddComponent<Image>();
            checkImg.color        = new Color(0.2f, 0.9f, 0.4f, 0.9f);
            checkImg.raycastTarget = false;
            var checkTmp      = checkGo.AddComponent<TextMeshProUGUI>();
            checkTmp.text      = "V";
            checkTmp.fontSize  = 32f;
            checkTmp.fontStyle = FontStyles.Bold;
            checkTmp.color     = Color.white;
            checkTmp.alignment = TextAlignmentOptions.Center;
            checkGo.SetActive(false);

            return new ObjectiveEntryViewData
            {
                Root          = root,
                Icon          = iconImg,
                CountText     = countTmp,
                CompletedMark = checkGo,
            };
        }

        // ── Board Area ───────────────────────────────────────────────────
        private static void CreateBoardArea(GameObject parent)
        {
            var border    = CreateUIObject("BoardBorder", parent);
            var brt       = border.GetComponent<RectTransform>();
            brt.anchorMin        = new Vector2(0.5f, 0.5f);
            brt.anchorMax        = new Vector2(0.5f, 0.5f);
            brt.pivot            = new Vector2(0.5f, 0.5f);
            brt.anchoredPosition = new Vector2(0f, -20f);
            brt.sizeDelta        = new Vector2(BoardSize + 8f, BoardSize + 8f);
            var borderImg        = border.AddComponent<Image>();
            borderImg.color      = ColorBoardBorder;
            borderImg.raycastTarget = false;

            var board    = CreateUIObject("Board", border);
            var rt       = board.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(4f, 4f);
            rt.offsetMax = new Vector2(-4f, -4f);
            var boardImg         = board.AddComponent<Image>();
            boardImg.color       = ColorBoard;
            boardImg.raycastTarget = false;

            // LayerContainer (под фишками)
            var layerContainer = CreateUIObject("LayerContainer", board);
            StretchFull(layerContainer);
            var layerView = layerContainer.AddComponent<LayerView>();

            var layerCells = CreateUIObject("LayerCells", layerContainer);
            var lcrt       = layerCells.GetComponent<RectTransform>();
            lcrt.anchorMin = Vector2.zero;
            lcrt.anchorMax = Vector2.one;
            lcrt.offsetMin = lcrt.offsetMax = Vector2.zero;
            lcrt.pivot     = new Vector2(0f, 1f);
            SerializedObjectHelper.SetField(layerView, "_layerContainer", lcrt);

            // GemContainer (поверх)
            var gemContainer = CreateUIObject("GemContainer", board);
            var gcrt         = gemContainer.GetComponent<RectTransform>();
            gcrt.anchorMin = Vector2.zero;
            gcrt.anchorMax = Vector2.one;
            gcrt.offsetMin = gcrt.offsetMax = Vector2.zero;
            gcrt.pivot     = new Vector2(0f, 1f);

            var boardView = board.AddComponent<BoardView>();
            SerializedObjectHelper.SetField(boardView, "_gemContainer",  gcrt);
            SerializedObjectHelper.SetField(boardView, "_cellContainer", gcrt);
        }

        // ── Result Panel ─────────────────────────────────────────────────
        private static void CreateResultPanel(GameObject parent)
        {
            var overlay    = CreateUIObject("ResultOverlay", parent);
            StretchFull(overlay);
            var overlayImg = overlay.AddComponent<Image>();
            overlayImg.color        = ColorOverlay;
            overlayImg.raycastTarget = true;
            overlay.SetActive(false);

            var card = CreateUIObject("ResultCard", overlay);
            var crt  = card.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.1f, 0.3f);
            crt.anchorMax = new Vector2(0.9f, 0.7f);
            crt.offsetMin = crt.offsetMax = Vector2.zero;

            var winPanel   = CreateResultCard(card, "WinPanel",  ColorWin,  "Level Complete!", ColorTextWhite);
            var nextBtn    = CreateButton(winPanel,  "NextLevelButton", "Next ->",      ColorBtnPrimary);
            var losePanel  = CreateResultCard(card, "LosePanel", ColorLose, "Try Again",      ColorTextWhite);
            var restartBtn = CreateButton(losePanel, "RestartButton",   "Restart",      ColorBtnSecondary);

            winPanel .SetActive(false);
            losePanel.SetActive(false);

            var view = overlay.AddComponent<LevelResultView>();
            SerializedObjectHelper.SetField(view, "_winPanel",        winPanel);
            SerializedObjectHelper.SetField(view, "_losePanel",       losePanel);
            SerializedObjectHelper.SetField(view, "_restartButton",   restartBtn.GetComponent<Button>());
            SerializedObjectHelper.SetField(view, "_nextLevelButton", nextBtn.GetComponent<Button>());
        }

        private static GameObject CreateResultCard(
            GameObject parent, string name, Color bgColor, string titleText, Color titleColor)
        {
            var go  = CreateUIObject(name, parent);
            StretchFull(go);
            var img = go.AddComponent<Image>();
            img.color = bgColor;

            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.padding                = new RectOffset(24, 24, 32, 32);
            layout.spacing                = 20f;
            layout.childAlignment         = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth  = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth      = true;
            layout.childControlHeight     = false;

            var titleGo = CreateUIObject("Title", go);
            titleGo.AddComponent<LayoutElement>().preferredHeight = 60f;
            var titleTmp      = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.text      = titleText;
            titleTmp.fontSize  = 28f;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color     = titleColor;
            titleTmp.alignment = TextAlignmentOptions.Center;

            return go;
        }

        private static GameObject CreateButton(
            GameObject parent, string name, string label, Color color)
        {
            var go  = CreateUIObject(name, parent);
            var le  = go.AddComponent<LayoutElement>();
            le.preferredHeight = 56f;

            var img = go.AddComponent<Image>();
            img.color = color;

            var btn = go.AddComponent<Button>();
            var cb  = btn.colors;
            cb.highlightedColor = Color.Lerp(color, Color.white, 0.2f);
            cb.pressedColor     = Color.Lerp(color, Color.black, 0.2f);
            btn.colors          = cb;

            var outline = go.AddComponent<Outline>();
            outline.effectColor    = new Color(1f, 1f, 1f, 0.15f);
            outline.effectDistance = new Vector2(1f, -1f);

            var textGo = CreateUIObject("Label", go);
            StretchFull(textGo);
            var tmp           = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text           = label;
            tmp.fontSize       = 20f;
            tmp.fontStyle      = FontStyles.Bold;
            tmp.color          = Color.white;
            tmp.alignment      = TextAlignmentOptions.Center;
            tmp.raycastTarget  = false;

            return go;
        }

        // ── InputHandler ─────────────────────────────────────────────────
        private static void CreateInputHandler(GameObject parent)
        {
            var go  = CreateUIObject("InputHandler", parent);
            StretchFull(go);
            var img = go.AddComponent<Image>();
            img.color        = Color.clear;
            img.raycastTarget = true;
            go.AddComponent<Match3.Controllers.InputController>();
        }

        // ── Layer Cell Prefab ────────────────────────────────────────────
        private static void EnsureLayerCellPrefab()
        {
            const string path = "Assets/Match3/Prefabs/LayerCell.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

            System.IO.Directory.CreateDirectory("Assets/Match3/Prefabs");

            var go  = new GameObject("LayerCell");
            var rt  = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(100f, 100f);
            rt.pivot     = new Vector2(0f, 1f);

            var img = go.AddComponent<Image>();
            img.color        = ColorLayerCell;
            img.raycastTarget = false;

            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);

            Debug.LogWarning($"Match3 UI: LayerCell prefab -> {path}");
        }

        // ── Helpers ──────────────────────────────────────────────────────
        private static GameObject CreateUIObject(string name, GameObject parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private static void StretchFull(GameObject go)
        {
            var rt      = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static Color HexColor(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }
    }

    // ── SerializedObject Helper ──────────────────────────────────────────
    internal static class SerializedObjectHelper
    {
        public static void SetField(Object target, string fieldName, Object value)
        {
            var so   = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"Field '{fieldName}' not found on {target.GetType().Name}");
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetObjectiveEntries(
            ObjectiveView view,
            UISetupEditor.ObjectiveEntryViewData[] entries)
        {
            var so   = new SerializedObject(view);
            var prop = so.FindProperty("_entries");
            if (prop == null) return;

            prop.arraySize = entries.Length;
            for (var i = 0; i < entries.Length; i++)
            {
                var elem = prop.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("_root")         .objectReferenceValue = entries[i].Root;
                elem.FindPropertyRelative("_icon")         .objectReferenceValue = entries[i].Icon;
                elem.FindPropertyRelative("_countText")    .objectReferenceValue = entries[i].CountText;
                elem.FindPropertyRelative("_completedMark").objectReferenceValue = entries[i].CompletedMark;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
