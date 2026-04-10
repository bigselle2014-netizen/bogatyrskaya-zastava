using System;
using System.Collections.Generic;
using UnityEngine;

namespace BogatyrskayaZastava.Data
{
    [Serializable]
    public class WaveGroup
    {
        [Tooltip("Тип врага в этой группе")]
        public EnemyData enemyData;

        [Tooltip("Количество врагов в группе")]
        public int count;

        [Tooltip("Интервал между спавном врагов внутри группы (секунды)")]
        public float spawnInterval;

        [Tooltip("Задержка перед началом спавна этой группы (секунды)")]
        public float delayBeforeGroup;
    }

    [CreateAssetMenu(menuName = "BZ/Wave Data", fileName = "NewWaveData")]
    public class WaveData : ScriptableObject
    {
        [SerializeField] private List<WaveGroup> groups = new List<WaveGroup>();

        public IReadOnlyList<WaveGroup> Groups => groups;

        /// <summary>
        /// Возвращает суммарное количество врагов в волне
        /// </summary>
        public int TotalEnemyCount
        {
            get
            {
                int total = 0;
                for (int i = 0; i < groups.Count; i++)
                {
                    total += groups[i].count;
                }
                return total;
            }
        }
    }
}
