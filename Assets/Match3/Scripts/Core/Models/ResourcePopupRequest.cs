#nullable enable

using System;
using Match3.Services.Ads;
using R3;
using UnityEngine;

namespace Match3.Core.Models
{
    /// <summary>
    /// Запрос на открытие ResourcePopupView.
    /// Caller формирует запрос, публикует через ResourcePopupService,
    /// и подписывается на OnSuccess чтобы реагировать после получения ресурса.
    /// </summary>
    public sealed class ResourcePopupRequest
    {
        public string  Title { get; set; } = string.Empty;
        public Sprite? Icon  { get; set; }

        // Персонаж
        public Sprite? CharacterSprite  { get; set; }
        public string  CharacterDialog  { get; set; } = string.Empty;
        public string  DialogLocaleId   { get; set; } = string.Empty;
        // TODO: при подключении локализации читать по DialogLocaleId вместо CharacterDialog

        // Награды — иконки и количество (что получит игрок)
        public RewardData[] Rewards     { get; set; } = Array.Empty<RewardData>();
        public Sprite?[]    RewardIcons { get; set; } = Array.Empty<Sprite?>();

        // Кнопка рекламы
        public AdPlacementId AdPlacementId  { get; set; }
        public string        AdButtonLabel  { get; set; } = "👁 Смотреть рекламу";

        // Кнопка монет (null = скрыть кнопку)
        public int?   CoinPrice       { get; set; }
        public string CoinButtonLabel { get; set; } = "💰 Купить";

        private readonly Subject<Unit> _onSuccess = new();

        /// <summary>Caller подписывается сюда чтобы узнать об успехе.</summary>
        public Observable<Unit> OnSuccess => _onSuccess;

        /// <summary>Вызывается только из ResourcePopupView после успешного получения.</summary>
        internal void NotifySuccess()
        {
            _onSuccess.OnNext(Unit.Default);
            _onSuccess.OnCompleted();
        }
    }
}
