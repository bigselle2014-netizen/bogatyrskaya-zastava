# Lead Dev Plan: Создание Unity 2022.3.45f1 проекта

**Дата**: 2026-04-09  
**Статус**: Ожидает согласования  
**Рабочая директория**: `game-team/BogatyrskayaZastava/`

---

## Выбранный подход

Создаём Unity проект рядом с существующими скриптами, переносим 30 .cs файлов в `Assets/_Game/Scripts/` с сохранением namespace'ов. Assembly Definitions делим на 6 сборок (упрощение от 8 в `unity-project-structure.md` — обоснование ниже). SDK подключаем через EDM4U + AAR.

**Почему этот подход лучший:**
- Не меняет namespace'ы → минимум правок в .cs файлах
- Asmdef-структура реально соответствует зависимостям в коде (а не теоретической схеме)
- SDK через EDM4U — стандарт для Firebase + позволяет автоматическое разрешение Android-зависимостей

---

## КРИТИЧЕСКИЕ ПРОБЛЕМЫ (обнаружены при аудите кода)

### Проблема 1: Циклические зависимости в EventBus.cs

`EventBus.cs` (Core) содержит event-структуры, которые ссылаются на типы из других namespace'ов:

```csharp
// строка 118 — ссылка на Gameplay:
public struct RunCompletedEvent { ... public BogatyrskayaZastava.Gameplay.RunCompleteData data; }

// строка 135 — ссылка на Core (MonetizationManager.cs):
public struct AdWatchedEvent { public BogatyrskayaZastava.Core.RewardedContext context; }
```

**`RunCompleteData`** определён в `RunManager.cs` (Gameplay). Если EventBus в asmdef `Game.Core`, а RunManager в `Game.Gameplay`, то Core → Gameplay = **циклическая зависимость**.

**`RewardedContext`** определён в `MonetizationManager.cs`. Если MonetizationManager в отдельной сборке от EventBus — та же проблема.

**Решение (требует правок в 2 файлах):**

1. **Извлечь `RunCompleteData`** из `RunManager.cs` в новый файл `Scripts/Core/SharedTypes.cs` (namespace `BogatyrskayaZastava.Core`)
2. **Извлечь `RewardedContext`** из `MonetizationManager.cs` туда же
3. **Обновить ссылку** в `EventBus.cs` строка 118: `BogatyrskayaZastava.Core.RunCompleteData` вместо `BogatyrskayaZastava.Gameplay.RunCompleteData`
4. **Обновить using** в `RunManager.cs`: добавить `using BogatyrskayaZastava.Core;` (уже есть)

### Проблема 2: Gameplay → Idle зависимость

`TowerPlacementSystem.cs` (Gameplay) импортирует `BogatyrskayaZastava.Idle.ResourceManager`. В `unity-project-structure.md` эта зависимость не указана.

**Решение:** Добавить `Game.Idle` в зависимости `Game.Gameplay.asmdef`. Циклической зависимости нет (Idle не импортирует Gameplay).

### Проблема 3: UI → Idle зависимость

`GameHUD.cs` (UI) импортирует `BogatyrskayaZastava.Idle`. В `unity-project-structure.md` не указано.

**Решение:** Добавить `Game.Idle` в зависимости `Game.UI.asmdef`.

### Проблема 4: Устаревшая запись IronSource

В `unity-project-structure.md` папка `_Plugins/IronSource/` — IronSource удалён, используется Yandex Mobile Ads v7. Убрать из документации.

---

## Шаг A: Создание Unity проекта

**Критерий готовности:** Пустой проект открывается, PlayerSettings настроены.

```
1. Unity Hub → New Project → 2D URP → Unity 2022.3.45f1
   Путь: game-team/BogatyrskayaZastava/
   
2. PlayerSettings:
   - Company Name: BogatyrskayaZastava
   - Product Name: Богатырская Застава
   - Package Name: com.bogatyrskayazastava.td
   - Target API: 35 (Android 15)
   - Minimum API: 24 (Android 7.0)
   - Scripting Backend: IL2CPP
   - Target Architectures: ARMv7 + ARM64
   - Application.targetFrameRate = 30 (в Bootstrap)
   - Color Space: Linear
   
3. .gitignore для Unity (Library/, Temp/, obj/, Build/, Logs/)
```

---

## Шаг B: Создание структуры папок

```
Assets/
├── _Game/
│   ├── Scripts/
│   │   ├── Core/           ← EventBus, ServiceLocator, GameStateMachine, SharedTypes
│   │   ├── Data/           ← TowerData, EnemyData, WaveData, LevelData
│   │   ├── Services/       ← SDK managers, EventSystem, SaveSystem, Monetization, Analytics
│   │   │   └── SDK/        ← AppMetrica, Yandex Ads, RuStore, Firebase, Push
│   │   ├── Gameplay/       ← Tower*, Enemy*, Synergy, Run, Deck, Tutorial, Gate, EnemyPool
│   │   ├── Idle/           ← IdleManager, ResourceManager
│   │   └── UI/             ← GameHUD (+ будущие экраны)
│   ├── Prefabs/
│   │   ├── Towers/
│   │   ├── Enemies/
│   │   ├── Projectiles/
│   │   ├── VFX/
│   │   └── UI/
│   ├── Scenes/
│   │   ├── Bootstrap.unity
│   │   ├── MainMenu.unity
│   │   ├── Gameplay.unity
│   │   └── Loading.unity
│   ├── ScriptableObjects/
│   │   ├── Towers/         ← T_Ratnik.asset ... (15 штук)
│   │   ├── Enemies/
│   │   ├── Waves/
│   │   ├── Synergies/
│   │   └── Biomes/
│   └── Resources/
│       └── SynergyConfig.json  ← копия из gd-artifacts/
│
├── _Plugins/               ← SDK плагины (AAR/UPM)
│   ├── AppMetrica/
│   ├── RuStore/
│   ├── Firebase/           ← БЕЗ Analytics!
│   └── YandexAds/
│
├── _Art/
│   ├── Sprites/
│   ├── Atlases/            ← ASTC 6x6, макс 2048x2048
│   └── ФОРМАТ: ASTC 6x6
│
└── _Audio/
```

---

## Шаг C: Assembly Definitions (6 штук)

### Обоснование упрощения (6 вместо 8)

Исходная схема из `unity-project-structure.md` имела 8 asmdef:
- `Game.Core`, `Game.Services`, `Game.Data`, `Game.Gameplay`, `Game.Idle`, `Game.Meta`, `Game.UI`, `Game.StateMachine`

**Убраны:**
- `Game.StateMachine` → объединён с `Game.Core` (GameStateMachine в том же namespace, нет смысла разделять 1 файл)
- `Game.Meta` → объединён с `Game.Services` (SaveSystem, MonetizationManager, AnalyticsManager — это сервисы, не мета-прогрессия)

### Итоговые asmdef

| # | Asmdef | Папка | Зависит от | Файлы |
|---|--------|-------|------------|-------|
| 1 | `Game.Core` | `Scripts/Core/` | — | EventBus.cs, ServiceLocator.cs, GameStateMachine.cs, **SharedTypes.cs** (новый) |
| 2 | `Game.Data` | `Scripts/Data/` | Game.Core | TowerData.cs, EnemyData.cs, WaveData.cs, LevelData.cs |
| 3 | `Game.Services` | `Scripts/Services/` | Game.Core, Game.Data | SDK/*.cs, EventSystem.cs, SaveSystem.cs, MonetizationManager.cs, AnalyticsManager.cs |
| 4 | `Game.Idle` | `Scripts/Idle/` | Game.Core | IdleManager.cs, ResourceManager.cs |
| 5 | `Game.Gameplay` | `Scripts/Gameplay/` | Game.Core, Game.Data, Game.Services, **Game.Idle** | TowerBase.cs, TowerSlot.cs, TowerPlacementSystem.cs, EnemyBase.cs, EnemyWaveController.cs, EnemyPool.cs, SynergySystem.cs, RunManager.cs, DeckBuilder.cs, Tutorial.cs, GateController.cs |
| 6 | `Game.UI` | `Scripts/UI/` | Game.Core, Game.Gameplay, Game.Services, **Game.Idle** | GameHUD.cs |

### Пример Game.Core.asmdef

```json
{
  "name": "Game.Core",
  "rootNamespace": "BogatyrskayaZastava.Core",
  "references": [],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "autoReferenced": true
}
```

### Пример Game.Gameplay.asmdef

```json
{
  "name": "Game.Gameplay",
  "rootNamespace": "BogatyrskayaZastava.Gameplay",
  "references": [
    "GUID:xxx-Game.Core",
    "GUID:xxx-Game.Data",
    "GUID:xxx-Game.Services",
    "GUID:xxx-Game.Idle"
  ],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "autoReferenced": true
}
```

---

## Шаг D: Перенос 30 .cs файлов (маппинг)

### Core/ (3 существующих + 1 новый)

| Источник | Назначение |
|----------|-----------|
| `scripts/EventBus.cs` | `Scripts/Core/EventBus.cs` |
| `scripts/ServiceLocator.cs` | `Scripts/Core/ServiceLocator.cs` |
| `scripts/GameStateMachine.cs` | `Scripts/Core/GameStateMachine.cs` |
| **НОВЫЙ** | `Scripts/Core/SharedTypes.cs` ← RunCompleteData + RewardedContext |

### Data/ (4 файла)

| Источник | Назначение |
|----------|-----------|
| `unity-scripts/Data/TowerData.cs` | `Scripts/Data/TowerData.cs` |
| `unity-scripts/Data/EnemyData.cs` | `Scripts/Data/EnemyData.cs` |
| `unity-scripts/Data/WaveData.cs` | `Scripts/Data/WaveData.cs` |
| `unity-scripts/Data/LevelData.cs` | `Scripts/Data/LevelData.cs` |

### Services/ (7 файлов)

| Источник | Назначение |
|----------|-----------|
| `scripts/sdk/AppMetricaManager.cs` | `Scripts/Services/SDK/AppMetricaManager.cs` |
| `scripts/sdk/YandexAdsManager.cs` | `Scripts/Services/SDK/YandexAdsManager.cs` |
| `scripts/sdk/RuStorePayManager.cs` | `Scripts/Services/SDK/RuStorePayManager.cs` |
| `scripts/sdk/FirebaseManager.cs` | `Scripts/Services/SDK/FirebaseManager.cs` |
| `scripts/sdk/PushManager.cs` | `Scripts/Services/SDK/PushManager.cs` |
| `scripts/EventSystem.cs` | `Scripts/Services/EventSystem.cs` |
| `unity-scripts/Core/SaveSystem.cs` | `Scripts/Services/SaveSystem.cs` |
| `unity-scripts/Core/MonetizationManager.cs` | `Scripts/Services/MonetizationManager.cs` |
| `unity-scripts/Core/AnalyticsManager.cs` | `Scripts/Services/AnalyticsManager.cs` |

### Gameplay/ (11 файлов)

| Источник | Назначение |
|----------|-----------|
| `scripts/EnemyPool.cs` | `Scripts/Gameplay/EnemyPool.cs` |
| `unity-scripts/Gameplay/TowerBase.cs` | `Scripts/Gameplay/TowerBase.cs` |
| `unity-scripts/Gameplay/TowerSlot.cs` | `Scripts/Gameplay/TowerSlot.cs` |
| `unity-scripts/Gameplay/TowerPlacementSystem.cs` | `Scripts/Gameplay/TowerPlacementSystem.cs` |
| `unity-scripts/Gameplay/EnemyBase.cs` | `Scripts/Gameplay/EnemyBase.cs` |
| `unity-scripts/Gameplay/EnemyWaveController.cs` | `Scripts/Gameplay/EnemyWaveController.cs` |
| `unity-scripts/Gameplay/SynergySystem.cs` | `Scripts/Gameplay/SynergySystem.cs` |
| `unity-scripts/Gameplay/RunManager.cs` | `Scripts/Gameplay/RunManager.cs` |
| `unity-scripts/Gameplay/DeckBuilder.cs` | `Scripts/Gameplay/DeckBuilder.cs` |
| `unity-scripts/Gameplay/Tutorial.cs` | `Scripts/Gameplay/Tutorial.cs` |
| `unity-scripts/Gameplay/GateController.cs` | `Scripts/Gameplay/GateController.cs` |

### Idle/ (2 файла)

| Источник | Назначение |
|----------|-----------|
| `unity-scripts/Idle/IdleManager.cs` | `Scripts/Idle/IdleManager.cs` |
| `unity-scripts/Idle/ResourceManager.cs` | `Scripts/Idle/ResourceManager.cs` |

### UI/ (1 файл)

| Источник | Назначение |
|----------|-----------|
| `unity-scripts/UI/GameHUD.cs` | `Scripts/UI/GameHUD.cs` |

---

## Шаг E: Правки в .cs файлах (минимальные, только необходимые)

### E.1 Создать новый файл: `Scripts/Core/SharedTypes.cs`

```csharp
namespace BogatyrskayaZastava.Core
{
    public enum RewardedContext
    {
        AfterDefeat,
        LowGateHP,
        IdleCapHalf
    }
}

namespace BogatyrskayaZastava.Core
{
    public struct RunCompleteData
    {
        public int wavesCleared;
        public int runesEarned;
        public int coinsEarned;
        public int synergiesActivated;
        public bool isVictory;
        public float totalTime;
    }
}
```

### E.2 EventBus.cs — обновить ссылку (строка 118)

```csharp
// БЫЛО:
public struct RunCompletedEvent { public bool isVictory; public int wavesCompleted; public BogatyrskayaZastava.Gameplay.RunCompleteData data; }

// СТАЛО:
public struct RunCompletedEvent { public bool isVictory; public int wavesCompleted; public BogatyrskayaZastava.Core.RunCompleteData data; }
```

### E.3 RunManager.cs — убрать определение RunCompleteData

Удалить строки 19-27 (struct RunCompleteData) — теперь в SharedTypes.cs.
Добавить в начало: `using BogatyrskayaZastava.Core;` (уже есть).

### E.4 MonetizationManager.cs — убрать определение RewardedContext

Удалить строки 8-13 (enum RewardedContext) — теперь в SharedTypes.cs.

### E.5 Проверка: build.sh

После всех правок запустить `./game-team/build-check/build.sh` → должно быть 0 errors.

---

## Шаг F: Подключение SDK через UPM/AAR

### Порядок установки (важен!)

#### F.1 External Dependency Manager for Unity (EDM4U) — ПЕРВЫМ

```
Unity → Window → Package Manager → Add by git URL:
https://github.com/googlesamples/unity-jar-resolver.git
```

EDM4U разрешает Android-зависимости автоматически. Без него Firebase и другие AAR-плагины будут конфликтовать.

#### F.2 Firebase SDK (БЕЗ Analytics!)

```
Загрузить: https://firebase.google.com/download/unity
Импортировать ТОЛЬКО:
  ✅ FirebaseAuth.unitypackage
  ✅ FirebaseFirestore.unitypackage
  ✅ FirebaseRemoteConfig.unitypackage
  ✅ FirebaseMessaging.unitypackage
  ❌ FirebaseAnalytics.unitypackage — ЗАПРЕЩЁН!
  
Положить google-services.json в Assets/
```

⚠️ **ВНИМАНИЕ**: FirebaseAnalytics подтягивается как зависимость FirebaseMessaging. В `Assets/Firebase/m2repository/` удалить `firebase-analytics-*.aar` если появится. Проверить что `Firebase.Analytics` namespace нигде не импортируется.

#### F.3 AppMetrica SDK (единственная аналитика)

```
Загрузить: https://appmetrica.yandex.ru/docs/mobile-sdk-dg/concepts/unity-plugin.html
Версия: latest (2026)
Импорт: AppMetrica.unitypackage → Assets/_Plugins/AppMetrica/
```

#### F.4 RuStore SDK (Pay + Push)

```
Загрузить: https://www.rustore.ru/help/sdk/payments
Файлы:
  rustore-core-*.aar        → Assets/_Plugins/RuStore/
  rustore-pay-v10.1.1.aar   → Assets/_Plugins/RuStore/
  rustore-push-*.aar         → Assets/_Plugins/RuStore/

AndroidManifest.xml — добавить:
  <meta-data android:name="rustore_app_id" android:value="YOUR_APP_ID"/>
  
⚠️ НЕ использовать BillingClient SDK (deprecated 01.08.2026)
```

#### F.5 Yandex Mobile Ads SDK v7

```
Загрузить: https://yandex.ru/dev/mobile-ads/doc/intro/about.html
Версия: v7.x
Файлы: AAR → Assets/_Plugins/YandexAds/
```

#### F.6 TextMeshPro (уже в UPM, но подтвердить)

```
Unity → Package Manager → TextMeshPro → Install/Update
Импортировать TMP Essential Resources
```

### После установки всех SDK

```
1. EDM4U → Assets → External Dependency Manager → Android Resolver → Force Resolve
2. Убедиться что нет дубликатов AAR
3. Build → Android → проверить что компиляция проходит
```

---

## Шаг G: Bootstrap сцена + GameBootstrap.cs

### Создать `Scripts/Core/GameBootstrap.cs` (НОВЫЙ ФАЙЛ)

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;
using BogatyrskayaZastava.Core;

namespace BogatyrskayaZastava.Core
{
    public class GameBootstrap : MonoBehaviour
    {
        [Header("SDK Config")]
        [SerializeField] private string appMetricaApiKey = "YOUR_KEY";
        [SerializeField] private string yandexAdsBlockId = "YOUR_BLOCK_ID";

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = 30;
        }

        private async void Start()
        {
            // 1. Firebase (Auth, Firestore, RemoteConfig, FCM)
            var firebase = new FirebaseManager();
            firebase.Initialize(success =>
            {
                if (!success) Debug.LogError("[Bootstrap] Firebase init failed!");
                
                ServiceLocator.Register<IAuthManager>(firebase.Auth);
                ServiceLocator.Register<ICloudSaveManager>(firebase.CloudSave);
                ServiceLocator.Register<IRemoteConfigManager>(firebase.RemoteConfig);
                ServiceLocator.Register<ICloudMessagingManager>(firebase.Messaging);

                // 2. AppMetrica (единственная аналитика)
                var appMetrica = new AppMetricaManager();
                appMetrica.Initialize(appMetricaApiKey);
                ServiceLocator.Register<IAnalyticsManager>(appMetrica);
                ServiceLocator.Register<IAnalyticsService>(appMetrica);

                // 3. RuStore Pay
                var ruStorePay = new RuStorePayManager();
                ruStorePay.Initialize(paySuccess =>
                {
                    ServiceLocator.Register<IPaymentManager>(ruStorePay);
                });

                // 4. Yandex Ads (Rewarded)
                var yandexAds = new YandexAdsManager();
                yandexAds.Initialize(yandexAdsBlockId);
                ServiceLocator.Register<IRewardedAdManager>(yandexAds);

                // 5. Push (RuStore primary, FCM fallback)
                var push = new PushManager();
                push.Initialize(pushSuccess =>
                {
                    ServiceLocator.Register<IPushManager>(push);
                });

                // 6. Переход в MainMenu
                GameStateMachine.TransitionTo(GameState.MainMenu);
                // SceneManager.LoadScene("MainMenu");
            });
        }
    }
}
```

### Bootstrap.unity сцена — состав

```
[Bootstrap] (GameObject, DontDestroyOnLoad)
├── GameBootstrap.cs          ← инициализация SDK + ServiceLocator
├── SaveSystem.cs             ← self-registers in Awake
├── IdleManager.cs            ← self-registers in Awake
├── ResourceManager.cs        ← self-registers in Awake
├── MonetizationManager.cs    ← self-registers in Awake
├── AnalyticsManager.cs       ← self-registers in Awake
└── EventManager.cs           ← загружает EventCalendar.json

[GameplaySystems] (GameObject, активируется при загрузке Gameplay сцены)
├── TowerPlacementSystem.cs   ← self-registers in Awake
├── EnemyWaveController.cs
├── GateController.cs
├── SynergySystem.cs          ← self-registers in Awake
├── RunManager.cs             ← self-registers in Awake
└── DeckBuilder.cs            ← self-registers in Awake
```

**Порядок регистрации (Awake → Start):**
1. Awake: все MonoBehaviour self-register (SaveSystem, ResourceManager, etc.)
2. Start: GameBootstrap инициализирует non-MonoBehaviour SDK сервисы
3. Callback: после инициализации Firebase → регистрация остальных SDK
4. Переход: GameStateMachine → MainMenu

---

## Шаг H: Resources + ScriptableObjects

```
1. Скопировать gd-artifacts/SynergyConfig.json → Assets/_Game/Resources/SynergyConfig.json
   (SynergySystem загружает через Resources.Load<TextAsset>("SynergyConfig"))

2. Скопировать gd-artifacts/TowerConfig.json → Assets/_Game/Resources/TowerConfig.json
   (для будущего использования)

3. Создать 15 TowerData SO (T_Ratnik.asset → T_Beloyyar.asset)
   по данным из TowerConfig.json в Assets/_Game/ScriptableObjects/Towers/

4. Создать EnemyData SO в Assets/_Game/ScriptableObjects/Enemies/

5. Создать WaveData SO в Assets/_Game/ScriptableObjects/Waves/
```

---

## Шаг I: Верификация

```
1. Unity → File → Build Settings → Android → Switch Platform
2. Проверить 0 compile errors в Console
3. Проверить что все 6 asmdef разрешаются без циклических зависимостей
4. Запустить Bootstrap.unity → проверить лог:
   - "[ServiceLocator] Registered: ..." для каждого сервиса
   - "[AppMetrica STUB] Initialized..."
   - "[RuStorePay STUB] Initialized..."
   - "[YandexAds STUB] Initialized..."
   - "[Push STUB] Initialized..."
5. ./game-team/build-check/build.sh → 0 errors (после правок E.1-E.4)
```

---

## Риски и митигация

| Риск | Вероятность | Митигация |
|------|-------------|-----------|
| EDM4U конфликт AAR версий (Firebase vs RuStore) | Высокая | Force Resolve после каждого SDK, вручную проверить дубликаты |
| Firebase Analytics подтянулся как транзитивная зависимость | Средняя | Проверить m2repository, удалить firebase-analytics-*.aar |
| RuStore Pay SDK v10.1.1 нет в UPM — только AAR | Факт | Скачать с сайта, положить в _Plugins/RuStore/ |
| AppMetrica Unity SDK не поддерживает 2022.3 | Низкая | Проверить changelog, если нет — использовать AAR напрямую |
| Namespace в разных asmdef — internal классы невидимы | Низкая | Проверено: все internal классы используются только внутри своего файла |

---

## Что НЕ делаем и почему

- **Не меняем namespace'ы** — существующий код работает, изменение namespace = массовые правки без пользы
- **Не подключаем Addressables** — избыточно для MVP (см. ArchitectureDoc)
- **Не подключаем Firebase Analytics** — ЗАПРЕЩЁН архитектурным документом
- **Не создаём unit-тесты сейчас** — по CI плану это неделя 4+
- **Не оптимизируем под Redmi A2+** — это Phase 3
- **Не создаём IronSource/AdMob** — используем только Yandex Mobile Ads v7

---

## Порядок выполнения (A → I)

```
A. Создать Unity проект                    ← 30 мин
B. Создать структуру папок                 ← 15 мин
C. Создать 6 asmdef файлов                ← 30 мин
D. Перенести 30 .cs файлов                ← 20 мин
E. Правки в .cs (SharedTypes + ссылки)     ← 30 мин + build.sh
F. Подключить SDK (EDM4U → Firebase → ...) ← 2-3 часа
G. Создать Bootstrap сцену + GameBootstrap ← 1 час
H. Resources + ScriptableObjects           ← 1-2 часа
I. Верификация                             ← 30 мин
```

**Общая оценка: 1 рабочий день (6-8 часов)**

**Gate:** Unity проект компилируется, все сервисы регистрируются в ServiceLocator, `build.sh` проходит с 0 errors.
