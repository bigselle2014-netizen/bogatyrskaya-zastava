# TechnicalLimits — «Богатырская Застава»

**Дата**: Апрель 2026  
**Статус**: Обязателен к исполнению. Нарушение лимитов = баг P0.  
**Ответственный**: Lead Dev / CTO

---

## Baseline устройство

**Xiaomi Redmi A2+**

| Параметр | Значение |
|---|---|
| Процессор | MediaTek Helio G81 |
| RAM | 3GB |
| GPU | Mali-G52 MC2 |
| Android | 12 |
| OpenGL ES | 3.2 |
| Vulkan | Не используется (совместимость с G52) |

Все лимиты ниже — проверяются на этом устройстве. Если работает на Redmi A2+ — работает на 80%+ целевой аудитории РФ.

---

## Лимиты FPS

| Параметр | Значение |
|---|---|
| Целевой FPS | **30 FPS** |
| Минимально допустимый | **25 FPS** (дропы ниже = баг P0) |
| Настройка в коде | `Application.targetFrameRate = 30` |
| Где выставляется | Bootstrap-сцена, при инициализации |
| VSync | Отключён (`QualitySettings.vSyncCount = 0`) |

Мониторинг: AppMetrica → кастомное событие `fps_drop` при < 25 FPS дольше 2 секунд подряд.

---

## Лимиты памяти

| Параметр | Значение |
|---|---|
| Максимум RAM в рантайме | **300 MB** |
| Предупреждение при | 250 MB |
| Критическое при | 290 MB → автоматическая выгрузка кешей |
| Базовая сцена (MainMenu) | ≤ 80 MB |
| Gameplay с волной | ≤ 220 MB |
| Пик (анимации + VFX) | ≤ 300 MB |

При `OnApplicationMemoryWarning()` — немедленно вызвать `Resources.UnloadUnusedAssets()`.

---

## Лимиты текстур

| Параметр | Значение |
|---|---|
| Формат | **ASTC 6x6** — единственный формат |
| Максимальный размер атласа | **2048×2048** |
| Максимальный размер спрайта башни | **512×512** |
| Максимальный размер иконки UI | **256×256** |
| Мипмапы | Только для фоновых текстур (биомы) |
| Mip генерация | Автоматически в Unity Texture Importer |

ASTC 4x4 — запрещён. ETC2 — запрещён. RGBA32 — только для временных ассетов в редакторе.

---

## Аудио

| Параметр | Значение |
|---|---|
| dspBufferSize | **2048** (устраняет crackling на бюджетных устройствах) |
| AudioSampleRate | 44100 Hz |
| Формат SFX | OGG Vorbis, качество 70% |
| Формат музыки | OGG Vorbis, качество 60% |
| Одновременных AudioSource | ≤ 16 |
| Настройка в коде | `AudioSettings.GetConfiguration()` → `.dspBufferSize = 2048` |

```csharp
// Bootstrap-сцена: выставить до инициализации AudioSource
var config = AudioSettings.GetConfiguration();
config.dspBufferSize = 2048;
AudioSettings.Reset(config);
```

---

## Target API и платформа

| Параметр | Значение |
|---|---|
| Target API Level | **35** (Android 15) |
| Minimum SDK | **24** (Android 7.0) |
| Scripting Backend (develop) | Mono |
| Scripting Backend (main) | IL2CPP |
| Build Output | **.aab** для сторов (RuStore, NashStore) |
| Build Output | **.apk** только для внутреннего QA |
| Architecture | ARM64 + ARMv7 (fat binary для .aab) |

---

## Quality пресеты

### Low (Helio G81 и слабее — основная аудитория)

```
Texture Quality:       Full Res
Shadow Quality:        Disabled (нет теней в 2D игре)
Particle Raycast:      Disabled
Soft Particles:        Disabled
Realtime Reflection:   Disabled
LOD Bias:              0.7
Pixel Light Count:     0
```

### High (Snapdragon 600+ и выше)

```
Texture Quality:       Full Res
Soft Particles:        Enabled
Particle Raycast:      Medium
LOD Bias:              1.0
Pixel Light Count:     1
```

Определение пресета при запуске:

```csharp
// Простой критерий по памяти устройства
if (SystemInfo.systemMemorySize < 4096) // < 4GB
    QualitySettings.SetQualityLevel(0); // Low
else
    QualitySettings.SetQualityLevel(1); // High
```

---

## Полигоны (2D игра)

N/A — 2D спрайтовая игра, ограничений по полигонам нет.  
Ограничения применимы только к VFX-партиклам:

| Параметр | Значение |
|---|---|
| Максимум частиц на экране | **500** одновременно |
| Максимум активных Particle Systems | **10** |

---

## Object Pooling — обязательно

| Тип объекта | Pool обязателен |
|---|---|
| Враги (EnemyPool.cs) | Да |
| Снаряды (ProjectilePool.cs) | Да |
| VFX (взрывы, попадания) | Да |
| Числа урона (damage numbers) | Да |
| Башни | Нет (не спавнятся в hot path) |

`Instantiate()` и `Destroy()` в hot path (волна активна) — баг P0.

---

## Мониторинг лимитов

Все лимиты проверяются через AppMetrica:

| Событие | Триггер |
|---|---|
| `fps_drop` | FPS < 25 на 2+ секунды |
| `memory_warning` | RAM > 250 MB |
| `memory_critical` | RAM > 290 MB |
| `texture_oom` | OutOfMemoryException |

---

*Lead Dev / CTO | Апрель 2026*
