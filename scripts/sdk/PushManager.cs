using System;
using System.Collections.Generic;
using UnityEngine;

namespace BogatyrskayaZastava.Core
{
    // ─────────────────────────────────────────────────────────────────
    // INTERFACE
    // ─────────────────────────────────────────────────────────────────

    public interface IPushManager
    {
        void Initialize(Action<bool> onComplete);
        void ScheduleLocal(string id, string title, string body, TimeSpan delay);
        void CancelLocal(string id);
        void CancelAll();
        void RegisterRemote(Action<string> onTokenReceived);
        bool IsInitialized { get; }
    }

    // ─────────────────────────────────────────────────────────────────
    // STUB IMPLEMENTATION
    //
    // TODO: Заменить на реальные SDK:
    //   Primary: RuStore Push SDK — для RuStore-пользователей
    //   Fallback: Firebase FCM — для не-RuStore устройств
    //
    // Push-стратегия (от GD, PushSchedule.md → неделя 10):
    //   - Idle income ready (8h cap reached)
    //   - Event started / ending soon
    //   - Daily reminder (если не заходил 24h)
    //   - Battle Pass reward available
    //
    // Регистрация: ServiceLocator.Register<IPushManager>(new PushManager());
    // ─────────────────────────────────────────────────────────────────

    public class PushManager : IPushManager
    {
        private bool _initialized;
        private readonly Dictionary<string, ScheduledNotification> _scheduled =
            new Dictionary<string, ScheduledNotification>();

        public bool IsInitialized => _initialized;

        private class ScheduledNotification
        {
            public string Id;
            public string Title;
            public string Body;
            public DateTime FireTime;
        }

        public void Initialize(Action<bool> onComplete)
        {
            // TODO: Determine push channel:
            //   1. Check if RuStore app installed → use RuStore Push SDK
            //   2. Otherwise → use Firebase FCM
            //
            // RuStore Push SDK init:
            //   RuStorePushClient.Init(projectId);
            //   RuStorePushClient.OnNewToken += OnRuStoreToken;
            //   RuStorePushClient.OnMessageReceived += OnMessage;
            //
            // FCM fallback:
            //   Already initialized in FirebaseManager.Messaging

            _initialized = true;
            Debug.Log("[Push STUB] Initialized. Channel: auto-detect (RuStore primary, FCM fallback)");
            onComplete?.Invoke(true);
        }

        public void ScheduleLocal(string id, string title, string body, TimeSpan delay)
        {
            if (!_initialized)
            {
                Debug.LogWarning("[Push STUB] Not initialized.");
                return;
            }

            // TODO: Android NotificationManager / Unity Mobile Notifications package
            // AndroidNotificationChannel channel = new AndroidNotificationChannel { ... };
            // var notification = new AndroidNotification { Title = title, Text = body, FireTime = DateTime.Now + delay };
            // AndroidNotificationCenter.SendNotificationWithExplicitID(notification, channelId, id.GetHashCode());

            var scheduled = new ScheduledNotification
            {
                Id = id,
                Title = title,
                Body = body,
                FireTime = DateTime.UtcNow + delay
            };

            _scheduled[id] = scheduled;
            Debug.Log($"[Push STUB] Local scheduled: '{title}' in {delay.TotalMinutes:F0} min (id: {id})");
        }

        public void CancelLocal(string id)
        {
            // TODO: AndroidNotificationCenter.CancelNotification(id.GetHashCode());
            if (_scheduled.Remove(id))
                Debug.Log($"[Push STUB] Cancelled local: {id}");
        }

        public void CancelAll()
        {
            // TODO: AndroidNotificationCenter.CancelAllNotifications();
            _scheduled.Clear();
            Debug.Log("[Push STUB] All local notifications cancelled.");
        }

        public void RegisterRemote(Action<string> onTokenReceived)
        {
            // TODO: Determine channel and get token:
            //   RuStore: RuStorePushClient.GetToken()
            //   FCM: handled by FirebaseManager.Messaging.Initialize()

            var stubToken = "stub_push_token_" + Guid.NewGuid().ToString("N").Substring(0, 12);
            Debug.Log($"[Push STUB] Remote token: {stubToken}");
            onTokenReceived?.Invoke(stubToken);
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // PUSH EVENT TYPES (convenience, for game systems to schedule pushes)
    // ─────────────────────────────────────────────────────────────────

    public static class PushIds
    {
        public const string IDLE_INCOME_READY = "push_idle_ready";
        public const string EVENT_STARTING = "push_event_start";
        public const string EVENT_ENDING_SOON = "push_event_ending";
        public const string DAILY_REMINDER = "push_daily_reminder";
        public const string BATTLEPASS_REWARD = "push_bp_reward";
    }
}
