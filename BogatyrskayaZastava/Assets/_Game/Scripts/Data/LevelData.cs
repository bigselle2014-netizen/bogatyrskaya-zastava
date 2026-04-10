using System;
using System.Collections.Generic;
using UnityEngine;

namespace BogatyrskayaZastava.Data
{
    public enum BiomeType
    {
        BerezoviyTrakt,
        SelaUReki,
        DikoePolye,
        KikimorinyTopi,
        PeshheryKoshheja,
        PolnochniyPredel,
        ChernayaKrepost,
        IrijSad
    }

    [Serializable]
    public class TowerSlotConfig
    {
        [Tooltip("Позиция слота в сетке (col, row)")]
        public Vector2Int gridPos;

        [Tooltip("Доступен ли слот для постановки башен")]
        public bool isAvailable;
    }

    [CreateAssetMenu(menuName = "BZ/Level Data", fileName = "NewLevelData")]
    public class LevelData : ScriptableObject
    {
        [Header("Идентификация")]
        [SerializeField] private int levelNumber;
        [SerializeField] private string levelName;
        [SerializeField] private BiomeType biome;

        [Header("Волны")]
        [SerializeField] private List<WaveData> waves = new List<WaveData>();
        [Tooltip("Время между волнами (рекомендуется 10-15 сек)")]
        [SerializeField] private float timeBetweenWaves = 12f;

        [Header("Ворота")]
        [SerializeField] private int gateMaxHp = 100;

        [Header("Сетка башен")]
        [Tooltip("Ширина игровой сетки. Прототип: 8")]
        [SerializeField] private int gridWidth = 8;
        [Tooltip("Высота игровой сетки. Прототип: 12")]
        [SerializeField] private int gridHeight = 12;
        [SerializeField] private List<TowerSlotConfig> slots = new List<TowerSlotConfig>();

        [Header("Награды")]
        [SerializeField] private int rewardGold;
        [SerializeField] private int rewardRuneStones;

        public int LevelNumber => levelNumber;
        public string LevelName => levelName;
        public BiomeType Biome => biome;
        public IReadOnlyList<WaveData> Waves => waves;
        public float TimeBetweenWaves => timeBetweenWaves;
        public int GateMaxHp => gateMaxHp;
        public int GridWidth => gridWidth;
        public int GridHeight => gridHeight;
        public IReadOnlyList<TowerSlotConfig> Slots => slots;
        public int RewardGold => rewardGold;
        public int RewardRuneStones => rewardRuneStones;

        /// <summary>
        /// Возвращает количество волн на уровне
        /// </summary>
        public int WaveCount => waves.Count;
    }
}
