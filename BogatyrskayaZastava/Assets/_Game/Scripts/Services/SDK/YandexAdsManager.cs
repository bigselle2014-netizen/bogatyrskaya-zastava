using System;
using UnityEngine;

namespace BogatyrskayaZastava.Core
{
    // ─────────────────────────────────────────────────────────────────
    // INTERFACE (registrable via ServiceLocator)
    // ─────────────────────────────────────────────────────────────────

    public interface IRewardedAdManager
    {
        void Initialize(string blockId);
        void ShowRewarded(Action<bool> onComplete);
        bool IsRewardedReady();
        void SetCooldown(float seconds);
        float GetRemainingCooldown();
        void PreloadNext();
    }

    // ─────────────────────────────────────────────────────────────────
    // STUB IMPLEMENTATION — Yandex Mobile Ads SDK v7
    //
    // TODO: Заменить на реальный Yandex Mobile Ads SDK v7
    // Docs: https://yandex.ru/dev/mobile-ads/doc/intro/about.html
    //
    // Rewarded Video — единственный формат в MVP. Никаких interstitial.
    // Лимит: 1 показ за 600 сек (10 мин) — захардкожен, не Remote Config.
    // Точки показа:
    //   - x2 ресурсы после уровня (добровольно)
    //   - Дополнительная попытка при поражении
    //   - Ускорение idle
    //
    // Регистрация: ServiceLocator.Register<IRewardedAdManager>(new YandexAdsManager());
    // ─────────────────────────────────────────────────────────────────

    public class YandexAdsManager : IRewardedAdManager
    {
        private const float DEFAULT_COOLDOWN_SECONDS = 600f; // 10 min

        private bool _initialized;
        private bool _adLoaded;
        private float _cooldownSeconds = DEFAULT_COOLDOWN_SECONDS;
        private float _lastShowTimestamp = -DEFAULT_COOLDOWN_SECONDS; // allow first show immediately

        public void Initialize(string blockId)
        {
            // TODO: MobileAds.Initialize();
            // TODO: _rewardedAd = new RewardedAd(blockId);
            // TODO: _rewardedAd.OnAdLoaded += OnAdLoaded;
            // TODO: _rewardedAd.OnRewarded += OnRewarded;
            // TODO: _rewardedAd.RequestAd();

            _initialized = true;
            _adLoaded = true; // stub: always ready
            Debug.Log($"[YandexAds STUB] Initialized with blockId: {blockId}");
        }

        public void ShowRewarded(Action<bool> onComplete)
        {
            if (!_initialized)
            {
                Debug.LogWarning("[YandexAds STUB] Not initialized.");
                onComplete?.Invoke(false);
                return;
            }

            if (!IsRewardedReady())
            {
                float remaining = GetRemainingCooldown();
                Debug.Log($"[YandexAds STUB] Cooldown active. {remaining:F0}s remaining.");
                onComplete?.Invoke(false);
                return;
            }

            if (!_adLoaded)
            {
                Debug.Log("[YandexAds STUB] Ad not loaded. Preloading...");
                PreloadNext();
                onComplete?.Invoke(false);
                return;
            }

            // TODO: _rewardedAd.Show();
            // TODO: in OnRewarded callback → onComplete(true)
            // TODO: in OnAdFailedToShow → onComplete(false)

            _lastShowTimestamp = Time.realtimeSinceStartup;
            _adLoaded = false;
            Debug.Log("[YandexAds STUB] Rewarded shown → user rewarded (stub)");

            if (ServiceLocator.TryGet<IAnalyticsService>(out var analytics))
            {
                analytics.LogEvent(AnalyticsEvents.AD_REVENUE,
                    new System.Collections.Generic.Dictionary<string, object>
                    {
                        { "ad_type", "rewarded" },
                        { "network", "yandex" }
                    });
            }

            onComplete?.Invoke(true);

            PreloadNext();
        }

        public bool IsRewardedReady()
        {
            if (!_initialized) return false;

            float elapsed = Time.realtimeSinceStartup - _lastShowTimestamp;
            return elapsed >= _cooldownSeconds && _adLoaded;
        }

        public void SetCooldown(float seconds)
        {
            _cooldownSeconds = Mathf.Max(0f, seconds);
            Debug.Log($"[YandexAds STUB] Cooldown set: {_cooldownSeconds}s");
        }

        public float GetRemainingCooldown()
        {
            float elapsed = Time.realtimeSinceStartup - _lastShowTimestamp;
            return Mathf.Max(0f, _cooldownSeconds - elapsed);
        }

        public void PreloadNext()
        {
            // TODO: _rewardedAd.RequestAd();
            // Fill rate Yandex = 85-95%, preload снижает latency
            _adLoaded = true; // stub
            Debug.Log("[YandexAds STUB] Preloading next rewarded ad...");
        }
    }
}
