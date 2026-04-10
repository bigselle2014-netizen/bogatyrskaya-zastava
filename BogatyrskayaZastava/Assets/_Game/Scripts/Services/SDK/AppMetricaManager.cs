using System;
using System.Collections.Generic;
using UnityEngine;

namespace BogatyrskayaZastava.Core
{
    // ─────────────────────────────────────────────────────────────────
    // INTERFACE
    // ─────────────────────────────────────────────────────────────────

    public interface IAnalyticsManager
    {
        void Initialize(string apiKey);
        void TrackEvent(string name, Dictionary<string, string> parameters = null);
        void TrackRevenue(string productId, decimal amount, string currency);
        void SetUserProperty(string key, string value);
        void SetUserId(string userId);
        void Flush();
    }

    // ─────────────────────────────────────────────────────────────────
    // STUB IMPLEMENTATION
    // TODO: Replace with real AppMetrica SDK (minimum 3.x, 2026)
    // Real SDK: https://appmetrica.yandex.ru/docs/mobile-sdk-dg/concepts/unity-plugin.html
    //
    // AppMetrica — единственная аналитика. Firebase Analytics ЗАПРЕЩЁН.
    // Все вызовы — через ServiceLocator.Get<IAnalyticsManager>()
    // ─────────────────────────────────────────────────────────────────

    public class AppMetricaManager : IAnalyticsManager, IAnalyticsService
    {
        private bool _initialized;
        private string _userId;

        public void Initialize(string apiKey)
        {
            // TODO: AppMetrica.Instance.ActivateWithConfiguration(config);
            _initialized = true;
            // BUG-007: Substring краш если ключ короче 8 символов (пустое поле в Inspector)
            string keyPreview = apiKey != null && apiKey.Length >= 8 ? apiKey.Substring(0, 8) : apiKey;
            Debug.Log($"[AppMetrica STUB] Initialized with key: {keyPreview}...");
        }

        public void TrackEvent(string name, Dictionary<string, string> parameters = null)
        {
            if (!_initialized)
            {
                Debug.LogWarning("[AppMetrica STUB] Not initialized. Dropping event.");
                return;
            }

            // TODO: AppMetrica.Instance.ReportEvent(name, parameters);
            var paramStr = parameters != null ? string.Join(", ", parameters) : "none";
            Debug.Log($"[AppMetrica STUB] Event: {name} | Params: {paramStr}");
        }

        public void TrackRevenue(string productId, decimal amount, string currency)
        {
            if (!_initialized) return;

            // TODO: var revenue = new YandexAppMetricaRevenue(amount, currency);
            //       revenue.ProductID = productId;
            //       AppMetrica.Instance.ReportRevenue(revenue);
            Debug.Log($"[AppMetrica STUB] Revenue: {productId} = {amount} {currency}");
        }

        public void SetUserProperty(string key, string value)
        {
            if (!_initialized) return;

            // TODO: var profile = new YandexAppMetricaUserProfile();
            //       profile.Apply(YandexAppMetricaAttribute.CustomString(key).WithValue(value));
            //       AppMetrica.Instance.ReportUserProfile(profile);
            Debug.Log($"[AppMetrica STUB] UserProperty: {key} = {value}");
        }

        public void SetUserId(string userId)
        {
            _userId = userId;
            // TODO: AppMetrica.Instance.SetUserProfileID(userId);
            Debug.Log($"[AppMetrica STUB] UserId: {userId}");
        }

        public void Flush()
        {
            // TODO: AppMetrica.Instance.SendEventsBuffer();
            Debug.Log("[AppMetrica STUB] Flush called.");
        }

        // ─── IAnalyticsService ───────────────────────────────────────

        void IAnalyticsService.LogEvent(string eventName, Dictionary<string, object> parameters)
        {
            Dictionary<string, string> converted = null;
            if (parameters != null)
            {
                converted = new Dictionary<string, string>(parameters.Count);
                foreach (var kv in parameters)
                    converted[kv.Key] = kv.Value?.ToString() ?? "";
            }
            TrackEvent(eventName, converted);
        }

        void IAnalyticsService.SetUserProperty(string key, string value)
        {
            SetUserProperty(key, value);
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // CANONICAL EVENT NAMES (funnel from lead-dev-plan)
    // ─────────────────────────────────────────────────────────────────

    public static class AnalyticsEvents
    {
        public const string INSTALL = "install";
        public const string FIRST_OPEN = "first_open";
        public const string TUTORIAL_START = "tutorial_start";
        public const string TUTORIAL_STEP = "tutorial_step";       // + param "step": N
        public const string TUTORIAL_COMPLETE = "tutorial_complete";
        public const string TOWER_PLACED_FIRST = "tower_placed_first";
        public const string LEVEL_COMPLETE = "level_complete";     // + param "level": N
        public const string ROGUELITE_UNLOCKED = "roguelite_unlocked";
        public const string DAY_1_RETURN = "day_1_return";
        public const string IDLE_INCOME_COLLECTED_FIRST = "idle_income_collected_first";
        public const string FIRST_AD_VIEW = "first_ad_view";
        public const string FIRST_IAP = "first_iap";
        public const string AD_REVENUE = "ad_revenue_event";
        public const string EVENT_STARTED = "event_started";
        public const string EVENT_COMPLETED = "event_completed";
        public const string CULTURAL_EVENT_REWARD = "cultural_event_reward_collected";
    }
}
