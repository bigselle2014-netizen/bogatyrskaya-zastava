using System;
using System.Collections.Generic;
using UnityEngine;

namespace BogatyrskayaZastava.Core
{
    // ─────────────────────────────────────────────────────────────────
    // EVENT TYPES
    // ─────────────────────────────────────────────────────────────────

    public enum GameEventType
    {
        WeeklyChallenge,
        LimitedTimeBoss,
        FactionBonus,
        DoubleDrops
    }

    // ─────────────────────────────────────────────────────────────────
    // EVENT DATA (ScriptableObject)
    // ─────────────────────────────────────────────────────────────────

    [CreateAssetMenu(fileName = "NewGameEvent", menuName = "BogatyrskayaZastava/GameEventData")]
    public class GameEventData : ScriptableObject
    {
        public string eventId;
        public string eventName;
        public GameEventType eventType;
        public string description;

        [Header("Schedule")]
        public string cronExpression;
        public float durationHours;

        [Header("Rewards")]
        public int bonusGold;
        public int bonusRuneStones;
        public float dropMultiplier = 1f;
        public float damageMultiplier = 1f;

        [Header("Faction Bonus (if FactionBonus type)")]
        public string targetFactionId;
        public float factionBonusPercent;

        [Header("Limited Boss (if LimitedTimeBoss type)")]
        public string bossEnemyDataId;
        public int bossWaveCount;

        [Header("Versioning")]
        public int dataVersion = 1;
    }

    // ─────────────────────────────────────────────────────────────────
    // EVENT CALENDAR ENTRY (JSON → deserialized)
    // ─────────────────────────────────────────────────────────────────

    [Serializable]
    public class EventCalendarEntry
    {
        public string eventId;
        public string startDate;    // ISO 8601: "2026-06-21T00:00:00Z"
        public string endDate;
        public string cronSchedule; // cron-like: "0 0 * * 1" (every Monday)
        public bool enabled;
    }

    [Serializable]
    public class EventCalendar
    {
        public List<EventCalendarEntry> events;
    }

    // ─────────────────────────────────────────────────────────────────
    // EVENT BUS STRUCTS (for EventBus integration)
    // ─────────────────────────────────────────────────────────────────

    public struct GameEventStartedEvent   { public string eventId; public GameEventType eventType; }
    public struct GameEventEndedEvent     { public string eventId; public GameEventType eventType; }
    public struct GameEventPausedEvent    { public string eventId; }
    public struct GameEventResumedEvent   { public string eventId; }

    // ─────────────────────────────────────────────────────────────────
    // EVENT MANAGER
    // ─────────────────────────────────────────────────────────────────

    public class EventManager : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private TextAsset _eventCalendarJson;
        [SerializeField] private List<GameEventData> _eventDatabase;

        private EventCalendar _calendar;
        private readonly Dictionary<string, GameEventData> _eventLookup = new Dictionary<string, GameEventData>();
        private readonly Dictionary<string, ActiveGameEvent> _activeEvents = new Dictionary<string, ActiveGameEvent>();
        private bool _isPaused;

        private class ActiveGameEvent
        {
            public GameEventData Data;
            public DateTime StartTime;
            public DateTime EndTime;
            public bool IsPaused;
        }

        // ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            BuildEventLookup();
            LoadCalendar();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void Update()
        {
            if (_isPaused) return;

            CheckScheduledEvents();
            CheckExpiredEvents();
        }

        // ─────────────────────────────────────────────────────────────
        // INITIALIZATION
        // ─────────────────────────────────────────────────────────────

        private void BuildEventLookup()
        {
            _eventLookup.Clear();

            if (_eventDatabase == null) return;

            foreach (var eventData in _eventDatabase)
            {
                if (eventData == null) continue;

                if (_eventLookup.ContainsKey(eventData.eventId))
                {
                    Debug.LogWarning($"[EventManager] Duplicate eventId: {eventData.eventId}");
                    continue;
                }

                _eventLookup[eventData.eventId] = eventData;
            }
        }

        private void LoadCalendar()
        {
            if (_eventCalendarJson == null)
            {
                Debug.LogWarning("[EventManager] EventCalendar.json not assigned. No events will be scheduled.");
                _calendar = new EventCalendar { events = new List<EventCalendarEntry>() };
                return;
            }

            try
            {
                _calendar = JsonUtility.FromJson<EventCalendar>(_eventCalendarJson.text);

                if (_calendar == null || _calendar.events == null)
                {
                    _calendar = new EventCalendar { events = new List<EventCalendarEntry>() };
                    Debug.LogWarning("[EventManager] EventCalendar parsed but empty.");
                }
                else
                {
                    Debug.Log($"[EventManager] Loaded {_calendar.events.Count} calendar entries.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EventManager] Failed to parse EventCalendar.json: {ex.Message}");
                _calendar = new EventCalendar { events = new List<EventCalendarEntry>() };
            }
        }

        public void ReloadCalendar(string json)
        {
            try
            {
                _calendar = JsonUtility.FromJson<EventCalendar>(json);
                Debug.Log($"[EventManager] Hot-reloaded calendar: {_calendar.events.Count} entries.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EventManager] Hot-reload failed: {ex.Message}");
            }
        }

        // ─────────────────────────────────────────────────────────────
        // SCHEDULING
        // ─────────────────────────────────────────────────────────────

        private void CheckScheduledEvents()
        {
            var now = DateTime.UtcNow;

            foreach (var entry in _calendar.events)
            {
                if (!entry.enabled) continue;
                if (_activeEvents.ContainsKey(entry.eventId)) continue;

                if (!_eventLookup.TryGetValue(entry.eventId, out var eventData))
                    continue;

                if (!TryParseDate(entry.startDate, out var start)) continue;
                if (!TryParseDate(entry.endDate, out var end)) continue;

                if (now >= start && now < end)
                {
                    ActivateEvent(eventData, start, end);
                }
            }
        }

        private void CheckExpiredEvents()
        {
            var now = DateTime.UtcNow;
            var expired = new List<string>();

            foreach (var kvp in _activeEvents)
            {
                if (now >= kvp.Value.EndTime)
                    expired.Add(kvp.Key);
            }

            foreach (var id in expired)
            {
                DeactivateEvent(id);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // ACTIVATION / DEACTIVATION
        // ─────────────────────────────────────────────────────────────

        private void ActivateEvent(GameEventData data, DateTime start, DateTime end)
        {
            var active = new ActiveGameEvent
            {
                Data = data,
                StartTime = start,
                EndTime = end,
                IsPaused = false
            };

            _activeEvents[data.eventId] = active;
            Debug.Log($"[EventManager] Event ACTIVATED: {data.eventName} ({data.eventType}) until {end:u}");

            EventBus.Publish(new GameEventStartedEvent
            {
                eventId = data.eventId,
                eventType = data.eventType
            });

            if (ServiceLocator.TryGet<IAnalyticsService>(out var analytics))
            {
                analytics.LogEvent("event_started", new Dictionary<string, object>
                {
                    { "event_id", data.eventId },
                    { "event_type", data.eventType.ToString() }
                });
            }
        }

        private void DeactivateEvent(string eventId)
        {
            if (!_activeEvents.TryGetValue(eventId, out var active))
                return;

            _activeEvents.Remove(eventId);
            Debug.Log($"[EventManager] Event ENDED: {active.Data.eventName}");

            EventBus.Publish(new GameEventEndedEvent
            {
                eventId = active.Data.eventId,
                eventType = active.Data.eventType
            });

            if (ServiceLocator.TryGet<IAnalyticsService>(out var analytics))
            {
                analytics.LogEvent("event_completed", new Dictionary<string, object>
                {
                    { "event_id", active.Data.eventId },
                    { "event_type", active.Data.eventType.ToString() }
                });
            }
        }

        // ─────────────────────────────────────────────────────────────
        // PAUSE / RESUME (via GameState)
        // ─────────────────────────────────────────────────────────────

        private void OnGameStateChanged(GameStateChangedEvent e)
        {
            if (e.to == GameState.Idle || e.to == GameState.MainMenu)
            {
                PauseAllEvents();
            }
            else if (e.from == GameState.Idle || e.from == GameState.MainMenu)
            {
                ResumeAllEvents();
            }
        }

        private void PauseAllEvents()
        {
            if (_isPaused) return;

            _isPaused = true;

            foreach (var kvp in _activeEvents)
            {
                if (!kvp.Value.IsPaused)
                {
                    kvp.Value.IsPaused = true;
                    EventBus.Publish(new GameEventPausedEvent { eventId = kvp.Key });
                }
            }
        }

        private void ResumeAllEvents()
        {
            if (!_isPaused) return;

            _isPaused = false;

            foreach (var kvp in _activeEvents)
            {
                if (kvp.Value.IsPaused)
                {
                    kvp.Value.IsPaused = false;
                    EventBus.Publish(new GameEventResumedEvent { eventId = kvp.Key });
                }
            }
        }

        // ─────────────────────────────────────────────────────────────
        // PUBLIC API
        // ─────────────────────────────────────────────────────────────

        public bool IsEventActive(string eventId)
        {
            return _activeEvents.ContainsKey(eventId);
        }

        public GameEventData GetActiveEventData(string eventId)
        {
            return _activeEvents.TryGetValue(eventId, out var active) ? active.Data : null;
        }

        public List<GameEventData> GetAllActiveEvents()
        {
            var result = new List<GameEventData>();
            foreach (var kvp in _activeEvents)
                result.Add(kvp.Value.Data);
            return result;
        }

        public float GetDropMultiplier()
        {
            float multiplier = 1f;
            foreach (var kvp in _activeEvents)
            {
                if (kvp.Value.IsPaused) continue;
                multiplier *= kvp.Value.Data.dropMultiplier;
            }
            return multiplier;
        }

        public float GetDamageMultiplier()
        {
            float multiplier = 1f;
            foreach (var kvp in _activeEvents)
            {
                if (kvp.Value.IsPaused) continue;
                multiplier *= kvp.Value.Data.damageMultiplier;
            }
            return multiplier;
        }

        public float GetFactionBonus(string factionId)
        {
            float bonus = 0f;
            foreach (var kvp in _activeEvents)
            {
                if (kvp.Value.IsPaused) continue;
                var data = kvp.Value.Data;
                if (data.eventType == GameEventType.FactionBonus && data.targetFactionId == factionId)
                    bonus += data.factionBonusPercent;
            }
            return bonus;
        }

        public void ForceActivateEvent(string eventId, float durationHours)
        {
            if (!_eventLookup.TryGetValue(eventId, out var data))
            {
                Debug.LogError($"[EventManager] ForceActivate failed: eventId '{eventId}' not in database.");
                return;
            }

            var now = DateTime.UtcNow;
            ActivateEvent(data, now, now.AddHours(durationHours));
        }

        public void ForceDeactivateEvent(string eventId)
        {
            DeactivateEvent(eventId);
        }

        // ─────────────────────────────────────────────────────────────
        // UTILITY
        // ─────────────────────────────────────────────────────────────

        private static bool TryParseDate(string iso8601, out DateTime result)
        {
            return DateTime.TryParse(iso8601, null,
                System.Globalization.DateTimeStyles.RoundtripKind, out result);
        }
    }
}
