using System;
using System.Collections.Generic;
using UnityEngine;

namespace BogatyrskayaZastava.Core
{
    /// <summary>
    /// Static service locator. Services are registered in Bootstrap scene before any gameplay starts.
    /// Usage: ServiceLocator.Register<IAudioService>(new AudioService());
    ///        ServiceLocator.Get<IAudioService>().PlaySFX("sword_hit");
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        // ─────────────────────────────────────────────────────────────
        // Register
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Register a service. Overwrites if already registered (allows hot-swap for testing).
        /// Call from Bootstrap scene only.
        /// </summary>
        public static void Register<T>(T service) where T : class
        {
            var type = typeof(T);

            if (_services.ContainsKey(type))
                Debug.LogWarning($"[ServiceLocator] Overwriting existing service: {type.Name}");

            _services[type] = service;
            Debug.Log($"[ServiceLocator] Registered: {type.Name}");
        }

        // ─────────────────────────────────────────────────────────────
        // Get
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Get a registered service. Throws if not registered — fail fast, no silent nulls.
        /// </summary>
        public static T Get<T>() where T : class
        {
            var type = typeof(T);

            if (_services.TryGetValue(type, out var service))
                return (T)service;

            throw new InvalidOperationException(
                $"[ServiceLocator] Service not registered: {type.Name}. " +
                $"Did you forget to call Register<{type.Name}>() in Bootstrap?");
        }

        /// <summary>
        /// Try-get variant for optional services (e.g. services not yet initialized).
        /// </summary>
        public static bool TryGet<T>(out T service) where T : class
        {
            var type = typeof(T);

            if (_services.TryGetValue(type, out var raw))
            {
                service = (T)raw;
                return true;
            }

            service = null;
            return false;
        }

        // ─────────────────────────────────────────────────────────────
        // Unregister
        // ─────────────────────────────────────────────────────────────

        /// <summary>Unregister a service. Call on cleanup/scene unload.</summary>
        public static void Unregister<T>() where T : class
        {
            var type = typeof(T);

            if (_services.ContainsKey(type))
            {
                _services.Remove(type);
                Debug.Log($"[ServiceLocator] Unregistered: {type.Name}");
            }
            else
            {
                Debug.LogWarning($"[ServiceLocator] Tried to unregister non-existing service: {type.Name}");
            }
        }

        /// <summary>Unregister all services. Call on full app restart.</summary>
        public static void Clear()
        {
            _services.Clear();
        }

        /// <summary>Check if a service is registered.</summary>
        public static bool IsRegistered<T>() where T : class
        {
            return _services.ContainsKey(typeof(T));
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // SERVICE INTERFACES
    // Конкретные реализации — в Scripts/Services/
    // ─────────────────────────────────────────────────────────────────

    /// <summary>Audio service interface. Implementation: AudioService.cs</summary>
    public interface IAudioService
    {
        void PlaySFX(string clipId);
        void PlayMusic(string trackId, bool loop = true);
        void StopMusic();
        void SetSFXVolume(float volume);    // 0..1
        void SetMusicVolume(float volume);  // 0..1
        // TODO: реализовать пул AudioSource для zero-allocation воспроизведения SFX
    }

    /// <summary>
    /// Analytics service interface.
    /// Implementation: AppMetricaAnalyticsService.cs
    /// ВАЖНО: Firebase Analytics НЕ используется. Только AppMetrica.
    /// </summary>
    public interface IAnalyticsService
    {
        void LogEvent(string eventName, Dictionary<string, object> parameters = null);
        void SetUserProperty(string key, string value);
        // TODO: список обязательных событий воронки определяет Lead Dev совместно с GD
    }

    /// <summary>Save service interface. Implementation: FirebaseSaveService.cs</summary>
    public interface ISaveService
    {
        void SaveRunProgress(object runProgressData, Action<bool> onComplete);
        void LoadUserProfile(string userId, Action<object> onComplete);
        void SaveIdleState(long lastSeenTimestamp, float baseRate, float multiplier);
        void SavePermanentUnlocks(object unlocksData, Action<bool> onComplete);
        // TODO: офлайн-кеш на PlayerPrefs на случай отсутствия сети
    }

    /// <summary>
    /// IAP manager interface.
    /// Implementation: RuStoreIAPManager.cs (RuStore Pay SDK v10.1.1)
    /// </summary>
    public interface IIAPManager
    {
        void Initialize(Action<bool> onComplete);
        void Purchase(string productId, Action<PurchaseResult> onComplete);
        void RestorePurchases(Action<bool> onComplete);
        bool IsInitialized { get; }
        // TODO: первое IAP-предложение не ранее дня 3 (из game-bible)
    }

    // ─────────────────────────────────────────────────────────────────
    // SUPPORTING TYPES
    // ─────────────────────────────────────────────────────────────────

    public enum PurchaseResult { Success, Cancelled, Failed, AlreadyOwned }
}
