# ArchitectureDoc — «Богатырская Застава»

**Дата**: Апрель 2026  
**Статус**: Обязателен к исполнению. Изменения — только через Lead Dev с датой и причиной.

---

## 1. Паттерны проекта

### EventBus (Publisher/Subscriber)

Статический централизованный шина событий. Используется для слабой связанности систем.

```
Кто использует:  GameStateMachine → UIManager, AudioService
                 WaveManager → HUD, Analytics
                 TowerController → SynergySystem, HUD
Реализация:      EventBus.cs (Scripts/Core/)
```

**Правило**: не создавать прямые ссылки между Gameplay-скриптами и UI. Только через EventBus.

### Service Locator

Глобальный реестр сервисов. Сервисы регистрируются в Bootstrap-сцене.

```
Регистрируемые сервисы:
  IAudioService     — реализация: AudioService.cs
  IRewardedAdManager — реализация: YandexAdsManager.cs
  IAnalyticsService — реализация: AppMetricaAnalyticsService.cs (только AppMetrica!)
  ISaveService      — реализация: FirebaseSaveService.cs
  IIAPManager       — реализация: RuStoreIAPManager.cs
```

**Правило**: доступ к сервисам только через интерфейс (`ServiceLocator.Get<IAnalyticsService>()`), не через конкретный класс.

### State Machine (конечный автомат)

Управляет состоянием всей игры. Реализован в `GameStateMachine.cs`.

```
Состояния:
  MainMenu → Loading → Tutorial → Gameplay → RunComplete
  Gameplay → Idle (фон, при сворачивании)
  MainMenu → Shop
  MainMenu → Settings
  Gameplay → EventLobby (при активном ивенте)
```

При смене состояния публикуется событие `GameStateChangedEvent` через EventBus.

### ScriptableObject-driven Data (SO Data)

Все игровые данные — в ScriptableObjects, не в коде. Имена башен — только из game-bible, только через SO.

```
TowerData.cs    → 15 SO-конфигов (T_Ratnik.asset ... T_Beloyyar.asset)
EnemyData.cs    → SO-конфиги врагов
WaveData.cs     → SO-конфиги волн по актам
SynergyData.cs  → 12 SO-конфигов синергий (S-01 ... S-12)
```

---

## 2. Naming Conventions

### Классы и ScriptableObjects
- **PascalCase**: `TowerController`, `WaveManager`, `EnemyPool`
- Интерфейсы с префиксом `I`: `IAudioService`, `IAdManager`
- События с суффиксом `Event`: `TowerPlacedEvent`, `WaveStartedEvent`

### Поля и переменные
- **camelCase** для приватных полей: `private float _attackSpeed;` (с префиксом `_`)
- **PascalCase** для публичных свойств: `public float AttackSpeed { get; private set; }`
- **UPPER_SNAKE_CASE** для констант: `const int MAX_TOWERS_ON_FIELD = 6;`

### Префиксы файлов-ассетов

| Тип | Префикс | Пример |
|---|---|---|
| SO башни | `T_` | `T_Ratnik.asset`, `T_Vityaz.asset` |
| SO врага | `E_` | `E_Goblin.asset` |
| SO волны | `W_` | `W_Act1_Wave01.asset` |
| Синергия | `S_` | `S_01_DruzhinnyStroy.asset` |
| Биом | `B_` | `B_BeryozovyTrakt.asset` |

### Канонические имена башен в коде (строго по game-bible)

Имена башен **НИГДЕ не хардкодятся в строки**. Только через `TowerData.towerName` из SO.

```
Фракция ДРУЖИНА:   Ратник (DR-1), Витязь (DR-2), Святогор (DR-3)
Фракция ВОЛХВЫ:    Ведун (VL-1), Чародей (VL-2), Мерлин Русич (VL-3)
Фракция ЛУЧНИКИ:   Стрелец (LU-1), Соколиный глаз (LU-2), Добрыня-стрелок (LU-3)
Фракция БЕРСЕРКИ:  Рьяный (BE-1), Буян (BE-2), Еруслан (BE-3)
Фракция ЗНАХАРИ:   Травник (ZN-1), Знахарка (ZN-2), Белояр (ZN-3)
```

---

## 3. Assembly Definitions и зависимости

```
Game.Core           ← базовая сборка, нет внешних зависимостей
Game.Data           ← Game.Core
Game.Services       ← Game.Core
Game.StateMachine   ← Game.Core, Game.Services
Game.Gameplay       ← Game.Core, Game.Services, Game.Data
Game.Idle           ← Game.Core, Game.Services, Game.Data
Game.Meta           ← Game.Core, Game.Services, Game.Data
Game.UI             ← Game.Core, Game.Services, Game.Gameplay, Game.Meta
```

Подробнее — в `unity-project-structure.md`.

---

## 4. Аналитика — КРИТИЧЕСКИ ВАЖНО

### РАЗРЕШЕНО
- **AppMetrica** — единственная система аналитики в проекте
- Все события воронки (`level_start`, `level_complete`, `tower_placed`, `run_end`, `iap_purchase` и т.д.) отправляются только через `IAnalyticsService` → `AppMetricaAnalyticsService`

### ЗАПРЕЩЕНО
- **Firebase Analytics** — НЕ инициализируется, НЕ подключается, даже если Firebase SDK установлен
- **Yandex AppMetrica** — только официальная AppMetrica (не путать с Yandex Metrica)
- **AdMob Analytics** — не подключается
- Прямые вызовы `Analytics.LogEvent()` — только через интерфейс `IAnalyticsService`

```csharp
// ПРАВИЛЬНО:
ServiceLocator.Get<IAnalyticsService>().LogEvent("level_start", parameters);

// ЗАПРЕЩЕНО:
Firebase.Analytics.FirebaseAnalytics.LogEvent("level_start");  // ← НИКОГДА
```

---

## 5. Форматы текстур

**Единственный разрешённый формат: ASTC 6x6**

- ASTC 4x4 — запрещён (избыточный размер для целевых устройств)
- ETC2 — запрещён
- RGBA32 — запрещён в production-сборках
- Атласы: максимум 2048×2048
- Baseline: Xiaomi Redmi A2+ поддерживает ASTC, проблем нет

---

## 6. Запрещённые подходы

| Подход | Причина запрета |
|---|---|
| Firebase Analytics | Дублирует AppMetrica, усложняет GDPR/конфиденциальность |
| AdMob | Используется Yandex Mobile Ads v7 |
| RuStore BillingClient (старый) | Только RuStore Pay SDK v10.1.1 |
| Zenject / VContainer | Избыточно для MVP, заменён Service Locator |
| Addressables | Не нужны в MVP, усложняют CI/CD |
| UniRx / R3 | Заменён собственным EventBus |
| `Instantiate()` / `Destroy()` в hot path | Только Object Pool (EnemyPool, ProjectilePool) |
| Хардкод имён башен | Только через TowerData ScriptableObject |
| Хардкод числовых балансовых значений | Только через SO или RemoteConfig |

---

## 7. Baseline устройство и целевые характеристики

**Baseline**: Xiaomi Redmi A2+ (Helio G81, 3GB RAM, Mali-G52 MC2, Android 12)

- Целевые FPS: **30 FPS стабильно**, без дропов ниже 25 FPS
- RAM в рантайме: **не более 300MB**
- `Application.targetFrameRate = 30` — выставляется в Bootstrap-сцене
- `dspBufferSize = 2048` — во избежание crackling на бюджетных устройствах

Подробнее — в `TechnicalLimits.md`.

---

## 8. Версионирование данных

- Каждый SO-класс содержит поле `int dataVersion`
- При изменении структуры данных — `dataVersion++` и миграция в `SaveService`
- Firebase Remote Config используется для горячего обновления балансовых значений (без апдейта)

---

---

## 9. SDK Architecture (Phase 2)

### Интерфейсы и реализации

Все SDK-заглушки находятся в `Scripts/sdk/`. Каждый менеджер реализует интерфейс и регистрируется через `ServiceLocator`. Геймплейный код обращается ТОЛЬКО к интерфейсу, никогда к конкретному классу.

| Интерфейс | Stub-реализация | Файл | Назначение |
|---|---|---|---|
| `IAnalyticsManager` | `AppMetricaManager` | `sdk/AppMetricaManager.cs` | Единственная аналитика (AppMetrica). Firebase Analytics ЗАПРЕЩЁН |
| `IPaymentManager` | `RuStorePayManager` | `sdk/RuStorePayManager.cs` | RuStore Pay SDK v10.1.1. Серверная валидация через Cloud Functions |
| `IRewardedAdManager` | `YandexAdsManager` | `sdk/YandexAdsManager.cs` | Rewarded Video (Yandex Mobile Ads v7). Кулдаун 600 сек |
| `IAuthManager` | `StubAuthManager` | `sdk/FirebaseManager.cs` | Anonymous sign-in + Google/VK linking |
| `ICloudSaveManager` | `StubCloudSaveManager` | `sdk/FirebaseManager.cs` | Firestore: users/{userId}/progress, unlocks, idle |
| `IRemoteConfigManager` | `StubRemoteConfigManager` | `sdk/FirebaseManager.cs` | Remote Config: баланс без релиза |
| `ICloudMessagingManager` | `StubCloudMessagingManager` | `sdk/FirebaseManager.cs` | FCM push (fallback для не-RuStore устройств) |
| `IPushManager` | `PushManager` | `sdk/PushManager.cs` | RuStore Push (primary) + FCM (fallback) |

### Event-система (игровые ивенты)

Файл: `Scripts/EventSystem.cs` — НЕ путать с `EventBus.cs` (шина событий).

`EventManager` — загружает `EventCalendar.json` от GD, активирует/деактивирует культурные и геймплейные ивенты по расписанию.

| Тип | Enum | Описание |
|---|---|---|
| `WeeklyChallenge` | Еженедельные испытания с наградами |
| `LimitedTimeBoss` | Ограниченный по времени босс |
| `FactionBonus` | Бонус для конкретной фракции |
| `DoubleDrops` | Удвоенный дроп ресурсов |

`GameEventData : ScriptableObject` — конфиг одного ивента (id, name, type, duration, rewards, cron schedule).

EventManager подписан на `GameStateChangedEvent` через EventBus — при уходе в Idle/MainMenu все ивенты ставятся на паузу, при возврате — возобновляются.

### Порядок инициализации SDK (Bootstrap-сцена)

```
1. FirebaseManager.Initialize()
   ├── Auth.SignInAnonymously()
   ├── RemoteConfig.FetchAndActivate()
   └── Messaging.Initialize()

2. ServiceLocator.Register<IAuthManager>(firebase.Auth)
   ServiceLocator.Register<ICloudSaveManager>(firebase.CloudSave)
   ServiceLocator.Register<IRemoteConfigManager>(firebase.RemoteConfig)
   ServiceLocator.Register<ICloudMessagingManager>(firebase.Messaging)

3. ServiceLocator.Register<IAnalyticsManager>(new AppMetricaManager())
   → Initialize(apiKey)

4. ServiceLocator.Register<IPaymentManager>(new RuStorePayManager())
   → Initialize(callback)

5. ServiceLocator.Register<IRewardedAdManager>(new YandexAdsManager())
   → Initialize(blockId)

6. ServiceLocator.Register<IPushManager>(new PushManager())
   → Initialize(callback)

7. EventManager (MonoBehaviour на сцене)
   → Awake(): загружает EventCalendar.json + строит lookup
   → OnEnable(): подписывается на GameStateChangedEvent
```

### Anti-fraud (серверная валидация покупок)

```
Клиент (RuStorePayManager)
  → PurchaseProduct() → получает purchaseToken
  → Отправляет ReceiptValidationRequest в Cloud Functions

Cloud Functions (Firebase, зона Lead Dev)
  → Валидирует receipt через RuStore API
  → При успехе: пишет транзакцию в Firestore
  → Возвращает ReceiptValidationResponse клиенту

Клиент
  → isValid == true → выдаёт товар игроку
  → isValid == false → rollback, показывает ошибку
```

### Каноническая воронка событий (AppMetrica)

```
install → first_open
→ tutorial_start → tutorial_step_{N} → tutorial_complete
→ tower_placed_first → level_1_complete → synergy_activated
→ roguelite_unlocked → day_1_return → idle_income_collected_first
→ first_rewarded_ad_shown → first_rewarded_ad_watched
→ first_iap_initiated → first_iap_complete → ad_revenue_event
→ event_started → event_completed → cultural_event_reward_collected
```

Константы имён — в `AnalyticsEvents` (файл `sdk/AppMetricaManager.cs`). Прямые строковые литералы в геймплейном коде ЗАПРЕЩЕНЫ.

---

*Версия: 1.1 | Lead Dev / CTO | Апрель 2026*  
*REVISION 2026-04-09: добавлен раздел 9 — SDK Architecture (Phase 2)*  
*Изменения — только через Lead Dev с датой и причиной изменения*
