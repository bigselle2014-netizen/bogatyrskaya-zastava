using System;
using System.Collections.Generic;
using UnityEngine;
using BogatyrskayaZastava.Core;

namespace BogatyrskayaZastava.Gameplay
{
    public enum RunState
    {
        Preparing,
        InWave,
        BetweenWaves,
        DeckChoice,
        MiniBoss,
        FinalBoss,
        RunComplete
    }

    public class RunManager : MonoBehaviour
    {
        private const int TotalWaves = 10;
        private const int MiniBossWave = 5;
        private const float RandomEventChance = 0.3f;
        private const int DeckChoiceCount = 3;

        private RunState _state;
        private int _currentWave;
        private int _runesEarned;
        private int _coinsEarned;
        private int _synergiesActivatedCount;
        private float _runStartTime;
        private bool _runActive;

        private readonly System.Random _rng = new System.Random();

        public RunState State => _state;
        public int CurrentWave => _currentWave;
        public bool IsRunActive => _runActive;

        private void Awake()
        {
            ServiceLocator.Register<RunManager>(this);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<WaveCompletedEvent>(OnWaveCompleted);
            EventBus.Subscribe<SynergyActivatedEvent>(OnSynergyActivated);
            EventBus.Subscribe<GateDestroyedEvent>(OnGateDestroyed);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<WaveCompletedEvent>(OnWaveCompleted);
            EventBus.Unsubscribe<SynergyActivatedEvent>(OnSynergyActivated);
            EventBus.Unsubscribe<GateDestroyedEvent>(OnGateDestroyed);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<RunManager>();
        }

        public void StartRun()
        {
            _currentWave = 0;
            _runesEarned = 0;
            _coinsEarned = 0;
            _synergiesActivatedCount = 0;
            _runStartTime = Time.time;
            _runActive = true;

            SetState(RunState.Preparing);

            if (ServiceLocator.TryGet<DeckBuilder>(out var deckBuilder))
            {
                deckBuilder.InitializeForRun();
            }

            EventBus.Publish(new RunStartedEvent { runIndex = GetRunIndex() });

            TransitionToDeckChoice();
        }

        public void OnDeckChoiceComplete()
        {
            if (_state != RunState.DeckChoice) return;

            StartNextWave();
        }

        public void OnRandomEventResolved()
        {
            if (_state != RunState.BetweenWaves) return;

            TransitionToDeckChoice();
        }

        private void OnWaveCompleted(WaveCompletedEvent evt)
        {
            if (!_runActive) return;

            _currentWave = evt.waveNumber;
            _coinsEarned += CalculateWaveCoins(evt.waveNumber);

            if (_currentWave >= TotalWaves)
            {
                SetState(RunState.FinalBoss);
                StartBossWave(isFinal: true);
                return;
            }

            if (_currentWave == MiniBossWave)
            {
                SetState(RunState.MiniBoss);
                StartBossWave(isFinal: false);
                return;
            }

            SetState(RunState.BetweenWaves);

            if (RollRandomEvent())
            {
                EventBus.Publish(new RandomEventTriggeredEvent
                {
                    waveNumber = _currentWave,
                    eventSeed = _rng.Next()
                });
            }
            else
            {
                TransitionToDeckChoice();
            }
        }

        private void OnSynergyActivated(SynergyActivatedEvent evt)
        {
            if (_runActive)
                _synergiesActivatedCount++;
        }

        private void OnGateDestroyed(GateDestroyedEvent evt)
        {
            if (!_runActive) return;

            CompleteRun(isVictory: false);
        }

        public void OnBossDefeated(bool isFinalBoss)
        {
            if (!_runActive) return;

            if (isFinalBoss)
            {
                _runesEarned += CalculateFinalBossRunes();
                CompleteRun(isVictory: true);
            }
            else
            {
                _runesEarned += CalculateMiniBossRunes();
                SetState(RunState.BetweenWaves);
                TransitionToDeckChoice();
            }
        }

        private void CompleteRun(bool isVictory)
        {
            _runActive = false;
            SetState(RunState.RunComplete);

            if (isVictory)
                _runesEarned += CalculateVictoryBonus();

            RunCompleteData data = new RunCompleteData
            {
                wavesCleared = _currentWave,
                runesEarned = _runesEarned,
                coinsEarned = _coinsEarned,
                synergiesActivated = _synergiesActivatedCount,
                isVictory = isVictory,
                totalTime = Time.time - _runStartTime
            };

            EventBus.Publish(new RunCompletedEvent
            {
                isVictory = isVictory,
                wavesCompleted = _currentWave,
                data = data
            });
        }

        private void TransitionToDeckChoice()
        {
            SetState(RunState.DeckChoice);

            if (ServiceLocator.TryGet<DeckBuilder>(out var deckBuilder))
            {
                var choices = deckBuilder.GenerateChoices(DeckChoiceCount);
                EventBus.Publish(new DeckChoiceEvent
                {
                    choiceCount = choices.Count,
                    waveNumber = _currentWave
                });
            }
        }

        private void StartNextWave()
        {
            _currentWave++;
            SetState(RunState.InWave);
            // WaveStartedEvent публикуется EnemyWaveController — здесь не дублируем
        }

        private void StartBossWave(bool isFinal)
        {
            SetState(isFinal ? RunState.FinalBoss : RunState.MiniBoss);
            // WaveStartedEvent публикуется EnemyWaveController
        }

        private bool RollRandomEvent()
        {
            return (float)_rng.NextDouble() < RandomEventChance;
        }

        private void SetState(RunState newState)
        {
            _state = newState;
        }

        private int GetRunIndex()
        {
            return PlayerPrefs.GetInt("run_index", 0);
        }

        private static int CalculateWaveCoins(int waveNumber)
        {
            return 10 + waveNumber * 5;
        }

        private static int CalculateMiniBossRunes()
        {
            return 2;
        }

        private static int CalculateFinalBossRunes()
        {
            return 5;
        }

        private static int CalculateVictoryBonus()
        {
            return 3;
        }
    }
}
