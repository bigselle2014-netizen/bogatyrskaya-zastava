using System;
using UnityEngine;
using BogatyrskayaZastava.Core;

namespace BogatyrskayaZastava.Core
{
    public interface ISaveSystem
    {
        /// <summary>
        /// Сохраняет данные игры в PlayerPrefs
        /// </summary>
        void Save(GameSaveData data);

        /// <summary>
        /// Загружает данные игры из PlayerPrefs. Возвращает null если сохранения нет.
        /// </summary>
        GameSaveData Load();

        /// <summary>
        /// Возвращает true если существует валидное сохранение
        /// </summary>
        bool HasSave();

        /// <summary>
        /// Удаляет сохранение полностью
        /// </summary>
        void DeleteSave();
    }

    [Serializable]
    public class GameSaveData
    {
        public int gold;
        public int runeStones;
        public long idleExitTimeTicks;
        public string[] unlockedTowerIds;
        public int highestLevelReached;
        public bool tutorialComplete;
        public long firstLaunchTimeTicks;
        public int battlePassTier;
    }

    public class SaveSystem : MonoBehaviour, ISaveSystem
    {
        private const string SaveKey = "save_v1";
        private const string SaveVersionKey = "save_version";
        private const string CurrentSaveVersion = "1";

        private void Awake()
        {
            ServiceLocator.Register<SaveSystem>(this);
            MigrateIfNeeded();
        }

        /// <summary>
        /// Сериализует GameSaveData в JSON и сохраняет в PlayerPrefs
        /// </summary>
        public void Save(GameSaveData data)
        {
            if (data == null) return;

            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(SaveKey, json);
            PlayerPrefs.SetString(SaveVersionKey, CurrentSaveVersion);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Загружает и десериализует GameSaveData из PlayerPrefs.
        /// Возвращает null если сохранения нет.
        /// </summary>
        public GameSaveData Load()
        {
            if (!HasSave()) return null;

            string json = PlayerPrefs.GetString(SaveKey, string.Empty);
            if (string.IsNullOrEmpty(json)) return null;

            GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
            return data;
        }

        /// <summary>
        /// Возвращает true если существует валидное сохранение текущей версии
        /// </summary>
        public bool HasSave()
        {
            string version = PlayerPrefs.GetString(SaveVersionKey, string.Empty);
            if (version != CurrentSaveVersion) return false;

            return PlayerPrefs.HasKey(SaveKey);
        }

        /// <summary>
        /// Удаляет сохранение и версию из PlayerPrefs
        /// </summary>
        public void DeleteSave()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.DeleteKey(SaveVersionKey);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Загружает сохранение или создаёт новое с дефолтными значениями
        /// </summary>
        public GameSaveData LoadOrCreate()
        {
            GameSaveData data = Load();
            if (data != null) return data;

            data = new GameSaveData
            {
                gold = 0,
                runeStones = 0,
                idleExitTimeTicks = DateTime.UtcNow.Ticks,
                unlockedTowerIds = new string[0],
                highestLevelReached = 0,
                tutorialComplete = false,
                firstLaunchTimeTicks = DateTime.UtcNow.Ticks,
                battlePassTier = 0
            };

            Save(data);
            return data;
        }

        private void MigrateIfNeeded()
        {
            string version = PlayerPrefs.GetString(SaveVersionKey, string.Empty);
            if (!string.IsNullOrEmpty(version) && version != CurrentSaveVersion)
            {
                // Несовместимая версия сохранения — чистый старт
                DeleteSave();
            }
        }
    }
}
