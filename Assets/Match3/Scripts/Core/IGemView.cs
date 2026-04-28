#nullable enable

using Match3.Core.Enums;
using UnityEngine;

namespace Match3.Core
{
    public interface IGemView
    {
        NodeType     GemType      { get; }
        SuperGemType SuperGemType { get; }
        Vector2Int   CurrentIndex { get; }
        GemMatch?    CurrentMatch { get; set; }
        GemState     CurrentState { get; }
        bool         CanMove      { get; }

        void Init(Vector2Int index, NodeType type);
        void MoveTo(Vector2Int newIndex);
        void SetBusy();
        void SetStill();
        void MarkDestroyed();
        void SetSuperGemType(SuperGemType superGemType);

        /// <summary>
        /// Меняет тип фишки без пересоздания объекта — используется при shuffle.
        /// </summary>
        void SetGemType(NodeType type);
    }
}
