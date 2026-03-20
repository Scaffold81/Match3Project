#nullable enable

using System;
using Match3.Core.Enums;

namespace Match3.Core.Models
{
    [Serializable]
    public sealed class ObjectiveData
    {
        public NodeType nodeType;
        public int count;
    }
}
