using System;
using UnityEngine;

namespace BogatyrskayaZastava.Data
{
    public enum FactionType
    {
        Druzina,
        Volhvy,
        Luchniki,
        Berserki,
        Znahari
    }

    public enum AbilityType
    {
        None,
        StopSingle,
        SlowSingle,
        SlowCone,
        AoEStun,
        FireDot,
        PoisonDot,
        HealNearest,
        HealAoe,
        ReviveTower,
        StopAll,
        ArrowRain,
        BerserkBuff,
        ShieldAoe
    }

    [Serializable]
    public class AbilityParams
    {
        [Tooltip("Длительность эффекта (стоп, стан, замедление) в секундах")]
        public float duration;

        [Tooltip("Процент замедления в виде числа 0–100, например 20 = 20%. В коде делится на 100.")]
        public float slowPercent;

        /// <summary>Возвращает slowPercent как долю (0.0–1.0) для использования в расчётах.</summary>
        public float SlowFraction => slowPercent / 100f;

        [Tooltip("Радиус AoE-эффекта")]
        public float aoeRadius;

        [Tooltip("Максимальное количество целей для AoE/конуса")]
        public int maxTargets;

        [Tooltip("Кулдаун способности в секундах")]
        public float cooldown;

        [Tooltip("Лечение в HP/сек для лечащих башен")]
        public float healPerSecond;

        [Tooltip("Урон DoT в единицах урона/сек")]
        public float dotDamage;

        [Tooltip("Длительность DoT-эффекта в секундах")]
        public float dotDuration;

        [Tooltip("Процент HP при воскрешении башни (0.0 - 1.0)")]
        public float reviveHpPercent;

        [Tooltip("Количество использований способности за волну (0 = не ограничено)")]
        public int usesPerWave;

        [Tooltip("Длительность блокировки пути препятствием (секунды)")]
        public float blockDuration;

        [Tooltip("Если true — лечение снимает активные дебаффы с цели")]
        public bool cleansesDebuff;

        [Tooltip("Угол конуса атаки в градусах")]
        public float coneAngle;

        [Tooltip("Бонус к урону в единицах (абсолютный)")]
        public float damageBonus;

        [Tooltip("Множитель урона (1.0 = без бонуса)")]
        public float damageMult;
    }

    [CreateAssetMenu(menuName = "BZ/Tower Data", fileName = "NewTowerData")]
    public class TowerData : ScriptableObject
    {
        [Header("Идентификация")]
        [SerializeField] private string towerId;
        [SerializeField] private string towerName;
        [SerializeField] private FactionType faction;
        [SerializeField] private int level;

        [Header("Характеристики")]
        [SerializeField] private float maxHp;
        [SerializeField] private float damage;
        [SerializeField] private float attackSpeed;
        [SerializeField] private float range;
        [SerializeField] private float cost;
        [SerializeField] private float upgradeCost;

        [Header("Способность")]
        [SerializeField] private AbilityType abilityType;
        [SerializeField] private AbilityParams abilityParams;

        [Header("Визуал")]
        [SerializeField] private Sprite idleSprite;
        [SerializeField] private Sprite attackSprite;
        [SerializeField] private Sprite upgradeSprite;

        [Header("Прогрессия")]
        [Tooltip("null если уровень 3 (максимальный)")]
        [SerializeField] private TowerData nextLevelData;

        public string TowerId => towerId;
        public string TowerName => towerName;
        public FactionType Faction => faction;
        public int Level => level;
        public float MaxHp => maxHp;
        public float Damage => damage;
        public float AttackSpeed => attackSpeed;
        public float Range => range;
        public float Cost => cost;
        public float UpgradeCost => upgradeCost;
        public AbilityType AbilityType => abilityType;
        public AbilityParams AbilityParams => abilityParams;
        public Sprite IdleSprite => idleSprite;
        public Sprite AttackSprite => attackSprite;
        public Sprite UpgradeSprite => upgradeSprite;
        public TowerData NextLevelData => nextLevelData;

        /// <summary>
        /// Возвращает true если башня является максимальным уровнем
        /// </summary>
        public bool IsMaxLevel => nextLevelData == null;

        /// <summary>
        /// Возвращает true если башня имеет лечащую способность
        /// </summary>
        public bool IsHealer => abilityType == AbilityType.HealNearest || abilityType == AbilityType.HealAoe;

        /// <summary>
        /// Возвращает true если башня является поддержкой (не атакует напрямую)
        /// </summary>
        public bool IsSupportOnly => damage <= 0f && attackSpeed <= 0f;
    }
}
