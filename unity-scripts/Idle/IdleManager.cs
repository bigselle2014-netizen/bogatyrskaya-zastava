using System;
using UnityEngine;
using BogatyrskayaZastava.Core;

namespace BogatyrskayaZastava.Idle
{
    [Serializable]
    public class IdleConfig
    {
        public float ratePerSecond;
        public float maxOfflineCapSeconds;
    }

    public class IdleManager : MonoBehaviour
    {
        // STUB: значения хардкодные до недели 6
        // НЕ менять эти числа самостоятельно — Lead Dev передаст IdleOfflineConfig к неделе 6
        private const float STUB_RATE_PER_SECOND = 2f; // game-bible: 2 монеты/сек (stub до IdleOfflineConfig от Lead Dev к неделе 6)
        private const float STUB_MAX_OFFLINE_CAP = 28800f; // 8 часов

        private const string PrefsKeyExitTime = "idle_exit_time";

        private void Awake()
        {
            ServiceLocator.Register<IdleManager>(this);
        }

        private void OnEnable()
        {
            Application.quitting += SaveExitTime;
        }

        private void OnDisable()
        {
            Application.quitting -= SaveExitTime;
        }

        private void Start()
        {
            OnApplicationStart();
        }

        /// <summary>
        /// Сохраняет время выхода из игры в PlayerPrefs
        /// </summary>
        public void SaveExitTime()
        {
            long ticks = DateTime.UtcNow.Ticks;
            PlayerPrefs.SetString(PrefsKeyExitTime, ticks.ToString());
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Рассчитывает оффлайн-доход с момента последнего выхода.
        /// Возвращает (gold, runeStones). Защита от читов встроена.
        /// </summary>
        public (float gold, float runeStones) CalculateOfflineIncome()
        {
            string savedStr = PlayerPrefs.GetString(PrefsKeyExitTime, string.Empty);
            if (string.IsNullOrEmpty(savedStr)) return (0f, 0f);

            long savedTicks;
            if (!long.TryParse(savedStr, out savedTicks)) return (0f, 0f);

            long nowTicks = DateTime.UtcNow.Ticks;

            // Анти-чит: время устройства не может быть меньше сохранённого
            if (nowTicks < savedTicks) return (0f, 0f);

            float deltaSeconds = (float)TimeSpan.FromTicks(nowTicks - savedTicks).TotalSeconds;

            // Анти-чит: ограничиваем кап независимо от дельты
            float cappedSeconds = Mathf.Min(deltaSeconds, STUB_MAX_OFFLINE_CAP);

            float goldEarned = cappedSeconds * STUB_RATE_PER_SECOND;

            // В Фазе 1 руны не начисляются оффлайн (конфига нет)
            float runeStonesEarned = 0f;

            return (goldEarned, runeStonesEarned);
        }

        private void OnApplicationStart()
        {
            (float gold, float runeStones) = CalculateOfflineIncome();

            if (gold > 0f || runeStones > 0f)
            {
                IdleIncomeReadyEvent evt = new IdleIncomeReadyEvent
                {
                    gold = gold,
                    runeStones = runeStones
                };
                EventBus.Publish(evt);
            }

            // Обновляем время выхода — теперь фиксируем текущий сеанс
            SaveExitTime();
        }
    }
}
