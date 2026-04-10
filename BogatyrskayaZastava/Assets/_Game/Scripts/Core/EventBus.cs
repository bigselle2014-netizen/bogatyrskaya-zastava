using System;
using System.Collections.Generic;
using UnityEngine;

namespace BogatyrskayaZastava.Core
{
    /// <summary>
    /// Static event bus for decoupled communication between systems.
    /// Usage: EventBus.Subscribe<TowerPlacedEvent>(OnTowerPlaced);
    ///        EventBus.Publish(new TowerPlacedEvent { towerId = "DR-1" });
    ///        EventBus.Unsubscribe<TowerPlacedEvent>(OnTowerPlaced);
    /// </summary>
    public static class EventBus
    {
        // Dictionary<event type, delegate list>
        private static readonly Dictionary<Type, List<Delegate>> _subscribers =
            new Dictionary<Type, List<Delegate>>();

        // ─────────────────────────────────────────────────────────────
        // Subscribe
        // ─────────────────────────────────────────────────────────────

        /// <summary>Subscribe to an event of type T.</summary>
        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            var type = typeof(T);

            if (!_subscribers.ContainsKey(type))
                _subscribers[type] = new List<Delegate>();

            if (!_subscribers[type].Contains(handler))
                _subscribers[type].Add(handler);
        }

        // ─────────────────────────────────────────────────────────────
        // Unsubscribe
        // ─────────────────────────────────────────────────────────────

        /// <summary>Unsubscribe from an event of type T. Always call in OnDestroy.</summary>
        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            var type = typeof(T);

            if (_subscribers.TryGetValue(type, out var list))
                list.Remove(handler);
        }

        // ─────────────────────────────────────────────────────────────
        // Publish
        // ─────────────────────────────────────────────────────────────

        /// <summary>Publish an event to all subscribers. Safe to call with no subscribers.</summary>
        public static void Publish<T>(T eventData) where T : struct
        {
            var type = typeof(T);

            if (!_subscribers.TryGetValue(type, out var list))
                return;

            // Copy list to avoid modification during iteration
            var snapshot = new List<Delegate>(list);

            foreach (var handler in snapshot)
            {
                try
                {
                    ((Action<T>)handler)?.Invoke(eventData);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[EventBus] Exception in handler for {type.Name}: {ex}");
                }
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Utility
        // ─────────────────────────────────────────────────────────────

        /// <summary>Clear all subscribers. Call on scene unload or full restart.</summary>
        public static void Clear()
        {
            _subscribers.Clear();
        }

        /// <summary>Clear subscribers for a specific event type.</summary>
        public static void Clear<T>() where T : struct
        {
            var type = typeof(T);
            if (_subscribers.ContainsKey(type))
                _subscribers.Remove(type);
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // EVENT STRUCTS
    // Все события — value-типы (struct) для zero-allocation publishing.
    // Имена башен НЕ хардкодятся — только towerDataId из TowerData SO.
    // ─────────────────────────────────────────────────────────────────

    // Gameplay events
    // TowerPlacedEvent и TowerRemovedEvent определены в TowerSlot.cs (namespace BogatyrskayaZastava.Gameplay)
    public struct TowerUpgradedEvent    { public string towerDataId; public int newLevel; }
    public struct EnemyDiedEvent        { public string enemyDataId; public Vector3 position; }
    public struct EnemyReachedGateEvent { public string enemyId; public int damage; }
    public struct WaveStartedEvent      { public int waveNumber; public int totalWaves; }
    public struct WaveCompletedEvent    { public int waveNumber; public float timeBetweenWaves; }
    public struct LevelCompletedEvent   { public int levelNumber; public int remainingGateHp; }
    public struct SynergyActivatedEvent   { public string synergyId; } // S-01 ... S-12
    public struct SynergyDeactivatedEvent { public string synergyId; }

    // Gate events
    public struct GateDestroyedEvent    { }
    public struct GateHpChangedEvent    { public int current; public int max; }

    // Run events
    public struct RunStartedEvent       { public int runIndex; }
    public struct RunCompletedEvent     { public bool isVictory; public int wavesCompleted; public BogatyrskayaZastava.Core.RunCompleteData data; }
    public struct DeckChoiceEvent       { public int choiceCount; public int waveNumber; }
    public struct RandomEventTriggeredEvent { public int waveNumber; public int eventSeed; }

    // Resource events
    public struct ResourceChangedEvent  { public int newGold; public int newRuneStones; }
    public struct IdleCollectedEvent    { public long amount; public float offlineSeconds; }
    public struct IdleIncomeReadyEvent  { public float gold; public float runeStones; }

    // State events
    public struct GameStateChangedEvent { public GameState from; public GameState to; }

    // Wave control (published by HUD → consumed by EnemyWaveController — QA-027/028)
    public struct StartNextWaveRequestEvent { }

    // Tutorial events
    public struct TutorialCompleteEvent { }

    // Monetization events
    public struct PurchaseCompletedEvent { public string productId; }
    public struct AdWatchedEvent         { public BogatyrskayaZastava.Core.RewardedContext context; }
}
