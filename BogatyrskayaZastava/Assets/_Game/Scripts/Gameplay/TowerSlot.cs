using UnityEngine;
using UnityEngine.EventSystems;
using BogatyrskayaZastava.Data;
using BogatyrskayaZastava.Core;

namespace BogatyrskayaZastava.Gameplay
{
    public struct TowerPlacedEvent
    {
        public TowerBase tower;
        public Vector2Int gridPosition;
    }

    public struct TowerRemovedEvent
    {
        public TowerData towerData;
        public Vector2Int gridPosition;
    }

    public class TowerSlot : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Vector2Int gridPosition;
        [SerializeField] private bool isPathCell;

        private bool _isOccupied;
        private TowerBase _placedTower;

        public Vector2Int GridPosition => gridPosition;
        public bool IsOccupied => _isOccupied;
        public TowerBase PlacedTower => _placedTower;
        public bool IsPathCell => isPathCell;

        /// <summary>
        /// Инициализирует слот позицией в сетке и флагом пути
        /// </summary>
        public void Setup(Vector2Int pos, bool pathCell)
        {
            gridPosition = pos;
            isPathCell = pathCell;
        }

        /// <summary>
        /// Пытается разместить башню в слоте. Возвращает false если слот занят, path-cell или нет данных.
        /// </summary>
        public bool TryPlaceTower(TowerData data)
        {
            if (_isOccupied || isPathCell || data == null) return false;

            // QA-030: используем TowerPool вместо new GameObject + AddComponent
            TowerBase tower = TowerPool.Get(data, transform);
            if (tower == null) return false;

            tower.transform.position = transform.position;

            _placedTower = tower;
            _isOccupied = true;

            TowerPlacedEvent evt = new TowerPlacedEvent
            {
                tower = tower,
                gridPosition = gridPosition
            };
            EventBus.Publish(evt);

            return true;
        }

        /// <summary>
        /// Удаляет башню из слота
        /// </summary>
        public void RemoveTower()
        {
            if (!_isOccupied || _placedTower == null) return;

            TowerData removedData = _placedTower.Data;

            // QA-031: возврат в пул вместо Destroy
            TowerPool.Return(_placedTower.gameObject);
            _placedTower = null;
            _isOccupied = false;

            TowerRemovedEvent evt = new TowerRemovedEvent
            {
                towerData = removedData,
                gridPosition = gridPosition
            };
            EventBus.Publish(evt);
        }

        /// <summary>
        /// Заменяет башню на следующий уровень (nextLevelData). Ничего не делает если башни нет или нет nextLevelData.
        /// </summary>
        public void UpgradeTower()
        {
            if (!_isOccupied || _placedTower == null) return;
            if (_placedTower.Data.IsMaxLevel) return;

            TowerData nextData = _placedTower.Data.NextLevelData;
            RemoveTower();
            TryPlaceTower(nextData);
        }

        /// <summary>
        /// Обработчик клика по слоту. Вызывает TowerPlacementSystem.OnSlotClicked.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (ServiceLocator.TryGet<TowerPlacementSystem>(out var placement))
            {
                placement.OnSlotClicked(this);
            }
        }
    }
}
