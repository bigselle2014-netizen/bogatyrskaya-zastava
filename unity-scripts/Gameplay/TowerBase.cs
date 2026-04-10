using System.Collections.Generic;
using UnityEngine;
using BogatyrskayaZastava.Data;
using BogatyrskayaZastava.Core;

namespace BogatyrskayaZastava.Gameplay
{
    public struct SynergyEffect
    {
        public string effectId;
        public float value;
    }

    public class TowerBase : MonoBehaviour
    {
        private TowerData _data;
        private float _currentHp;
        private bool _isAlive;
        private float _lastAttackTime;
        private float _abilityCooldownTimer;
        private int _abilityUsesThisWave;

        private static readonly List<EnemyBase> _targetsBuffer = new List<EnemyBase>(16);

        public TowerData Data => _data;
        public float CurrentHp => _currentHp;
        public bool IsAlive => _isAlive;

        /// <summary>
        /// Инициализирует башню данными из TowerData
        /// </summary>
        public void Initialize(TowerData data)
        {
            _data = data;
            _currentHp = data.MaxHp;
            _isAlive = true;
            _lastAttackTime = -999f;
            _abilityCooldownTimer = 0f;
            _abilityUsesThisWave = 0;
        }

        /// <summary>
        /// Наносит урон башне (используется системами синергии, например ZN-3)
        /// </summary>
        public void TakeDamage(float amount)
        {
            if (!_isAlive) return;

            _currentHp -= amount;
            if (_currentHp <= 0f)
            {
                _currentHp = 0f;
                _isAlive = false;
            }
        }

        /// <summary>
        /// Восстанавливает HP башни (ZN-1 Травник, ZN-2 Знахарка)
        /// </summary>
        public void Heal(float amount)
        {
            if (!_isAlive) return;

            _currentHp += amount;
            if (_currentHp > _data.MaxHp)
            {
                _currentHp = _data.MaxHp;
            }
        }

        /// <summary>
        /// Воскрешает башню с заданным процентом HP (ZN-3 Белояр)
        /// </summary>
        public void Revive(float hpPercent)
        {
            if (_isAlive) return;

            _isAlive = true;
            _currentHp = _data.MaxHp * Mathf.Clamp01(hpPercent);
        }

        /// <summary>
        /// Применяет эффект синергии фракции. Реализация в Фазе 2.
        /// </summary>
        public void ApplySynergyBonus(SynergyEffect effect)
        {
            // TODO (Фаза 2): реализовать систему синергий фракций
        }

        /// <summary>
        /// Главный метод атаки. Проверяет кулдаун attackSpeed, находит цели, вызывает UseAbility.
        /// Вызывается из WaveManager каждый кадр.
        /// </summary>
        public void TryAttack(List<EnemyBase> allEnemies)
        {
            if (!_isAlive) return;
            if (_data == null) return;

            float now = Time.time;

            if (_abilityCooldownTimer > 0f)
            {
                _abilityCooldownTimer -= Time.deltaTime;
            }

            bool isHealerOrSupport = _data.IsSupportOnly;

            if (isHealerOrSupport)
            {
                TryUsePassiveAbility();
                return;
            }

            float attackInterval = _data.AttackSpeed > 0f ? 1f / _data.AttackSpeed : float.MaxValue;
            if (now - _lastAttackTime < attackInterval) return;

            EnemyBase primaryTarget = FindTarget(allEnemies);
            if (primaryTarget == null) return;

            _lastAttackTime = now;

            if (_data.Damage > 0f)
            {
                primaryTarget.TakeDamage(_data.Damage);
            }

            FillNearbyTargets(primaryTarget, allEnemies);
            UseAbility(primaryTarget, _targetsBuffer);
        }

        /// <summary>
        /// Применяет способность башни к цели и ближайшим врагам
        /// </summary>
        public void UseAbility(EnemyBase primaryTarget, List<EnemyBase> nearbyTargets)
        {
            if (_data == null || _data.AbilityParams == null) return;

            AbilityParams p = _data.AbilityParams;

            switch (_data.AbilityType)
            {
                case AbilityType.StopSingle:
                    primaryTarget.StopMovement(p.duration);
                    break;

                case AbilityType.SlowSingle:
                    primaryTarget.ApplySlow(p.SlowFraction, p.duration);
                    break;

                case AbilityType.SlowCone:
                    int coneCount = Mathf.Min(nearbyTargets.Count, p.maxTargets > 0 ? p.maxTargets : nearbyTargets.Count);
                    for (int i = 0; i < coneCount; i++)
                    {
                        nearbyTargets[i].ApplySlow(p.SlowFraction, p.duration);
                    }
                    break;

                case AbilityType.AoEStun:
                    if (_abilityCooldownTimer > 0f) break;
                    int stunCount = Mathf.Min(nearbyTargets.Count, p.maxTargets > 0 ? p.maxTargets : nearbyTargets.Count);
                    for (int i = 0; i < stunCount; i++)
                    {
                        nearbyTargets[i].StopMovement(p.duration);
                    }
                    _abilityCooldownTimer = p.cooldown;
                    break;

                case AbilityType.FireDot:
                    primaryTarget.ApplyDot(p.dotDamage, p.dotDuration, DotType.Fire);
                    break;

                case AbilityType.PoisonDot:
                    primaryTarget.ApplyDot(p.dotDamage, p.dotDuration, DotType.Poison);
                    break;

                case AbilityType.StopAll:
                    if (_abilityCooldownTimer > 0f) break;
                    if (p.usesPerWave > 0 && _abilityUsesThisWave >= p.usesPerWave) break;
                    for (int i = 0; i < nearbyTargets.Count; i++)
                    {
                        nearbyTargets[i].StopMovement(p.duration);
                    }
                    _abilityUsesThisWave++;
                    _abilityCooldownTimer = p.cooldown;
                    break;

                case AbilityType.ArrowRain:
                    if (_abilityCooldownTimer > 0f) break;
                    int rainCount = Mathf.Min(nearbyTargets.Count, p.maxTargets > 0 ? p.maxTargets : nearbyTargets.Count);
                    // i=1: primary target (index 0) already damaged in TryAttack
                    for (int i = 1; i < rainCount; i++)
                    {
                        nearbyTargets[i].TakeDamage(_data.Damage);
                    }
                    _abilityCooldownTimer = p.cooldown;
                    break;

                case AbilityType.BerserkBuff:
                    // TODO (Фаза 2): баф урона для соседних башен фракции Берсерки
                    break;

                case AbilityType.ShieldAoe:
                    // TODO (Фаза 2): щит для союзных башен в радиусе
                    break;

                case AbilityType.None:
                default:
                    break;
            }
        }

        /// <summary>
        /// Сбрасывает счётчики использований способностей в конце волны
        /// </summary>
        public void OnWaveEnd()
        {
            _abilityUsesThisWave = 0;
        }

        private void TryUsePassiveAbility()
        {
            if (_data.AbilityParams == null) return;
            AbilityParams p = _data.AbilityParams;

            switch (_data.AbilityType)
            {
                case AbilityType.HealNearest:
                    TryHealNearestTower(p.healPerSecond * Time.deltaTime);
                    break;

                case AbilityType.HealAoe:
                    TryHealAllInRange(p.healPerSecond * Time.deltaTime, p.aoeRadius);
                    break;

                case AbilityType.ReviveTower:
                    if (_abilityCooldownTimer <= 0f)
                    {
                        if (p.usesPerWave <= 0 || _abilityUsesThisWave < p.usesPerWave)
                        {
                            TryReviveNearestDeadTower(p.reviveHpPercent);
                        }
                    }
                    break;
            }
        }

        private void TryHealNearestTower(float healAmount)
        {
            if (!ServiceLocator.TryGet<TowerPlacementSystem>(out var placement)) return;

            IReadOnlyList<TowerBase> towers = placement.GetActiveTowers();
            TowerBase nearestAlive = null;
            float nearestDist = float.MaxValue;
            float myRange = _data.Range;

            for (int i = 0; i < towers.Count; i++)
            {
                TowerBase t = towers[i];
                if (t == this || !t.IsAlive) continue;
                if (t.CurrentHp >= t.Data.MaxHp) continue;

                float dist = Vector3.Distance(transform.position, t.transform.position);
                if (dist <= myRange && dist < nearestDist)
                {
                    nearestDist = dist;
                    nearestAlive = t;
                }
            }

            if (nearestAlive != null)
            {
                nearestAlive.Heal(healAmount);
            }
        }

        private void TryHealAllInRange(float healAmount, float radius)
        {
            if (!ServiceLocator.TryGet<TowerPlacementSystem>(out var placement)) return;

            IReadOnlyList<TowerBase> towers = placement.GetActiveTowers();
            float effectiveRadius = radius > 0f ? radius : _data.Range;

            for (int i = 0; i < towers.Count; i++)
            {
                TowerBase t = towers[i];
                if (t == this || !t.IsAlive) continue;
                if (t.CurrentHp >= t.Data.MaxHp) continue;

                float dist = Vector3.Distance(transform.position, t.transform.position);
                if (dist <= effectiveRadius)
                {
                    t.Heal(healAmount);
                }
            }
        }

        private void TryReviveNearestDeadTower(float hpPercent)
        {
            if (!ServiceLocator.TryGet<TowerPlacementSystem>(out var placement)) return;

            IReadOnlyList<TowerBase> towers = placement.GetActiveTowers();
            TowerBase nearestDead = null;
            float nearestDist = float.MaxValue;

            for (int i = 0; i < towers.Count; i++)
            {
                TowerBase t = towers[i];
                if (t == this || t.IsAlive) continue;

                float dist = Vector3.Distance(transform.position, t.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearestDead = t;
                }
            }

            if (nearestDead != null)
            {
                nearestDead.Revive(hpPercent);
                _abilityUsesThisWave++;
                _abilityCooldownTimer = _data.AbilityParams.cooldown;
            }
        }

        private void FillNearbyTargets(EnemyBase primary, List<EnemyBase> allEnemies)
        {
            _targetsBuffer.Clear();
            _targetsBuffer.Add(primary);

            float radius = _data.AbilityParams != null && _data.AbilityParams.aoeRadius > 0f
                ? _data.AbilityParams.aoeRadius
                : _data.Range;

            Vector3 myPos = transform.position;
            for (int i = 0; i < allEnemies.Count; i++)
            {
                EnemyBase e = allEnemies[i];
                if (e == primary || e.IsDead) continue;

                float dist = Vector3.Distance(myPos, e.transform.position);
                if (dist <= radius)
                {
                    _targetsBuffer.Add(e);
                }
            }
        }

        /// <summary>
        /// Стратегия first-in-path: выбирает врага с минимальным remainingPathDistance
        /// </summary>
        private EnemyBase FindTarget(List<EnemyBase> enemies)
        {
            EnemyBase best = null;
            float bestDist = float.MaxValue;
            float myRange = _data.Range;
            Vector3 myPos = transform.position;

            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyBase e = enemies[i];
                if (e == null || e.IsDead) continue;

                float spatialDist = Vector3.Distance(myPos, e.transform.position);
                if (spatialDist > myRange) continue;

                if (e.RemainingPathDistance < bestDist)
                {
                    bestDist = e.RemainingPathDistance;
                    best = e;
                }
            }

            return best;
        }
    }
}
