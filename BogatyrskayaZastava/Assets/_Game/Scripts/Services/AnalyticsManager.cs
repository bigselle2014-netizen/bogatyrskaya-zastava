using System.Collections.Generic;
using UnityEngine;
using BogatyrskayaZastava.Core;

namespace BogatyrskayaZastava.Core
{
    public class AnalyticsManager : MonoBehaviour
    {
        private IAnalyticsService _analytics;
        private bool _initialized;

        // BUG-014: instance field, не static — иначе параллельные вызовы (корутины) могут смешать данные
        private readonly Dictionary<string, object> _paramsBuffer = new Dictionary<string, object>(8);

        private void Awake()
        {
            ServiceLocator.Register<AnalyticsManager>(this);
        }

        private void Start()
        {
            if (ServiceLocator.TryGet<IAnalyticsService>(out var svc))
            {
                _analytics = svc;
                _initialized = true;
            }
            else
            {
                Debug.LogWarning("[AnalyticsManager] IAnalyticsService not registered. Events will be buffered to log.");
            }
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<AnalyticsManager>();
        }

        public void TrackTutorialStart()
        {
            LogSimple("tutorial_start");
        }

        public void TrackTutorialComplete()
        {
            LogSimple("tutorial_complete");
        }

        public void TrackLevelComplete(int level, int stars, float seconds)
        {
            _paramsBuffer.Clear();
            _paramsBuffer["level"] = level;
            _paramsBuffer["stars"] = stars;
            _paramsBuffer["seconds"] = seconds;
            Log("level_complete", _paramsBuffer);
        }

        public void TrackSynergyActivated(string synergyId)
        {
            _paramsBuffer.Clear();
            _paramsBuffer["synergy_id"] = synergyId;
            Log("synergy_activated", _paramsBuffer);
        }

        public void TrackPurchase(string productId, float price, string currency)
        {
            _paramsBuffer.Clear();
            _paramsBuffer["product_id"] = productId;
            _paramsBuffer["price"] = price;
            _paramsBuffer["currency"] = currency;
            Log("iap_purchase", _paramsBuffer);
        }

        public void TrackAdWatched(RewardedContext context)
        {
            _paramsBuffer.Clear();
            _paramsBuffer["context"] = context.ToString();
            Log("ad_watched", _paramsBuffer);
        }

        public void TrackRetention(int dayNumber)
        {
            _paramsBuffer.Clear();
            _paramsBuffer["day"] = dayNumber;
            Log("retention", _paramsBuffer);
        }

        public void TrackRunStarted(int runIndex)
        {
            _paramsBuffer.Clear();
            _paramsBuffer["run_index"] = runIndex;
            Log("run_started", _paramsBuffer);
        }

        public void TrackRunCompleted(bool isVictory, int wavesCleared)
        {
            _paramsBuffer.Clear();
            _paramsBuffer["is_victory"] = isVictory;
            _paramsBuffer["waves_cleared"] = wavesCleared;
            Log("run_completed", _paramsBuffer);
        }

        private void Log(string eventName, Dictionary<string, object> parameters)
        {
            if (_initialized && _analytics != null)
            {
                _analytics.LogEvent(eventName, parameters);
            }
            else
            {
                Debug.Log($"[AnalyticsManager] (not initialized) {eventName}");
            }
        }

        private void LogSimple(string eventName)
        {
            if (_initialized && _analytics != null)
            {
                _analytics.LogEvent(eventName, null);
            }
            else
            {
                Debug.Log($"[AnalyticsManager] (not initialized) {eventName}");
            }
        }
    }
}
