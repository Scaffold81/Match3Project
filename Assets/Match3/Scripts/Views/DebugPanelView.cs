#nullable enable

using System.Collections.Generic;
using Match3.Services.Debugging;
using R3;
using UnityEngine;

namespace Match3.Views
{
    /// <summary>
    /// Отрисовка дебаг-панели через OnGUI. Не содержит бизнес-логики —
    /// список команд и видимость приходят от DebugPresenter,
    /// клики по кнопкам публикуются наружу через OnActionClicked.
    /// </summary>
    public sealed class DebugPanelView : MonoBehaviour
    {
        // Ширина/отступы/шрифты заданы в "виртуальных" пикселях референсного
        // разрешения — реальный вывод масштабируется под Screen.width в OnGUI.
        private const float ReferenceWidth = 400f;
        private const float PanelWidth     = 320f;
        private const float RowHeight      = 64f;
        private const int   FontSize       = 24;

        private readonly Subject<int> _onActionClicked = new();
        public Observable<int> OnActionClicked => _onActionClicked;

        private bool _isVisible;
        private IReadOnlyList<DebugAction> _actions = System.Array.Empty<DebugAction>();
        private Vector2 _scroll;
        private GUIStyle? _labelStyle;
        private GUIStyle? _buttonStyle;

        public void SetVisible(bool visible) => _isVisible = visible;

        public void SetActions(IReadOnlyList<DebugAction> actions) => _actions = actions;

        private void OnGUI()
        {
            if (!_isVisible) return;

            EnsureStyles();

            var scale = Screen.width / ReferenceWidth;
            GUIUtility.ScaleAroundPivot(new Vector2(scale, scale), Vector2.zero);

            var virtualScreenHeight = Screen.height / scale;
            var height = Mathf.Min(virtualScreenHeight * 0.8f, 80f + _actions.Count * RowHeight);
            var rect   = new Rect(10f, 10f, PanelWidth, height);

            GUILayout.BeginArea(rect, GUI.skin.box);
            GUILayout.Label("Debug Panel", _labelStyle);
            _scroll = GUILayout.BeginScrollView(_scroll);

            string? currentCategory = null;
            for (var i = 0; i < _actions.Count; i++)
            {
                var action = _actions[i];
                if (action.Category != currentCategory)
                {
                    currentCategory = action.Category;
                    GUILayout.Label($"— {currentCategory} —", _labelStyle);
                }

                if (GUILayout.Button(action.Name, _buttonStyle, GUILayout.Height(RowHeight - 8f)))
                    _onActionClicked.OnNext(i);
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            if (_labelStyle != null) return;

            _labelStyle = new GUIStyle(GUI.skin.label) { fontSize = FontSize };
            _buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = FontSize };
        }

        private void OnDestroy() => _onActionClicked.Dispose();
    }
}
