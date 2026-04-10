# Unity Dev Plan — Богатырская Застава
**Роль**: Unity Developer (Middle) | Зона ответственности: геймплейные системы + монетизация  
**Дата**: Апрель 2026 | **Engine**: Unity 2022 LTS + C# / IL2CPP

---

## 1. ФАЗА 1 — ПРОТОТИП (Месяц 1–2)

**Цель**: рабочий core loop, проверяемый на 20 тестерах. Никакого контента, только механики.

### Неделя 1–2: Сборка из ассетов + базовая TD
- Интегрирую **Tower Defense Starter Kit ($50)** — берём готовые системы: PathFinding, WaveManager, TowerBase, EnemyBase
- Интегрирую **Solo Tower 2D Template ($19)** — 2D-проекция, раскладка слотов
- Создаю `GameConfig.cs` — ScriptableObject для всех числовых параметров (HP врагов, урон башен, стоимость размещения). Это обязательно с нуля, чтобы GD мог балансировать без кода
- Реализую `TowerSlot.cs` — размещение башни на поле (drag & drop или tap-to-place)
- `EnemyWaveController.cs` — запускает волны по таймеру, читает конфиг из ScriptableObject

**Критерий готовности**: можно поставить башню, волна пошла, враги доходят или умирают.

### Неделя 3–4: Базовый Idle + сохранение
- `IdleManager.cs` — оффлайн-прогрессия. Схема: фиксирую `DateTime.UtcNow` при выходе (`Application.quitting`), при старте считаю дельту, начисляю ресурсы с формулой `min(deltaSeconds * ratePerSecond, maxOfflineCap)`. Cap = 8 часов по умолчанию (согласно Core Loop из game-bible.md). **ЗАГЛУШКА — хардкодные дефолты (ratePerSecond=1, maxOfflineCap=8h). Реальный конфиг (IdleOfflineConfig.json) приходит от Lead Dev к неделе 6. На неделе 7 заглушка заменяется конфигом.**
- `ResourceManager.cs` — синглтон, хранит Gold + RuneStones, нотифицирует UI через события (`UnityEvent` или `Action<int>`)
- `SaveSystem.cs` — сериализация в `PlayerPrefs` (JSON через JsonUtility). На прототипе достаточно, Firebase — в Фазе 2
- `UIController.cs` — минимальный HUD: Gold, волна X/Y, кнопка "Разместить башню"

**Критерий готовности**: вышел из игры на 30 минут, вернулся — увидел накопленное золото.

### Что НЕ делаю в Фазе 1:
- Не делаю синергии (это Фаза 2)
- Не подключаю SDK монетизации (только моки)
- Не делаю анимации — жду арт-команду

---

## 2. ФАЗА 2 — MVP (Месяц 3–5)

**Цель**: D1 ≥ 35%, D7 ≥ 12%. Все ключевые системы, монетизация, аналитика.

### Месяц 3: Синергии + Roguelite база

- `SynergySystem.cs` — ключевая фишка проекта
  - Хранит `Dictionary<FactionType, List<TowerBase>>` — текущие башни на поле
  - Метод `CheckSynergies()` вызывается при каждом размещении/уничтожении башни
  - Применяет эффекты через `TowerBase.ApplySynergyBonus(SynergyEffect effect)`
  - 5 фракций: Дружина, Волхвы, Лучники, Берсерки, Знахари (канонические ID из game-bible.md: DR-x, VL-x, LU-x, BE-x, ZN-x)
  - Начинаю после получения `SynergyConfig.json` от GD (неделя 6 → я начинаю неделя 7, хендовер Блок А)

- `RunManager.cs` — управление "походом" (рун)
  - Старт рана: генерирует пул предложений башен (из доступных unlocked-башен с весами)
  - В процессе рана: предлагает игроку выбрать башни из пула между волнами или при старте рана (логика показа — согласовывается с GD)
  - Конец рана: собирает метрики (волны, урон, время), начисляет постоянный прогресс (RuneStones)
  - `PermanentUpgradeData` — ScriptableObject с перманентными бонусами между ранами

- `DeckBuilder.cs` — **НЕ преселект 6 башен до рана**. Согласно Core Loop (game-bible.md §7): башни выдаются и выбираются **в процессе рана**. `DeckBuilder.cs` — это UI-менеджер выбора башен во время паузы между волнами (10–15 сек). Логика:
  - `TowerOfferPool` — пул доступных башен для текущего рана, формируется `RunManager`-ом при старте
  - Между волнами `DeckBuilder` показывает 2–3 башни на выбор из пула (как дальнейшая расстановка)
  - Игрок выбирает, башня добавляется в `ActiveTowerSlots` на поле
  - Max слотов на поле: определяется конфигом уровня (GD задаёт в `LevelConfig`)
  - Предлагаемый набор каждый раз случайный, но взвешенный (редкие башни реже)

**Тест синергий**: юнит-тест в Edit Mode — создаю 3 башни-Берсерка, проверяю что `GetActiveSynergies()` возвращает `FactionBonus(Berserkers, 0.30f)`.

### Месяц 4: Монетизация + SDK
- **RuStore Pay SDK v10.1.1** — приоритет №1
  - `RuStoreIAPManager.cs` — обёртка над SDK: `PurchaseProduct(productId)`, callback на успех/ошибку/отмену
  - Продукты: `starter_pack_69`, `rune_stones_79`, `rune_stones_499`, `battle_pass_399`
  - Валидация чека на сервере (через Firebase Cloud Function — реализует Lead Dev, я жду endpoint)
- **Unity IAP** — резервный путь (Google Play / App Store)
  - `UnifiedIAPManager.cs` — фасад, который по платформе выбирает RuStore или Unity IAP
- **Yandex Mobile Ads SDK v7** — Rewarded Video
  - `YandexAdsManager.cs` — загрузка rewarded-ролика при старте уровня, callback `OnRewardGranted`
  - `RewardedAdOffer.cs` — логика показа оффера: не чаще раз в 10–15 минут, только добровольно
  - Триггеры показа: после победы ("x2 ресурсы"), при поражении ("ещё одна попытка"), в idle ("ускорить прогрессию")
- `MonetizationConfig.cs` (ScriptableObject) — задержка первого показа монетизации (по умолчанию: день 3, то есть проверяю `PlayerPrefs["firstLaunchDate"]` + TimeSpan 72 часа)

### Месяц 5: Аналитика + оптимизация + туториал + пуши

- **AppMetrica SDK** — единственная аналитика. Firebase Analytics не дублирую (владелец AppMetrica-событий — Lead Dev, бэкап я, согласно game-bible.md §5)
  - `AnalyticsManager.cs` — синглтон, методы: `TrackTutorialStep(int step)`, `TrackLevelComplete(int level, float time)`, `TrackIAPAttempt(string productId)`, `TrackIAPSuccess(string productId)`, `TrackAdWatched(string placement)`, `TrackIdleIncome(int amount)`
  - Воронка: `tutorial_complete → tower_placed → level_1_complete → first_ad_view → first_iap`
  - Все события — только в AppMetrica. Firebase Analytics в этом проекте не использую

- **Туториал** (владелец реализации — я, game-bible.md §5; дизайн флоу — GD)
  - Получаю `TutorialFlow.md` от GD к неделе 4 (хендовер Блок А) → начинаю `Tutorial.cs` с недели 5
  - `Tutorial.cs` — state machine: `TutorialState` enum (PlaceTower, WatchWave, SynergyHint, Complete)
  - Реализую highlight-оверлей, стрелки-указатели, блокировку ввода вне туториального экшена
  - Skip-условие: берётся из `TutorialFlow.md` (GD определяет)
  - После `TutorialComplete` → сбрасываю флаг в `SaveSystem`, трекаю `TrackTutorialStep(COMPLETE)`

- **Push-уведомления** (логика показа — я, стратегия сегментов — GD + Lead Dev интегрирует в AppMetrica, game-bible.md §5)
  - `PushNotificationManager.cs` — логика показа: проверяю условия перед запросом разрешения, определяю триггеры
  - Триггеры показа пушей (реализую в коде):
    - Idle: ресурсы накоплены на 80% от cap → пуш "Богатыри ждут тебя"
    - Неактивность: не заходил 24 часа → пуш (текст от GD из `PushSchedule.md`)
    - После победы: следующий уровень доступен → пуш на следующий день
  - Расписание и тексты получаю из `PushSchedule.md` от GD (к неделе 10; Lead Dev интегрирует в AppMetrica с недели 11, хендовер Блок А)
  - Запрос разрешения на пуши: только после туториала + не ранее дня 2

- `PerformanceManager.cs` — динамический FPS cap
  - Критерий: Helio G81 и слабее → Low preset (30 FPS). Определяю через `SystemInfo.processorType` + `SystemInfo.systemMemorySize`. Граница согласована с Lead Dev
  - `Application.targetFrameRate = targetFPS`
  - Shader Warmup на экране загрузки (до первого уровня)
- Object Pool для врагов и снарядов: `ObjectPool<Enemy>`, `ObjectPool<Projectile>` — ноль `new()` в hot path
- Тест на Xiaomi Redmi A2+ (Helio G81, 3GB RAM): 10 минут игры без перегрева

---

## 3. СИСТЕМЫ — ПОЛНЫЙ СПИСОК

| Система | Класс | Описание | Фаза |
|---------|-------|----------|------|
| Конфиг параметров | `GameConfig.cs` (ScriptableObject) | Все балансные числа, без хардкода | 1 |
| Размещение башен | `TowerSlot.cs`, `TowerBase.cs` | Постановка, апгрейд, продажа | 1 |
| Волны врагов | `EnemyWaveController.cs`, `EnemyBase.cs` | Спавн, патфайндинг, HP | 1 |
| Idle-прогрессия | `IdleManager.cs` | Оффлайн ресурсы, cap 8 часов | 1 |
| Ресурсы | `ResourceManager.cs` | Gold + RuneStones, события | 1 |
| Сохранения | `SaveSystem.cs` | JSON в PlayerPrefs → Firebase Cloud Save в Ф2 | 1→2 |
| Синергии | `SynergySystem.cs` | 5 фракций, проверка комбо, применение бонусов | 2 |
| Roguelite / управление раном | `RunManager.cs` | Управление ранoм, пул башен, перманентный прогресс | 2 |
| Выбор башен в ране | `DeckBuilder.cs` | UI выбора башен между волнами (не преселект до рана) | 2 |
| IAP (RuStore) | `RuStoreIAPManager.cs` | Покупки, валидация | 2 |
| IAP (Unity IAP) | `UnifiedIAPManager.cs` | Фасад, мультиплатформа | 2 |
| Rewarded Ads | `YandexAdsManager.cs`, `RewardedAdOffer.cs` | Yandex Ads, добровольный показ | 2 |
| Монетизация-конфиг | `MonetizationConfig.cs` (ScriptableObject) | Задержка показов, флаги A/B | 2 |
| Аналитика | `AnalyticsManager.cs` | Только AppMetrica, вся воронка | 2 |
| Туториал | `Tutorial.cs` | State machine, highlight, skip-условие от GD | 2 |
| Push-уведомления | `PushNotificationManager.cs` | Логика триггеров показа пушей | 2 |
| Производительность | `PerformanceManager.cs` | FPS cap (Helio G81 = Low), shader warmup, object pool | 2 |
| Battle Pass | `BattlePassManager.cs` | Трек наград, прогресс, получение XP | 3 |
| Попапы монетизации | `MonetizationPopupController.cs` | Starter Pack, Limited Offers с таймером | 3 |

---

## 4. UNITY ASSET STORE — ЧТО БЕРУ И КАК ИСПОЛЬЗУЮ

| Asset | Цена | Что конкретно использую | Что переписываю |
|-------|------|------------------------|-----------------|
| **Tower Defense Starter Kit** | $50 | `WaveManager`, `PathFinding`, базовые классы `TowerBase`/`EnemyBase` | Добавляю фракции, синергии, интеграцию с `GameConfig.cs` |
| **Solo Tower 2D Template** | $19 | 2D-раскладка слотов, camera setup | Меняю UI под наш дизайн |
| **2D Enemy Sprite Pack** | $20 | Placeholder-спрайты для прототипа | Заменяю на финальный арт в Фазе 2–3 |
| **UI Icon Pack** | $10 | Кнопки, иконки, HUD-элементы | Заменяю на branded UI |
| **SFX Pack (100+ звуков)** | $15 | Выстрелы, взрывы, UI-клики | Заменяю финальный саундтрек аутсорс-звучачем |

**Итого**: $114. Экономит ~200–300 часов. Главная ценность — Tower Defense Starter Kit даёт работающий pathfinding и wave spawning, которые можно адаптировать за 2–3 дня вместо 2–3 недель.

**Риск**: код ассета может быть legacy (Unity 2019–2020 style). Потребую у Lead Dev проверить совместимость с Unity 2022 LTS перед покупкой.

---

## 5. ЗАВИСИМОСТИ

### Хендоверы из game-bible.md (§6) — строго по контракту

| Откуда | Кому | Что | Срок | Моё действие |
|--------|------|-----|------|-------------|
| GD → я | Unity Dev Middle | `TowerConfig.json` (15 башен, stats v1.0) | к неделе 3 | Начинаю Tower Placement System с недели 4 |
| GD → я | Unity Dev Middle | `SynergyConfig.json` (12 синергий) | к неделе 6 | Начинаю `SynergySystem.cs` с недели 7 |
| GD → я | Unity Dev Middle | `TutorialFlow.md` (флоу + skip-условия) | к неделе 4 | Начинаю `Tutorial.cs` с недели 5 |
| Lead Dev → я | Unity Dev Middle | `FirebaseSetup.md` (структура DB, auth) | к неделе 3 | Начинаю Cloud Save интеграцию с недели 4 |
| Lead Dev → я | Unity Dev Middle | `IdleOfflineConfig` (формулы расчёта) | к неделе 6 | Реализую `IdleManager.cs` с недели 7 |
| Я → QA | QA | Build для smoke-теста (Core Loop рабочий) | к неделе 5 | **Unity Dev не собирает APK самостоятельно. Пушу код в main → CI Lead Dev (GitHub Actions, готов с недели 1–2) собирает APK → Firebase App Distribution раздаёт QA автоматически.** QA начинает регрессию с недели 5 |
| Я → QA | QA | Build для MVP (полный) | к неделе 14 | QA финальная проверка устройств неделя 14–15 |

### Дополнительные зависимости от GD
- **Неделя 1**: схема слотов на карте (количество, расположение) → не могу делать TowerSlot без этого
- **Неделя 2**: параметры первых 3 башен (урон, скорость, стоимость) → заполню в GameConfig.cs
- **Месяц 2**: механика 10 тестовых уровней (путь врагов, количество волн, типы врагов)
- **Месяц 3**: полная таблица синергий (12 комбо) → реализую через ScriptableObject, данные от GD
- **Месяц 3–4**: схема IAP-пакетов (что входит в Starter Pack, что в каждом bundle)
- **Месяц 4**: механика Battle Pass (треки, награды, XP за действия)

### От арт-команды (2D Художник)
- **Месяц 1**: не жду ничего — работаю на placeholder-спрайтах из ассет-стора
- **Месяц 2**: 5 спрайтов башен (по одной на фракцию) для первых тестов
- **Месяц 3**: UI-кит (кнопки, панели, иконки фракций)
- **Месяц 4**: анимации башен и врагов (idle, attack, death) — ключи анимаций согласовываем заранее

### От Lead Dev
- **День 1**: архитектурное решение по сохранениям → делаю интерфейс `ISaveSystem`
- **Неделя 1**: настройка Unity проекта (IL2CPP, target API 34–35, .aab, ASTC)
- **Месяц 2**: доступ к RuStore Developer Console + тестовый аккаунт
- **Месяц 2**: Firebase проект (`google-services.json`)
- **Месяц 3**: backend endpoint для валидации чеков RuStore (Cloud Function — его зона, game-bible.md §5)
- **Месяц 4**: AppMetrica API Key + Yandex Ads Unit IDs

---

## 6. ТЕХНИЧЕСКИЕ РИСКИ В МОЕЙ ЗОНЕ

### Риск 1: Idle оффлайн-прогрессия
**Проблема**: игрок меняет системное время → бесконечные ресурсы.  
**Митигация**:
- Сравниваю `DateTime.UtcNow` c timestamp с сервера при первом онлайн-запросе
- Если delta > 24 часов — cap всё равно 8 часов
- Если device time < last_saved_time — игнорирую

### Риск 2: Система синергий — комбинаторный взрыв
**Проблема**: O(n²) проверок при каждом изменении поля.  
**Митигация**:
- Синергии проверяются только при событии (размещение/удаление башни), не каждый frame
- Архитектура через `Dictionary<FactionType, int>` (счётчик башен по фракции) → O(1) lookup
- 12 канонических синергий из game-bible.md §3 — масштаб контролируем

### Риск 3: DeckBuilder — выбор башен в процессе рана
**Проблема**: пауза 10–15 сек между волнами мала для вдумчивого выбора → стресс игрока.  
**Митигация**:
- UI показывает максимум 3 варианта (не все доступные башни)
- Таймер отображается явно, но не давит: по истечении времени башня выбирается автоматически (случайная из показанных)
- Точное время и UX-логику согласовываю с GD до начала реализации

### Риск 4: Монетизационные попапы — UX и timing
**Проблема**: агрессивные попапы в первые 72 часа → игрок уходит.  
**Митигация**:
- `MonetizationConfig.cs` хранит `firstShowAfterHours = 72`
- Все показы логирую в `AnalyticsManager` → смотрю конверсию в AppMetrica
- Starter Pack показывается ровно 1 раз (флаг в SaveSystem)
- Limited Offer — только после туториала + день 3+

### Риск 5: Производительность на Xiaomi Redmi A2+
**Проблема**: Helio G81, 3GB RAM — перегрев и фризы.  
**Митигация**:
- Object Pool для всех врагов и снарядов с самого начала
- Helio G81 и слабее → 30 FPS (Low preset), критерий согласован с Lead Dev
- Shader Warmup до первого уровня
- GC-friendly: без `LINQ`, `string concatenation`, `new()` внутри Update/FixedUpdate
- Тестирую на реальном устройстве каждую неделю

### Риск 6: RuStore Pay SDK совместимость
**Проблема**: RuStore BillingClient умирает 1 августа 2026.  
**Митигация**:
- С первого дня только Pay SDK v10.1.1
- Слежу за changelog developer.rustore.ru/news

### Риск 7: Отсутствие валидации IAP на сервере
**Проблема**: без серверной валидации — chargeback-атаки.  
**Митигация**:
- Блокирую выдачу контента до OK от сервера
- Cloud Function — зона Lead Dev (game-bible.md §5), фиксирую как hard dependency Месяца 3

---

## ПОРЯДОК РЕАЛИЗАЦИИ (сводная таблица)

| Неделя | Что делаю | Результат |
|--------|-----------|-----------|
| 1 | Интеграция ассетов + TowerSlot + EnemyWaveController | Башни стоят, враги ходят |
| 2 | GameConfig.cs + ResourceManager + базовый HUD | Балансные числа в ScriptableObject |
| 3 | IdleManager + SaveSystem (PlayerPrefs) | Оффлайн-прогрессия работает |
| 4 | Полировка core loop + тест на 20 пользователях | Проверен fun-фактор |
| 5 | Tutorial.cs (получил TutorialFlow.md от GD на неделе 4) | Туториал проходим |
| 5–6 | SynergySystem + первые комбо (ожидаю SynergyConfig.json к неделе 6) | Синергии готовы к данным GD |
| 7–8 | RunManager + DeckBuilder (выбор башен между волнами) | Roguelite режим запускается |
| 9–10 | RuStoreIAPManager + UnifiedIAPManager | Тестовые покупки проходят |
| 11 | YandexAdsManager + RewardedAdOffer | Rewarded video добровольно |
| 12 | AnalyticsManager (AppMetrica only) + PerformanceManager | Воронка в AppMetrica, Low preset на Helio G81 |
| 12–13 | PushNotificationManager (получаю PushSchedule.md от GD к неделе 10) | Пуш-триггеры работают |
| 13–14 | MonetizationConfig + попапы с таймером | Starter Pack, Limited Offer работают |
| 15–16 | BattlePassManager | Battle Pass трек работает |
| 17–20 | Балансировка, QA, bug fixing, оптимизация | Готово к Soft Launch |
