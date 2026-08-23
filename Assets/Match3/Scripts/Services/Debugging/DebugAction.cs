#nullable enable

using System;

namespace Match3.Services.Debugging
{
    /// <summary>
    /// Одна команда дебаг-панели: подпись, категория и действие для выполнения.
    /// </summary>
    public readonly struct DebugAction
    {
        public string Category { get; }
        public string Name     { get; }
        public Action Execute  { get; }

        public DebugAction(string category, string name, Action execute)
        {
            Category = category;
            Name     = name;
            Execute  = execute;
        }
    }
}
