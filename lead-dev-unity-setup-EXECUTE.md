# ИСПОЛНИТЕЛЬНАЯ ИНСТРУКЦИЯ: Создание Unity проекта

**Для кого**: Lead Dev / Unity Dev  
**Дата**: 2026-04-09  
**Время**: ~6-8 часов  
**Предварительные условия**: Unity Hub установлен, Unity 2022.3.45f1 загружен, .NET 8 SDK установлен

> Каждый этап заканчивается **контрольной проверкой**. НЕ переходить к следующему этапу без прохождения проверки.

---

## ЭТАП 1: Создание Unity проекта (30 мин)

### 1.1 Создать проект

```bash
# Из терминала (если Unity Hub CLI настроен):
cd /path/to/analyz-rinka/game-team/

# Или через Unity Hub GUI:
# 1. Unity Hub → Projects → New Project
# 2. Template: 2D (URP) — Universal 2D
# 3. Editor Version: 2022.3.45f1
# 4. Project Name: BogatyrskayaZastava
# 5. Location: .../analyz-rinka/game-team/
# → Итоговый путь: game-team/BogatyrskayaZastava/
```

### 1.2 Player Settings (Edit → Project Settings → Player)

```
Company Name:           BogatyrskayaZastava
Product Name:           Богатырская Застава
Version:                0.1.0
Default Icon:           (пропустить, позже)

=== Android ===
Package Name:           com.bogatyrskayazastava.td
Minimum API Level:      Android 7.0 (API 24)
Target API Level:       Android 15 (API 35)

Other Settings:
  Color Space:          Linear
  Auto Graphics API:    OFF
  Graphics APIs:        Vulkan, OpenGLES3 (в таком порядке)
  Scripting Backend:    IL2CPP
  API Compatibility:    .NET Standard 2.1
  Target Architectures: ☑ ARMv7  ☑ ARM64
  Allow unsafe code:    OFF
  Active Input Handling: Both (Input Manager + Input System)
  
Publishing Settings:
  Build App Bundle:     ON (AAB)
  Minify → Release:     R8
  Custom Gradle Templates: включить позже при интеграции SDK
```

### 1.3 Quality Settings (Edit → Project Settings → Quality)

```
Удалить все уровни кроме одного.
Оставить один уровень "Medium":
  V Sync Count:         Don't Sync (FPS контролируем через Application.targetFrameRate)
  Anti Aliasing:        Disabled
  Texture Quality:      Full Res
  Anisotropic Textures: Per Texture
```

### 1.4 Добавить TextMeshPro

```
Window → Package Manager → Unity Registry → TextMeshPro → Install
Затем: Window → TextMeshPro → Import TMP Essential Resources
```

### 1.5 Контрольная проверка этапа 1

```
☐ Проект открывается без ошибок в Console
☐ Build Settings → Android платформа выбрана (Switch Platform если нужно)
☐ Player Settings выставлены как выше
☐ TextMeshPro установлен
```

---

## ЭТАП 2: Структура папок (15 мин)

### 2.1 Создать папки

В Unity: правый клик в Project → Create → Folder. Или из терминала:

```bash
cd game-team/BogatyrskayaZastava/Assets/

# Основные папки
mkdir -p _Game/Scripts/Core
mkdir -p _Game/Scripts/Data
mkdir -p _Game/Scripts/Services/SDK
mkdir -p _Game/Scripts/Gameplay
mkdir -p _Game/Scripts/Idle
mkdir -p _Game/Scripts/UI

mkdir -p _Game/Prefabs/Towers
mkdir -p _Game/Prefabs/Enemies
mkdir -p _Game/Prefabs/Projectiles
mkdir -p _Game/Prefabs/VFX
mkdir -p _Game/Prefabs/UI

mkdir -p _Game/Scenes

mkdir -p _Game/ScriptableObjects/Towers
mkdir -p _Game/ScriptableObjects/Enemies
mkdir -p _Game/ScriptableObjects/Waves
mkdir -p _Game/ScriptableObjects/Synergies
mkdir -p _Game/ScriptableObjects/Biomes

mkdir -p _Game/Resources

mkdir -p _Plugins/AppMetrica
mkdir -p _Plugins/RuStore
mkdir -p _Plugins/Firebase
mkdir -p _Plugins/YandexAds

mkdir -p _Art/Sprites/Towers
mkdir -p _Art/Sprites/Enemies
mkdir -p _Art/Sprites/UI
mkdir -p _Art/Sprites/Biomes
mkdir -p _Art/Atlases

mkdir -p _Audio/Music
mkdir -p _Audio/SFX
```

### 2.2 Создать пустые сцены

В Unity: File → New Scene → Save As:
```
Assets/_Game/Scenes/Bootstrap.unity
Assets/_Game/Scenes/MainMenu.unity
Assets/_Game/Scenes/Gameplay.unity
Assets/_Game/Scenes/Loading.unity
```

Добавить все 4 в Build Settings (File → Build Settings → Add Open Scenes):
```
0: Bootstrap     ← всегда первый!
1: Loading
2: MainMenu
3: Gameplay
```

### 2.3 Скопировать JSON-ресурсы

```bash
# SynergyConfig.json — загружается через Resources.Load<TextAsset>()
cp game-team/gd-artifacts/SynergyConfig.json \
   game-team/BogatyrskayaZastava/Assets/_Game/Resources/SynergyConfig.json

# TowerConfig.json — для референса при создании SO
cp game-team/gd-artifacts/TowerConfig.json \
   game-team/BogatyrskayaZastava/Assets/_Game/Resources/TowerConfig.json
```

### 2.4 Контрольная проверка этапа 2

```
☐ Все папки видны в Unity Project window
☐ 4 сцены существуют и добавлены в Build Settings
☐ SynergyConfig.json лежит в Assets/_Game/Resources/
☐ Console: 0 errors
```

---

## ЭТАП 3: Assembly Definitions (30 мин)

### 3.1 Создать 6 файлов .asmdef

В каждой папке Scripts/*: правый клик → Create → Assembly Definition.

#### Assets/_Game/Scripts/Core/Game.Core.asmdef

```json
{
    "name": "Game.Core",
    "rootNamespace": "BogatyrskayaZastava.Core",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

#### Assets/_Game/Scripts/Data/Game.Data.asmdef

```json
{
    "name": "Game.Data",
    "rootNamespace": "BogatyrskayaZastava.Data",
    "references": [
        "Game.Core"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

#### Assets/_Game/Scripts/Services/Game.Services.asmdef

```json
{
    "name": "Game.Services",
    "rootNamespace": "BogatyrskayaZastava.Core",
    "references": [
        "Game.Core",
        "Game.Data"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

#### Assets/_Game/Scripts/Idle/Game.Idle.asmdef

```json
{
    "name": "Game.Idle",
    "rootNamespace": "BogatyrskayaZastava.Idle",
    "references": [
        "Game.Core"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

#### Assets/_Game/Scripts/Gameplay/Game.Gameplay.asmdef

```json
{
    "name": "Game.Gameplay",
    "rootNamespace": "BogatyrskayaZastava.Gameplay",
    "references": [
        "Game.Core",
        "Game.Data",
        "Game.Services",
        "Game.Idle"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

#### Assets/_Game/Scripts/UI/Game.UI.asmdef

```json
{
    "name": "Game.UI",
    "rootNamespace": "BogatyrskayaZastava.UI",
    "references": [
        "Game.Core",
        "Game.Gameplay",
        "Game.Services",
        "Game.Idle",
        "Unity.TextMeshPro"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

### 3.2 ВАЖНО: TMPro в asmdef

Файлы, использующие `using TMPro;` (Tutorial.cs, GameHUD.cs), находятся в asmdef, которые должны ссылаться на `Unity.TextMeshPro`.

- `Game.UI` — уже добавлено выше
- `Game.Gameplay` — Tutorial.cs использует TMPro → **добавить `"Unity.TextMeshPro"` в references Game.Gameplay.asmdef**

Обновлённый Game.Gameplay.asmdef references:
```json
"references": [
    "Game.Core",
    "Game.Data",
    "Game.Services",
    "Game.Idle",
    "Unity.TextMeshPro"
]
```

### 3.3 ВАЖНО: UnityEngine.UI в asmdef

Файлы, использующие `using UnityEngine.UI;` (GameHUD.cs — Slider, Button) и `using UnityEngine.EventSystems;` (TowerSlot.cs):

Эти namespace'ы находятся в `UnityEngine.UI` module. В Unity 2022.3 они доступны автоматически если asmdef не указывает `noEngineReferences: true` (у нас false). Но для надёжности проверить в Inspector каждого asmdef: Unity UI modules видны.

### 3.4 Контрольная проверка этапа 3

```
☐ 6 .asmdef файлов созданы в правильных папках
☐ В Inspector каждого asmdef видны правильные References
☐ Console: 0 errors (пока файлов нет — ошибок быть не должно)
☐ Game.Gameplay ссылается на: Core, Data, Services, Idle, TextMeshPro
☐ Game.UI ссылается на: Core, Gameplay, Services, Idle, TextMeshPro
```

---

## ЭТАП 4: Перенос .cs файлов (20 мин)

### 4.1 Копирование файлов

Выполнить из корня `game-team/`:

```bash
# ═══════════════════════════════════════════
# CORE (4 файла)
# ═══════════════════════════════════════════
cp scripts/EventBus.cs            BogatyrskayaZastava/Assets/_Game/Scripts/Core/
cp scripts/ServiceLocator.cs      BogatyrskayaZastava/Assets/_Game/Scripts/Core/
cp scripts/GameStateMachine.cs    BogatyrskayaZastava/Assets/_Game/Scripts/Core/
cp scripts/SharedTypes.cs         BogatyrskayaZastava/Assets/_Game/Scripts/Core/

# ═══════════════════════════════════════════
# DATA (4 файла)
# ═══════════════════════════════════════════
cp unity-scripts/Data/TowerData.cs    BogatyrskayaZastava/Assets/_Game/Scripts/Data/
cp unity-scripts/Data/EnemyData.cs    BogatyrskayaZastava/Assets/_Game/Scripts/Data/
cp unity-scripts/Data/WaveData.cs     BogatyrskayaZastava/Assets/_Game/Scripts/Data/
cp unity-scripts/Data/LevelData.cs    BogatyrskayaZastava/Assets/_Game/Scripts/Data/

# ═══════════════════════════════════════════
# SERVICES (9 файлов)
# ═══════════════════════════════════════════
cp scripts/EventSystem.cs                     BogatyrskayaZastava/Assets/_Game/Scripts/Services/
cp unity-scripts/Core/SaveSystem.cs           BogatyrskayaZastava/Assets/_Game/Scripts/Services/
cp unity-scripts/Core/MonetizationManager.cs  BogatyrskayaZastava/Assets/_Game/Scripts/Services/
cp unity-scripts/Core/AnalyticsManager.cs     BogatyrskayaZastava/Assets/_Game/Scripts/Services/

cp scripts/sdk/AppMetricaManager.cs   BogatyrskayaZastava/Assets/_Game/Scripts/Services/SDK/
cp scripts/sdk/YandexAdsManager.cs    BogatyrskayaZastava/Assets/_Game/Scripts/Services/SDK/
cp scripts/sdk/RuStorePayManager.cs   BogatyrskayaZastava/Assets/_Game/Scripts/Services/SDK/
cp scripts/sdk/FirebaseManager.cs     BogatyrskayaZastava/Assets/_Game/Scripts/Services/SDK/
cp scripts/sdk/PushManager.cs         BogatyrskayaZastava/Assets/_Game/Scripts/Services/SDK/

# ═══════════════════════════════════════════
# GAMEPLAY (11 файлов)
# ═══════════════════════════════════════════
cp scripts/EnemyPool.cs                            BogatyrskayaZastava/Assets/_Game/Scripts/Gameplay/
cp unity-scripts/Gameplay/TowerBase.cs             BogatyrskayaZastava/Assets/_Game/Scripts/Gameplay/
cp unity-scripts/Gameplay/TowerSlot.cs             BogatyrskayaZastava/Assets/_Game/Scripts/Gameplay/
cp unity-scripts/Gameplay/TowerPlacementSystem.cs  BogatyrskayaZastava/Assets/_Game/Scripts/Gameplay/
cp unity-scripts/Gameplay/EnemyBase.cs             BogatyrskayaZastava/Assets/_Game/Scripts/Gameplay/
cp unity-scripts/Gameplay/EnemyWaveController.cs   BogatyrskayaZastava/Assets/_Game/Scripts/Gameplay/
cp unity-scripts/Gameplay/SynergySystem.cs         BogatyrskayaZastava/Assets/_Game/Scripts/Gameplay/
cp unity-scripts/Gameplay/RunManager.cs            BogatyrskayaZastava/Assets/_Game/Scripts/Gameplay/
cp unity-scripts/Gameplay/DeckBuilder.cs           BogatyrskayaZastava/Assets/_Game/Scripts/Gameplay/
cp unity-scripts/Gameplay/Tutorial.cs              BogatyrskayaZastava/Assets/_Game/Scripts/Gameplay/
cp unity-scripts/Gameplay/GateController.cs        BogatyrskayaZastava/Assets/_Game/Scripts/Gameplay/

# ═══════════════════════════════════════════
# IDLE (2 файла)
# ═══════════════════════════════════════════
cp unity-scripts/Idle/IdleManager.cs      BogatyrskayaZastava/Assets/_Game/Scripts/Idle/
cp unity-scripts/Idle/ResourceManager.cs  BogatyrskayaZastava/Assets/_Game/Scripts/Idle/

# ═══════════════════════════════════════════
# UI (1 файл)
# ═══════════════════════════════════════════
cp unity-scripts/UI/GameHUD.cs  BogatyrskayaZastava/Assets/_Game/Scripts/UI/
```

### 4.2 Проверка количества

```bash
find BogatyrskayaZastava/Assets/_Game/Scripts/ -name "*.cs" | wc -l
# Ожидаемый результат: 31 (30 перенесённых + SharedTypes.cs)
```

### 4.3 Контрольная проверка этапа 4

```
☐ find показывает 31 файл
☐ Вернуться в Unity → дождаться компиляции
☐ Console: 0 errors — ВСЕ ФАЙЛЫ КОМПИЛИРУЮТСЯ
☐ Если есть ошибки — НЕ переходить к этапу 5, исправить сначала
```

### 4.4 Типичные ошибки на этом этапе и как чинить

| Ошибка | Причина | Решение |
|--------|---------|---------|
| `type or namespace 'TMPro' could not be found` | asmdef не ссылается на Unity.TextMeshPro | Добавить `Unity.TextMeshPro` в references asmdef |
| `type 'RunCompleteData' could not be found in namespace Gameplay` | Старый EventBus.cs скопирован | Убедиться что скопирован ОБНОВЛЁННЫЙ EventBus.cs (ссылка на Core.RunCompleteData) |
| `type 'RewardedContext' does not exist` | Старый MonetizationManager.cs скопирован | Убедиться что скопирован ОБНОВЛЁННЫЙ MonetizationManager.cs (без enum RewardedContext) |
| `type 'ResourceManager' could not be found` | Game.Gameplay не ссылается на Game.Idle | Добавить Game.Idle в references Game.Gameplay.asmdef |
| `type 'IRewardedAdManager' could not be found` | MonetizationManager в Services, а IRewardedAdManager в Services/SDK | Они в одном asmdef — ОК. Проверить что оба файла в папке Services/ |
| `type 'Slider' could not be found` | asmdef без ссылки на UnityEngine.UI | По умолчанию доступен. Если нет — проверить Unity modules |

---

## ЭТАП 5: Подключение SDK (2-3 часа)

### 5.1 External Dependency Manager for Unity (EDM4U) — ПЕРВЫМ

```
1. Window → Package Manager → + → Add package by git URL:
   https://github.com/googlesamples/unity-jar-resolver.git#v1.2.182

2. Дождаться импорта
3. Assets → External Dependency Manager → Android Resolver → Settings:
   ☑ Enable Auto-Resolution
   ☑ Patch androidManifest.xml
   ☑ Patch mainTemplate.gradle
```

**Контрольная проверка:**
```
☐ Меню Assets → External Dependency Manager появилось
☐ Console: 0 errors
```

### 5.2 Firebase SDK (БЕЗ Analytics!)

```
1. Скачать Firebase Unity SDK: https://firebase.google.com/download/unity
   Версия: latest для Unity 2022.3 (≥11.x)

2. Assets → Import Package → Custom Package:
   ☑ FirebaseAuth.unitypackage
   ☑ FirebaseFirestore.unitypackage
   ☑ FirebaseRemoteConfig.unitypackage
   ☑ FirebaseMessaging.unitypackage
   ❌ FirebaseAnalytics.unitypackage — НЕ ИМПОРТИРОВАТЬ!

3. Скопировать google-services.json в Assets/
   (получить из Firebase Console → Project Settings → Android app)

4. EDM4U: Assets → External Dependency Manager → Android Resolver → Force Resolve

5. КРИТИЧЕСКАЯ ПРОВЕРКА:
   В Assets/Plugins/Android/ НЕ должно быть firebase-analytics-*.aar
   Если есть — УДАЛИТЬ! Firebase Messaging тянет Analytics как зависимость.
   
   Способ блокировки:
   Создать файл Assets/Firebase/Editor/generate_xml_from_google_services.py → 
   или добавить в mainTemplate.gradle:
     configurations.all {
         exclude group: 'com.google.firebase', module: 'firebase-analytics'
     }
```

**Контрольная проверка:**
```
☐ Console: 0 errors
☐ Assets/Plugins/Android/ — нет firebase-analytics-*.aar
☐ В коде нет using Firebase.Analytics
```

### 5.3 AppMetrica SDK

```
1. Скачать: https://appmetrica.yandex.ru/docs/mobile-sdk-dg/concepts/unity-plugin.html
   Версия: latest 2026 (≥5.x)

2. Assets → Import Package → Custom Package → AppMetrica.unitypackage
   Папка назначения: Assets/_Plugins/AppMetrica/

3. Конфигурация:
   - В Unity Inspector на любом компоненте или через код:
     API Key: (получить из AppMetrica Console)
   - Location tracking: OFF (не нужно для TD)
   - Crash reporting: ON
```

**Контрольная проверка:**
```
☐ Console: 0 errors
☐ Namespace AppMetrica доступен
```

### 5.4 RuStore Pay SDK v10.1.1

```
1. Скачать с https://www.rustore.ru/help/sdk/payments
   Файлы:
   - rustore-core-*.aar
   - rustore-pay-client-*.aar (версия 10.1.1!)

2. Скопировать AAR файлы в Assets/_Plugins/RuStore/

3. Создать/обновить Assets/Plugins/Android/AndroidManifest.xml:
   Добавить в <application>:
   
   <meta-data
       android:name="rustore_app_id"
       android:value="YOUR_RUSTORE_APP_ID" />

4. В Assets/_Plugins/RuStore/ создать RuStorePayDependencies.xml:

   <dependencies>
     <androidPackages>
       <androidPackage spec="ru.rustore.sdk:pay-client:10.1.1" />
     </androidPackages>
   </dependencies>

5. EDM4U → Force Resolve
```

⚠️ **НЕ использовать BillingClient SDK** — deprecated, умирает 01.08.2026.

**Контрольная проверка:**
```
☐ Console: 0 errors
☐ AAR файлы в Assets/_Plugins/RuStore/
☐ EDM4U resolve прошёл без конфликтов
```

### 5.5 Yandex Mobile Ads SDK v7

```
1. Скачать: https://yandex.ru/dev/mobile-ads/doc/intro/about.html
   Версия: v7.x

2. Импорт: Assets → Import Package → Custom Package
   Или положить AAR в Assets/_Plugins/YandexAds/

3. Создать Assets/_Plugins/YandexAds/YandexAdsDependencies.xml:

   <dependencies>
     <androidPackages>
       <androidPackage spec="com.yandex.android:mobileads:7.+" />
     </androidPackages>
   </dependencies>

4. EDM4U → Force Resolve
```

**Контрольная проверка:**
```
☐ Console: 0 errors  
☐ EDM4U resolve прошёл без конфликтов
```

### 5.6 RuStore Push SDK

```
1. Скачать с https://www.rustore.ru/help/sdk/push-notifications
   Файл: rustore-push-client-*.aar

2. Скопировать в Assets/_Plugins/RuStore/
   (rustore-core уже там от Pay SDK)

3. Обновить RuStorePayDependencies.xml → переименовать в RuStoreDependencies.xml:

   <dependencies>
     <androidPackages>
       <androidPackage spec="ru.rustore.sdk:pay-client:10.1.1" />
       <androidPackage spec="ru.rustore.sdk:push-client:+" />
     </androidPackages>
   </dependencies>

4. EDM4U → Force Resolve
```

### 5.7 Финальный Force Resolve + проверка дубликатов

```bash
# После установки ВСЕХ SDK:
1. Assets → External Dependency Manager → Android Resolver → Force Resolve
2. Проверить Assets/Plugins/Android/ на дубликаты:
   - Не должно быть двух версий одной библиотеки
   - Не должно быть firebase-analytics-*.aar
   - rustore-core должен быть в одном экземпляре
```

### 5.8 Обновить asmdef ссылки на SDK (если нужно)

Если SDK плагины создали свои .asmdef файлы, добавить их как references:

```
Game.Services.asmdef → добавить ссылки на:
  - AppMetrica asmdef (если есть)
  - Firebase asmdef файлы (если есть)
  
Если SDK не создали asmdef → они в default Assembly-CSharp → 
наши asmdef их не видят → нужно добавить в Game.Services:
  "overrideReferences": true,
  "precompiledReferences": ["Firebase.Auth.dll", ...]
  
Или (проще): пока SDK — заглушки, этот шаг не нужен.
Реальные SDK подключатся когда заменим стабы на настоящие вызовы.
```

### 5.9 Контрольная проверка этапа 5

```
☐ Все 5 SDK установлены
☐ EDM4U Force Resolve прошёл
☐ Нет дубликатов AAR
☐ Нет firebase-analytics-*.aar
☐ Console: 0 errors
☐ Build → Android → Build (тестовый) → проходит без ошибок
```

---

## ЭТАП 6: Bootstrap сцена (1 час)

### 6.1 Создать GameBootstrap.cs

Файл: `Assets/_Game/Scripts/Core/GameBootstrap.cs`

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BogatyrskayaZastava.Core
{
    public class GameBootstrap : MonoBehaviour
    {
        [Header("SDK Config — заменить на реальные ключи")]
        [SerializeField] private string appMetricaApiKey = "PLACEHOLDER_APPMETRICA_KEY";
        [SerializeField] private string yandexAdsBlockId = "PLACEHOLDER_YANDEX_ADS_BLOCK";

        [Header("Scenes")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = 30;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        private void Start()
        {
            InitializeSDKs();
        }

        private void InitializeSDKs()
        {
            Debug.Log("[Bootstrap] === Инициализация SDK ===");

            // ——— Шаг 1: Firebase (Auth, Firestore, RemoteConfig, FCM) ———
            var firebase = new FirebaseManager();
            firebase.Initialize(firebaseSuccess =>
            {
                if (!firebaseSuccess)
                {
                    Debug.LogError("[Bootstrap] Firebase init FAILED! Продолжаем без облака.");
                }
                else
                {
                    ServiceLocator.Register<IAuthManager>(firebase.Auth);
                    ServiceLocator.Register<ICloudSaveManager>(firebase.CloudSave);
                    ServiceLocator.Register<IRemoteConfigManager>(firebase.RemoteConfig);
                    ServiceLocator.Register<ICloudMessagingManager>(firebase.Messaging);
                    Debug.Log("[Bootstrap] Firebase: OK (Auth, Firestore, RemoteConfig, FCM)");
                }

                // ——— Шаг 2: AppMetrica (единственная аналитика!) ———
                var appMetrica = new AppMetricaManager();
                appMetrica.Initialize(appMetricaApiKey);
                ServiceLocator.Register<IAnalyticsManager>(appMetrica);
                ServiceLocator.Register<IAnalyticsService>(appMetrica);
                Debug.Log("[Bootstrap] AppMetrica: OK");

                // ——— Шаг 3: RuStore Pay SDK v10.1.1 ———
                var ruStorePay = new RuStorePayManager();
                ruStorePay.Initialize(paySuccess =>
                {
                    ServiceLocator.Register<IPaymentManager>(ruStorePay);
                    Debug.Log($"[Bootstrap] RuStore Pay: {(paySuccess ? "OK" : "FAIL")}");
                });

                // ——— Шаг 4: Yandex Mobile Ads v7 (Rewarded) ———
                var yandexAds = new YandexAdsManager();
                yandexAds.Initialize(yandexAdsBlockId);
                ServiceLocator.Register<IRewardedAdManager>(yandexAds);
                Debug.Log("[Bootstrap] Yandex Ads: OK");

                // ——— Шаг 5: Push (RuStore primary, FCM fallback) ———
                var pushManager = new PushManager();
                pushManager.Initialize(pushSuccess =>
                {
                    ServiceLocator.Register<IPushManager>(pushManager);
                    Debug.Log($"[Bootstrap] Push: {(pushSuccess ? "OK" : "FAIL")}");
                });

                // ——— Шаг 6: Переход на MainMenu ———
                Debug.Log("[Bootstrap] === Все SDK инициализированы ===");
                GameStateMachine.TransitionTo(GameState.Loading);

                // Загружаем MainMenu аддитивно или напрямую
                SceneManager.LoadScene(mainMenuSceneName);
            });
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                // Уходим в фон — сохранить idle timestamp
                if (ServiceLocator.TryGet<Idle.IdleManager>(out var idle))
                {
                    idle.SaveExitTime();
                }

                if (ServiceLocator.TryGet<IAnalyticsManager>(out var analytics))
                {
                    analytics.Flush();
                }
            }
        }
    }
}
```

### 6.2 Собрать Bootstrap сцену

Открыть `Assets/_Game/Scenes/Bootstrap.unity` и создать иерархию:

```
=== Hierarchy ===

[Bootstrap]                          ← пустой GameObject, DontDestroyOnLoad
  ├── GameBootstrap.cs               ← Inspector: заполнить ключи
  ├── SaveSystem.cs                  ← self-registers in Awake
  ├── MonetizationManager.cs         ← self-registers in Awake
  └── AnalyticsManager.cs            ← self-registers in Awake

[IdleSystems]                        ← пустой GameObject, DontDestroyOnLoad
  ├── IdleManager.cs                 ← self-registers in Awake
  └── ResourceManager.cs             ← self-registers in Awake

[EventSystem]                        ← пустой GameObject
  └── EventManager.cs               ← загружает EventCalendar.json
      Inspector:
        Event Calendar Json: (пока null — создать позже)
        Event Database: (пока пустой список)
```

**Как добавить компоненты:**
1. Создать пустой GameObject (правый клик в Hierarchy → Create Empty)
2. Переименовать в "Bootstrap"
3. Add Component → найти GameBootstrap → добавить
4. Повторить для каждого компонента
5. Для [Bootstrap] — НЕ ставить DontDestroyOnLoad вручную (GameBootstrap.cs делает это в Awake)

### 6.3 Контрольная проверка этапа 6

```
☐ Bootstrap.unity содержит все необходимые MonoBehaviour
☐ Play Mode → Bootstrap.unity:
  ☐ В Console видно "[Bootstrap] === Инициализация SDK ==="
  ☐ Видно "[ServiceLocator] Registered: IAuthManager"
  ☐ Видно "[ServiceLocator] Registered: IAnalyticsManager"
  ☐ Видно "[ServiceLocator] Registered: IAnalyticsService"
  ☐ Видно "[ServiceLocator] Registered: IPaymentManager"
  ☐ Видно "[ServiceLocator] Registered: IRewardedAdManager"
  ☐ Видно "[ServiceLocator] Registered: IPushManager"
  ☐ Видно "[Bootstrap] === Все SDK инициализированы ==="
  ☐ 0 errors в Console
```

---

## ЭТАП 7: ScriptableObjects для башен (1-2 часа)

### 7.1 Создать 15 TowerData SO

В `Assets/_Game/ScriptableObjects/Towers/` создать для каждой башни:

**Правый клик → Create → BZ → Tower Data**

| Файл | towerId | towerName | faction | level | hp | damage | atkSpd | range | cost | upgCost | ability |
|------|---------|-----------|---------|-------|-----|--------|--------|-------|------|---------|---------|
| T_Ratnik.asset | DR-1 | Ратник | Druzina | 1 | 120 | 15 | 1.0 | 1.5 | 50 | 80 | StopSingle |
| T_Vityaz.asset | DR-2 | Витязь | Druzina | 2 | 200 | 25 | 0.9 | 1.5 | 100 | 150 | AoEStun |
| T_Svyatogor.asset | DR-3 | Святогор | Druzina | 3 | 380 | 45 | 0.6 | 3.0 | 200 | 0 | AoEStun |
| T_Vedun.asset | VL-1 | Ведун | Volhvy | 1 | 80 | 8 | 1.2 | 2.0 | 55 | 85 | SlowSingle |
| T_Charodey.asset | VL-2 | Чародей | Volhvy | 2 | 130 | 12 | 1.0 | 2.5 | 110 | 165 | SlowCone |
| T_MerlinRusich.asset | VL-3 | Мерлин Русич | Volhvy | 3 | 220 | 18 | 0.8 | 4.0 | 220 | 0 | StopAll |
| T_Strelets.asset | LU-1 | Стрелец | Luchniki | 1 | 70 | 20 | 1.5 | 3.0 | 55 | 80 | None |
| T_SokoliniyGlaz.asset | LU-2 | Соколиный глаз | Luchniki | 2 | 110 | 32 | 1.4 | 3.5 | 110 | 160 | PoisonDot |
| T_DobrynyaStrelok.asset | LU-3 | Добрыня-стрелок | Luchniki | 3 | 190 | 55 | 1.2 | 4.0 | 210 | 0 | ArrowRain |
| T_Ryaniy.asset | BE-1 | Рьяный | Berserki | 1 | 100 | 18 | 1.3 | 1.5 | 50 | 75 | FireDot |
| T_Buyan.asset | BE-2 | Буян | Berserki | 2 | 160 | 28 | 1.1 | 2.0 | 100 | 150 | FireDot |
| T_Yeruslan.asset | BE-3 | Еруслан | Berserki | 3 | 280 | 50 | 0.8 | 2.5 | 195 | 0 | AoEStun |
| T_Travnik.asset | ZN-1 | Травник | Znahari | 1 | 90 | 0 | 0 | 1.5 | 60 | 90 | HealNearest |
| T_Znaharka.asset | ZN-2 | Знахарка | Znahari | 2 | 140 | 0 | 0 | 3.0 | 115 | 170 | HealAoe |
| T_Beloyar.asset | ZN-3 | Белояр | Znahari | 3 | 240 | 0 | 0 | 99 | 230 | 0 | ReviveTower |

### 7.2 Связать nextLevelData

После создания всех 15 SO:
- T_Ratnik.asset → Next Level Data: T_Vityaz.asset
- T_Vityaz.asset → Next Level Data: T_Svyatogor.asset
- T_Svyatogor.asset → Next Level Data: None (null)
- (повторить для каждой фракции: уровень 1 → 2 → 3 → null)

### 7.3 Заполнить AbilityParams

Для каждого SO с ability ≠ None заполнить AbilityParams в Inspector.
Данные из TowerConfig.json (abilityParams):

| SO | duration | slowPct | aoeRad | maxTgt | cd | healPS | dotDmg | dotDur | reviveHP | uses | blockDur | cleanse | cone | dmgBonus | dmgMult |
|-----|----------|---------|--------|--------|-----|--------|--------|--------|----------|------|----------|---------|------|----------|---------|
| DR-1 | 0.5 | 0 | 0 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | false | 0 | 0 | 0 |
| DR-2 | 1.0 | 0 | 2 | 99 | 5 | 0 | 0 | 0 | 0 | 0 | 0 | false | 0 | 0 | 0 |
| DR-3 | 0 | 0 | 3 | 99 | 8 | 0 | 0 | 0 | 0 | 0 | 2.0 | false | 0 | 0 | 0 |
| VL-1 | 3.0 | 20 | 0 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | false | 0 | 0 | 0 |
| VL-2 | 5.0 | 40 | 0 | 3 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | false | 60 | 0 | 0 |
| VL-3 | 3.0 | 0 | 4 | 99 | 20 | 0 | 0 | 0 | 0 | 0 | 0 | false | 0 | 0 | 0 |
| LU-2 | 0 | 0 | 0 | 3 | 0 | 0 | 5 | 5.0 | 0 | 0 | 0 | false | 0 | 0 | 0 |
| LU-3 | 0 | 0 | 4 | 99 | 10 | 0 | 8 | 8.0 | 0 | 0 | 0 | false | 0 | 0 | 0 |
| BE-1 | 0 | 0 | 0 | 1 | 0 | 0 | 6 | 3.0 | 0 | 0 | 0 | false | 0 | 0 | 0 |
| BE-2 | 0 | 0 | 2 | 99 | 6 | 0 | 10 | 4.0 | 0 | 0 | 0 | false | 0 | 0 | 0 |
| BE-3 | 0 | 0 | 3 | 99 | 12 | 0 | 15 | 10.0 | 0 | 0 | 0 | false | 0 | 0 | 0 |
| ZN-1 | 0 | 0 | 0 | 0 | 0 | 5 | 0 | 0 | 0 | 0 | 0 | false | 0 | 0 | 0 |
| ZN-2 | 0 | 0 | 3 | 0 | 0 | 8 | 0 | 0 | 0 | 0 | 0 | true | 0 | 0 | 0 |
| ZN-3 | 0 | 0 | 0 | 0 | 60 | 0 | 0 | 0 | 0.5 | 1 | 0 | false | 0 | 0 | 0 |

### 7.4 Контрольная проверка этапа 7

```
☐ 15 TowerData SO в Assets/_Game/ScriptableObjects/Towers/
☐ Все nextLevelData связи установлены (1→2→3→null для каждой фракции)
☐ AbilityParams заполнены для всех башен с ability ≠ None
☐ T_Strelets (LU-1) AbilityType = None (правильно — нет способности на 1 уровне)
☐ Знахари (ZN-1,2,3): damage = 0, attackSpeed = 0
```

---

## ЭТАП 8: Верификация всего проекта (30 мин)

### 8.1 Компиляция Unity

```
☐ Console: 0 errors
☐ Console: warnings только допустимые (unused fields в стабах)
```

### 8.2 Play Mode тест Bootstrap

```
1. Открыть Bootstrap.unity
2. Play
3. Проверить лог:
   ☐ Все [ServiceLocator] Registered: ... сообщения
   ☐ Все [STUB] Initialized сообщения
   ☐ "[Bootstrap] === Все SDK инициализированы ==="
   ☐ 0 runtime errors
```

### 8.3 dotnet build-check (обратная совместимость)

```bash
cd game-team/
./build-check/build.sh
# Ожидаемый результат: 0 errors
```

### 8.4 Android Build тест

```
1. File → Build Settings → Android → Build
2. Выбрать путь: game-team/BogatyrskayaZastava/Build/test.apk
3. Дождаться завершения
4. Проверить: сборка прошла без ошибок
```

### 8.5 Git

```bash
cd game-team/
git add BogatyrskayaZastava/
git add scripts/SharedTypes.cs
git add scripts/EventBus.cs
git add unity-scripts/Gameplay/RunManager.cs
git add unity-scripts/Core/MonetizationManager.cs
git commit -m "feat: Unity 2022.3.45f1 project setup — 31 .cs files, 6 asmdef, Bootstrap scene, 15 TowerData SO"
```

---

## ИТОГОВЫЙ ЧЕКЛИСТ

```
ЭТАП 1: Unity проект
  ☐ Проект создан в game-team/BogatyrskayaZastava/
  ☐ PlayerSettings: IL2CPP, API 24-35, AAB, Linear

ЭТАП 2: Папки
  ☐ Assets/_Game/Scripts/{Core,Data,Services/SDK,Gameplay,Idle,UI}
  ☐ 4 сцены в Build Settings

ЭТАП 3: asmdef
  ☐ 6 .asmdef файлов с правильными зависимостями
  ☐ TMPro в Gameplay и UI

ЭТАП 4: Файлы
  ☐ 31 .cs файл (30 + SharedTypes.cs)
  ☐ 0 compile errors в Unity

ЭТАП 5: SDK
  ☐ EDM4U установлен
  ☐ Firebase (без Analytics!)
  ☐ AppMetrica
  ☐ RuStore Pay v10.1.1
  ☐ Yandex Ads v7
  ☐ RuStore Push
  ☐ Force Resolve OK

ЭТАП 6: Bootstrap
  ☐ GameBootstrap.cs регистрирует все SDK
  ☐ MonoBehaviour сервисы на сцене
  ☐ Play Mode → все registered

ЭТАП 7: SO
  ☐ 15 TowerData SO с правильными данными
  ☐ nextLevelData связаны

ЭТАП 8: Верификация
  ☐ Unity compile: 0 errors
  ☐ build.sh: 0 errors
  ☐ Play Mode: все сервисы зарегистрированы
  ☐ Android Build: проходит
```

---

## СЛЕДУЮЩИЕ ШАГИ (после завершения этой инструкции)

1. **Unity Dev Middle** — интеграция Gameplay сцены: TowerPlacementSystem + EnemyWaveController + GateController
2. **Unity Dev Middle** — подключить SynergySystem + RunManager + DeckBuilder
3. **Lead Dev** — заменить SDK стабы на реальные вызовы (AppMetrica, RuStore Pay, Yandex Ads)
4. **Lead Dev** — CI: добавить Unity batch-mode build в GitHub Actions
5. **Lead Dev** — unit tests (EventBus, Synergy, DeckBuilder) — неделя 4+
