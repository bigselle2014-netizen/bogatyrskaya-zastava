using UnityEngine;
using BogatyrskayaZastava.Core;

namespace BogatyrskayaZastava.Gameplay
{
    public class GateController : MonoBehaviour
    {
        private int _maxHp;
        private int _currentHp;
        private bool _isDestroyed;

        public int MaxHp => _maxHp;
        public int CurrentHp => _currentHp;
        public bool IsDestroyed => _isDestroyed;

        private void Awake()
        {
            ServiceLocator.Register<GateController>(this);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<EnemyReachedGateEvent>(OnEnemyReachedGate);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EnemyReachedGateEvent>(OnEnemyReachedGate);
        }

        /// <summary>
        /// Инициализирует ворота максимальным HP (берётся из LevelData)
        /// </summary>
        public void Initialize(int maxHp)
        {
            _maxHp = maxHp;
            _currentHp = maxHp;
            _isDestroyed = false;

            GateHpChangedEvent evt = new GateHpChangedEvent { current = _currentHp, max = _maxHp };
            EventBus.Publish(evt);
        }

        /// <summary>
        /// Наносит урон воротам. При hp <= 0 вызывает OnGateDestroyed.
        /// Вызывается из EnemyBase.ReachGate() через EnemyReachedGateEvent.
        /// </summary>
        public void TakeDamage(int amount)
        {
            if (_isDestroyed) return;

            _currentHp -= amount;
            if (_currentHp < 0) _currentHp = 0;

            GateHpChangedEvent hpEvt = new GateHpChangedEvent { current = _currentHp, max = _maxHp };
            EventBus.Publish(hpEvt);

            if (_currentHp <= 0)
            {
                OnGateDestroyed();
            }
        }

        private void OnGateDestroyed()
        {
            if (_isDestroyed) return;
            _isDestroyed = true;

            EventBus.Publish(new GateDestroyedEvent());
        }

        private void OnEnemyReachedGate(EnemyReachedGateEvent evt)
        {
            TakeDamage(evt.damage);
        }
    }
}
