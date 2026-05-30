#nullable enable

using System;
using Match3.Core.Models;
using R3;

namespace Match3.Services
{
    /// <summary>
    /// Медиатор для открытия ResourcePopupView.
    /// Живёт в ProjectContext.
    ///
    /// Любой Presenter публикует запрос через Request().
    /// ResourcePopupView подписан на OnRequest и показывает себя автономно.
    /// </summary>
    public sealed class ResourcePopupService : IDisposable
    {
        private readonly Subject<ResourcePopupRequest> _onRequest = new();

        public Observable<ResourcePopupRequest> OnRequest => _onRequest;

        public void Request(ResourcePopupRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            _onRequest.OnNext(request);
        }

        public void Dispose() => _onRequest.Dispose();
    }
}
