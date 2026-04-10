# FirebaseSetup — Хендовер для Unity Dev Middle

**Дата**: Апрель 2026  
**Статус**: Передаётся к неделе 3. Unity Dev начинает Cloud Save интеграцию на неделе 4.  
**Источник**: Lead Dev / CTO → Unity Dev Middle  
**Контракт**: Unity Dev не начинает работу до получения этого документа (Блок В, game-bible §6).

---

## КРИТИЧЕСКИ ВАЖНО — читать в первую очередь

Firebase Analytics **НЕ инициализируется** и **НЕ подключается** ни при каких условиях.

В проекте используется **AppMetrica** как единственная аналитика.

Из Firebase SDK инициализировать только:
1. **FirebaseAuth** — анонимная авторизация
2. **FirebaseFirestore** — облачное сохранение прогресса
3. **FirebaseRemoteConfig** — горячее обновление баланса
4. **FirebaseMessaging (FCM)** — инициализация без активации (подключить, но не запрашивать разрешение — это делает GD через PushSchedule.md на неделе 11)

```csharp
// ПРАВИЛЬНО: инициализация без Analytics
await FirebaseApp.CheckAndFixDependenciesAsync();
// После: только Auth, Firestore, RemoteConfig, Messaging
// НЕ вызывать: FirebaseAnalytics.SetAnalyticsCollectionEnabled(true)
```

---

## SDK версии

| SDK | Версия | Совместимость |
|---|---|---|
| Firebase Unity SDK | **11.x** (11.6.0+) | Unity 2022.3 LTS — проверено |
| Unity версия проекта | **2022.3.45f1** | LTS, не обновлять без Lead Dev |

Скачивать с: `firebase.google.com/docs/unity/setup` → FirebaseUnity SDK 11.x.  
**Не устанавливать через Package Manager** — только manual import .unitypackage.

---

## Структура Firestore

### Схема документов

```
users/
  {userId}/                              ← userId = Firebase Auth UID
    profile: {
      displayName:       string          ← "Богатырь" по умолчанию
      createdAt:         Timestamp
      lastSeenTimestamp: Timestamp       ← обновляется при каждом запуске
    }
    runProgress: {
      currentAct:        number          ← 1, 2 или 3
      currentLevel:      number          ← 1–70 (71–100 = Endless Mode)
      deckConfig:        Array<string>   ← массив towerDataId (до 6 элементов)
    }
    permanentUnlocks: {
      towers:            Array<string>   ← разблокированные towerDataId
      perks:             Array<string>   ← перманентные перки из roguelite-слоя
      battlePassTier:    number          ← 0 = не куплен
    }
    idleState: {
      lastSeenTimestamp: Timestamp       ← момент выхода из приложения
      baseRate:          number          ← монет/сек (конфиг из IdleOfflineConfig.json, неделя 6)
      multiplier:        number          ← текущий множитель (Battle Pass и т.д.)
    }
```

### Правила индексации

Составные индексы не нужны в MVP — все запросы к одному документу пользователя.  
TODO: при добавлении лидербордов или событий — добавить индексы через Firebase Console.

---

## Схема авторизации

### Фаза 1 (MVP — реализовать сейчас)

**Anonymous sign-in** — автоматически при первом запуске, без UI.

```csharp
// Инициализация при старте приложения
var auth = FirebaseAuth.DefaultInstance;
if (auth.CurrentUser == null)
{
    await auth.SignInAnonymouslyAsync();
}
var userId = auth.CurrentUser.UserId;
// Использовать userId как ключ для users/{userId}/
```

- Прогресс сохраняется к анонимному аккаунту
- Пользователь не видит никакого UI авторизации
- При удалении приложения — прогресс теряется (это ок для MVP)

### Фаза 2 (НЕ реализовывать сейчас)

Привязка Google Account / VK ID к анонимному аккаунту.  
Реализуется в Фазе 2 по отдельному хендоверу. До этого — заглушка UI с надписью "В разработке".

---

## Правила безопасности Firestore

Вставить в Firebase Console → Firestore → Rules:

```javascript
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {

    // Пользователь читает и пишет только свой документ
    match /users/{userId}/{document=**} {
      allow read, write: if request.auth != null
                         && request.auth.uid == userId;
    }

    // Всё остальное — запрещено
    match /{document=**} {
      allow read, write: if false;
    }
  }
}
```

**Важно**: правила применяются немедленно. Тестировать через Firebase Console → Rules Playground.

---

## Remote Config — структура ключей

Remote Config используется для горячего обновления баланса без апдейта приложения.

| Ключ | Тип | Дефолт | Описание |
|---|---|---|---|
| `idle_base_rate` | float | 1.0 | Монет/сек в idle (до получения IdleOfflineConfig.json) |
| `idle_max_offline_cap_hours` | int | 8 | Максимальный офлайн-кап в часах (из game-bible) |
| `rewarded_video_cooldown_minutes` | int | 10 | Минимальный интервал между предложениями rewarded |
| `first_iap_offer_day` | int | 3 | День первого IAP-предложения (из game-bible) |
| `wave_count_act1` | int | 17 | Количество волн в Акте 1 |

```csharp
// Пример получения значения
var remoteConfig = FirebaseRemoteConfig.DefaultInstance;
await remoteConfig.FetchAndActivateAsync();
float idleRate = (float)remoteConfig.GetValue("idle_base_rate").DoubleValue;
```

**Правило**: дефолты Remote Config должны совпадать с константами в коде. Изменять значения в Remote Config без согласования с GD запрещено.

---

## FCM (Push Notifications) — инициализация без активации

```csharp
// Инициализировать Messaging, но НЕ запрашивать разрешение на уведомления
// Разрешение запрашивается только по логике из PushSchedule.md (неделя 11)
var messaging = FirebaseMessaging.DefaultInstance;
messaging.TokenReceived += OnTokenReceived;
// НЕ вызывать: NotificationManager.RequestPermission()
// TODO: Lead Dev передаст логику запроса разрешения на неделе 11
```

---

## Офлайн-кеш и конфликты

- Firestore SDK имеет встроенный offline cache — работает автоматически
- При конфликте (редкий случай) — server wins (серверная версия приоритетнее)
- TODO: при реализации ISaveService учесть `FirestoreSettings.PersistenceEnabled = true`

---

## Checklist для Unity Dev Middle (неделя 4)

- [ ] Установить Firebase Unity SDK 11.x (manual import)
- [ ] Добавить google-services.json в Assets/ (получить от Lead Dev)
- [ ] Реализовать `FirebaseSaveService : ISaveService`
- [ ] Anonymous sign-in при старте Bootstrap-сцены
- [ ] Сохранение `idleState.lastSeenTimestamp` при уходе в фон (OnApplicationPause)
- [ ] Загрузка `runProgress` при старте нового руна
- [ ] Проверить правила Firestore через Firebase Console → Rules Playground
- [ ] **НЕ инициализировать Firebase Analytics**

---

*Lead Dev / CTO | Апрель 2026 | Хендовер Блок В*
