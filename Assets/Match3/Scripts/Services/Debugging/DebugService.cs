#nullable enable

using System.Collections.Generic;
using R3;

namespace Match3.Services.Debugging
{
    /// <summary>
    /// Реестр команд дебаг-панели. Не использовать в продакшн-сборках.
    /// Любой сервис/презентер может зарегистрировать свою команду через Register.
    /// </summary>
    public sealed class DebugService
    {
        private readonly List<DebugAction> _actions = new();

        public ReactiveProperty<bool> IsVisible { get; } = new(false);

        public IReadOnlyList<DebugAction> Actions => _actions;

        public void Register(string category, string name, System.Action execute)
        {
            _actions.Add(new DebugAction(category, name, execute));
        }

        /// <summary>
        /// Очищает реестр. Вызывается перед перерегистрацией команд
        /// при (пере)инициализации сцены — чтобы не плодить дубликаты.
        /// </summary>
        public void Clear() => _actions.Clear();

        public void Toggle() => IsVisible.Value = !IsVisible.Value;
    }
}
