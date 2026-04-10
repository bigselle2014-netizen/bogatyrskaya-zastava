using System;
using System.Collections.Generic;
using UnityEngine;

namespace BogatyrskayaZastava.Core
{
    // ─────────────────────────────────────────────────────────────────
    // INTERFACES
    // ─────────────────────────────────────────────────────────────────

    public interface IAuthManager
    {
        void SignInAnonymously(Action<AuthResult> onComplete);
        void LinkWithGoogle(Action<AuthResult> onComplete);
        void LinkWithVK(Action<AuthResult> onComplete);
        string CurrentUserId { get; }
        bool IsAuthenticated { get; }
        void SignOut();
    }

    public interface ICloudSaveManager
    {
        void SaveDocument(string collection, string documentId, Dictionary<string, object> data, Action<bool> onComplete);
        void LoadDocument(string collection, string documentId, Action<Dictionary<string, object>> onComplete);
        void DeleteDocument(string collection, string documentId, Action<bool> onComplete);
        void SaveWithServerTimestamp(string collection, string documentId, Dictionary<string, object> data, Action<bool> onComplete);
    }

    public interface IRemoteConfigManager
    {
        void FetchAndActivate(Action<bool> onComplete);
        string GetString(string key, string defaultValue = "");
        int GetInt(string key, int defaultValue = 0);
        float GetFloat(string key, float defaultValue = 0f);
        bool GetBool(string key, bool defaultValue = false);
    }

    public interface ICloudMessagingManager
    {
        void Initialize(Action<string> onTokenReceived);
        void SubscribeToTopic(string topic);
        void UnsubscribeFromTopic(string topic);
        event Action<RemoteMessage> OnMessageReceived;
    }

    // ─────────────────────────────────────────────────────────────────
    // SUPPORTING TYPES
    // ─────────────────────────────────────────────────────────────────

    public class AuthResult
    {
        public bool IsSuccess;
        public string UserId;
        public string ErrorMessage;
    }

    [Serializable]
    public class RemoteMessage
    {
        public string title;
        public string body;
        public Dictionary<string, string> data;
    }

    // ─────────────────────────────────────────────────────────────────
    // FIREBASE MANAGER (facade)
    //
    // TODO: Заменить заглушки на реальный Firebase SDK
    // Модули: Auth (anonymous), Firestore (cloud save), Remote Config, FCM
    // Firebase Analytics — ЗАПРЕЩЁН (game-bible + ArchitectureDoc)
    //
    // Порядок инициализации:
    //   1. FirebaseApp.CheckAndFixDependenciesAsync()
    //   2. Auth.SignInAnonymously()
    //   3. RemoteConfig.FetchAndActivate()
    //   4. FCM.GetToken()
    //
    // Регистрация:
    //   ServiceLocator.Register<IAuthManager>(firebaseManager.Auth);
    //   ServiceLocator.Register<ICloudSaveManager>(firebaseManager.CloudSave);
    //   ServiceLocator.Register<IRemoteConfigManager>(firebaseManager.RemoteConfig);
    //   ServiceLocator.Register<ICloudMessagingManager>(firebaseManager.Messaging);
    // ─────────────────────────────────────────────────────────────────

    public class FirebaseManager
    {
        public IAuthManager Auth { get; private set; }
        public ICloudSaveManager CloudSave { get; private set; }
        public IRemoteConfigManager RemoteConfig { get; private set; }
        public ICloudMessagingManager Messaging { get; private set; }

        private bool _initialized;

        public void Initialize(Action<bool> onComplete)
        {
            // TODO: FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => { ... });
            Auth = new StubAuthManager();
            CloudSave = new StubCloudSaveManager();
            RemoteConfig = new StubRemoteConfigManager();
            Messaging = new StubCloudMessagingManager();

            _initialized = true;
            Debug.Log("[Firebase STUB] All modules initialized.");
            onComplete?.Invoke(true);
        }

        public bool IsInitialized => _initialized;
    }

    // ─────────────────────────────────────────────────────────────────
    // STUB: Auth (Anonymous sign-in)
    // ─────────────────────────────────────────────────────────────────

    internal class StubAuthManager : IAuthManager
    {
        private string _userId;
        private bool _authenticated;

        public string CurrentUserId => _userId;
        public bool IsAuthenticated => _authenticated;

        public void SignInAnonymously(Action<AuthResult> onComplete)
        {
            // TODO: FirebaseAuth.DefaultInstance.SignInAnonymouslyAsync()
            _userId = "stub_user_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            _authenticated = true;
            Debug.Log($"[Firebase Auth STUB] Anonymous sign-in: {_userId}");
            onComplete?.Invoke(new AuthResult { IsSuccess = true, UserId = _userId });
        }

        public void LinkWithGoogle(Action<AuthResult> onComplete)
        {
            // TODO: credential = GoogleAuthProvider.GetCredential(googleIdToken, googleAccessToken);
            //       FirebaseAuth.DefaultInstance.CurrentUser.LinkWithCredentialAsync(credential);
            Debug.Log("[Firebase Auth STUB] LinkWithGoogle → not implemented");
            onComplete?.Invoke(new AuthResult { IsSuccess = false, ErrorMessage = "Stub: not implemented" });
        }

        public void LinkWithVK(Action<AuthResult> onComplete)
        {
            // TODO: Custom auth token flow via Cloud Functions
            Debug.Log("[Firebase Auth STUB] LinkWithVK → not implemented");
            onComplete?.Invoke(new AuthResult { IsSuccess = false, ErrorMessage = "Stub: not implemented" });
        }

        public void SignOut()
        {
            // TODO: FirebaseAuth.DefaultInstance.SignOut();
            _userId = null;
            _authenticated = false;
            Debug.Log("[Firebase Auth STUB] Signed out.");
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // STUB: Firestore (Cloud Save)
    // Structure: users/{userId}/progress, users/{userId}/unlocks, users/{userId}/idle
    // ─────────────────────────────────────────────────────────────────

    internal class StubCloudSaveManager : ICloudSaveManager
    {
        private readonly Dictionary<string, Dictionary<string, object>> _store =
            new Dictionary<string, Dictionary<string, object>>();

        public void SaveDocument(string collection, string documentId,
            Dictionary<string, object> data, Action<bool> onComplete)
        {
            // TODO: FirebaseFirestore.DefaultInstance
            //       .Collection(collection).Document(documentId)
            //       .SetAsync(data);
            var key = $"{collection}/{documentId}";
            _store[key] = new Dictionary<string, object>(data);
            Debug.Log($"[Firestore STUB] Saved: {key} ({data.Count} fields)");
            onComplete?.Invoke(true);
        }

        public void LoadDocument(string collection, string documentId,
            Action<Dictionary<string, object>> onComplete)
        {
            // TODO: FirebaseFirestore.DefaultInstance
            //       .Collection(collection).Document(documentId)
            //       .GetSnapshotAsync();
            var key = $"{collection}/{documentId}";
            if (_store.TryGetValue(key, out var data))
            {
                Debug.Log($"[Firestore STUB] Loaded: {key}");
                onComplete?.Invoke(data);
            }
            else
            {
                Debug.Log($"[Firestore STUB] Not found: {key}");
                onComplete?.Invoke(null);
            }
        }

        public void DeleteDocument(string collection, string documentId, Action<bool> onComplete)
        {
            var key = $"{collection}/{documentId}";
            _store.Remove(key);
            Debug.Log($"[Firestore STUB] Deleted: {key}");
            onComplete?.Invoke(true);
        }

        public void SaveWithServerTimestamp(string collection, string documentId,
            Dictionary<string, object> data, Action<bool> onComplete)
        {
            // TODO: data["serverTimestamp"] = FieldValue.ServerTimestamp;
            data["serverTimestamp"] = DateTime.UtcNow.ToString("o");
            SaveDocument(collection, documentId, data, onComplete);
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // STUB: Remote Config
    // Used for: baseRate, synergyMultipliers, waveSpawnIntervals, event flags
    // ─────────────────────────────────────────────────────────────────

    internal class StubRemoteConfigManager : IRemoteConfigManager
    {
        private readonly Dictionary<string, string> _defaults = new Dictionary<string, string>();

        public void FetchAndActivate(Action<bool> onComplete)
        {
            // TODO: FirebaseRemoteConfig.DefaultInstance.FetchAndActivateAsync()
            Debug.Log("[RemoteConfig STUB] FetchAndActivate → using defaults.");
            onComplete?.Invoke(true);
        }

        public string GetString(string key, string defaultValue = "")
        {
            // TODO: FirebaseRemoteConfig.DefaultInstance.GetValue(key).StringValue;
            return _defaults.TryGetValue(key, out var val) ? val : defaultValue;
        }

        public int GetInt(string key, int defaultValue = 0)
        {
            var str = GetString(key);
            return int.TryParse(str, out var val) ? val : defaultValue;
        }

        public float GetFloat(string key, float defaultValue = 0f)
        {
            var str = GetString(key);
            return float.TryParse(str, out var val) ? val : defaultValue;
        }

        public bool GetBool(string key, bool defaultValue = false)
        {
            var str = GetString(key);
            return bool.TryParse(str, out var val) ? val : defaultValue;
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // STUB: FCM (Cloud Messaging)
    // Firebase FCM используется как fallback для не-RuStore устройств
    // Основной push: RuStore Push SDK (для RuStore-пользователей)
    // ─────────────────────────────────────────────────────────────────

    internal class StubCloudMessagingManager : ICloudMessagingManager
    {
        public event Action<RemoteMessage> OnMessageReceived;

        public void Initialize(Action<string> onTokenReceived)
        {
            // TODO: FirebaseMessaging.TokenReceived += (sender, token) => onTokenReceived(token.Token);
            // TODO: FirebaseMessaging.MessageReceived += (sender, e) => OnMessageReceived?.Invoke(...)
            var stubToken = "stub_fcm_token_" + Guid.NewGuid().ToString("N").Substring(0, 12);
            Debug.Log($"[FCM STUB] Token: {stubToken}");
            onTokenReceived?.Invoke(stubToken);
        }

        public void SubscribeToTopic(string topic)
        {
            // TODO: FirebaseMessaging.SubscribeAsync(topic);
            Debug.Log($"[FCM STUB] Subscribed to topic: {topic}");
        }

        public void UnsubscribeFromTopic(string topic)
        {
            // TODO: FirebaseMessaging.UnsubscribeAsync(topic);
            Debug.Log($"[FCM STUB] Unsubscribed from topic: {topic}");
        }
    }
}
