using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

namespace BogatyrskayaZastava.Gameplay
{
    /// <summary>
    /// Полностью самостоятельный прототип Tower Defense.
    /// Создаёт всё программно — не нужны префабы, спрайты или ScriptableObject-ы.
    /// </summary>
    public class SimpleTowerDefense : MonoBehaviour
    {
        // ─── Константы ─────────────────────────────────────────────────
        const int   COLS             = 10;
        const int   ROWS             = 6;
        const float CELL_PX          = 58f;
        const int   START_GOLD       = 120;
        const int   GATE_MAX_HP      = 100;
        const int   TOWER_COST       = 50;
        const float ENEMY_SPEED      = 80f;   // px/sec в reference-пространстве
        const float TOWER_RANGE      = 120f;
        const float TOWER_FIRE_RATE  = 1.2f;  // выстрелов в секунду
        const float TOWER_DAMAGE     = 25f;
        const int   ENEMY_MAX_HP     = 60;
        const int   ENEMY_REWARD     = 12;
        const int   ENEMY_GATE_DMGR  = 10;
        const float SPAWN_INTERVAL   = 1.2f;
        const int   ENEMIES_PER_WAVE = 8;
        const int   TOTAL_WAVES      = 5;

        // Путь через сетку (координаты ячеек)
        static readonly Vector2Int[] PATH = {
            new Vector2Int(0,3), new Vector2Int(1,3), new Vector2Int(2,3),
            new Vector2Int(2,2), new Vector2Int(2,1),
            new Vector2Int(3,1), new Vector2Int(4,1), new Vector2Int(5,1),
            new Vector2Int(5,2), new Vector2Int(5,3), new Vector2Int(5,4),
            new Vector2Int(6,4), new Vector2Int(7,4), new Vector2Int(8,4),
            new Vector2Int(8,3), new Vector2Int(8,2), new Vector2Int(8,1),
            new Vector2Int(9,1)
        };

        // ─── Состояние игры ────────────────────────────────────────────
        enum Phase { Playing, GameOver, Victory }

        int    _gold;
        int    _gateHp;
        int    _wave;
        bool   _waveRunning;
        Phase  _phase;

        // ─── Структуры данных ──────────────────────────────────────────
        class Cell
        {
            public int col, row;
            public bool isPath;
            public bool hasTower;
            public Image visual;
            public Button button;
        }

        class EnemyUnit
        {
            public RectTransform rt;
            public Image img;
            public Image hpBar;
            public float hp;
            public float maxHp;
            public int   wpIdx;      // текущая точка пути
            public bool  dead;
            public bool  reached;
        }

        class TowerUnit
        {
            public int   col, row;
            public float fireTimer;
            public RectTransform rt;
        }

        readonly Cell[][]          _cells   = new Cell[COLS][];
        readonly List<EnemyUnit>   _enemies = new List<EnemyUnit>(32);
        readonly List<TowerUnit>   _towers  = new List<TowerUnit>(16);

        // ─── UI ────────────────────────────────────────────────────────
        Canvas     _canvas;
        RectTransform _gridRoot;
        Text       _goldText;
        Text       _waveText;
        Text       _gateText;
        Text       _statusText;
        Button     _waveBtn;

        // Центр в reference-пространстве для пересчёта позиций
        Vector2 _gridOrigin;

        // ─── Инициализация ─────────────────────────────────────────────
        private void Awake()
        {
            if (FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }
        }

        private void Start()
        {
            _gold   = START_GOLD;
            _gateHp = GATE_MAX_HP;
            _wave   = 0;
            _phase  = Phase.Playing;

            if (Camera.main != null)
                Camera.main.backgroundColor = new Color(0.05f, 0.08f, 0.18f);

            BuildUI();
            BuildGrid();
            RefreshHUD();
        }

        // ─── Построение UI ─────────────────────────────────────────────
        private void BuildUI()
        {
            var go = new GameObject("Canvas");
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960, 540);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();

            // Фон
            MakePanel(go.transform, V2(0,0), V2(1,1), new Color(0.05f, 0.08f, 0.18f));

            // Верхняя панель HUD
            MakePanel(go.transform, V2(0f, 0.89f), V2(1f, 1f), new Color(0.08f, 0.12f, 0.25f));

            // Золото
            _goldText = MakeText(go.transform, V2(0.01f,0.90f), V2(0.33f,1.00f),
                "ЗОЛОТО: 0", 22, Color.yellow);

            // Волна
            _waveText = MakeText(go.transform, V2(0.34f,0.90f), V2(0.66f,1.00f),
                "ВОЛНА: 0/0", 22, Color.white);

            // Ворота
            _gateText = MakeText(go.transform, V2(0.67f,0.90f), V2(0.99f,1.00f),
                "ВОРОТА: 100", 22, new Color(1f,0.5f,0.5f));

            // Статус / подсказка
            _statusText = MakeText(go.transform, V2(0f,0.82f), V2(0.77f,0.90f),
                "Кликни синюю клетку — поставь башню (50 золота).  Затем нажми СЛЕДУЮЩАЯ ВОЛНА", 16, new Color(0.7f,0.9f,1f));

            // Кнопка «Следующая волна»
            _waveBtn = MakeButton(go.transform, V2(0.35f,0.73f), V2(0.65f,0.82f),
                "▶ СЛЕДУЮЩАЯ ВОЛНА", 18, new Color(0.15f, 0.45f, 0.15f));
            _waveBtn.onClick.AddListener(OnWaveButtonClick);

            // Кнопка «Меню»
            var menuBtn = MakeButton(go.transform, V2(0.00f,0.00f), V2(0.18f,0.08f),
                "← Меню", 16, new Color(0.2f,0.2f,0.35f));
            menuBtn.onClick.AddListener(() => SceneManager.LoadScene("MainMenu"));

            // Легенда
            MakePanel(go.transform, V2(0.78f,0.00f), V2(1.00f,0.70f), new Color(0.07f,0.10f,0.22f));
            MakeText(go.transform, V2(0.79f,0.63f), V2(0.99f,0.70f), "ЛЕГЕНДА", 16, Color.white);
            MakeLegendRow(go.transform, V2(0.79f,0.55f), V2(0.99f,0.62f),
                new Color(0.3f,0.7f,0.3f), "Башня (50)");
            MakeLegendRow(go.transform, V2(0.79f,0.47f), V2(0.99f,0.54f),
                new Color(0.8f,0.25f,0.25f), "Враг");
            MakeLegendRow(go.transform, V2(0.79f,0.39f), V2(0.99f,0.46f),
                new Color(0.45f,0.30f,0.08f), "Путь врагов");

            // Корень для сетки
            var gridGO = new GameObject("GridRoot");
            gridGO.transform.SetParent(go.transform, false);
            _gridRoot = gridGO.AddComponent<RectTransform>();
            _gridRoot.anchorMin = V2(0f, 0.08f);
            _gridRoot.anchorMax = V2(0.77f, 0.73f);
            _gridRoot.offsetMin = Vector2.zero;
            _gridRoot.offsetMax = Vector2.zero;
        }

        private void MakeLegendRow(Transform parent, Vector2 aMin, Vector2 aMax,
            Color color, string label)
        {
            MakePanel(parent, aMin, new Vector2(aMin.x + 0.04f, aMax.y), color);
            MakeText(parent, new Vector2(aMin.x + 0.05f, aMin.y), aMax, label, 14, Color.white);
        }

        // ─── Построение сетки ──────────────────────────────────────────
        private void BuildGrid()
        {
            var pathSet = new HashSet<Vector2Int>(PATH);

            for (int c = 0; c < COLS; c++)
            {
                _cells[c] = new Cell[ROWS];
                for (int r = 0; r < ROWS; r++)
                {
                    var key = new Vector2Int(c, r);
                    bool isPath = pathSet.Contains(key);

                    var cellGO = new GameObject($"Cell_{c}_{r}");
                    cellGO.transform.SetParent(_gridRoot, false);

                    var rt = cellGO.AddComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.zero;
                    rt.pivot     = V2(0.5f, 0.5f);
                    rt.sizeDelta = V2(CELL_PX - 2f, CELL_PX - 2f);

                    var img = cellGO.AddComponent<Image>();
                    img.color = isPath
                        ? new Color(0.45f, 0.30f, 0.08f, 1f)
                        : new Color(0.15f, 0.22f, 0.38f, 0.9f);

                    var cell = new Cell
                    {
                        col    = c,
                        row    = r,
                        isPath = isPath,
                        visual = img
                    };

                    if (!isPath)
                    {
                        var btn = cellGO.AddComponent<Button>();
                        var colors = btn.colors;
                        colors.highlightedColor = new Color(0.25f, 0.40f, 0.60f);
                        colors.pressedColor     = new Color(0.10f, 0.15f, 0.25f);
                        btn.colors  = colors;
                        cell.button = btn;

                        int cc = c, rr = r;
                        btn.onClick.AddListener(() => OnCellClick(cc, rr));
                    }

                    _cells[c][r] = cell;
                }
            }

            StartCoroutine(PositionCells());
        }

        private IEnumerator PositionCells()
        {
            yield return null;

            float w = _gridRoot.rect.width;
            float h = _gridRoot.rect.height;

            float startX = (w - COLS * CELL_PX) * 0.5f + CELL_PX * 0.5f;
            float startY = (h - ROWS * CELL_PX) * 0.5f + CELL_PX * 0.5f;

            for (int c = 0; c < COLS; c++)
                for (int r = 0; r < ROWS; r++)
                {
                    var rt = _cells[c][r].visual.GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(startX + c * CELL_PX, startY + r * CELL_PX);
                }

            AddPathArrows(startX, startY);
        }

        /// <summary>Добавляет стрелки направления и метки СТАРТ/ФИНИШ на путь.</summary>
        private void AddPathArrows(float startX, float startY)
        {
            string[] arrows = { "→", "↑", "↓", "←" };

            for (int i = 0; i < PATH.Length; i++)
            {
                var pos = new Vector2(startX + PATH[i].x * CELL_PX, startY + PATH[i].y * CELL_PX);

                // Определяем направление к следующей точке
                string arrow = "";
                string label = "";

                if (i == 0)
                    label = "СТАРТ";
                else if (i == PATH.Length - 1)
                    label = "ФИНИШ";
                else
                {
                    var d = PATH[i + 1] - PATH[i];
                    if      (d.x > 0) arrow = ">";
                    else if (d.x < 0) arrow = "<";
                    else if (d.y > 0) arrow = "^";
                    else              arrow = "v";
                }

                string text = string.IsNullOrEmpty(label) ? arrow : label;
                if (string.IsNullOrEmpty(text)) continue;

                var go = new GameObject("PathLabel");
                go.transform.SetParent(_gridRoot, false);
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.zero;
                rt.pivot     = V2(0.5f, 0.5f);
                rt.sizeDelta = V2(CELL_PX, CELL_PX);
                rt.anchoredPosition = pos;

                var txt = go.AddComponent<Text>();
                txt.text      = text;
                txt.font      = GetFont();
                txt.fontSize  = string.IsNullOrEmpty(label) ? 28 : 14;
                txt.fontStyle = FontStyle.Bold;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color     = i == 0
                    ? new Color(0.3f, 1f, 0.3f)
                    : i == PATH.Length - 1
                        ? new Color(1f, 0.3f, 0.3f)
                        : new Color(1f, 0.85f, 0.3f, 0.9f);
            }
        }

        // ─── Размещение башни ──────────────────────────────────────────
        private void OnCellClick(int c, int r)
        {
            if (_phase != Phase.Playing) return;
            var cell = _cells[c][r];
            if (cell.isPath || cell.hasTower) return;
            if (_gold < TOWER_COST)
            {
                SetStatus("Недостаточно золота! Нужно " + TOWER_COST + " 🪙");
                return;
            }

            _gold -= TOWER_COST;
            cell.hasTower = true;
            cell.visual.color = new Color(0.2f, 0.55f, 0.22f);

            // Иконка башни
            var labelGO = new GameObject("TowerLabel");
            labelGO.transform.SetParent(cell.visual.transform, false);
            var txt = labelGO.AddComponent<Text>();
            txt.text      = "T";
            txt.font      = GetFont();
            txt.fontSize  = 26;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color     = Color.white;
            var trt = labelGO.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            _towers.Add(new TowerUnit
            {
                col       = c,
                row       = r,
                fireTimer = 0f,
                rt        = cell.visual.GetComponent<RectTransform>()
            });

            SetStatus("Башня поставлена! Осталось золота: " + _gold);
            RefreshHUD();
        }

        // ─── Кнопка волны ──────────────────────────────────────────────
        private void OnWaveButtonClick()
        {
            if (_waveRunning || _phase != Phase.Playing) return;
            if (_wave >= TOTAL_WAVES) return;

            _wave++;
            _waveRunning = true;
            _waveBtn.interactable = false;
            SetStatus("Волна " + _wave + " началась! Защищай ворота!");
            RefreshHUD();
            StartCoroutine(SpawnWave());
        }

        private IEnumerator SpawnWave()
        {
            int count = ENEMIES_PER_WAVE + (_wave - 1) * 2;
            for (int i = 0; i < count; i++)
            {
                SpawnEnemy();
                yield return new WaitForSeconds(SPAWN_INTERVAL);
            }

            // Ждём гибели всех врагов
            while (HasLivingEnemies())
                yield return null;

            if (_phase != Phase.Playing) yield break;

            _waveRunning = false;

            if (_wave >= TOTAL_WAVES)
            {
                _phase = Phase.Victory;
                ShowEndScreen("ПОБЕДА! Застава выстояла! 🏆");
            }
            else
            {
                _waveBtn.interactable = true;
                SetStatus("Волна " + _wave + " пройдена! Готовься к следующей...");
            }
        }

        private void SpawnEnemy()
        {
            if (PATH.Length == 0) return;

            var enemyGO = new GameObject("Enemy");
            enemyGO.transform.SetParent(_gridRoot, false);

            var rt = enemyGO.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot     = V2(0.5f, 0.5f);
            rt.sizeDelta = V2(CELL_PX * 0.6f, CELL_PX * 0.6f);
            rt.anchoredPosition = CellToPos(PATH[0].x, PATH[0].y);

            var img = enemyGO.AddComponent<Image>();
            img.color = new Color(0.8f, 0.25f, 0.25f);

            var lblGO = new GameObject("Label");
            lblGO.transform.SetParent(enemyGO.transform, false);
            var lbl = lblGO.AddComponent<Text>();
            lbl.text      = "!";
            lbl.font      = GetFont();
            lbl.fontSize  = 20;
            lbl.fontStyle = FontStyle.Bold;
            lbl.alignment = TextAnchor.MiddleCenter;
            lbl.color     = Color.white;
            var lblRt = lblGO.GetComponent<RectTransform>();
            lblRt.anchorMin = Vector2.zero;
            lblRt.anchorMax = Vector2.one;
            lblRt.offsetMin = Vector2.zero;
            lblRt.offsetMax = Vector2.zero;

            // HP-бар под врагом
            var hpBG = new GameObject("HpBG");
            hpBG.transform.SetParent(enemyGO.transform, false);
            var hpBgImg = hpBG.AddComponent<Image>();
            hpBgImg.color = Color.black;
            var hpBgRt = hpBG.GetComponent<RectTransform>();
            hpBgRt.anchorMin = V2(0f, -0.25f);
            hpBgRt.anchorMax = V2(1f, -0.05f);
            hpBgRt.offsetMin = Vector2.zero;
            hpBgRt.offsetMax = Vector2.zero;

            var hpGO = new GameObject("HpBar");
            hpGO.transform.SetParent(enemyGO.transform, false);
            var hpImg = hpGO.AddComponent<Image>();
            hpImg.color = Color.green;
            var hpRt = hpGO.GetComponent<RectTransform>();
            hpRt.anchorMin = V2(0f, -0.25f);
            hpRt.anchorMax = V2(1f, -0.05f);
            hpRt.offsetMin = Vector2.zero;
            hpRt.offsetMax = Vector2.zero;

            float maxHp = ENEMY_MAX_HP + (_wave - 1) * 10f;
            _enemies.Add(new EnemyUnit
            {
                rt      = rt,
                img     = img,
                hpBar   = hpImg,
                hp      = maxHp,
                maxHp   = maxHp,
                wpIdx   = 0,
                dead    = false,
                reached = false
            });
        }

        // ─── Игровой цикл ──────────────────────────────────────────────
        private void Update()
        {
            if (_phase != Phase.Playing) return;

            float dt = Time.deltaTime;

            MoveEnemies(dt);
            TowerAttack(dt);
        }

        private void MoveEnemies(float dt)
        {
            for (int i = _enemies.Count - 1; i >= 0; i--)
            {
                var e = _enemies[i];
                if (e.dead || e.reached) continue;

                int nextWp = e.wpIdx + 1;
                if (nextWp >= PATH.Length)
                {
                    EnemyReached(e);
                    continue;
                }

                Vector2 target = CellToPos(PATH[nextWp].x, PATH[nextWp].y);
                Vector2 pos    = e.rt.anchoredPosition;
                Vector2 dir    = target - pos;
                float   dist   = dir.magnitude;
                float   step   = ENEMY_SPEED * dt;

                if (step >= dist)
                {
                    e.rt.anchoredPosition = target;
                    e.wpIdx = nextWp;

                    if (e.wpIdx + 1 >= PATH.Length)
                        EnemyReached(e);
                }
                else
                {
                    e.rt.anchoredPosition = pos + dir.normalized * step;
                }
            }
        }

        private void EnemyReached(EnemyUnit e)
        {
            e.reached = true;
            if (e.rt != null && e.rt.gameObject != null)
                Destroy(e.rt.gameObject);

            _gateHp = Mathf.Max(0, _gateHp - ENEMY_GATE_DMGR);
            RefreshHUD();

            if (_gateHp <= 0)
            {
                _phase = Phase.GameOver;
                ShowEndScreen("ВОРОТА РАЗРУШЕНЫ! Игра окончена.");
            }
        }

        private void TowerAttack(float dt)
        {
            foreach (var tower in _towers)
            {
                tower.fireTimer -= dt;
                if (tower.fireTimer > 0f) continue;

                Vector2 tPos = CellToPos(tower.col, tower.row);
                EnemyUnit target = FindNearestEnemy(tPos);
                if (target == null) continue;

                tower.fireTimer = 1f / TOWER_FIRE_RATE;
                target.hp -= TOWER_DAMAGE;

                float hpFrac = Mathf.Clamp01(target.hp / target.maxHp);
                if (target.hpBar != null)
                {
                    target.hpBar.color = Color.Lerp(Color.red, Color.green, hpFrac);
                    var rt = target.hpBar.GetComponent<RectTransform>();
                    rt.anchorMax = new Vector2(hpFrac, rt.anchorMax.y);
                }

                if (target.hp <= 0f)
                {
                    target.dead = true;
                    if (target.rt != null && target.rt.gameObject != null)
                        Destroy(target.rt.gameObject);

                    _gold += ENEMY_REWARD;
                    RefreshHUD();
                    SetStatus("Враг повержен! +" + ENEMY_REWARD + " 🪙");
                }
            }
        }

        private EnemyUnit FindNearestEnemy(Vector2 towerPos)
        {
            EnemyUnit best = null;
            float bestDist = float.MaxValue;

            foreach (var e in _enemies)
            {
                if (e.dead || e.reached) continue;
                if (e.rt == null) continue;

                float d = Vector2.Distance(towerPos, e.rt.anchoredPosition);
                if (d <= TOWER_RANGE && d < bestDist)
                {
                    bestDist = d;
                    best     = e;
                }
            }
            return best;
        }

        private bool HasLivingEnemies()
        {
            foreach (var e in _enemies)
                if (!e.dead && !e.reached) return true;
            return false;
        }

        // ─── Вспомогательные методы ────────────────────────────────────

        /// <summary>
        /// Переводит координаты ячейки в anchoredPosition в GridRoot
        /// (вызывается только после PositionCells корутины)
        /// </summary>
        private Vector2 CellToPos(int c, int r)
        {
            float w = _gridRoot.rect.width;
            float h = _gridRoot.rect.height;
            float startX = (w - COLS * CELL_PX) * 0.5f + CELL_PX * 0.5f;
            float startY = (h - ROWS * CELL_PX) * 0.5f + CELL_PX * 0.5f;
            return new Vector2(startX + c * CELL_PX, startY + r * CELL_PX);
        }

        private void ShowEndScreen(string message)
        {
            StopAllCoroutines();

            var overlayGO = new GameObject("Overlay");
            overlayGO.transform.SetParent(_canvas.transform, false);
            var img = overlayGO.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.75f);
            var rt = overlayGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            MakeText(overlayGO.transform, V2(0.1f, 0.55f), V2(0.9f, 0.75f),
                message, 40, _phase == Phase.Victory ? Color.yellow : Color.red);

            var restartBtn = MakeButton(overlayGO.transform, V2(0.30f, 0.40f), V2(0.70f, 0.52f),
                "Играть снова", 28, new Color(0.15f, 0.45f, 0.15f));
            restartBtn.onClick.AddListener(() => SceneManager.LoadScene("Gameplay"));

            var menuBtn = MakeButton(overlayGO.transform, V2(0.30f, 0.25f), V2(0.70f, 0.37f),
                "Главное меню", 28, new Color(0.2f, 0.2f, 0.35f));
            menuBtn.onClick.AddListener(() => SceneManager.LoadScene("MainMenu"));
        }

        private void RefreshHUD()
        {
            if (_goldText != null) _goldText.text = "ЗОЛОТО: " + _gold + " 🪙";
            if (_waveText != null) _waveText.text = "ВОЛНА: " + _wave + "/" + TOTAL_WAVES;
            if (_gateText != null)
            {
                _gateText.text  = "ВОРОТА: " + _gateHp + "/" + GATE_MAX_HP;
                _gateText.color = _gateHp < 30 ? Color.red : new Color(1f, 0.5f, 0.5f);
            }
        }

        private void SetStatus(string msg)
        {
            if (_statusText != null) _statusText.text = msg;
        }

        // ─── UI-фабрика ────────────────────────────────────────────────
        private void MakePanel(Transform parent, Vector2 aMin, Vector2 aMax, Color color)
        {
            var go = new GameObject("Bg");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            ApplyAnchors(go.GetComponent<RectTransform>(), aMin, aMax);
        }

        private Text MakeText(Transform parent, Vector2 aMin, Vector2 aMax,
            string text, int fontSize, Color color)
        {
            var go = new GameObject("Txt");
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.text      = text;
            t.font      = GetFont();
            t.fontSize  = fontSize;
            t.alignment = TextAnchor.MiddleCenter;
            t.color     = color;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize    = 8;
            t.resizeTextMaxSize    = fontSize;
            ApplyAnchors(go.GetComponent<RectTransform>(), aMin, aMax);
            return t;
        }

        private Button MakeButton(Transform parent, Vector2 aMin, Vector2 aMax,
            string label, int fontSize, Color bgColor)
        {
            var go = new GameObject("Btn");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = bgColor;
            var btn = go.AddComponent<Button>();
            ApplyAnchors(go.GetComponent<RectTransform>(), aMin, aMax);

            var colors = btn.colors;
            colors.highlightedColor = new Color(bgColor.r + 0.15f, bgColor.g + 0.15f, bgColor.b + 0.15f);
            colors.pressedColor     = new Color(bgColor.r - 0.1f, bgColor.g - 0.1f, bgColor.b - 0.1f);
            btn.colors = colors;

            var txtGO = new GameObject("Label");
            txtGO.transform.SetParent(go.transform, false);
            var t = txtGO.AddComponent<Text>();
            t.text      = label;
            t.font      = GetFont();
            t.fontSize  = fontSize;
            t.alignment = TextAnchor.MiddleCenter;
            t.color     = Color.white;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize    = 8;
            t.resizeTextMaxSize    = fontSize;
            var trt = txtGO.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            return btn;
        }

        private void ApplyAnchors(RectTransform rt, Vector2 aMin, Vector2 aMax)
        {
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private Font GetFont()
        {
            Font f = Resources.Load<Font>("Fonts/Roboto-Regular");
            if (f != null) return f;
            f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f != null) return f;
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static Vector2 V2(float x, float y) => new Vector2(x, y);
    }
}
