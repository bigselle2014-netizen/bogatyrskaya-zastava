// UnityEngine stubs — minimal types for dotnet build verification only.
// NOT real Unity types. DO NOT ship. Replace with Unity project in Week 1-2.
#pragma warning disable CS0067 // unused events
#pragma warning disable CS0169 // unused fields
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine
{
    // ── Attributes ──────────────────────────────────────────────────────

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SerializeFieldAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class)]
    public sealed class TooltipAttribute : Attribute
    { public TooltipAttribute(string tooltip) { } }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class HeaderAttribute : Attribute
    { public HeaderAttribute(string header) { } }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class CreateAssetMenuAttribute : Attribute
    {
        public string fileName;
        public string menuName;
        public int order;
    }

    // ── Static Utilities ─────────────────────────────────────────────────

    public static class Debug
    {
        public static void Log(object message) { }
        public static void LogWarning(object message) { }
        public static void LogError(object message) { }
    }

    public static class Time
    {
        public static float time;
        public static float deltaTime = 0.016f;
        public static float realtimeSinceStartup;
        public static float fixedDeltaTime = 0.02f;
    }

    public static class Application
    {
        public static int targetFrameRate { get; set; } = 60;
        public static event Action quitting;
    }

    public static class Screen
    {
        public static int sleepTimeout { get; set; }
    }

    public static class SleepTimeout
    {
        public const int NeverSleep = -1;
    }

    public static class PlayerPrefs
    {
        private static readonly Dictionary<string, string> _s = new Dictionary<string, string>();
        private static readonly Dictionary<string, int>    _i = new Dictionary<string, int>();
        private static readonly Dictionary<string, float>  _f = new Dictionary<string, float>();

        public static string GetString(string key, string defaultValue = "")
            => _s.TryGetValue(key, out var v) ? v : defaultValue;
        public static void SetString(string key, string value) => _s[key] = value;

        public static int GetInt(string key, int defaultValue = 0)
            => _i.TryGetValue(key, out var v) ? v : defaultValue;
        public static void SetInt(string key, int value) => _i[key] = value;

        public static float GetFloat(string key, float defaultValue = 0f)
            => _f.TryGetValue(key, out var v) ? v : defaultValue;
        public static void SetFloat(string key, float value) => _f[key] = value;

        public static bool HasKey(string key)
            => _s.ContainsKey(key) || _i.ContainsKey(key) || _f.ContainsKey(key);
        public static void DeleteKey(string key) { _s.Remove(key); _i.Remove(key); _f.Remove(key); }
        public static void Save() { }
    }

    public static class Resources
    {
        public static T   Load<T>(string path) where T : Object => default;
        public static T[] LoadAll<T>(string path) where T : Object => Array.Empty<T>();
    }

    public static class JsonUtility
    {
        public static T      FromJson<T>(string json) => default;
        public static string ToJson(object obj) => "{}";
        public static string ToJson(object obj, bool prettyPrint) => "{}";
    }

    public static class Mathf
    {
        public const float PI = 3.14159265f;
        public static float Clamp01(float v)                     => Math.Max(0f, Math.Min(1f, v));
        public static float Clamp(float v, float min, float max) => Math.Max(min, Math.Min(max, v));
        public static int   Min(int a, int b)                    => Math.Min(a, b);
        public static float Min(float a, float b)                => Math.Min(a, b);
        public static int   Max(int a, int b)                    => Math.Max(a, b);
        public static float Max(float a, float b)                => Math.Max(a, b);
        public static int   CeilToInt(float f)                   => (int)Math.Ceiling(f);
        public static float Abs(float f)                         => Math.Abs(f);
        public static float Sqrt(float f)                        => (float)Math.Sqrt(f);
    }

    // ── Value Types ──────────────────────────────────────────────────────

    public struct Vector3
    {
        public float x, y, z;

        public static readonly Vector3 zero    = new Vector3(0, 0, 0);
        public static readonly Vector3 one     = new Vector3(1, 1, 1);
        public static readonly Vector3 up      = new Vector3(0, 1, 0);
        public static readonly Vector3 forward = new Vector3(0, 0, 1);

        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }

        public float magnitude => Mathf.Sqrt(x * x + y * y + z * z);

        public Vector3 normalized
        {
            get
            {
                float m = magnitude;
                return m > 0.00001f ? new Vector3(x / m, y / m, z / m) : zero;
            }
        }

        public static float Distance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x, dy = a.y - b.y, dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator *(Vector3 a, float f)   => new Vector3(a.x * f, a.y * f, a.z * f);
        public static Vector3 operator *(float f, Vector3 a)   => new Vector3(a.x * f, a.y * f, a.z * f);

        public override string ToString() => $"({x:F2}, {y:F2}, {z:F2})";
    }

    public struct Vector2Int
    {
        public int x, y;
        public Vector2Int(int x, int y) { this.x = x; this.y = y; }
    }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a = 1f) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white => new Color(1, 1, 1, 1);
        public static Color red   => new Color(1, 0, 0, 1);
        public static Color black => new Color(0, 0, 0, 1);
        public static Color clear => new Color(0, 0, 0, 0);
    }

    // ── Object Hierarchy ─────────────────────────────────────────────────

    public class Object
    {
        public string name { get; set; } = "";
        public virtual int GetInstanceID() => GetHashCode();

        public static void Destroy(Object obj) { }
        public static void DontDestroyOnLoad(Object target) { }

        // Generic Instantiate — compiler infers T from first argument
        public static T Instantiate<T>(T original) where T : Object => original;
        public static T Instantiate<T>(T original, Transform parent) where T : Object => original;
    }

    public class Component : Object
    {
        // Returning null is fine for compilation — these are never executed in stub context
        public Transform  transform  => null;
        public GameObject gameObject => null;
        public T GetComponent<T>() where T : Component => default;
    }

    public class Transform : Component
    {
        public Vector3 position { get; set; }
        public Vector3 localPosition { get; set; }
        public void SetParent(Transform parent) { }
        public void SetParent(Transform parent, bool worldPositionStays) { }
    }

    public class GameObject : Object
    {
        public Transform transform        => null;
        public bool      activeInHierarchy { get; private set; } = true;

        public GameObject() { }
        public GameObject(string name) { this.name = name; }

        public void SetActive(bool value) { activeInHierarchy = value; }
        public T    GetComponent<T>() where T : Component => default;
        public T    AddComponent<T>() where T : Component => default;
    }

    public abstract class Behaviour     : Component { public bool enabled { get; set; } = true; }

    public abstract class MonoBehaviour : Behaviour
    {
        protected virtual void Awake()     { }
        protected virtual void Start()     { }
        protected virtual void Update()    { }
        protected virtual void OnEnable()  { }
        protected virtual void OnDisable() { }
        protected virtual void OnDestroy() { }

        public Coroutine StartCoroutine(IEnumerator routine) => new Coroutine();
        public void      StopCoroutine(Coroutine coroutine) { }
        public void      StopCoroutine(IEnumerator routine) { }
        public void      StopAllCoroutines() { }
    }

    public abstract class ScriptableObject : Object { }

    // ── Specific Components ──────────────────────────────────────────────

    public class SpriteRenderer : Component
    {
        public Color  color  { get; set; } = Color.white;
        public Sprite sprite { get; set; }
    }

    // ── Asset Types ──────────────────────────────────────────────────────

    public class Sprite    : Object { }
    public class TextAsset : Object { public string text { get; set; } = ""; }

    // ── Coroutine & Yield Instructions ───────────────────────────────────

    public class Coroutine         { }
    public class WaitForSeconds    { public WaitForSeconds(float seconds) { } }
    public class WaitForEndOfFrame { }
}

namespace UnityEngine.SceneManagement
{
    public enum LoadSceneMode { Single, Additive }

    public struct Scene
    {
        public string name { get; }
        public int buildIndex { get; }
        public bool IsValid() => true;
    }

    public static class SceneManager
    {
        public static event System.Action<Scene> sceneUnloaded;
        public static event System.Action<Scene, LoadSceneMode> sceneLoaded;
        public static void LoadScene(string sceneName) { }
        public static void LoadScene(int sceneBuildIndex) { }
    }
}

namespace UnityEngine.EventSystems
{
    public class PointerEventData { }

    public interface IPointerClickHandler
    {
        void OnPointerClick(PointerEventData eventData);
    }
}
