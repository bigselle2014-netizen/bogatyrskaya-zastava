using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BogatyrskayaZastava.Core;

namespace BogatyrskayaZastava.UI
{
    public class GameHUD : MonoBehaviour
    {
        [Header("Ресурсы")]
        [SerializeField] private TextMeshProUGUI goldText;

        [Header("Волны")]
        [SerializeField] private TextMeshProUGUI waveText;

        [Header("Ворота")]
        [SerializeField] private TextMeshProUGUI gateHpText;
        [SerializeField] private Slider gateHpSlider;

        [Header("Таймер между волнами")]
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private GameObject timerPanel;

        [Header("Управление волной")]
        [SerializeField] private Button startWaveButton;

        [Header("Дек башен")]
        [SerializeField] private Transform deckPanel;

        private int _totalWaves;
        private Coroutine _timerCoroutine;

        private void OnEnable()
        {
            EventBus.Subscribe<ResourceChangedEvent>(UpdateGoldDisplay);
            EventBus.Subscribe<WaveStartedEvent>(UpdateWaveDisplay);
            EventBus.Subscribe<WaveCompletedEvent>(OnWaveCompleted);
            EventBus.Subscribe<GateHpChangedEvent>(UpdateGateHp);
            EventBus.Subscribe<LevelCompletedEvent>(OnLevelCompleted);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<ResourceChangedEvent>(UpdateGoldDisplay);
            EventBus.Unsubscribe<WaveStartedEvent>(UpdateWaveDisplay);
            EventBus.Unsubscribe<WaveCompletedEvent>(OnWaveCompleted);
            EventBus.Unsubscribe<GateHpChangedEvent>(UpdateGateHp);
            EventBus.Unsubscribe<LevelCompletedEvent>(OnLevelCompleted);
        }

        private void Start()
        {
            if (startWaveButton != null)
            {
                startWaveButton.onClick.AddListener(StartWaveButtonPressed);
            }

            if (timerPanel != null) timerPanel.SetActive(false);
            if (gateHpSlider != null) gateHpSlider.value = 1f;
        }

        private void OnDestroy()
        {
            if (startWaveButton != null)
            {
                startWaveButton.onClick.RemoveListener(StartWaveButtonPressed);
            }
        }

        /// <summary>
        /// Обновляет отображение золота
        /// </summary>
        public void UpdateGoldDisplay(ResourceChangedEvent evt)
        {
            if (goldText == null) return;
            goldText.text = evt.newGold.ToString();
        }

        /// <summary>
        /// Обновляет текст текущей волны, скрывает таймер и показывает кнопку
        /// </summary>
        public void UpdateWaveDisplay(WaveStartedEvent evt)
        {
            _totalWaves = evt.totalWaves;

            if (waveText != null)
            {
                waveText.text = "Волна " + evt.waveNumber + " / " + evt.totalWaves;
            }

            if (timerPanel != null) timerPanel.SetActive(false);

            if (startWaveButton != null)
            {
                startWaveButton.interactable = false;
            }

            if (_timerCoroutine != null)
            {
                StopCoroutine(_timerCoroutine);
                _timerCoroutine = null;
            }
        }

        /// <summary>
        /// Обновляет полосу и текст HP ворот
        /// </summary>
        public void UpdateGateHp(GateHpChangedEvent evt)
        {
            if (gateHpText != null)
            {
                gateHpText.text = evt.current + " / " + evt.max;
            }

            if (gateHpSlider != null && evt.max > 0)
            {
                gateHpSlider.value = (float)evt.current / evt.max;
            }
        }

        private void OnWaveCompleted(WaveCompletedEvent evt)
        {
            if (startWaveButton != null)
            {
                startWaveButton.interactable = true;
            }

            if (timerPanel != null) timerPanel.SetActive(true);

            // QA-027: таймер берётся из event, а не через прямой вызов EnemyWaveController
            float waitTime = evt.timeBetweenWaves > 0f ? evt.timeBetweenWaves : 12f;

            if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);
            _timerCoroutine = StartCoroutine(WaveTimerCountdown(waitTime));
        }

        private void OnLevelCompleted(LevelCompletedEvent evt)
        {
            if (startWaveButton != null)
            {
                startWaveButton.gameObject.SetActive(false);
            }

            if (timerPanel != null) timerPanel.SetActive(false);

            if (_timerCoroutine != null)
            {
                StopCoroutine(_timerCoroutine);
                _timerCoroutine = null;
            }
        }

        /// <summary>
        /// Нажатие кнопки "Начать волну" — форсирует старт следующей волны через EventBus (QA-028)
        /// </summary>
        public void StartWaveButtonPressed()
        {
            if (_timerCoroutine != null)
            {
                StopCoroutine(_timerCoroutine);
                _timerCoroutine = null;
            }

            if (timerPanel != null) timerPanel.SetActive(false);

            EventBus.Publish(new StartNextWaveRequestEvent());
        }

        /// <summary>
        /// Обратный отсчёт таймера между волнами. По окончании автоматически запускает волну.
        /// </summary>
        private IEnumerator WaveTimerCountdown(float seconds)
        {
            float remaining = seconds;

            while (remaining > 0f)
            {
                if (timerText != null)
                {
                    timerText.text = Mathf.CeilToInt(remaining).ToString();
                }
                remaining -= Time.deltaTime;
                yield return null;
            }

            if (timerText != null) timerText.text = "0";
            if (timerPanel != null) timerPanel.SetActive(false);

            _timerCoroutine = null;

            EventBus.Publish(new StartNextWaveRequestEvent());
        }
    }
}
