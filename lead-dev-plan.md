# Lead Dev / CTO — Рабочий план: "Богатырская Застава"

**Дата**: Апрель 2026  
**Роль**: Lead Developer / CTO  
**Горизонт**: Фаза 1–2 (5 месяцев), далее поддержка архитектурных решений

---

## 1. ФАЗА 1 — ПРОТОТИП (Месяц 1–2, бюджет ~$30K)

### Неделя 1–2: Фундамент проекта

**Настраиваю среду и репозиторий:**
- Unity 2022.3 LTS (не 2023, не 6000 — стабильность важнее фич). Конкретно: Unity 2022.3.x — последний LTS-патч на момент старта.
- IL2CPP включаю сразу в Build Settings → Scripting Backend. Mono — только для редактора (ускоряет итерации). Это +40% FPS на релизе, нет выбора.
- Target API Level: 35 (min SDK: 24). Google Play требует 35 с августа 2025, закладываю заранее.
- Формат сборки: Android App Bundle (.aab) — обязателен для RuStore и Google Play. APK только для внутреннего тестирования.
- Репозиторий: Git + Git LFS для бинарных ассетов (спрайты, аудио). Настраиваю `.gitignore` под Unity (Library/, Temp/, Builds/).
- Ветки: `main` (стабильный билд) / `develop` (текущая разработка) / `feature/*`. Фичи мерджатся только через PR после прохождения билда.

**Покупаю ассеты сразу (не откладываю):**
- Tower Defense Starter Kit ($50) — беру за основу систему волн, pathfinding, логику размещения башен
- Solo Tower 2D Template ($19) — как второй референс для 2D-раскладки
- SFX Pack 100+ ($15)
- Итого ~$84 экономит нам 200+ часов разработки

**Настраиваю базовую архитектуру проекта** (папки, namespace, assembly definitions — сразу, до первой строчки геймплейного кода):
```
Assets/
  _Game/
    Scripts/
      Core/          ← SceneLoader, GameManager, EventBus
      Gameplay/      ← TowerController, EnemyController, WaveManager
      Idle/          ← IdleManager, ResourceAccumulator
      Meta/          ← ProgressionSystem, RunManager
      UI/            ← UIManager, HUD, Popups
      Data/          ← SO-конфиги башен, врагов, уровней
    Prefabs/
    Scenes/
    ScriptableObjects/
  _Plugins/          ← SDK-плагины (RuStore, AppMetrica и т.д.)
  _Art/              ← Спрайты, атласы
  _Audio/
```

**К неделе 3 передаю Unity Dev:**
- `FirebaseSetup.md` — структура DB, auth схема (Anonymous sign-in, схема документов Firestore)
- Unity Dev начинает Cloud Save интеграцию с недели 4 (хендовер по game-bible секция 6, Блок В)

### Неделя 3–6: Core Loop (TD + базовый Idle)

**Что реализую лично (или ставлю задачи Middle Dev под свой контроль):**

**Tower Defense система:**
- `WaveManager` — ScriptableObject-конфиги волн. Волны описываются как данные, не захардкожены в коде. Это позволяет GD менять баланс без программиста.
- `EnemySpawner` — Object Pool обязателен. Никаких `Instantiate/Destroy` в hot path. Пул инициализируется при загрузке сцены.
- `TowerPlacementSystem` — grid-based расстановка. Grid описывается как ScriptableObject с конфигом уровня.
- `TowerAttackController` — базовая логика атаки (таргетинг по приоритету: первый в пути, наименьшее HP, максимальное HP — выбор через enum).
- `SynergyDetector` — отслеживает расстановку башен, вычисляет активные синергии. Событийная архитектура: башня поставлена → EventBus → SynergyDetector пересчитывает → SynergyUI обновляется. Никакого polling каждый кадр.

**Idle-механизм (базовый, без сервера):**
- `IdleManager` — при выходе из игры записывает `Application.Quit` timestamp в PlayerPrefs. При входе вычисляет дельту времени, начисляет ресурсы по формуле: `накоплено = baseRate × time × multiplier`. Кап: 8 часов офлайна (не даём бесконечный фарм).
- Хранение: PlayerPrefs для прототипа (Firebase — в Фазе 2).

**К неделе 6 передаю Unity Dev:**
- `IdleOfflineConfig` — формулы расчёта офлайн-дохода (baseRate, multiplier, кап 8 часов)
- Unity Dev начинает `IdleManager.cs` с недели 7 (хендовер по game-bible секция 6, Блок В)

**Управление FPS для бюджетных устройств:**
- Сразу ставлю `Application.targetFrameRate = 30` + `QualitySettings` с двумя пресетами: Low (для Helio G81 и слабее) и High (для SD 600+).
- Baseline device для тестирования: Xiaomi Redmi A2+ (3GB RAM, Mali-G52). Если на нём 30 FPS стабильно — значит, покрываем 70% рынка РФ.

**Audio:**
- `AudioSource.dspBufferSize = 2048` — иначе crackling на бюджетных девайсах.

### Неделя 7–8: Тестирование прототипа

- Собираю APK (debug, Mono) и раздаю 20 тестерам.
- Никакой аналитики ещё нет — только Google Form с одним вопросом: "Хочешь сыграть ещё раз? Да/Нет/Объясни".
- **Критерий выхода из Фазы 1 (строго из game-bible секция 4)**: D1 self-report = 70% тестеров отвечают "хочу ещё сыграть". Выборка: не менее 20 человек. Период измерения: последние 3 дня, не пиковые значения.
- Если D1 self-report < 70% — останавливаю работы на Фазе 2 и итерирую по feedback. Не 68%, не 65% — именно 70%.
- Нет критических крашей на тестовых устройствах.

---

## 2. ФАЗА 2 — MVP (Месяц 3–5, бюджет ~$80K)

### Месяц 3: Контент + Синергии + Roguelite

**Расширяю систему башен:**
- 5 фракций × 3 башни = 15 unit'ов (имена строго по game-bible секция 1: Ратник, Витязь, Святогор, Ведун, Чародей, Мерлин Русич, Стрелец, Соколиный глаз, Добрыня-стрелок, Рьяный, Буян, Еруслан, Травник, Знахарка, Белояр). Все описаны через `TowerConfig : ScriptableObject`.
- `SynergyConfig : ScriptableObject` — описывает условия синергии (12 канонических синергий из game-bible секция 3). Добавление новой синергии = создание нового SO.

**Roguelite система:**
- `RunManager` — управляет состоянием рана. Ран = набор уровней с нарастающей сложностью.
- `PermanentProgressionData` — то, что сохраняется между рунами (разблокированные башни, перки). Хранится в Firebase (с этого месяца).
- `RandomEventSystem` — пул событий (SO-описания). При выборе события игрок видит 3 варианта. Никакого полного рандома — только выбор из доступных опций.

**Event-система культурных ивентов (мой владелец по game-bible секция 5):**
- Жду `EventCalendar.json` от GD к неделе 8 (хендовер по game-bible секция 6, Блок А).
- С недели 9 начинаю `EventSystem.cs` — техническая реализация принадлежит мне.
- Структура: `EventConfig : ScriptableObject` с полями (eventId, startDate, endDate, rewards, modifiedWaves). Активация — через Remote Config (флаг) + локальный EventCalendar.
- Тип ивентов: сезонные (Масленица, Иван Купала, Новый год) + геймплейные (двойной доход, особые волны). Система должна включаться/выключаться без релиза.

**50 уровней:**
- Уровни 1–10 делаю сам как технический эталон (проверяю pipeline: SceneLoader → LevelConfig SO → WaveManager → EndScreen).
- Уровни 11–50 делает GD по готовому pipeline. Я только ревьюю технические PR.

### Месяц 4: SDK-интеграции (в этом порядке, каждый SDK отдельным PR)

**Порядок подключения:**

1. **Firebase** (Auth + Firestore + Remote Config + FCM)
   - Auth: Anonymous sign-in для всех + привязка аккаунта через Google/VK позже.
   - Cloud Save: `UserProgressDocument` — перенос прогресса между устройствами. Структура: userId → runProgress, permanentUnlocks, idleState.
   - Remote Config: все игровые константы (baseRate, synergyMultipliers, waveSpawnIntervals) выносятся в Remote Config. GD меняет баланс без релиза.
   - FCM: push-уведомления — передаю этот модуль в Фазу 3 (пока только инициализирую).
   - **Firebase Analytics НЕ подключаю** — основная аналитика AppMetrica. Подключение Firebase Analytics создаст конфликт с Unity Dev и дублирование данных. AppMetrica даёт всё необходимое бесплатно.

2. **AppMetrica SDK** (последняя стабильная версия на момент интеграции, 2026 — минимум 3.x)
   - Это **единственная и основная аналитика**. Firebase Analytics не подключается — только AppMetrica. Это моё решение, зафиксировано для всей команды.
   - Реализую воронку событий:
     ```
     install → first_open → tutorial_step_{N} → tutorial_complete
     → tower_placed_first → level_1_complete → roguelite_unlocked
     → day_1_return → idle_income_collected_first
     → first_ad_view → first_iap → ad_revenue_event
     → event_started → event_completed → cultural_event_reward_collected
     ```
   - Все события — через единый `AnalyticsService` (wrapper). Никаких прямых вызовов SDK в геймплейном коде.
   - Push-уведомления — через AppMetrica Push (не FCM, не RuStore Push как основной канал). RuStore Push — дополнительный канал для RuStore-пользователей.

3. **RuStore Pay SDK v10.1.1** (мой владелец по game-bible секция 5)
   - ВАЖНО: RuStore BillingClient SDK умирает 1 августа 2026. Использую только Pay SDK v10.1.1 — это не устаревший BillingClient.
   - Интеграция через Unity IAP как абстракцию. RuStore — кастомный `IStore` поверх Unity IAP.
   - Продукты: Starter Pack (one-time), Рунные камни (consumable), Battle Pass (subscription).
   - Тестирование: sandbox-режим RuStore. Проверяю весь flow: выбор товара → RuStore checkout → receipt → сервер валидирует → выдача товара.
   - **Серверная валидация через Firebase Cloud Functions** — пишу сам (мой владелец по game-bible секция 5). Клиент не доверяет сам себе. Cloud Functions также в моей зоне ответственности полностью.

4. **Firebase Cloud Functions — моя зона (game-bible секция 5)**
   - Валидация RuStore receipts: Cloud Function принимает receipt от клиента, обращается к RuStore API, подтверждает транзакцию, выдаёт товар через Firestore.
   - Расчёт серверного idle-времени: Cloud Function проверяет дельту `lastSeenTimestamp`, валидирует начисление ресурсов (защита от читов с системными часами).
   - Бэкап владелец: Unity Dev Middle (если я недоступен).

5. **Yandex Mobile Ads SDK v7**
   - Rewarded Video — единственный формат в MVP. Никаких interstitial.
   - Точки показа рекламы: x2 ресурсы после уровня (добровольно, кнопка видна 3 сек потом исчезает), дополнительная попытка при поражении, ускорение idle.
   - Частота: не чаще 1 раза в 10–15 минут — захардкожено в `AdManager` с timestamp.
   - `AdManager` — singleton с очередью запросов. Preload следующей рекламы сразу после показа предыдущей (fill rate Yandex = 85–95%, но preload снижает latency).

6. **myTracker (VK)** — инициализирую, базовая атрибуция. Нужен для трекинга VK Ads в Фазе 3.

7. **RuStore Push SDK** — push-уведомления (idle накоплен, event стартовал) для RuStore-пользователей.

**К неделе 10 жду от GD:**
- `PushSchedule.md` — сегменты, тексты, триггеры push-уведомлений
- С недели 11 интегрирую в AppMetrica (хендовер по game-bible секция 6, Блок А)

### Месяц 5: Оптимизация + Release pipeline

**Оптимизация под бюджетные устройства:**
- Texture compression: ASTC 6x6 для всех спрайтов. Собираю атласы через Sprite Atlas (не больше 2048×2048 на атлас).
- Shader Warmup: `ShaderVariantCollection` — прогреваю все шейдерные варианты при загрузке. Без этого фризы в первые секунды уровня.
- Object Pooling: EnemyPool, ProjectilePool, VFXPool. Пул с динамическим расширением (не фиксированный размер).
- GC pressure: профилирую Unity Profiler. Цель — 0 allocations в hot path (Update методов WaveManager, TowerAttack, EnemyMovement). Заменяю `List<T>` на `NativeArray` там, где нет необходимости в динамике.
- Memory: `Resources.UnloadUnusedAssets()` после каждого уровня. Addressables — рассматриваю как апгрейд в Фазе 3 (пока слишком сложно для команды).

**Release pipeline (CI/CD):**
- GitHub Actions: push в `main` → автосборка .aab (IL2CPP) → загрузка в Firebase App Distribution → автотест на облачных устройствах (Firebase Test Lab, минимум 5 девайсов: Redmi A2+, Samsung A14, A-серия Xiaomi, Realme C).
- Подписывание: keystore хранится в GitHub Secrets, не в репозитории.
- Versioning: `{major}.{minor}.{build}`. Build — auto-increment в GitHub Actions.

**Итог Фазы 2 — критерии готовности (строго из game-bible секция 4):**
- **D1 Retention = 35%** (не 34%, не диапазон 30–35%)
- **D7 Retention = 12%**
- **IAP conversion = 3%**
- Все три порога достигнуты одновременно. Период измерения — последние 3 дня.
- Мерю через AppMetrica.
- 0 крашей P0 на baseline device
- RuStore оплата проходит в тестовом режиме
- Rewarded Video показывается без зависаний

---

## 3. АРХИТЕКТУРНЫЕ РЕШЕНИЯ

### EventBus (центральная шина событий)

**Почему**: Tower Defense с синергиями — много объектов, которые должны реагировать друг на друга (башня поставлена → пересчитать синергии → обновить UI → запустить аналитику). Прямые ссылки создадут спагетти.

**Решение**: Простой `EventBus<T>` — статический словарь Action<T> по типу события. Минус: нет compile-time гарантий. Это приемлемо для команды 2–3 разработчика.

**Альтернативы, от которых отказался**:
- UniRx/R3 — избыточно, learning curve для Middle Dev
- Signals (Zenject) — нужен DI-контейнер, это overhead для прототипа
- Прямые ссылки — спагетти при >5 взаимодействующих систем

### ScriptableObject-driven Data

**Почему**: Все конфиги (башни, враги, волны, синергии, уровни, ивенты) — ScriptableObjects. GD работает в Unity Editor без программиста. Нет magic numbers в коде. A/B тест баланса через Remote Config поверх SO.

### Service Locator (не полноценный DI)

**Почему**: Zenject — overhead для команды 2–3 человека и 8-месячного проекта. Простой `ServiceLocator` (статический реестр сервисов) достаточен. Регистрирую: AudioService, AdManager, AnalyticsService, SaveService, IAPManager, EventSystem.

**Риск**: глобальное состояние сложнее тестировать. Приемлемо, пока нет unit-тестов (в прототипе их не будет).

### Паттерн State Machine для GameManager

`GameState` enum: MainMenu → Loading → Tutorial → Gameplay → RunComplete → Idle → Shop → Settings → EventLobby.  
Переходы явные, нет `bool isInTutorial && bool isShopOpen && ...`.

### Idle-прогрессия: формула и хранение

```
offlineTime = Mathf.Min(now - lastSeen, 8 * 3600)  // кап 8 часов
resources = baseRate * offlineTime * (1 + upgradeMultiplier)
```

Хранение: Firebase Firestore (поле `lastSeenTimestamp`). Время — серверное (Firebase Server Timestamp), не клиентское. Серверная валидация через Cloud Functions (моя зона).

### Монетизация: архитектура без vendor lock-in

`IPaymentProvider` интерфейс → `RuStorePayProvider`, `UnityIAPProvider`.  
При смене SDK — меняю только реализацию, не точки вызова.

Аналогично: `IAdProvider` → `YandexAdProvider`, `VKAdProvider`, `IronSourceProvider`.  
`AdManager` управляет приоритетами: Yandex primary (85%), VK secondary, IronSource fallback.

---

## 4. SDK И ИНТЕГРАЦИИ — ПОРЯДОК ПОДКЛЮЧЕНИЯ

| # | SDK | Когда | Зачем |
|---|-----|-------|-------|
| 1 | Firebase Auth + Firestore | Месяц 3 | Cloud Save, серверная валидация |
| 2 | Firebase Remote Config | Месяц 3 | Удалённый баланс без релиза |
| 3 | Firebase Cloud Functions | Месяц 4 | Валидация RuStore receipts (мой владелец) |
| 4 | AppMetrica | Месяц 4 | **Единственная** основная аналитика + push |
| 5 | RuStore Pay SDK v10.1.1 | Месяц 4 | Платежи (основной канал монетизации, мой владелец) |
| 6 | Unity IAP (абстракция) | Месяц 4 | Обёртка поверх RuStore Pay |
| 7 | Yandex Mobile Ads SDK v7 | Месяц 4 | Rewarded Video (primary) |
| 8 | myTracker (VK) | Месяц 4 | Атрибуция VK Ads трафика |
| 9 | RuStore Push SDK | Месяц 5 | Push fallback для RuStore-пользователей |
| 10 | Firebase FCM | Месяц 5 | Push fallback (не-RuStore устройства) |
| 11 | IronSource (mediation) | Фаза 3 | Backup fill для рекламы |
| 12 | AppsFlyer | Фаза 3 (при 10K DAU) | Глобальная атрибуция, $99+/мес |

**Что НЕ подключаю:**
- **Firebase Analytics** — не подключаю. AppMetrica покрывает все нужды. Параллельная аналитика создаёт конфликт владельцев и дублирует данные.
- AdMob — заблокирован в РФ с 2022
- RuStore BillingClient (старый) — умирает 01.08.2026, только Pay SDK v10.1.1
- Zenject/Extenject — overhead для команды
- Addressables в MVP — усложняет pipeline, откладываю на Фазу 3

---

## 5. ХЕНДОВЕРЫ (по game-bible секция 6)

### Мои хендоверы — что я отдаю (Блок В: Lead Dev → Unity Dev)

```
Lead Dev → Unity Dev (Middle):
  FirebaseSetup.md (структура DB, auth схема)    → к неделе 3
  Unity Dev начинает Cloud Save интеграцию       → неделя 4

Lead Dev → Unity Dev (Middle):
  IdleOfflineConfig (формулы расчёта офлайн-дохода) → к неделе 6
  Unity Dev реализует IdleManager.cs             → неделя 7
```

### Мои хендоверы — что я получаю (Блоки А и Д)

```
GD → Lead Dev:
  PushSchedule.md (сегменты, тексты, триггеры)   → к неделе 10
  Lead Dev интегрирует в AppMetrica               → неделя 11

GD → Lead Dev:
  EventCalendar.json (ивенты до месяца 12)        → к неделе 8
  Lead Dev начинает EventSystem.cs               → неделя 9

Marketing/UA → Lead Dev:
  PlayableAdsSpec.md (сценарий, ключевые механики) → к неделе 16
  Lead Dev интегрирует Playable Ads в VK Ads      → неделя 17
```

### Дополнительные блоки (моя зона)

```
Lead Dev → Команда:
  ArchitectureDoc.md (паттерны, naming conventions,
  assembly definitions, запрет Firebase Analytics) → неделя 1
  Все разработчики читают до старта кодинга

Lead Dev → QA:
  AppDistributionSetup (автосборки через Firebase
  App Distribution при каждом merge в develop)    → месяц 3

Lead Dev → Marketing:
  AppMetrica Events Spec (события, параметры,
  значения для UA-кампаний)                       → месяц 4
```

---

## 6. ЗАВИСИМОСТИ

### Что мне нужно от команды

| От кого | Что нужно | Когда нужно | Блокирует меня |
|---------|-----------|-------------|----------------|
| Game Designer | TowerConfig-таблицы (урон, радиус, стоимость, фракция) | К концу месяца 2 | Реализацию 5 фракций |
| Game Designer | LevelConfig: маршруты врагов, кол-во волн, состав волн | К началу месяца 3 | Систему уровней |
| Game Designer | SynergyConfig: условия и эффекты синергий | К середине месяца 3 | SynergyDetector |
| Game Designer | EventCalendar.json (ивенты до месяца 12) | К неделе 8 | EventSystem.cs |
| Game Designer | PushSchedule.md (сегменты, тексты, триггеры) | К неделе 10 | Интеграцию push в AppMetrica |
| Game Designer | Структуру Battle Pass (треки, награды) | К месяцу 4 | IAP интеграцию |
| 2D Художник | Спрайты башен (5 фракций × 3 состояния: idle/attack/upgrade) | К концу месяца 2 | Визуальный прототип |
| 2D Художник | UI макеты (HUD, Shop, Run Complete экран) | К месяцу 3 | UI реализацию |
| 2D Художник | Атласы в формате PNG для Sprite Atlas (не PSD!) | Постоянно | Оптимизацию текстур |
| QA | Тест-план на baseline устройствах (Redmi A2+, Samsung A14) | К месяцу 5 | Release pipeline |
| Marketing/UA | PlayableAdsSpec.md | К неделе 16 | Интеграцию Playable Ads в VK Ads |

### Что я даю команде

| Кому | Что | Когда |
|------|-----|-------|
| Вся команда | ArchitectureDoc.md: паттерны, naming, правило "AppMetrica — единственная аналитика" | Неделя 1 |
| Game Designer | Pipeline создания уровней (SceneLoader + SO) — может делать уровни без кода | Конец месяца 2 |
| Game Designer | Remote Config доступ для баланса в реальном времени | Месяц 3 |
| Game Designer | AppMetrica дашборд: воронка, retention, монетизация | Месяц 4 |
| 2D Художник | Sprite Atlas workflow + требования к формату спрайтов | Неделя 1 |
| Unity Dev (Middle) | FirebaseSetup.md (структура DB, auth схема) | Неделя 3 |
| Unity Dev (Middle) | IdleOfflineConfig (формулы расчёта офлайн-дохода) | Неделя 6 |
| Unity Dev (Middle) | Code review всех PR в gameplay/ и meta/ | Постоянно |
| QA | Автосборки через Firebase App Distribution (каждый merge в develop) | Месяц 3 |
| Marketing | AppMetrica события и их значения для настройки UA-кампаний | Месяц 4 |

---

## 7. ТЕХНИЧЕСКИЕ РИСКИ В МОЕЙ ЗОНЕ

| Риск | Вероятность | Влияние | Моя митигация |
|------|-------------|---------|---------------|
| RuStore Pay SDK v10.1.1 нестабилен в sandbox | Средняя | Критическое (нет оплаты = нет монетизации) | Раннее тестирование в месяце 4, держу контакт с RuStore Developer Support |
| Cloud Functions: задержка валидации >3 сек | Средняя | Высокое (UX оплаты) | Оптимистичный UI на клиенте, rollback при ошибке Functions |
| Firebase заблокирован РКН | Низкая | Высокое | Yandex Cloud (DataSphere + Object Storage) как backup. БД мигрируется за 1 день через export/import. Remote Config заменяется на собственный endpoint. |
| IL2CPP build time > 30 мин (блокирует итерации) | Высокая | Среднее | В develop-ветке собираем Mono для скорости, IL2CPP только в main. CI/CD кеширует IL2CPP incremental build. |
| GC spikes на бюджетных устройствах (фризы 100+ мс) | Высокая | Высокое | Unity Profiler + Memory Profiler каждые 2 недели. Pool все объекты. Цель: 0 allocations/frame в Update hot path. |
| Crash при старте (shader compilation) | Средняя | Критическое (удаление игры в первые 30 сек) | ShaderVariantCollection + Shader Warmup loading screen. Тест на 5 устройствах через Firebase Test Lab. |
| Idle timestamp manipulation (читы) | Средняя | Среднее (нарушает экономику) | Серверный timestamp Firebase, кап офлайна 8 часов, Cloud Functions валидирует дельту |
| Задержка апрува в RuStore | Средняя | Среднее (срываем Soft Launch) | Подаю первый build в RuStore в конце месяца 4 (не 5). Первый сабмит всегда медленнее. |
| Unity IAP + RuStore Pay конфликт версий | Средняя | Высокое | Отдельный тестовый проект для SDK compatibility check перед интеграцией в основной проект. |
| Target API 35 — поведенческие изменения Android | Низкая | Среднее | Тест на Android 14 эмуляторе + реальном устройстве (Samsung A14 с Android 14). |
| EventCalendar.json не пришёл от GD к неделе 8 | Средняя | Среднее | EventSystem.cs разрабатываю с stub-данными, подключаю реальный конфиг по факту прихода |

---

## ИТОГ: ПЕРВЫЕ 10 ДЕЙСТВИЙ (Неделя 1)

1. Создать репозиторий, настроить Git LFS, `.gitignore`, ветки
2. Установить Unity 2022.3 LTS, настроить IL2CPP + Target API 34/35
3. Купить ассеты ($84) и интегрировать Tower Defense Starter Kit
4. Создать базовую структуру папок и Assembly Definitions
5. Написать ArchitectureDoc.md для команды (паттерны, конвенции, **правило: только AppMetrica, без Firebase Analytics**)
6. Поставить задачу художнику: требования к спрайтам + Sprite Atlas workflow
7. Поставить задачу GD: шаблон TowerConfig SO (таблица с полями)
8. Реализовать EventBus + ServiceLocator + GameStateMachine (скелет)
9. Настроить GitHub Actions: автосборка APK (Mono) на push в develop
10. Заказать тестовое устройство Xiaomi Redmi A2+ (если нет в команде)
