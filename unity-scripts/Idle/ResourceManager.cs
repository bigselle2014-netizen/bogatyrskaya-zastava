using UnityEngine;
using BogatyrskayaZastava.Core;

namespace BogatyrskayaZastava.Idle
{
    public class ResourceManager : MonoBehaviour
    {
        private int _gold;
        private int _runeStones;

        public int Gold => _gold;
        public int RuneStones => _runeStones;

        private void Awake()
        {
            ServiceLocator.Register<ResourceManager>(this);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<IdleIncomeReadyEvent>(OnIdleIncome);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<IdleIncomeReadyEvent>(OnIdleIncome);
        }

        /// <summary>
        /// Загружает ресурсы из сохранения при старте
        /// </summary>
        public void InitFromSave(int gold, int runeStones)
        {
            _gold = gold;
            _runeStones = runeStones;
            PublishChanged();
        }

        /// <summary>
        /// Начисляет золото и рассылает событие изменения ресурсов
        /// </summary>
        public void AddGold(int amount)
        {
            if (amount <= 0) return;
            _gold += amount;
            PublishChanged();
        }

        /// <summary>
        /// Тратит золото. Возвращает false если недостаточно средств.
        /// </summary>
        public bool SpendGold(int amount)
        {
            if (amount <= 0) return true;
            if (_gold < amount) return false;

            _gold -= amount;
            PublishChanged();
            return true;
        }

        /// <summary>
        /// Начисляет рунные камни и рассылает событие изменения ресурсов
        /// </summary>
        public void AddRuneStones(int amount)
        {
            if (amount <= 0) return;
            _runeStones += amount;
            PublishChanged();
        }

        /// <summary>
        /// Тратит рунные камни. Возвращает false если недостаточно.
        /// </summary>
        public bool SpendRuneStones(int amount)
        {
            if (amount <= 0) return true;
            if (_runeStones < amount) return false;

            _runeStones -= amount;
            PublishChanged();
            return true;
        }

        /// <summary>
        /// Проверяет, хватает ли золота на покупку
        /// </summary>
        public bool CanAfford(int goldCost)
        {
            return _gold >= goldCost;
        }

        /// <summary>
        /// Возвращает текущее количество золота
        /// </summary>
        public int GetGold()
        {
            return _gold;
        }

        /// <summary>
        /// Возвращает текущее количество рунных камней
        /// </summary>
        public int GetRuneStones()
        {
            return _runeStones;
        }

        private void PublishChanged()
        {
            ResourceChangedEvent evt = new ResourceChangedEvent
            {
                newGold = _gold,
                newRuneStones = _runeStones
            };
            EventBus.Publish(evt);
        }

        private void OnIdleIncome(IdleIncomeReadyEvent evt)
        {
            AddGold((int)evt.gold);
            AddRuneStones((int)evt.runeStones);
        }
    }
}
