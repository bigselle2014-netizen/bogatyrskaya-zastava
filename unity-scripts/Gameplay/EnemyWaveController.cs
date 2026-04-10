using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BogatyrskayaZastava.Data;
using BogatyrskayaZastava.Core;

namespace BogatyrskayaZastava.Gameplay
{
    public class EnemyWaveController : MonoBehaviour
    {
        [SerializeField] private Transform gateTransform;
        [SerializeField] private Transform[] waypointTransforms;

        private LevelData _currentLevel;
        private int _currentWaveIndex;
        private int _totalWaves;
        private float _timeBetweenWaves;
        private bool _isWaveActive;
        private readonly List<EnemyBase> _activeEnemies = new List<EnemyBase>(32);
        private List<Vector3> _enemyPath;
        private Coroutine _waveCoroutine;
        private Coroutine _betweenWavesCoroutine;
        private TowerPlacementSystem _placementSystemCache;

        public bool IsWaveActive => _isWaveActive;
        public int CurrentWaveIndex => _currentWaveIndex;
        public float TimeBetweenWaves => _timeBetweenWaves;

        private void OnDestroy()
        {
            EventBus.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
            EventBus.Unsubscribe<EnemyReachedGateEvent>(OnEnemyReachedGate);
        }

        /// <summary>
        /// Начинает уровень: инициализирует данные, строит путь из waypoints, подписывается на события
        /// </summary>
        public void StartLevel(LevelData level)
        {
            _currentLevel = level;
            _currentWaveIndex = 0;
            _totalWaves = level.WaveCount;
            _timeBetweenWaves = level.TimeBetweenWaves;
            _isWaveActive = false;
            _activeEnemies.Clear();

            BuildPathFromWaypoints();

            EventBus.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
            EventBus.Unsubscribe<EnemyReachedGateEvent>(OnEnemyReachedGate);

            EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
            EventBus.Subscribe<EnemyReachedGateEvent>(OnEnemyReachedGate);

            ServiceLocator.TryGet<TowerPlacementSystem>(out _placementSystemCache);
        }

        /// <summary>
        /// Запускает следующую волну. Вызывается кнопкой HUD или таймером.
        /// </summary>
        public void StartNextWave()
        {
            if (_isWaveActive) return;
            if (_currentWaveIndex >= _totalWaves) return;
            if (_currentLevel == null) return;

            if (_betweenWavesCoroutine != null)
            {
                StopCoroutine(_betweenWavesCoroutine);
                _betweenWavesCoroutine = null;
            }

            WaveData wave = _currentLevel.Waves[_currentWaveIndex];
            _waveCoroutine = StartCoroutine(SpawnWave(wave));
        }

        private IEnumerator SpawnWave(WaveData wave)
        {
            _isWaveActive = true;

            WaveStartedEvent startEvt = new WaveStartedEvent
            {
                waveNumber = _currentWaveIndex + 1,
                totalWaves = _totalWaves
            };
            EventBus.Publish(startEvt);

            IReadOnlyList<WaveGroup> groups = wave.Groups;
            for (int g = 0; g < groups.Count; g++)
            {
                WaveGroup group = groups[g];

                if (group.delayBeforeGroup > 0f)
                {
                    yield return new WaitForSeconds(group.delayBeforeGroup);
                }

                for (int e = 0; e < group.count; e++)
                {
                    SpawnEnemy(group.enemyData);

                    if (group.spawnInterval > 0f)
                    {
                        yield return new WaitForSeconds(group.spawnInterval);
                    }
                }
            }

            _waveCoroutine = null;
        }

        private void SpawnEnemy(EnemyData data)
        {
            if (data == null) return;

            GameObject enemyGo = EnemyPool.Get(data.EnemyId);
            if (enemyGo == null) return;

            EnemyBase enemy = enemyGo.GetComponent<EnemyBase>();
            if (enemy == null) return;

            enemy.Initialize(data, _enemyPath, gateTransform);
            _activeEnemies.Add(enemy);
        }

        private void OnEnemyDied(EnemyDiedEvent evt)
        {
            RemoveDeadAndInactiveFromActive();
            CheckWaveEnd();
        }

        private void OnEnemyReachedGate(EnemyReachedGateEvent evt)
        {
            RemoveDeadAndInactiveFromActive();
            CheckWaveEnd();
        }

        private void RemoveDeadAndInactiveFromActive()
        {
            for (int i = _activeEnemies.Count - 1; i >= 0; i--)
            {
                if (_activeEnemies[i] == null || _activeEnemies[i].IsDead || !_activeEnemies[i].gameObject.activeInHierarchy)
                {
                    _activeEnemies.RemoveAt(i);
                }
            }
        }

        private void CheckWaveEnd()
        {
            if (!_isWaveActive) return;
            if (_waveCoroutine != null) return;
            if (_activeEnemies.Count > 0) return;

            OnWaveComplete();
        }

        private void OnWaveComplete()
        {
            _isWaveActive = false;

            WaveCompletedEvent evt = new WaveCompletedEvent
            {
                waveNumber = _currentWaveIndex + 1
            };
            EventBus.Publish(evt);

            NotifyTowersWaveEnd();

            _currentWaveIndex++;

            if (_currentWaveIndex >= _totalWaves)
            {
                OnLevelComplete();
                return;
            }

            _betweenWavesCoroutine = StartCoroutine(WaitAndAutoStartNext());
        }

        private IEnumerator WaitAndAutoStartNext()
        {
            yield return new WaitForSeconds(_timeBetweenWaves);
            _betweenWavesCoroutine = null;
            StartNextWave();
        }

        private void OnLevelComplete()
        {
            ServiceLocator.TryGet<GateController>(out var gate);
            int remainingHp = gate != null ? gate.CurrentHp : 0;

            LevelCompletedEvent evt = new LevelCompletedEvent
            {
                levelNumber = _currentLevel.LevelNumber,
                remainingGateHp = remainingHp
            };
            EventBus.Publish(evt);
        }

        private void NotifyTowersWaveEnd()
        {
            if (!ServiceLocator.TryGet<TowerPlacementSystem>(out var placement)) return;

            IReadOnlyList<TowerBase> towers = placement.GetActiveTowers();
            for (int i = 0; i < towers.Count; i++)
            {
                towers[i].OnWaveEnd();
            }
        }

        private void BuildPathFromWaypoints()
        {
            _enemyPath = new List<Vector3>(waypointTransforms != null ? waypointTransforms.Length : 0);

            if (waypointTransforms == null || waypointTransforms.Length == 0) return;

            for (int i = 0; i < waypointTransforms.Length; i++)
            {
                if (waypointTransforms[i] != null)
                {
                    _enemyPath.Add(waypointTransforms[i].position);
                }
            }
        }

        private void Update()
        {
            if (!_isWaveActive || _activeEnemies.Count == 0) return;
            if (_placementSystemCache == null) return;

            IReadOnlyList<TowerBase> towers = _placementSystemCache.GetActiveTowers();
            for (int i = 0; i < towers.Count; i++)
            {
                towers[i].TryAttack(_activeEnemies);
            }
        }
    }
}
