# AssetsSpec — Ассеты для покупки на Unity Asset Store

**Дата**: Апрель 2026  
**Статус**: Утверждено Lead Dev. Покупку выполняет Lead Dev, импорт — Unity Dev Middle.  
**Итог**: ~$84 экономит 200+ часов разработки

---

## Итоговый бюджет

| Пакет | Цена | Экономия часов |
|---|---|---|
| Tower Defense Starter Kit | $50 | ~120 ч |
| Solo Tower 2D Template | $19 | ~50 ч |
| SFX Pack 100+ | $15 | ~40 ч |
| **Итого** | **$84** | **~210 ч** |

---

## 1. Tower Defense Starter Kit — $50

**Unity Asset Store**: поиск "Tower Defense Starter Kit" автор Gigi Labs / аналоги с рейтингом 4.5+

### Что берём

| Компонент | Используем | Комментарий |
|---|---|---|
| Grid placement system | Да | Основа TowerController.cs |
| Pathfinding (A*) | Да | Враги идут по пути к воротам |
| Wave spawner (базовый) | Да | Переписываем под WaveData SO |
| Tower targeting logic | Да | Ближайший / самый толстый / первый |
| HP bar компонент | Да | Для башен и врагов |
| Basic tower prefabs | Нет | Заменяем на авторский арт (ASTC 6x6) |
| Basic enemy prefabs | Нет | Заменяем на авторский арт |
| Demo сцены | Нет | Удалить после изучения |
| Built-in analytics | Нет | Удалить, вызов AppMetrica через IAnalyticsService |

### Что переделать после импорта

1. Заменить все `Instantiate()`/`Destroy()` на вызовы `EnemyPool.Get()`/`EnemyPool.Return()`
2. Убрать все прямые ссылки на UI — заменить на EventBus-события
3. Переписать конфигурацию волн: вместо хардкода → `WaveData` ScriptableObject
4. Проверить совместимость с ASTC 6x6 (если пакет использует RGBA32 — перенастроить Texture Importer)

---

## 2. Solo Tower 2D Template — $19

**Unity Asset Store**: поиск "Solo Tower 2D" / "2D Tower Defense Template" автор с рейтингом 4.0+

### Что берём

| Компонент | Используем | Комментарий |
|---|---|---|
| Idle accumulation logic | Да | Основа IdleManager.cs |
| Offline time calculation | Да | Формула офлайн-дохода (заглушка до IdleOfflineConfig.json) |
| Resource display HUD | Да | Адаптировать под стиль игры |
| Upgrade panel UI | Да | Адаптировать |
| Save/Load (PlayerPrefs) | Частично | Используем как офлайн-кеш; основное сохранение — Firebase |
| IAP integration | Нет | Только RuStore Pay SDK v10.1.1 |
| AdMob integration | Нет | Только IronSource |
| Social features | Нет | MVP без соцсетей |

### Что переделать после импорта

1. Заменить `PlayerPrefs`-сохранение на вызов `ISaveService` (Firebase)
2. Оставить `PlayerPrefs` только как офлайн-кеш при отсутствии сети
3. Привязать накопление к параметрам из `IdleOfflineConfig.json` (получить от Lead Dev, неделя 6)
4. До получения конфига: `ratePerSecond = 1`, `maxOfflineCap = 8h` (из game-bible)

---

## 3. SFX Pack 100+ — $15

**Unity Asset Store**: поиск "SFX Pack Fantasy 100", "Casual Game SFX" / аналог с 100+ звуков, рейтинг 4.0+

### Что берём

| Звук | Использование |
|---|---|
| Sword hit 1–3 | Башня Ратник, Витязь (удары) |
| Shield bash | Башня Витязь (удар щитом) |
| Rock impact | Башня Святогор (бросок валуна) |
| Magic charge, magic release | Башни Ведун, Чародей, Мерлин Русич |
| Arrow shoot, arrow hit | Башни Стрелец, Соколиный глаз, Добрыня-стрелок |
| Fire hit, fire loop | Башни Рьяный, Буян, Еруслан |
| Heal, positive chime | Башни Травник, Знахарка, Белояр |
| Coin pickup | Сбор idle-ресурсов |
| Level complete, level fail | Конец волны |
| UI click, UI confirm, UI deny | Интерфейс |

### Что не берём

| Звук | Причина |
|---|---|
| Sci-fi, modern combat SFX | Не в теме игры |
| Voice acting | Нет VO в MVP |
| Music tracks | Заказывается отдельно у композитора |
| Ambient loops (длинные) | Слишком большой размер, заказывается отдельно |

### Требования к импорту

- Формат: **OGG Vorbis**, качество 70% для SFX, 60% для музыки
- Частота: 44100 Hz
- Переименовать по схеме: `sfx_{башня}_{действие}_{номер}.ogg`  
  Пример: `sfx_ratnik_hit_01.ogg`, `sfx_vedun_cast_01.ogg`
- Организовать в `Assets/_Audio/SFX/Towers/`, `Assets/_Audio/SFX/UI/`, `Assets/_Audio/SFX/Enemies/`

---

## Протокол импорта (для Unity Dev Middle)

1. Lead Dev покупает все 3 пакета и расшаривает через Unity Organization
2. Unity Dev импортирует в отдельную ветку `feature/asset-integration`
3. Удалить из пакетов: Demo-сцены, Sample-скрипты, README-файлы (снижают размер проекта)
4. Провести Import Settings Review: все текстуры → ASTC 6x6, все аудио → OGG 70%
5. Запустить на Baseline устройстве, проверить FPS (не ниже 30), RAM (не выше 300 MB)
6. После ревью Lead Dev → merge в develop

---

## Что НЕ покупаем и почему

| Ассет | Причина |
|---|---|
| Complete Tower Defense Engine ($100+) | Избыточен, сложнее адаптировать |
| Spine-анимации врагов | Дорого в MVP, используем покадровую анимацию |
| Готовые UI Kit под фэнтези | Обычно не под русскую тематику, заказываем у художника |
| Procedural Level Generator | Уровни фиксированные (game-bible §2: 70 уровней + Endless) |

---

*Lead Dev / CTO | Апрель 2026*
