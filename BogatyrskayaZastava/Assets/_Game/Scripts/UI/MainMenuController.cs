using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

namespace BogatyrskayaZastava.UI
{
    public class MainMenuController : MonoBehaviour
    {
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
            if (Camera.main != null)
                Camera.main.backgroundColor = new Color(0.05f, 0.08f, 0.18f);

            var canvasGO = new GameObject("Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960, 540);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            CreatePanel(canvasGO.transform, Vector2.zero, Vector2.one, new Color(0.05f, 0.08f, 0.18f));

            // Decorative top accent
            CreatePanel(canvasGO.transform,
                new Vector2(0f, 0.88f), new Vector2(1f, 0.92f),
                new Color(0.55f, 0.40f, 0.10f));

            // Decorative bottom accent
            CreatePanel(canvasGO.transform,
                new Vector2(0f, 0.08f), new Vector2(1f, 0.12f),
                new Color(0.55f, 0.40f, 0.10f));

            // Title
            CreateText(canvasGO.transform,
                new Vector2(0.05f, 0.68f), new Vector2(0.95f, 0.87f),
                "БОГАТЫРСКАЯ\nЗАСТАВА", 60, FontStyle.Bold,
                new Color(1f, 0.88f, 0.45f));

            // Subtitle
            CreateText(canvasGO.transform,
                new Vector2(0.15f, 0.60f), new Vector2(0.85f, 0.68f),
                "Защищай заставу от нашествия врагов!", 22, FontStyle.Italic,
                new Color(0.75f, 0.85f, 1f));

            // Play button
            var playBtn = CreateButton(canvasGO.transform,
                new Vector2(0.30f, 0.42f), new Vector2(0.70f, 0.56f),
                "⚔  ИГРАТЬ", 36, new Color(0.15f, 0.50f, 0.18f));
            playBtn.onClick.AddListener(() => SceneManager.LoadScene("Gameplay"));

            // Version text
            CreateText(canvasGO.transform,
                new Vector2(0f, 0.00f), new Vector2(1f, 0.08f),
                "v0.1.0 — Prototype   |   bigselle2014-netizen.github.io", 16, FontStyle.Normal,
                new Color(0.4f, 0.5f, 0.6f));
        }

        private void CreatePanel(Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var go = new GameObject("Panel");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private void CreateText(Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            string text, int fontSize, FontStyle style, Color color)
        {
            var go = new GameObject("Text_" + text.Substring(0, Mathf.Min(8, text.Length)));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.text = text;
            t.font = GetFont();
            t.fontSize = fontSize;
            t.fontStyle = style;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = color;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 10;
            t.resizeTextMaxSize = fontSize;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private Button CreateButton(Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            string label, int fontSize, Color bgColor)
        {
            var go = new GameObject("Button_" + label);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = bgColor;
            var btn = go.AddComponent<Button>();
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var colors = btn.colors;
            colors.highlightedColor = new Color(bgColor.r + 0.15f, bgColor.g + 0.15f, bgColor.b + 0.15f);
            colors.pressedColor = new Color(bgColor.r - 0.1f, bgColor.g - 0.1f, bgColor.b - 0.1f);
            btn.colors = colors;

            var textGO = new GameObject("Label");
            textGO.transform.SetParent(go.transform, false);
            var t = textGO.AddComponent<Text>();
            t.text = label;
            t.font = GetFont();
            t.fontSize = fontSize;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 10;
            t.resizeTextMaxSize = fontSize;
            var textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;

            return btn;
        }

        private Font GetFont()
        {
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return f;
        }
    }
}
