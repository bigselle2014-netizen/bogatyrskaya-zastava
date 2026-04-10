using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BogatyrskayaZastava.Data;
using BogatyrskayaZastava.Core;

namespace BogatyrskayaZastava.Gameplay
{
    public enum DotType
    {
        Fire,
        Poison
    }

    internal struct SlowEntry
    {
        public float percent;
        public float remainingTime;
    }

    internal struct DotEntry
    {
        public DotType type;
        public float damagePerSecond;
        public float remainingTime;
    }

    internal struct StopEntry
    {
        public float remainingTime;
    }

    public class EnemyBase : MonoBehaviour
    {
        private EnemyData _data;
        private float _currentHp;
        private float _currentSpeed;
        private float _speedMultiplier = 1f;
        private bool _isDead;
        private int _pathIndex;
        private float _remainingPathDistance;

        private List<Vector3> _path;
        private Transform _gateTransform;

        private readonly List<SlowEntry> _slowEntries = new List<SlowEntry>(4);
        private readonly List<DotEntry> _dotEntries = new List<DotEntry>(4);
        private float _stopTimer;

        public EnemyData Data => _data;
        public float CurrentHp => _currentHp;
        public float CurrentSpeed => _currentSpeed;
        public bool IsDead => _isDead;
        public float RemainingPathDistance => _remainingPathDistance;

        /// <summary>
        /// Инициализирует врага данными и путём движения
        /// </summary>
        public void Initialize(EnemyData data, List<Vector3> path, Transform gateTransform = null)
        {
            _data = data;
            _currentHp = data.BaseHp;
            _currentSpeed = data.BaseSpeed;
            _speedMultiplier = 1f;
            _isDead = false;
            _pathIndex = 0;
            _stopTimer = 0f;
            _path = path;
            _gateTransform = gateTransform;

            _slowEntries.Clear();
            _dotEntries.Clear();

            if (_path != null && _path.Count > 0)
            {
                transform.position = _path[0];
                _remainingPathDistance = CalculateRemainingDistance();
            }
        }

        /// <summary>
        /// Наносит урон врагу. При hp <= 0 вызывает Die()
        /// </summary>
        public void TakeDamage(float amount)
        {
            if (_isDead) return;

            _currentHp -= amount;
            if (_currentHp <= 0f)
            {
                Die();
            }
        }

        /// <summary>
        /// Применяет замедление (стакается мультипликативно)
        /// </summary>
        public void ApplySlow(float percent, float duration)
        {
            if (_isDead) return;

            SlowEntry entry = new SlowEntry
            {
                percent = percent,
                remainingTime = duration
            };
            _slowEntries.Add(entry);
            RecalculateSpeed();
        }

        /// <summary>
        /// Применяет DoT-эффект (Fire или Poison). Стакается.
        /// </summary>
        public void ApplyDot(float damagePerSecond, float duration, DotType type)
        {
            if (_isDead) return;

            DotEntry entry = new DotEntry
            {
                type = type,
                damagePerSecond = damagePerSecond,
                remainingTime = duration
            };
            _dotEntries.Add(entry);
        }

        /// <summary>
        /// Останавливает движение врага на заданное время (DR-1 стоп 0.5с)
        /// </summary>
        public void StopMovement(float duration)
        {
            if (_isDead) return;
            if (duration > _stopTimer)
            {
                _stopTimer = duration;
            }
        }

        /// <summary>
        /// Сбрасывает всё состояние врага перед возвратом в пул.
        /// Вызывается из EnemyPool.Return() — UD-06.
        /// </summary>
        public void ResetState()
        {
            _currentHp = 0f;
            _currentSpeed = 0f;
            _speedMultiplier = 1f;
            _isDead = true;
            _pathIndex = 0;
            _remainingPathDistance = 0f;
            _stopTimer = 0f;
            _slowEntries.Clear();
            _dotEntries.Clear();
            _data = null;
            _path = null;
            _gateTransform = null;
        }

        private void Die()
        {
            if (_isDead) return;
            _isDead = true;

            EnemyDiedEvent evt = new EnemyDiedEvent
            {
                enemyDataId = _data.EnemyId,
                position = transform.position
            };
            EventBus.Publish(evt);

            EnemyPool.Return(gameObject);
        }

        private void ReachGate()
        {
            if (_isDead) return;
            _isDead = true;

            EnemyReachedGateEvent gateEvt = new EnemyReachedGateEvent
            {
                enemyId = _data.EnemyId,
                damage = (int)_data.GateDamage
            };
            EventBus.Publish(gateEvt);

            EnemyPool.Return(gameObject);
        }

        private void Update()
        {
            if (_isDead) return;

            float dt = Time.deltaTime;

            UpdateStopTimer(dt);
            UpdateSlowTimers(dt);
            UpdateDotTimers(dt);

            if (_stopTimer > 0f) return;

            if (_data.IgnoresPath)
            {
                MoveDirectToGate(dt);
            }
            else
            {
                MoveAlongPath(dt);
            }
        }

        private void UpdateStopTimer(float dt)
        {
            if (_stopTimer > 0f)
            {
                _stopTimer -= dt;
                if (_stopTimer < 0f) _stopTimer = 0f;
            }
        }

        private void UpdateSlowTimers(float dt)
        {
            bool changed = false;
            for (int i = _slowEntries.Count - 1; i >= 0; i--)
            {
                SlowEntry entry = _slowEntries[i];
                entry.remainingTime -= dt;
                if (entry.remainingTime <= 0f)
                {
                    _slowEntries.RemoveAt(i);
                    changed = true;
                }
                else
                {
                    _slowEntries[i] = entry;
                }
            }
            if (changed) RecalculateSpeed();
        }

        private void UpdateDotTimers(float dt)
        {
            float totalDot = 0f;
            for (int i = _dotEntries.Count - 1; i >= 0; i--)
            {
                DotEntry entry = _dotEntries[i];
                totalDot += entry.damagePerSecond * dt;
                entry.remainingTime -= dt;
                if (entry.remainingTime <= 0f)
                {
                    _dotEntries.RemoveAt(i);
                }
                else
                {
                    _dotEntries[i] = entry;
                }
            }
            if (totalDot > 0f)
            {
                TakeDamage(totalDot);
            }
        }

        private void RecalculateSpeed()
        {
            if (_data == null) return;
            float multiplier = 1f;
            for (int i = 0; i < _slowEntries.Count; i++)
            {
                multiplier *= (1f - _slowEntries[i].percent);
            }
            _speedMultiplier = multiplier;
            _currentSpeed = _data.BaseSpeed * _speedMultiplier;
        }

        private void MoveAlongPath(float dt)
        {
            if (_path == null || _pathIndex >= _path.Count) return;

            Vector3 target = _path[_pathIndex];
            Vector3 direction = target - transform.position;
            float distanceToTarget = direction.magnitude;
            float step = _currentSpeed * dt;

            if (step >= distanceToTarget)
            {
                transform.position = target;
                _pathIndex++;

                if (_pathIndex >= _path.Count)
                {
                    ReachGate();
                    return;
                }

                float leftover = step - distanceToTarget;
                if (leftover > 0f && _pathIndex < _path.Count)
                {
                    Vector3 nextDir = (_path[_pathIndex] - transform.position).normalized;
                    transform.position += nextDir * leftover;
                }
            }
            else
            {
                transform.position += direction.normalized * step;
            }

            _remainingPathDistance = CalculateRemainingDistance();
        }

        private void MoveDirectToGate(float dt)
        {
            if (_gateTransform == null) return;

            Vector3 target = _gateTransform.position;
            Vector3 direction = target - transform.position;
            float distance = direction.magnitude;
            float step = _currentSpeed * dt;

            if (step >= distance)
            {
                transform.position = target;
                ReachGate();
            }
            else
            {
                transform.position += direction.normalized * step;
                _remainingPathDistance = distance - step;
            }
        }

        private float CalculateRemainingDistance()
        {
            if (_path == null || _pathIndex >= _path.Count) return 0f;

            float dist = Vector3.Distance(transform.position, _path[_pathIndex]);
            for (int i = _pathIndex; i < _path.Count - 1; i++)
            {
                dist += Vector3.Distance(_path[i], _path[i + 1]);
            }
            return dist;
        }
    }
}
