using System;
using System.Collections.Generic;
using UnityEngine;
using BogatyrskayaZastava.Core;
using BogatyrskayaZastava.Data;

namespace BogatyrskayaZastava.Gameplay
{
    [Serializable]
    public struct SynergyData
    {
        public string id;
        public string name;
        public string[] requiredTowerIds;
        public int minCount;
        public string type;
        public string effectType;
        public float effectValue;
        public int maxDistance;
        public bool stackable;
    }

    public class SynergySystem : MonoBehaviour
    {
        private const int MaxTowersOnField = 6;
        private const string ConfigResourcePath = "SynergyConfig";

        private List<SynergyData> _allSynergies;
        private readonly List<SynergyData> _activeSynergies = new List<SynergyData>(12);
        private readonly List<SynergyData> _previousActive = new List<SynergyData>(12);
        private readonly Dictionary<string, int> _factionCounts = new Dictionary<string, int>(8);
        private readonly List<TowerBase> _towersBuffer = new List<TowerBase>(MaxTowersOnField);

        public IReadOnlyList<SynergyData> ActiveSynergies => _activeSynergies;

        private void Awake()
        {
            ServiceLocator.Register<SynergySystem>(this);
            LoadConfig();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<TowerPlacedEvent>(OnTowerPlaced);
            EventBus.Subscribe<TowerRemovedEvent>(OnTowerRemoved);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<TowerPlacedEvent>(OnTowerPlaced);
            EventBus.Unsubscribe<TowerRemovedEvent>(OnTowerRemoved);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<SynergySystem>();
        }

        private void LoadConfig()
        {
            TextAsset asset = Resources.Load<TextAsset>(ConfigResourcePath);
            if (asset == null)
            {
                Debug.LogError("[SynergySystem] SynergyConfig not found in Resources/");
                _allSynergies = new List<SynergyData>(0);
                return;
            }

            SynergyConfigRoot root = JsonUtility.FromJson<SynergyConfigRoot>(asset.text);
            if (root == null || root.synergies == null)
            {
                Debug.LogError("[SynergySystem] Failed to parse SynergyConfig");
                _allSynergies = new List<SynergyData>(0);
                return;
            }

            _allSynergies = new List<SynergyData>(root.synergies.Length);
            for (int i = 0; i < root.synergies.Length; i++)
            {
                SynergyConfigEntry e = root.synergies[i];
                SynergyData sd = new SynergyData
                {
                    id = e.id,
                    name = e.name,
                    requiredTowerIds = e.requiredTowers,
                    minCount = e.minCount,
                    type = e.type,
                    effectType = e.effectType,
                    effectValue = e.effectValue,
                    maxDistance = e.maxDistance,
                    stackable = e.stackable
                };
                _allSynergies.Add(sd);
            }
        }

        private void OnTowerPlaced(TowerPlacedEvent evt)
        {
            RefreshSynergies();
        }

        private void OnTowerRemoved(TowerRemovedEvent evt)
        {
            RefreshSynergies();
        }

        public List<SynergyData> CheckActiveSynergies(List<TowerBase> placedTowers)
        {
            _activeSynergies.Clear();
            if (_allSynergies == null || _allSynergies.Count == 0) return _activeSynergies;
            if (placedTowers == null || placedTowers.Count == 0) return _activeSynergies;

            BuildFactionCounts(placedTowers);

            for (int i = 0; i < _allSynergies.Count; i++)
            {
                SynergyData synergy = _allSynergies[i];

                if (IsFactionSynergy(synergy.type))
                {
                    if (CheckFactionSynergy(synergy, placedTowers))
                        _activeSynergies.Add(synergy);
                }
                else if (synergy.type == "cross_faction_pair" || synergy.type == "cross_faction_trio")
                {
                    if (CheckCrossFactionSynergy(synergy, placedTowers))
                        _activeSynergies.Add(synergy);
                }
            }

            return _activeSynergies;
        }

        public void ApplySynergyEffect(SynergyData synergy)
        {
            if (!ServiceLocator.TryGet<TowerPlacementSystem>(out var placement)) return;

            IReadOnlyList<TowerBase> towers = placement.GetActiveTowers();

            SynergyEffect effect = new SynergyEffect
            {
                effectId = synergy.id,
                value = synergy.effectValue
            };

            for (int i = 0; i < towers.Count; i++)
            {
                TowerBase tower = towers[i];
                if (!tower.IsAlive) continue;

                if (ShouldApplyToTower(synergy, tower))
                    tower.ApplySynergyBonus(effect);
            }
        }

        public void RemoveSynergyEffect(SynergyData synergy)
        {
            if (!ServiceLocator.TryGet<TowerPlacementSystem>(out var placement)) return;

            IReadOnlyList<TowerBase> towers = placement.GetActiveTowers();

            SynergyEffect effect = new SynergyEffect
            {
                effectId = synergy.id,
                value = -synergy.effectValue
            };

            for (int i = 0; i < towers.Count; i++)
            {
                TowerBase tower = towers[i];
                if (!tower.IsAlive) continue;

                if (ShouldApplyToTower(synergy, tower))
                    tower.ApplySynergyBonus(effect);
            }
        }

        private void RefreshSynergies()
        {
            if (!ServiceLocator.TryGet<TowerPlacementSystem>(out var placement)) return;

            IReadOnlyList<TowerBase> towers = placement.GetActiveTowers();
            _towersBuffer.Clear();
            for (int i = 0; i < towers.Count; i++)
                _towersBuffer.Add(towers[i]);

            _previousActive.Clear();
            _previousActive.AddRange(_activeSynergies);

            CheckActiveSynergies(_towersBuffer);

            for (int i = 0; i < _previousActive.Count; i++)
            {
                SynergyData prev = _previousActive[i];
                if (!IsInActiveList(prev.id))
                {
                    RemoveSynergyEffect(prev);
                    EventBus.Publish(new SynergyDeactivatedEvent { synergyId = prev.id });
                }
            }

            for (int i = 0; i < _activeSynergies.Count; i++)
            {
                SynergyData current = _activeSynergies[i];
                if (!WasInPreviousList(current.id))
                {
                    ApplySynergyEffect(current);
                    EventBus.Publish(new SynergyActivatedEvent { synergyId = current.id });
                }
            }
        }

        private bool IsInActiveList(string synergyId)
        {
            for (int i = 0; i < _activeSynergies.Count; i++)
            {
                if (_activeSynergies[i].id == synergyId) return true;
            }
            return false;
        }

        private bool WasInPreviousList(string synergyId)
        {
            for (int i = 0; i < _previousActive.Count; i++)
            {
                if (_previousActive[i].id == synergyId) return true;
            }
            return false;
        }

        private void BuildFactionCounts(List<TowerBase> placedTowers)
        {
            _factionCounts.Clear();
            for (int i = 0; i < placedTowers.Count; i++)
            {
                TowerBase tower = placedTowers[i];
                if (tower.Data == null || !tower.IsAlive) continue;

                string factionPrefix = GetFactionPrefix(tower.Data.Faction);
                if (_factionCounts.ContainsKey(factionPrefix))
                    _factionCounts[factionPrefix]++;
                else
                    _factionCounts[factionPrefix] = 1;
            }
        }

        private bool CheckFactionSynergy(SynergyData synergy, List<TowerBase> placedTowers)
        {
            if (synergy.requiredTowerIds == null || synergy.requiredTowerIds.Length == 0)
                return false;

            string factionPrefix = ExtractFactionPrefix(synergy.requiredTowerIds[0]);
            if (!_factionCounts.TryGetValue(factionPrefix, out int count))
                return false;

            return count >= synergy.minCount;
        }

        private bool CheckCrossFactionSynergy(SynergyData synergy, List<TowerBase> placedTowers)
        {
            if (synergy.requiredTowerIds == null) return false;

            for (int r = 0; r < synergy.requiredTowerIds.Length; r++)
            {
                string prefix = synergy.requiredTowerIds[r];
                bool found = false;
                for (int t = 0; t < placedTowers.Count; t++)
                {
                    if (placedTowers[t].Data == null || !placedTowers[t].IsAlive) continue;
                    if (GetFactionPrefix(placedTowers[t].Data.Faction) == prefix)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found) return false;
            }

            if (synergy.maxDistance > 0)
                return CheckProximity(synergy, placedTowers);

            return true;
        }

        private bool CheckProximity(SynergyData synergy, List<TowerBase> placedTowers)
        {
            for (int a = 0; a < placedTowers.Count; a++)
            {
                if (placedTowers[a].Data == null || !placedTowers[a].IsAlive) continue;
                string prefA = GetFactionPrefix(placedTowers[a].Data.Faction);
                if (!IsRequiredFaction(synergy, prefA)) continue;

                for (int b = a + 1; b < placedTowers.Count; b++)
                {
                    if (placedTowers[b].Data == null || !placedTowers[b].IsAlive) continue;
                    string prefB = GetFactionPrefix(placedTowers[b].Data.Faction);
                    if (!IsRequiredFaction(synergy, prefB)) continue;
                    if (prefA == prefB) continue;

                    float dist = Vector3.Distance(
                        placedTowers[a].transform.position,
                        placedTowers[b].transform.position
                    );
                    if (dist <= synergy.maxDistance)
                        return true;
                }
            }
            return false;
        }

        private bool IsRequiredFaction(SynergyData synergy, string factionPrefix)
        {
            for (int i = 0; i < synergy.requiredTowerIds.Length; i++)
            {
                if (synergy.requiredTowerIds[i] == factionPrefix) return true;
            }
            return false;
        }

        private bool ShouldApplyToTower(SynergyData synergy, TowerBase tower)
        {
            if (tower.Data == null) return false;

            if (IsFactionSynergy(synergy.type))
            {
                string towerPrefix = GetFactionPrefix(tower.Data.Faction);
                string requiredPrefix = synergy.requiredTowerIds != null && synergy.requiredTowerIds.Length > 0
                    ? ExtractFactionPrefix(synergy.requiredTowerIds[0])
                    : "";
                return towerPrefix == requiredPrefix;
            }

            string prefix = GetFactionPrefix(tower.Data.Faction);
            return IsRequiredFaction(synergy, prefix);
        }

        private static bool IsFactionSynergy(string type)
        {
            return type == "faction" || type == "faction_enhanced";
        }

        private static string GetFactionPrefix(FactionType faction)
        {
            switch (faction)
            {
                case FactionType.Druzina:  return "DR";
                case FactionType.Volhvy:   return "VL";
                case FactionType.Luchniki: return "LU";
                case FactionType.Berserki: return "BE";
                case FactionType.Znahari:  return "ZN";
                default:                   return "";
            }
        }

        private static string ExtractFactionPrefix(string towerId)
        {
            if (string.IsNullOrEmpty(towerId)) return "";
            int dash = towerId.IndexOf('-');
            return dash > 0 ? towerId.Substring(0, dash) : towerId;
        }

        [Serializable]
        private class SynergyConfigRoot
        {
            public SynergyConfigEntry[] synergies;
        }

        [Serializable]
        private class SynergyConfigEntry
        {
            public string id;
            public string name;
            public string type;
            public string[] requiredTowers;
            public int minCount;
            public int maxDistance;
            public string effectType;
            public float effectValue;
            public bool stackable;
            public int phase;
            public string faction;
        }
    }
}
