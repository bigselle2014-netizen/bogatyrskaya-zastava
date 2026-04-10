using System.Collections.Generic;
using UnityEngine;
using BogatyrskayaZastava.Data;
using BogatyrskayaZastava.Core;
using BogatyrskayaZastava.Idle;

namespace BogatyrskayaZastava.Gameplay
{
    public class TowerPlacementSystem : MonoBehaviour
    {
        [SerializeField] private GameObject towerSlotPrefab;
        [SerializeField] private Transform gridRoot;

        private TowerSlot[,] _grid;
        private TowerData _selectedTowerData;
        private readonly List<TowerBase> _activeTowers = new List<TowerBase>(12);

        private const int MaxTowers = 6;

        private void Awake()
        {
            ServiceLocator.Register<TowerPlacementSystem>(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<TowerPlacementSystem>();
            EventBus.Unsubscribe<TowerPlacedEvent>(OnTowerPlaced);
            EventBus.Unsubscribe<TowerRemovedEvent>(OnTowerRemoved);
        }

        /// <summary>
        /// Инициализирует сетку слотов по данным уровня
        /// </summary>
        public void InitGrid(LevelData level)
        {
            ClearAll();

            int width = level.GridWidth;
            int height = level.GridHeight;
            _grid = new TowerSlot[width, height];

            IReadOnlyList<TowerSlotConfig> slots = level.Slots;
            for (int i = 0; i < slots.Count; i++)
            {
                TowerSlotConfig cfg = slots[i];
                int x = cfg.gridPos.x;
                int y = cfg.gridPos.y;

                if (x < 0 || x >= width || y < 0 || y >= height) continue;

                GameObject slotGo = towerSlotPrefab != null
                    ? Instantiate(towerSlotPrefab, gridRoot)
                    : new GameObject("Slot_" + x + "_" + y);

                slotGo.transform.SetParent(gridRoot);
                slotGo.transform.position = new Vector3(x, y, 0f);

                TowerSlot slot = slotGo.GetComponent<TowerSlot>() ?? slotGo.AddComponent<TowerSlot>();
                slot.Setup(cfg.gridPos, !cfg.isAvailable);

                _grid[x, y] = slot;
            }

            EventBus.Subscribe<TowerPlacedEvent>(OnTowerPlaced);
            EventBus.Subscribe<TowerRemovedEvent>(OnTowerRemoved);
        }

        /// <summary>
        /// Выбирает башню для постановки
        /// </summary>
        public void SelectTower(TowerData data)
        {
            _selectedTowerData = data;
        }

        /// <summary>
        /// Вызывается при клике по слоту. Ставит башню если выбрана и хватает золота.
        /// </summary>
        public void OnSlotClicked(TowerSlot slot)
        {
            if (_selectedTowerData == null) return;
            if (slot.IsOccupied || slot.IsPathCell) return;
            if (_activeTowers.Count >= MaxTowers) return;
            if (!CanAfford(_selectedTowerData)) return;

            if (!ServiceLocator.TryGet<ResourceManager>(out var resources)) return;

            bool spent = resources.SpendGold((int)_selectedTowerData.Cost);
            if (!spent) return;

            bool placed = slot.TryPlaceTower(_selectedTowerData);
            if (!placed)
            {
                resources.AddGold((int)_selectedTowerData.Cost);
            }
        }

        /// <summary>
        /// Проверяет, хватает ли золота на башню
        /// </summary>
        public bool CanAfford(TowerData data)
        {
            if (!ServiceLocator.TryGet<ResourceManager>(out var resources2)) return false;
            return resources2.CanAfford((int)data.Cost);
        }

        /// <summary>
        /// Возвращает список активных башен (только для чтения)
        /// </summary>
        public IReadOnlyList<TowerBase> GetActiveTowers()
        {
            return _activeTowers;
        }

        /// <summary>
        /// Возвращает количество поставленных башен
        /// </summary>
        public int GetTowerCount()
        {
            return _activeTowers.Count;
        }

        /// <summary>
        /// Удаляет все башни с поля (например, при старте нового рана)
        /// </summary>
        public void ClearAll()
        {
            EventBus.Unsubscribe<TowerPlacedEvent>(OnTowerPlaced);
            EventBus.Unsubscribe<TowerRemovedEvent>(OnTowerRemoved);

            if (_grid != null)
            {
                int width = _grid.GetLength(0);
                int height = _grid.GetLength(1);
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        if (_grid[x, y] != null)
                        {
                            Destroy(_grid[x, y].gameObject);
                        }
                    }
                }
                _grid = null;
            }

            _activeTowers.Clear();
            _selectedTowerData = null;
        }

        private void OnTowerPlaced(TowerPlacedEvent evt)
        {
            if (!_activeTowers.Contains(evt.tower))
            {
                _activeTowers.Add(evt.tower);
            }
        }

        private void OnTowerRemoved(TowerRemovedEvent evt)
        {
            // BUG-011: удаляем деактивированные башни (возвращённые в TowerPool.Return → SetActive(false))
            // Проверка на null не работает для pooled объектов (они не Destroy-ятся, только деактивируются)
            for (int i = _activeTowers.Count - 1; i >= 0; i--)
            {
                if (_activeTowers[i] == null || !_activeTowers[i].gameObject.activeSelf)
                {
                    _activeTowers.RemoveAt(i);
                }
            }
        }
    }
}
