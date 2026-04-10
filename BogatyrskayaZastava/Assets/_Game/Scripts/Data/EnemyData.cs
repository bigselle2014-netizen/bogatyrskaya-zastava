using UnityEngine;

namespace BogatyrskayaZastava.Data
{
    public enum EnemyType
    {
        Peshiy,
        Bronevaniy,
        Bystry,
        Letun
    }

    [CreateAssetMenu(menuName = "BZ/Enemy Data", fileName = "NewEnemyData")]
    public class EnemyData : ScriptableObject
    {
        [Header("Идентификация")]
        [SerializeField] private string enemyId;
        [SerializeField] private string enemyName;
        [SerializeField] private EnemyType enemyType;

        [Header("Характеристики")]
        [SerializeField] private float baseHp;
        [SerializeField] private float baseSpeed;
        [SerializeField] private float gateDamage;

        [Header("Поведение")]
        [Tooltip("Летун игнорирует waypoint-путь и летит напрямую к воротам")]
        [SerializeField] private bool ignoresPath;

        [Header("Визуал")]
        [SerializeField] private Sprite sprite;

        public string EnemyId => enemyId;
        public string EnemyName => enemyName;
        public EnemyType EnemyType => enemyType;
        public float BaseHp => baseHp;
        public float BaseSpeed => baseSpeed;
        public float GateDamage => gateDamage;
        public bool IgnoresPath => ignoresPath;
        public Sprite Sprite => sprite;
    }
}
