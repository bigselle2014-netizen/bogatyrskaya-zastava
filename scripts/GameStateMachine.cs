using System;
using UnityEngine;

namespace BogatyrskayaZastava.Core
{
    /// <summary>
    /// All valid game states. Transitions validated in GameStateMachine.
    /// </summary>
    public enum GameState
    {
        None,
        MainMenu,
        Loading,
        Tutorial,
        Gameplay,
        RunComplete,
        Idle,          // App backgrounded, idle accumulation active
        Shop,
        Settings,
        EventLobby     // Active when cultural event is running
    }

    /// <summary>
    /// Central game state machine. Controls all top-level app states.
    /// Transitions publish GameStateChangedEvent via EventBus.
    ///
    /// Usage:
    ///   GameStateMachine.TransitionTo(GameState.Gameplay);
    ///   GameState current = GameStateMachine.GetCurrentState();
    ///   GameStateMachine.OnStateChanged += (from, to) => { ... };
    /// </summary>
    public static class GameStateMachine
    {
        private static GameState _currentState = GameState.None;

        // Callback alternative to EventBus for cases where EventBus is unavailable
        public static event Action<GameState, GameState> OnStateChanged;

        // ─────────────────────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────────────────────

        public static GameState GetCurrentState() => _currentState;

        /// <summary>
        /// Transition to a new state. Validates transition, fires events.
        /// </summary>
        public static void TransitionTo(GameState newState)
        {
            if (_currentState == newState)
            {
                Debug.LogWarning($"[StateMachine] Already in state: {newState}");
                return;
            }

            if (!IsValidTransition(_currentState, newState))
            {
                Debug.LogError($"[StateMachine] Invalid transition: {_currentState} → {newState}");
                return;
            }

            var previousState = _currentState;
            _currentState = newState;

            Debug.Log($"[StateMachine] {previousState} → {newState}");

            // Notify via EventBus (primary)
            EventBus.Publish(new GameStateChangedEvent
            {
                from = previousState,
                to   = newState
            });

            // Notify via direct callback (fallback)
            OnStateChanged?.Invoke(previousState, newState);

            OnEnterState(newState, previousState);
        }

        // ─────────────────────────────────────────────────────────────
        // State entry logic
        // ─────────────────────────────────────────────────────────────

        private static void OnEnterState(GameState state, GameState previousState)
        {
            switch (state)
            {
                case GameState.MainMenu:
                    // TODO: выгрузить Gameplay сцену если переход из RunComplete
                    // TODO: проверить наличие активного ивента → предложить EventLobby
                    break;

                case GameState.Loading:
                    // TODO: запустить SceneLoader.LoadScene(targetScene)
                    break;

                case GameState.Tutorial:
                    // TODO: инициализировать Tutorial.cs (Unity Dev Middle, неделя 5)
                    break;

                case GameState.Gameplay:
                    // TODO: инициализировать WaveManager, HUD
                    Application.targetFrameRate = 30; // всегда 30 при входе в геймплей
                    break;

                case GameState.RunComplete:
                    // TODO: показать RunCompletePopup, начислить руны прокачки
                    break;

                case GameState.Idle:
                    // TODO: записать lastSeenTimestamp в ISaveService
                    // TODO: запустить ResourceAccumulator.StartAccumulation()
                    break;

                case GameState.Shop:
                    // TODO: загрузить ShopPopup, запросить IAP-продукты из RuStore
                    break;

                case GameState.EventLobby:
                    // TODO: загрузить конфиг ивента из RemoteConfig
                    break;

                case GameState.Settings:
                    // TODO: открыть SettingsPopup
                    break;
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Transition validation
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Defines all valid state transitions.
        /// Any unlisted transition is forbidden and logs an error.
        /// </summary>
        private static bool IsValidTransition(GameState from, GameState to)
        {
            switch (from)
            {
                case GameState.None:
                    return to == GameState.Loading || to == GameState.MainMenu;

                case GameState.MainMenu:
                    return to == GameState.Loading
                        || to == GameState.Tutorial
                        || to == GameState.Shop
                        || to == GameState.Settings
                        || to == GameState.EventLobby;

                case GameState.Loading:
                    return to == GameState.MainMenu
                        || to == GameState.Tutorial
                        || to == GameState.Gameplay;

                case GameState.Tutorial:
                    return to == GameState.Gameplay;

                case GameState.Gameplay:
                    return to == GameState.RunComplete
                        || to == GameState.Idle
                        || to == GameState.Loading; // Emergency exit

                case GameState.RunComplete:
                    return to == GameState.MainMenu
                        || to == GameState.Loading; // Retry run

                case GameState.Idle:
                    return to == GameState.Gameplay
                        || to == GameState.MainMenu;

                case GameState.Shop:
                    return to == GameState.MainMenu;

                case GameState.Settings:
                    return to == GameState.MainMenu
                        || to == GameState.Gameplay;

                case GameState.EventLobby:
                    return to == GameState.MainMenu
                        || to == GameState.Loading;

                default:
                    return false;
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Debug
        // ─────────────────────────────────────────────────────────────

        /// <summary>Force-set state without validation. For testing only.</summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void ForceState_EditorOnly(GameState state)
        {
            Debug.LogWarning($"[StateMachine] FORCE STATE (editor only): {state}");
            _currentState = state;
        }
    }
}
