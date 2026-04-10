using UnityEngine;
using UnityEngine.SceneManagement;
using BogatyrskayaZastava.Gameplay;

namespace BogatyrskayaZastava.Core
{
    public class GameBootstrap : MonoBehaviour
    {
        [Header("SDK Config")]
        [SerializeField] private string appMetricaApiKey = "PLACEHOLDER_APPMETRICA_KEY";
        [SerializeField] private string yandexAdsBlockId = "PLACEHOLDER_YANDEX_ADS_BLOCK";

        [Header("Scenes")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = 30;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            SceneManager.sceneUnloaded += OnSceneUnloaded; // UD-07
        }

        private void OnDestroy()
        {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        private void OnSceneUnloaded(UnityEngine.SceneManagement.Scene scene)
        {
            // UD-07: очищаем EventBus при выгрузке Gameplay — нет стейлых подписчиков
            if (scene.name == "Gameplay")
            {
                EventBus.Clear();
                TowerPool.ClearAll();
                EnemyPool.ClearAll();
            }
        }

        private void Start()
        {
            InitializeSDKs();
        }

        private void InitializeSDKs()
        {
            Debug.Log("[Bootstrap] === Инициализация SDK ===");

            var firebase = new FirebaseManager();
            firebase.Initialize(firebaseSuccess =>
            {
                // QA-034: Register Firebase services regardless of success.
                // FirebaseManager always creates stub implementations before calling onComplete,
                // so Auth/CloudSave/RemoteConfig/Messaging are never null here.
                ServiceLocator.Register<IAuthManager>(firebase.Auth);
                ServiceLocator.Register<ICloudSaveManager>(firebase.CloudSave);
                ServiceLocator.Register<IRemoteConfigManager>(firebase.RemoteConfig);
                ServiceLocator.Register<ICloudMessagingManager>(firebase.Messaging);

                if (!firebaseSuccess)
                {
                    Debug.LogError("[Bootstrap] Firebase init FAILED — running on stubs, cloud features disabled.");
                }
                else
                {
                    Debug.Log("[Bootstrap] Firebase: OK");
                }

                var appMetrica = new AppMetricaManager();
                appMetrica.Initialize(appMetricaApiKey);
                ServiceLocator.Register<IAnalyticsManager>(appMetrica);
                ServiceLocator.Register<IAnalyticsService>(appMetrica);
                Debug.Log("[Bootstrap] AppMetrica: OK");

                var ruStorePay = new RuStorePayManager();
                ruStorePay.Initialize(paySuccess =>
                {
                    ServiceLocator.Register<IPaymentManager>(ruStorePay);
                    Debug.Log($"[Bootstrap] RuStore Pay: {(paySuccess ? "OK" : "FAIL")}");
                });

                var yandexAds = new YandexAdsManager();
                yandexAds.Initialize(yandexAdsBlockId);
                ServiceLocator.Register<IRewardedAdManager>(yandexAds);
                Debug.Log("[Bootstrap] Yandex Ads: OK");

                var pushManager = new PushManager();
                pushManager.Initialize(pushSuccess =>
                {
                    ServiceLocator.Register<IPushManager>(pushManager);
                    Debug.Log($"[Bootstrap] Push: {(pushSuccess ? "OK" : "FAIL")}");
                });

                Debug.Log("[Bootstrap] === Все SDK инициализированы ===");

                // BUG-001: TransitionTo(Loading) ДО загрузки сцены ставит стейт-машину в Loading.
                // Никто потом не переводит её в MainMenu → все переходы из меню невалидны.
                // Фикс: переходим в MainMenu только ПОСЛЕ того как сцена реально загружена.
                SceneManager.sceneLoaded += OnMainMenuSceneLoaded;
                SceneManager.LoadScene(mainMenuSceneName);
            });
        }

        private void OnMainMenuSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            if (scene.name == mainMenuSceneName)
            {
                SceneManager.sceneLoaded -= OnMainMenuSceneLoaded;
                GameStateMachine.TransitionTo(GameState.MainMenu);
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                if (ServiceLocator.TryGet<Idle.IdleManager>(out var idle))
                    idle.SaveExitTime();

                if (ServiceLocator.TryGet<IAnalyticsManager>(out var analytics))
                    analytics.Flush();
            }
        }
    }
}
