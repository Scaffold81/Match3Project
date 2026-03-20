#nullable enable

using System;
using Match3.Core.Enums;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.Views
{
    public sealed class ObjectiveView : MonoBehaviour
    {
        [SerializeField] private ObjectiveEntryView[] _entries = Array.Empty<ObjectiveEntryView>();

        public void SetupObjectives(NodeType[] nodeTypes, int[] totals, Sprite?[] icons)
        {
            for (var i = 0; i < _entries.Length; i++)
            {
                if (i < nodeTypes.Length)
                {
                    _entries[i].gameObject.SetActive(true);
                    _entries[i].Setup(icons[i], totals[i]);
                }
                else
                {
                    _entries[i].gameObject.SetActive(false);
                }
            }
        }

        public void UpdateProgress(int index, int collected, int required)
        {
            if (index < 0 || index >= _entries.Length) return;
            _entries[index].UpdateProgress(collected, required);
        }

        public void MarkCompleted(int index)
        {
            if (index < 0 || index >= _entries.Length) return;
            _entries[index].MarkCompleted();
        }
    }

    [Serializable]
    public sealed class ObjectiveEntryView
    {
        [SerializeField] private GameObject _root = null!;
        [SerializeField] private Image _icon = null!;
        [SerializeField] private TMP_Text _countText = null!;
        [SerializeField] private GameObject _completedMark = null!;

        public GameObject gameObject => _root;

        public void Setup(Sprite? icon, int required)
        {
            _icon.sprite = icon;
            _countText.text = required.ToString();
            _completedMark.SetActive(false);
        }

        public void UpdateProgress(int collected, int required)
        {
            var remaining = Math.Max(0, required - collected);
            _countText.text = remaining.ToString();
        }

        public void MarkCompleted()
        {
            _countText.gameObject.SetActive(false);
            _completedMark.SetActive(true);
        }
    }
}
