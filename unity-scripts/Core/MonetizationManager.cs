using System;
using System.Collections.Generic;
using UnityEngine;
using BogatyrskayaZastava.Core;

namespace BogatyrskayaZastava.Core
{
    public class MonetizationManager : MonoBehaviour
    {
        private const float RewardedCooldownSeconds = 600f;
        private const int MinDaysBeforeIAP = 3;
        private const float BattlePassXPMultiplier = 1.3f;
        private const string PrefsKeyInstallDate = "firstLaunchDate";
        private const string PrefsKeyBattlePassActive = "bp_active";
        private const string PrefsKeyBattlePassLevel = "bp_level";
        private const string PrefsKeyBattlePassXP = "bp_xp";

        private float _lastRewardedTime = -RewardedCooldownSeconds;
        private bool _battlePassActive;
        private int _battlePassLevel;
        private float _battlePassXP;

        public bool IsBattlePassActive => _battlePassActive;
        public int BattlePassCurrentLevel => _battlePassLevel;
        public float XPMultiplier => _battlePassActive ? BattlePassXPMultiplier : 1f;

        private void Awake()
        {
            ServiceLocator.Register<MonetizationManager>(this);
            EnsureInstallDate();
            LoadBattlePassState();
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<MonetizationManager>();
        }

        public void ShowRewardedOffer(RewardedContext context)
        {
            float elapsed = Time.realtimeSinceStartup - _lastRewardedTime;
            if (elapsed < RewardedCooldownSeconds)
            {
                Debug.Log($"[MonetizationManager] Rewarded cooldown: {RewardedCooldownSeconds - elapsed:F0}s remaining");
                return;
            }

            if (!ServiceLocator.TryGet<IRewardedAdManager>(out var adManager))
            {
                Debug.LogWarning("[MonetizationManager] IRewardedAdManager not registered");
                return;
            }

            if (!adManager.IsRewardedReady())
            {
                Debug.Log("[MonetizationManager] Rewarded video not ready");
                return;
            }

            adManager.ShowRewarded(rewarded =>
            {
                if (rewarded)
                {
                    _lastRewardedTime = Time.realtimeSinceStartup;
                    EventBus.Publish(new AdWatchedEvent { context = context });

                    if (ServiceLocator.TryGet<IAnalyticsService>(out var analytics))
                    {
                        analytics.LogEvent("ad_watched", new Dictionary<string, object>
                        {
                            { "context", context.ToString() }
                        });
                    }
                }
            });
        }

        public bool CanShowIAP()
        {
            return GetDaysSinceInstall() >= MinDaysBeforeIAP;
        }

        public void OnPurchaseComplete(string productId)
        {
            EventBus.Publish(new PurchaseCompletedEvent { productId = productId });

            if (ServiceLocator.TryGet<IAnalyticsService>(out var analytics))
            {
                analytics.LogEvent("iap_purchase_complete", new Dictionary<string, object>
                {
                    { "product_id", productId }
                });
            }

            if (productId == "battle_pass_399")
                ActivateBattlePass();
        }

        public void ActivateBattlePass()
        {
            _battlePassActive = true;
            _battlePassLevel = Mathf.Max(_battlePassLevel, 1);
            SaveBattlePassState();
        }

        public void AddBattlePassXP(float xp)
        {
            if (!_battlePassActive) return;

            _battlePassXP += xp;

            float threshold = GetXPThresholdForLevel(_battlePassLevel + 1);
            while (_battlePassXP >= threshold && threshold > 0f)
            {
                _battlePassXP -= threshold;
                _battlePassLevel++;
                GrantBattlePassReward(_battlePassLevel);
                threshold = GetXPThresholdForLevel(_battlePassLevel + 1);
            }

            SaveBattlePassState();
        }

        public void GrantBattlePassReward(int level)
        {
            Debug.Log($"[MonetizationManager] BattlePass reward for level {level}");
        }

        public int GetDaysSinceInstall()
        {
            string installStr = PlayerPrefs.GetString(PrefsKeyInstallDate, string.Empty);
            if (string.IsNullOrEmpty(installStr)) return 0;

            long ticks;
            if (!long.TryParse(installStr, out ticks)) return 0;

            long nowTicks = DateTime.UtcNow.Ticks;
            if (nowTicks < ticks) return 0;

            int days = (int)TimeSpan.FromTicks(nowTicks - ticks).TotalDays;
            return days;
        }

        private void EnsureInstallDate()
        {
            string existing = PlayerPrefs.GetString(PrefsKeyInstallDate, string.Empty);
            if (string.IsNullOrEmpty(existing))
            {
                PlayerPrefs.SetString(PrefsKeyInstallDate, DateTime.UtcNow.Ticks.ToString());
                PlayerPrefs.Save();
            }
        }

        private void LoadBattlePassState()
        {
            _battlePassActive = PlayerPrefs.GetInt(PrefsKeyBattlePassActive, 0) == 1;
            _battlePassLevel = PlayerPrefs.GetInt(PrefsKeyBattlePassLevel, 0);
            _battlePassXP = PlayerPrefs.GetFloat(PrefsKeyBattlePassXP, 0f);
        }

        private void SaveBattlePassState()
        {
            PlayerPrefs.SetInt(PrefsKeyBattlePassActive, _battlePassActive ? 1 : 0);
            PlayerPrefs.SetInt(PrefsKeyBattlePassLevel, _battlePassLevel);
            PlayerPrefs.SetFloat(PrefsKeyBattlePassXP, _battlePassXP);
            PlayerPrefs.Save();
        }

        private static float GetXPThresholdForLevel(int level)
        {
            if (level <= 0) return 0f;
            return 100f + (level - 1) * 25f;
        }
    }
}
