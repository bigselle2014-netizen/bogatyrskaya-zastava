using System.Collections.Generic;
using UnityEngine;
using BogatyrskayaZastava.Data;

namespace BogatyrskayaZastava.Gameplay
{
    /// <summary>
    /// Object pool for towers. Eliminates Instantiate/Destroy from TowerSlot.
    /// QA-030/031: Все создание/уничтожение башен идёт через TowerPool.
    ///
    /// Usage:
    ///   TowerBase tower = TowerPool.Get(data, parentTransform);
    ///   TowerPool.Return(tower.gameObject);
    ///
    /// TODO (Фаза 2): заменить new GameObject на Instantiate(prefab) после создания TowerPrefab registry.
    /// </summary>
    public static class TowerPool
    {
        // Key: TowerId string → stack of inactive GameObjects
        private static readonly Dictionary<string, Stack<GameObject>> _pools =
            new Dictionary<string, Stack<GameObject>>();

        private static Transform _container;

        // ─────────────────────────────────────────────────────────────
        // Get
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Get a tower from the pool. Creates new instance if pool is empty.
        /// Caller does NOT need to call Initialize — Get does it.
        /// </summary>
        public static TowerBase Get(TowerData data, Transform parent)
        {
            if (data == null)
            {
                Debug.LogError("[TowerPool] Get: data is null");
                return null;
            }

            EnsureContainer();

            GameObject go;

            if (_pools.TryGetValue(data.TowerId, out var stack) && stack.Count > 0)
            {
                go = stack.Pop();
            }
            else
            {
                // TODO (Фаза 2): Instantiate(prefab) из registry вместо new GameObject
                go = new GameObject("Tower_" + data.TowerId);
                go.AddComponent<TowerBase>();
            }

            go.transform.SetParent(parent);
            go.transform.localPosition = Vector3.zero;
            go.SetActive(true);

            TowerBase tower = go.GetComponent<TowerBase>();
            tower.Initialize(data);
            return tower;
        }

        // ─────────────────────────────────────────────────────────────
        // Return
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Return a tower to the pool. Call on removal — never call Destroy directly.
        /// </summary>
        public static void Return(GameObject instance)
        {
            if (instance == null) return;

            EnsureContainer();

            TowerBase tower = instance.GetComponent<TowerBase>();
            string towerId = tower != null && tower.Data != null ? tower.Data.TowerId : null;

            instance.SetActive(false);
            instance.transform.SetParent(_container);

            if (towerId != null)
            {
                if (!_pools.ContainsKey(towerId))
                    _pools[towerId] = new Stack<GameObject>();

                _pools[towerId].Push(instance);
            }
            else
            {
                // Untracked instance — destroy it
                Object.Destroy(instance);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // ClearAll
        // ─────────────────────────────────────────────────────────────

        /// <summary>Destroy all pooled objects. Call on scene reload or app quit.</summary>
        public static void ClearAll()
        {
            foreach (var pool in _pools.Values)
            {
                foreach (var go in pool)
                {
                    if (go != null) Object.Destroy(go);
                }
            }
            _pools.Clear();

            // BUG-003: уничтожаем контейнер, чтобы DontDestroyOnLoad объект не копился между сессиями
            if (_container != null)
            {
                Object.Destroy(_container.gameObject);
                _container = null;
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Private
        // ─────────────────────────────────────────────────────────────

        private static void EnsureContainer()
        {
            if (_container != null) return;

            var go = new GameObject("[TowerPool]");
            Object.DontDestroyOnLoad(go);
            _container = go.transform;
        }
    }
}
