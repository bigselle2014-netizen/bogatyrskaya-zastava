# Структура Unity-проекта «Богатырская Застава»

**Дата**: Апрель 2026  
**Статус**: Утверждено Lead Dev / CTO  
**Правило**: Структура обязательна для всей команды. Отклонения — только через Lead Dev.

---

## Дерево папок

```
Assets/
├── _Game/
│   ├── Scripts/
│   │   ├── Core/
│   │   │   ├── SceneLoader.cs
│   │   │   ├── GameManager.cs
│   │   │   └── EventBus.cs
│   │   ├── Gameplay/
│   │   │   ├── TowerController.cs
│   │   │   ├── EnemyController.cs
│   │   │   ├── WaveManager.cs
│   │   │   ├── EnemyPool.cs
│   │   │   └── ProjectilePool.cs
│   │   ├── Idle/
│   │   │   ├── IdleManager.cs
│   │   │   └── ResourceAccumulator.cs
│   │   ├── Meta/
│   │   │   ├── ProgressionSystem.cs
│   │   │   ├── RunManager.cs
│   │   │   └── DeckManager.cs
│   │   ├── UI/
│   │   │   ├── UIManager.cs
│   │   │   ├── HUD.cs
│   │   │   ├── Popups/
│   │   │   │   ├── RunCompletePopup.cs
│   │   │   │   ├── ShopPopup.cs
│   │   │   │   └── SettingsPopup.cs
│   │   │   └── Screens/
│   │   │       ├── MainMenuScreen.cs
│   │   │       └── LoadingScreen.cs
│   │   ├── Data/
│   │   │   ├── TowerData.cs            ← ScriptableObject-определения башен
│   │   │   ├── EnemyData.cs
│   │   │   ├── WaveData.cs
│   │   │   ├── SynergyData.cs
│   │   │   └── BiomeData.cs
│   │   ├── Services/
│   │   │   ├── ServiceLocator.cs
│   │   │   ├── IAudioService.cs
│   │   │   ├── IAdManager.cs
│   │   │   ├── IAnalyticsService.cs
│   │   │   ├── ISaveService.cs
│   │   │   └── IIAPManager.cs
│   │   └── StateMachine/
│   │       └── GameStateMachine.cs
│   │
│   ├── Prefabs/
│   │   ├── Towers/                     ← Префабы всех 15 башен (имена строго из game-bible)
│   │   ├── Enemies/
│   │   ├── Projectiles/
│   │   ├── VFX/
│   │   └── UI/
│   │
│   ├── Scenes/
│   │   ├── Bootstrap.unity             ← Инициализация сервисов, не содержит геймплея
│   │   ├── MainMenu.unity
│   │   ├── Gameplay.unity
│   │   └── Loading.unity
│   │
│   └── ScriptableObjects/
│       ├── Towers/                     ← T_Ratnik.asset, T_Vityaz.asset и т.д. (15 штук)
│       ├── Enemies/                    ← E_Goblin.asset и т.д.
│       ├── Waves/                      ← W_Act1_Wave01.asset и т.д.
│       ├── Synergies/
│       └── Biomes/
│
├── _Plugins/
│   ├── AppMetrica/                     ← ЕДИНСТВЕННАЯ аналитика
│   ├── RuStore/                        ← RuStore Pay SDK v10.1.1
│   ├── Firebase/                       ← Auth, Firestore, RemoteConfig, FCM
│   │   └── ВАЖНО: Analytics НЕ инициализируется, см. ArchitectureDoc.md
│   └── IronSource/                     ← Медиация для rewarded video
│
├── _Art/
│   ├── Sprites/
│   │   ├── Towers/
│   │   ├── Enemies/
│   │   ├── UI/
│   │   └── Biomes/
│   ├── Atlases/                        ← TextureAtlas_Towers.spriteatlas и т.д. (макс 2048×2048)
│   └── ФОРМАТ: ASTC 6x6 (строго, никакой другой формат не используется)
│
└── _Audio/
    ├── Music/
    ├── SFX/
    └── Voice/
```

---

## Assembly Definition файлы (.asmdef)

### Структура и зависимости

| Файл (.asmdef) | Папка | Зависит от |
|---|---|---|
| `Game.Core.asmdef` | `Scripts/Core/` | *(нет зависимостей, базовая сборка)* |
| `Game.Services.asmdef` | `Scripts/Services/` | `Game.Core` |
| `Game.Data.asmdef` | `Scripts/Data/` | `Game.Core` |
| `Game.Gameplay.asmdef` | `Scripts/Gameplay/` | `Game.Core`, `Game.Services`, `Game.Data` |
| `Game.Idle.asmdef` | `Scripts/Idle/` | `Game.Core`, `Game.Services`, `Game.Data` |
| `Game.Meta.asmdef` | `Scripts/Meta/` | `Game.Core`, `Game.Services`, `Game.Data` |
| `Game.UI.asmdef` | `Scripts/UI/` | `Game.Core`, `Game.Services`, `Game.Gameplay`, `Game.Meta` |
| `Game.StateMachine.asmdef` | `Scripts/StateMachine/` | `Game.Core`, `Game.Services` |

### Пример содержимого Game.Core.asmdef

```json
{
  "name": "Game.Core",
  "rootNamespace": "BogatyrZastava.Core",
  "references": [],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "autoReferenced": true
}
```

### Пример содержимого Game.Gameplay.asmdef

```json
{
  "name": "Game.Gameplay",
  "rootNamespace": "BogatyrZastava.Gameplay",
  "references": [
    "Game.Core",
    "Game.Services",
    "Game.Data"
  ],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "autoReferenced": true
}
```

---

## Соглашения по именованию файлов

| Тип | Префикс | Пример |
|---|---|---|
| ScriptableObject башни | `T_` | `T_Ratnik.asset` |
| ScriptableObject врага | `E_` | `E_LeshaRyadovoy.asset` |
| ScriptableObject волны | `W_` | `W_Act1_Wave01.asset` |
| Сцены | *(нет префикса)* | `Gameplay.unity` |
| Префабы башни | `Tower_` | `Tower_Ratnik.prefab` |
| Префабы врага | `Enemy_` | `Enemy_LeshaRyadovoy.prefab` |

---

## Запрещённые папки/файлы в репозитории

- `Assets/StreamingAssets/` — не используется в MVP
- `Assets/AssetBundles/` — Addressables не используются в MVP (см. ArchitectureDoc.md)
- `Library/`, `Temp/`, `obj/`, `Build/` — добавлены в .gitignore
