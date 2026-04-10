using System.Collections.Generic;
using UnityEngine;

namespace BogatyrskayaZastava.Gameplay
{
    /// <summary>
    /// Object pool for enemies. Eliminates Instantiate/Destroy in the hot path.
    /// Supports dynamic expansion when pool is exhausted.
    ///
    /// Usage:
    ///   EnemyPool.PrewarmPool(enemyPrefab, count: 20);
    ///   GameObject enemy = EnemyPool.Get(enemyPrefab);
    ///   EnemyPool.Return(enemy);
    /// </summary>
    public static class EnemyPool
    {
        // Key: prefab instance ID, Value: stack of inactive objects
        private static readonly Dictionary<int, Stack<GameObject>> _pools =
            new Dictionary<int, Stack<GameObject>>();

        // Tracks which pool each active instance belongs to (for Return)
        private static readonly Dictionary<int, int> _instanceToPrefabId =
            new Dictionary<int, int>();

        // Active instances: instanceId → GameObject (for ReturnAll)
        private static readonly Dictionary<int, GameObject> _activeInstances =
            new Dictionary<int, GameObject>();

        // enemyId string → prefab (registered at level init)
        private static readonly Dictionary<string, GameObject> _prefabRegistry =
            new Dictionary<string, GameObject>();

        // Container object to keep hierarchy clean (created on first use)
        private static Transform _poolContainer;

        // ─────────────────────────────────────────────────────────────
        // PrewarmPool
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Pre-instantiate enemies before wave starts.
        /// Call from WaveManager during wave loading, not during gameplay.
        /// </summary>
        public static void PrewarmPool(GameObject prefab, int count)
        {
            if (prefab == null)
            {
                Debug.LogError("[EnemyPool] PrewarmPool: prefab is null");
                return;
            }

            EnsureContainer();
            var prefabId = prefab.GetInstanceID();

            if (!_pools.ContainsKey(prefabId))
                _pools[prefabId] = new Stack<GameObject>(count);

            for (var i = 0; i < count; i++)
            {
                var instance = CreateInstance(prefab, prefabId);
                instance.SetActive(false);
                _pools[prefabId].Push(instance);
            }

            Debug.Log($"[EnemyPool] Prewarmed {count}x {prefab.name}");
        }

        // ─────────────────────────────────────────────────────────────
        // RegisterPrefab
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Register a prefab by enemyId string. Call before wave starts (e.g. from EnemyWaveController.StartLevel).
        /// </summary>
        public static void RegisterPrefab(string enemyId, GameObject prefab)
        {
            if (string.IsNullOrEmpty(enemyId) || prefab == null)
            {
                Debug.LogError("[EnemyPool] RegisterPrefab: invalid arguments");
                return;
            }
            _prefabRegistry[enemyId] = prefab;
        }

        // ─────────────────────────────────────────────────────────────
        // Get (by string id)
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Get an enemy from the pool by enemyId string. Requires RegisterPrefab called first.
        /// </summary>
        public static GameObject Get(string enemyId)
        {
            if (string.IsNullOrEmpty(enemyId))
            {
                Debug.LogError("[EnemyPool] Get(string): enemyId is null or empty");
                return null;
            }

            if (!_prefabRegistry.TryGetValue(enemyId, out var prefab))
            {
                Debug.LogError($"[EnemyPool] Get(string): no prefab registered for enemyId='{enemyId}'. Call RegisterPrefab first.");
                return null;
            }

            return Get(prefab);
        }

        // ─────────────────────────────────────────────────────────────
        // Get (by prefab)
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Get an enemy from the pool. Dynamically creates a new instance if pool is empty.
        /// IMPORTANT: caller must call enemy.GetComponent<EnemyController>().Initialize(enemyData)
        /// </summary>
        public static GameObject Get(GameObject prefab)
        {
            if (prefab == null)
            {
                Debug.LogError("[EnemyPool] Get: prefab is null");
                return null;
            }

            EnsureContainer();
            var prefabId = prefab.GetInstanceID();

            if (!_pools.ContainsKey(prefabId))
                _pools[prefabId] = new Stack<GameObject>();

            GameObject instance;

            if (_pools[prefabId].Count > 0)
            {
                instance = _pools[prefabId].Pop();
            }
            else
            {
                // Dynamic expansion — pool was exhausted (wave larger than prewarmed)
                Debug.LogWarning($"[EnemyPool] Pool exhausted for {prefab.name}, creating new instance. " +
                                 $"Consider increasing PrewarmPool count.");
                instance = CreateInstance(prefab, prefabId);
            }

            instance.SetActive(true);
            _activeInstances[instance.GetInstanceID()] = instance;
            return instance;
        }

        // ─────────────────────────────────────────────────────────────
        // Return
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Return an enemy to the pool. Call on death or wave end — never call Destroy.
        /// </summary>
        public static void Return(GameObject instance)
        {
            if (instance == null)
                return;

            var instanceId = instance.GetInstanceID();

            if (!_instanceToPrefabId.TryGetValue(instanceId, out var prefabId))
            {
                Debug.LogWarning($"[EnemyPool] Return: {instance.name} is not tracked by pool. Destroying.");
                Object.Destroy(instance);
                return;
            }

            _activeInstances.Remove(instanceId);

            // UD-06: сброс состояния врага перед возвратом в пул
            var enemyBase = instance.GetComponent<EnemyBase>();
            if (enemyBase != null)
                enemyBase.ResetState();

            instance.SetActive(false);
            instance.transform.SetParent(_poolContainer);

            if (!_pools.ContainsKey(prefabId))
                _pools[prefabId] = new Stack<GameObject>();

            _pools[prefabId].Push(instance);
        }

        // ─────────────────────────────────────────────────────────────
        // ReturnAll
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Return all active enemies to pool. Call on wave end or run reset.
        /// </summary>
        public static void ReturnAll()
        {
            var snapshot = new List<GameObject>(_activeInstances.Values);
            foreach (var go in snapshot)
            {
                if (go != null)
                    Return(go);
            }
            _activeInstances.Clear();
        }

        // ─────────────────────────────────────────────────────────────
        // Clear
        // ─────────────────────────────────────────────────────────────

        /// <summary>Destroy all pooled objects and clear dictionaries. Call on scene unload or app quit.</summary>
        public static void ClearAll()
        {
            // BUG-002: уничтожаем активных врагов первыми, чтобы их Die()/ReachGate()
            // не вызвали Return() после очистки словарей (→ MissingReferenceException)
            foreach (var go in _activeInstances.Values)
            {
                if (go != null) Object.Destroy(go);
            }
            _activeInstances.Clear();

            foreach (var pool in _pools.Values)
            {
                foreach (var obj in pool)
                {
                    if (obj != null)
                        Object.Destroy(obj);
                }
            }

            _pools.Clear();
            _instanceToPrefabId.Clear();
            _prefabRegistry.Clear();

            if (_poolContainer != null)
            {
                Object.Destroy(_poolContainer.gameObject);
                _poolContainer = null;
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Stats (debug)
        // ─────────────────────────────────────────────────────────────

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void LogStats()
        {
            Debug.Log($"[EnemyPool] Tracked prefabs: {_pools.Count}");
            foreach (var kvp in _pools)
                Debug.Log($"  prefabId={kvp.Key}: {kvp.Value.Count} inactive");
        }

        // ─────────────────────────────────────────────────────────────
        // Private helpers
        // ─────────────────────────────────────────────────────────────

        private static GameObject CreateInstance(GameObject prefab, int prefabId)
        {
            var instance = Object.Instantiate(prefab, _poolContainer);
            instance.name = $"{prefab.name}_pooled";

            // Track which pool this instance belongs to
            _instanceToPrefabId[instance.GetInstanceID()] = prefabId;

            return instance;
        }

        private static void EnsureContainer()
        {
            if (_poolContainer != null)
                return;

            var go = new GameObject("[EnemyPool]");
            Object.DontDestroyOnLoad(go);
            _poolContainer = go.transform;
        }
    }
}
