using System;
using System.Collections.Generic;
using UnityEngine;
using BogatyrskayaZastava.Core;
using BogatyrskayaZastava.Data;

namespace BogatyrskayaZastava.Gameplay
{
    public class DeckBuilder : MonoBehaviour
    {
        [SerializeField] private List<TowerData> _starterPool;

        private readonly List<TowerData> _availablePool = new List<TowerData>(16);
        private readonly List<TowerData> _currentDeck = new List<TowerData>(8);
        private readonly List<TowerData> _choicesBuffer = new List<TowerData>(4);
        private readonly List<int> _indexBuffer = new List<int>(16);

        private readonly System.Random _rng = new System.Random();

        public IReadOnlyList<TowerData> CurrentDeck => _currentDeck;
        public IReadOnlyList<TowerData> AvailablePool => _availablePool;

        private void Awake()
        {
            ServiceLocator.Register<DeckBuilder>(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<DeckBuilder>();
        }

        public void InitializeForRun()
        {
            _currentDeck.Clear();
            _availablePool.Clear();

            if (_starterPool != null)
            {
                for (int i = 0; i < _starterPool.Count; i++)
                    _availablePool.Add(_starterPool[i]);
            }
        }

        public List<TowerData> GenerateChoices(int count = 3)
        {
            _choicesBuffer.Clear();
            _indexBuffer.Clear();

            for (int i = 0; i < _availablePool.Count; i++)
            {
                TowerData candidate = _availablePool[i];
                if (!IsInCurrentDeck(candidate))
                    _indexBuffer.Add(i);
            }

            int toSelect = Mathf.Min(count, _indexBuffer.Count);

            for (int i = 0; i < toSelect; i++)
            {
                int swapIdx = i + _rng.Next(_indexBuffer.Count - i);
                int tmp = _indexBuffer[i];
                _indexBuffer[i] = _indexBuffer[swapIdx];
                _indexBuffer[swapIdx] = tmp;

                _choicesBuffer.Add(_availablePool[_indexBuffer[i]]);
            }

            return _choicesBuffer;
        }

        public void AddToDeck(TowerData tower)
        {
            if (tower == null) return;
            if (IsInCurrentDeck(tower)) return;

            _currentDeck.Add(tower);
        }

        public List<TowerData> GetDeck()
        {
            return _currentDeck;
        }

        public void UnlockFaction(string factionId)
        {
            FactionType faction = ParseFaction(factionId);

            TowerData[] allTowers = Resources.LoadAll<TowerData>("Towers");
            if (allTowers == null) return;

            for (int i = 0; i < allTowers.Length; i++)
            {
                TowerData td = allTowers[i];
                if (td.Faction == faction && !IsInPool(td))
                    _availablePool.Add(td);
            }
        }

        public void ResetDeck()
        {
            _currentDeck.Clear();
        }

        private bool IsInCurrentDeck(TowerData tower)
        {
            for (int i = 0; i < _currentDeck.Count; i++)
            {
                if (_currentDeck[i].TowerId == tower.TowerId)
                    return true;
            }
            return false;
        }

        private bool IsInPool(TowerData tower)
        {
            for (int i = 0; i < _availablePool.Count; i++)
            {
                if (_availablePool[i].TowerId == tower.TowerId)
                    return true;
            }
            return false;
        }

        private static FactionType ParseFaction(string factionId)
        {
            switch (factionId)
            {
                case "DR": return FactionType.Druzina;
                case "VL": return FactionType.Volhvy;
                case "LU": return FactionType.Luchniki;
                case "BE": return FactionType.Berserki;
                case "ZN": return FactionType.Znahari;
                default:
                    Debug.LogWarning($"[DeckBuilder] Unknown factionId: {factionId}");
                    return FactionType.Druzina;
            }
        }
    }
}
