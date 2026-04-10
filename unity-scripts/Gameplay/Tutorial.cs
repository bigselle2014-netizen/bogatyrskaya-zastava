using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BogatyrskayaZastava.Core;
using BogatyrskayaZastava.Data;

namespace BogatyrskayaZastava.Gameplay
{
    public enum TutorialState
    {
        PlaceTower,
        SelectTower,
        WatchWave,
        CollectReward,
        UpgradeTower,
        WaveHint,
        IdleHint,
        Complete
    }

    public class Tutorial : MonoBehaviour
    {
        [SerializeField] private GameObject tutorialPanel;
        [SerializeField] private TextMeshProUGUI hintText;
        [SerializeField] private GameObject arrowIndicator;
        [SerializeField] private Button skipButton;

        [Header("Ссылки для подсветки")]
        [SerializeField] private Transform deckPanelTransform;
        [SerializeField] private Transform ratnikIconTransform;
        [SerializeField] private Transform[] towerSlotTransforms;

        private TutorialState _currentState;
        private bool _isActive;
        private Coroutine _skipEnableCoroutine;

        private void Awake()
        {
            if (skipButton != null)
            {
                skipButton.onClick.AddListener(OnSkipClicked);
            }
        }

        private void OnDestroy()
        {
            UnsubscribeAll();

            if (skipButton != null)
            {
                skipButton.onClick.RemoveListener(OnSkipClicked);
            }
        }

        /// <summary>
        /// Запускает туториал если он ещё не был завершён
        /// </summary>
        public void StartTutorial()
        {
            if (ServiceLocator.TryGet<SaveSystem>(out var saveSystem))
            {
                GameSaveData save = saveSystem.Load();
                if (save != null && save.tutorialComplete)
                {
                    return;
                }
            }

            _isActive = true;

            SubscribeToEvents();
            AdvanceState(TutorialState.PlaceTower);
        }

        /// <summary>
        /// Переходит к следующему состоянию туториала
        /// </summary>
        public void AdvanceState(TutorialState next)
        {
            _currentState = next;

            if (_skipEnableCoroutine != null)
            {
                StopCoroutine(_skipEnableCoroutine);
                _skipEnableCoroutine = null;
            }

            switch (next)
            {
                case TutorialState.PlaceTower:
                    ShowHint("Застава под угрозой! Расставь богатырей, чтобы остановить нечисть.", deckPanelTransform, false, 0f);
                    // Автопереход к SelectTower через короткую задержку — шаг 1 только показывает контекст
                    StartCoroutine(AutoAdvanceAfterDelay(1.5f, TutorialState.SelectTower));
                    break;

                case TutorialState.SelectTower:
                    ShowHint("Выбери Ратника — нажми на него в деке.", ratnikIconTransform, false, 0f);
                    break;

                case TutorialState.WatchWave:
                    ShowHint("Поставь Ратника на выделенный слот поля.", null, false, 0f);
                    HighlightSlots(true);
                    break;

                case TutorialState.CollectReward:
                    HighlightSlots(false);
                    ShowHint("Нажми «Начать волну» и смотри как Ратник останавливает врагов на 0.5с!", null, false, 0f);
                    break;

                case TutorialState.WaveHint:
                    ShowHint("Ратник останавливает врагов на 0.5с — используй это!", null, true, 2f);
                    break;

                case TutorialState.UpgradeTower:
                    ShowHint("Победа! Забери монеты и улучши Ратника — стоит 80 монет.", null, true, 2f);
                    break;

                case TutorialState.IdleHint:
                    ShowHint("Улучши Ратника или пропусти.\nДальше ты сам — удачи, богатырь!", null, true, 2f);
                    break;

                case TutorialState.Complete:
                    CompleteTutorial();
                    break;
            }
        }

        /// <summary>
        /// Показывает подсказку с опциональной стрелкой и возможностью пропуска
        /// </summary>
        public void ShowHint(string text, Transform target, bool canSkip, float skipDelay)
        {
            if (tutorialPanel != null) tutorialPanel.SetActive(true);
            if (hintText != null) hintText.text = text;

            if (arrowIndicator != null)
            {
                if (target != null)
                {
                    arrowIndicator.SetActive(true);
                    arrowIndicator.transform.position = target.position;
                }
                else
                {
                    arrowIndicator.SetActive(false);
                }
            }

            if (skipButton != null)
            {
                skipButton.gameObject.SetActive(canSkip);
                skipButton.interactable = false;

                if (canSkip && skipDelay > 0f)
                {
                    _skipEnableCoroutine = StartCoroutine(EnableSkipAfterDelay(skipDelay));
                }
                else if (canSkip)
                {
                    skipButton.interactable = true;
                }
            }
        }

        /// <summary>
        /// Завершает туториал и сохраняет флаг в SaveSystem
        /// </summary>
        public void CompleteTutorial()
        {
            _isActive = false;
            UnsubscribeAll();

            if (tutorialPanel != null) tutorialPanel.SetActive(false);
            if (arrowIndicator != null) arrowIndicator.SetActive(false);

            if (ServiceLocator.TryGet<SaveSystem>(out var saveSystem2))
            {
                GameSaveData save = saveSystem2.Load() ?? new GameSaveData();
                save.tutorialComplete = true;
                saveSystem2.Save(save);
            }

            EventBus.Publish(new TutorialCompleteEvent());
        }

        private void OnSkipClicked()
        {
            if (!_isActive) return;

            TutorialState next = GetNextSkippableState(_currentState);
            AdvanceState(next);
        }

        private TutorialState GetNextSkippableState(TutorialState current)
        {
            switch (current)
            {
                case TutorialState.WaveHint: return TutorialState.UpgradeTower;
                case TutorialState.UpgradeTower: return TutorialState.IdleHint;
                case TutorialState.IdleHint: return TutorialState.Complete;
                default: return TutorialState.Complete;
            }
        }

        private IEnumerator EnableSkipAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (skipButton != null)
            {
                skipButton.interactable = true;
            }
        }

        private IEnumerator AutoAdvanceAfterDelay(float delay, TutorialState next)
        {
            yield return new WaitForSeconds(delay);
            if (_isActive && _currentState == TutorialState.PlaceTower)
            {
                AdvanceState(next);
            }
        }

        private void SubscribeToEvents()
        {
            EventBus.Subscribe<TowerPlacedEvent>(OnTowerPlaced);
            EventBus.Subscribe<WaveStartedEvent>(OnWaveStarted);
            EventBus.Subscribe<WaveCompletedEvent>(OnWaveCompleted);
        }

        private void UnsubscribeAll()
        {
            EventBus.Unsubscribe<TowerPlacedEvent>(OnTowerPlaced);
            EventBus.Unsubscribe<WaveStartedEvent>(OnWaveStarted);
            EventBus.Unsubscribe<WaveCompletedEvent>(OnWaveCompleted);
        }

        private void OnTowerPlaced(TowerPlacedEvent evt)
        {
            if (!_isActive) return;

            if (_currentState == TutorialState.SelectTower)
            {
                AdvanceState(TutorialState.WatchWave);
                return;
            }

            if (_currentState == TutorialState.WatchWave)
            {
                AdvanceState(TutorialState.CollectReward);
            }
        }

        private void OnWaveStarted(WaveStartedEvent evt)
        {
            if (!_isActive) return;
            if (_currentState == TutorialState.CollectReward)
            {
                AdvanceState(TutorialState.WaveHint);
            }
        }

        private void OnWaveCompleted(WaveCompletedEvent evt)
        {
            if (!_isActive) return;

            if (_currentState == TutorialState.WaveHint)
            {
                AdvanceState(TutorialState.UpgradeTower);
            }
        }

        private void HighlightSlots(bool highlight)
        {
            if (towerSlotTransforms == null) return;

            for (int i = 0; i < towerSlotTransforms.Length; i++)
            {
                if (towerSlotTransforms[i] == null) continue;

                SpriteRenderer sr = towerSlotTransforms[i].GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = highlight ? new Color(1f, 1f, 0f, 0.6f) : Color.white;
                }
            }
        }
    }
}
