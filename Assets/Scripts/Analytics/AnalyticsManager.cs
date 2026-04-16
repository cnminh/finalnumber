using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;

namespace FinalNumber.Analytics
{
    /// <summary>
    /// Analytics manager for tracking player behavior and game events.
    /// Integrates with Unity Analytics for core metrics and provides
    /// extensible event system for custom tracking.
    /// </summary>
    public class AnalyticsManager : MonoBehaviour
    {
        public static AnalyticsManager Instance { get; private set; }

        [Header("Analytics Settings")]
        [Tooltip("Enable analytics collection")]
        public bool analyticsEnabled = true;

        [Tooltip("Enable detailed debug logging")]
        public bool debugMode = false;

        [Tooltip("Batch events for better performance")]
        public bool batchEvents = true;

        [Header("Session Tracking")]
        [Tooltip("Minimum session length to count as engaged (seconds)")]
        public float minEngagedSessionLength = 30f;

        // Session tracking
        private DateTime sessionStartTime;
        private float sessionLengthSeconds;
        private bool sessionActive = false;
        private int levelsCompletedThisSession = 0;
        private int levelAttemptsThisSession = 0;

        // Event queue for batching
        private Queue<AnalyticsEvent> eventQueue = new Queue<AnalyticsEvent>();
        private const int MaxBatchSize = 10;
        private float lastBatchSendTime = 0f;
        private const float BatchIntervalSeconds = 30f;

        // Analytics consent
        public bool UserOptedOut { get; private set; } = false;

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadOptOutState();
            InitializeAnalytics();
        }

        private void OnEnable()
        {
            // Subscribe to game events
            GameEventBus.OnLevelStarted += OnLevelStarted;
            GameEventBus.OnLevelCompleted += OnLevelCompleted;
            GameEventBus.OnLevelFailed += OnLevelFailed;
            GameEventBus.OnWorldUnlocked += OnWorldUnlocked;
            GameEventBus.OnPowerUpUsed += OnPowerUpUsed;
            GameEventBus.OnAchievementUnlocked += OnAchievementUnlocked;
        }

        private void OnDisable()
        {
            // Unsubscribe from game events
            GameEventBus.OnLevelStarted -= OnLevelStarted;
            GameEventBus.OnLevelCompleted -= OnLevelCompleted;
            GameEventBus.OnLevelFailed -= OnLevelFailed;
            GameEventBus.OnWorldUnlocked -= OnWorldUnlocked;
            GameEventBus.OnPowerUpUsed -= OnPowerUpUsed;
            GameEventBus.OnAchievementUnlocked -= OnAchievementUnlocked;
        }

        private void Start()
        {
            StartSession();
        }

        private void Update()
        {
            if (sessionActive)
            {
                sessionLengthSeconds += Time.unscaledDeltaTime;
            }

            // Process batch queue
            if (batchEvents && Time.time - lastBatchSendTime > BatchIntervalSeconds)
            {
                ProcessEventBatch();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                // App going to background - flush events
                FlushEvents();
            }
        }

        private void OnApplicationQuit()
        {
            EndSession();
        }

        #endregion

        #region Initialization

        private void InitializeAnalytics()
        {
            if (UserOptedOut || !analyticsEnabled)
            {
                LogDebug("Analytics disabled - user opted out or disabled in settings");
                return;
            }

#if UNITY_ANALYTICS
            // Unity Analytics auto-initializes, but we verify it's enabled
            LogDebug("Unity Analytics initialized");
            
            // Set user properties
            AnalyticsResult result = Analytics.CustomEvent("analytics_initialized", new Dictionary<string, object>
            {
                { "platform", Application.platform.ToString() },
                { "app_version", Application.version },
                { "unity_version", Application.unityVersion }
            });
            
            LogDebug($"Analytics init event result: {result}");
#else
            Debug.LogWarning("[AnalyticsManager] Unity Analytics package not found!");
#endif
        }

        #endregion

        #region Session Management

        private void StartSession()
        {
            if (UserOptedOut) return;

            sessionStartTime = DateTime.UtcNow;
            sessionLengthSeconds = 0f;
            sessionActive = true;
            levelsCompletedThisSession = 0;
            levelAttemptsThisSession = 0;

            TrackEvent("session_start", new Dictionary<string, object>
            {
                { "timestamp", sessionStartTime.ToString("yyyy-MM-ddTHH:mm:ssZ") }
            });

            LogDebug("Session started");
        }

        private void EndSession()
        {
            if (!sessionActive || UserOptedOut) return;

            sessionActive = false;
            bool engaged = sessionLengthSeconds >= minEngagedSessionLength;

            TrackEvent("session_end", new Dictionary<string, object>
            {
                { "session_length_seconds", (int)sessionLengthSeconds },
                { "session_length_minutes", Mathf.RoundToInt(sessionLengthSeconds / 60f) },
                { "engaged", engaged },
                { "levels_completed", levelsCompletedThisSession },
                { "level_attempts", levelAttemptsThisSession }
            });

            // Track retention milestones
            TrackRetention();

            LogDebug($"Session ended - Length: {sessionLengthSeconds:F1}s, Engaged: {engaged}");
        }

        private void TrackRetention()
        {
            // Track D1, D7, D30 retention data
            string installDate = PlayerPrefs.GetString("InstallDate", "");
            if (string.IsNullOrEmpty(installDate))
            {
                installDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
                PlayerPrefs.SetString("InstallDate", installDate);
                PlayerPrefs.Save();
            }

            DateTime install;
            if (DateTime.TryParse(installDate, out install))
            {
                int daysSinceInstall = (DateTime.UtcNow - install).Days;
                
                TrackEvent("retention_check", new Dictionary<string, object>
                {
                    { "days_since_install", daysSinceInstall },
                    { "install_date", installDate },
                    { "is_d1", daysSinceInstall == 1 },
                    { "is_d7", daysSinceInstall == 7 },
                    { "is_d30", daysSinceInstall == 30 }
                });

                // Mark retention milestones
                if (daysSinceInstall == 1) MarkRetentionMilestone("d1");
                if (daysSinceInstall == 7) MarkRetentionMilestone("d7");
                if (daysSinceInstall == 30) MarkRetentionMilestone("d30");
            }
        }

        private void MarkRetentionMilestone(string milestone)
        {
            string key = $"Retention_{milestone}";
            if (!PlayerPrefs.HasKey(key))
            {
                PlayerPrefs.SetInt(key, 1);
                PlayerPrefs.Save();

                TrackEvent($"retention_{milestone}", new Dictionary<string, object>
                {
                    { "milestone", milestone },
                    { "session_count", PlayerPrefs.GetInt("SessionCount", 0) }
                });
            }
        }

        #endregion

        #region Event Handlers

        private void OnLevelStarted(int levelId, int worldId)
        {
            levelAttemptsThisSession++;

            TrackEvent("level_start", new Dictionary<string, object>
            {
                { "level_id", levelId },
                { "world_id", worldId },
                { "attempt_number", levelAttemptsThisSession }
            });
        }

        private void OnLevelCompleted(int levelId, int worldId, int score, int moves, float timeSeconds)
        {
            levelsCompletedThisSession++;

            TrackEvent("level_complete", new Dictionary<string, object>
            {
                { "level_id", levelId },
                { "world_id", worldId },
                { "score", score },
                { "moves", moves },
                { "time_seconds", Mathf.RoundToInt(timeSeconds) },
                { "session_level_number", levelsCompletedThisSession }
            });

            // Track progression
            TrackEvent("level_progression", new Dictionary<string, object>
            {
                { "total_levels_completed", levelsCompletedThisSession },
                { "current_world", worldId },
                { "max_level_reached", Mathf.Max(levelId, PlayerPrefs.GetInt("MaxLevelReached", 0)) }
            });
        }

        private void OnLevelFailed(int levelId, int worldId, int score, string reason)
        {
            TrackEvent("level_fail", new Dictionary<string, object>
            {
                { "level_id", levelId },
                { "world_id", worldId },
                { "score", score },
                { "fail_reason", reason },
                { "attempt_number", levelAttemptsThisSession }
            });
        }

        private void OnWorldUnlocked(int worldId, string worldName)
        {
            TrackEvent("world_unlock", new Dictionary<string, object>
            {
                { "world_id", worldId },
                { "world_name", worldName },
                { "session_count", PlayerPrefs.GetInt("SessionCount", 0) }
            });
        }

        private void OnPowerUpUsed(string powerUpType, int levelId)
        {
            TrackEvent("power_up_used", new Dictionary<string, object>
            {
                { "power_up_type", powerUpType },
                { "level_id", levelId },
                { "session_time_seconds", (int)sessionLengthSeconds }
            });
        }

        private void OnAchievementUnlocked(string achievementId, string achievementName)
        {
            TrackEvent("achievement_unlocked", new Dictionary<string, object>
            {
                { "achievement_id", achievementId },
                { "achievement_name", achievementName },
                { "level_id", PlayerPrefs.GetInt("CurrentLevel", 0) }
            });
        }

        #endregion

        #region Public Tracking Methods

        /// <summary>
        /// Track a custom event with parameters
        /// </summary>
        public void TrackEvent(string eventName, Dictionary<string, object> parameters = null)
        {
            if (UserOptedOut || !analyticsEnabled) return;

            if (batchEvents)
            {
                eventQueue.Enqueue(new AnalyticsEvent(eventName, parameters));
                
                if (eventQueue.Count >= MaxBatchSize)
                {
                    ProcessEventBatch();
                }
            }
            else
            {
                SendEventImmediate(eventName, parameters);
            }

            LogDebug($"Tracked event: {eventName}");
        }

        /// <summary>
        /// Track a purchase event
        /// </summary>
        public void TrackPurchase(string productId, decimal price, string currency)
        {
            if (UserOptedOut) return;

            TrackEvent("purchase", new Dictionary<string, object>
            {
                { "product_id", productId },
                { "price", (double)price },
                { "currency", currency },
                { "level_id", PlayerPrefs.GetInt("CurrentLevel", 0) }
            });

#if UNITY_ANALYTICS
            Analytics.Transaction(productId, price, currency);
#endif
        }

        /// <summary>
        /// Track ad impression
        /// </summary>
        public void TrackAdImpression(string adType, string placement)
        {
            if (UserOptedOut) return;

            TrackEvent("ad_impression", new Dictionary<string, object>
            {
                { "ad_type", adType },
                { "placement", placement },
                { "session_time_seconds", (int)sessionLengthSeconds }
            });
        }

        /// <summary>
        /// Track tutorial progress
        /// </summary>
        public void TrackTutorialStep(int stepNumber, string stepName, bool completed)
        {
            if (UserOptedOut) return;

            TrackEvent("tutorial_step", new Dictionary<string, object>
            {
                { "step_number", stepNumber },
                { "step_name", stepName },
                { "completed", completed }
            });
        }

        #endregion

        #region Privacy & Consent

        /// <summary>
        /// Opt user out of analytics collection
        /// </summary>
        public void OptOut()
        {
            UserOptedOut = true;
            PlayerPrefs.SetInt("AnalyticsOptOut", 1);
            PlayerPrefs.Save();

#if UNITY_ANALYTICS
            Analytics.enabled = false;
#endif

            LogDebug("User opted out of analytics");
        }

        /// <summary>
        /// Opt user in to analytics collection
        /// </summary>
        public void OptIn()
        {
            UserOptedOut = false;
            PlayerPrefs.SetInt("AnalyticsOptOut", 0);
            PlayerPrefs.Save();

#if UNITY_ANALYTICS
            Analytics.enabled = true;
#endif

            LogDebug("User opted in to analytics");
        }

        private void LoadOptOutState()
        {
            UserOptedOut = PlayerPrefs.GetInt("AnalyticsOptOut", 0) == 1;
            
#if UNITY_ANALYTICS
            Analytics.enabled = !UserOptedOut && analyticsEnabled;
#endif
        }

        /// <summary>
        /// Get analytics data for privacy export
        /// </summary>
        public Dictionary<string, object> GetCollectedData()
        {
            return new Dictionary<string, object>
            {
                { "install_date", PlayerPrefs.GetString("InstallDate", "unknown") },
                { "session_count", PlayerPrefs.GetInt("SessionCount", 0) },
                { "total_play_time_seconds", PlayerPrefs.GetFloat("TotalPlayTime", 0f) },
                { "max_level_reached", PlayerPrefs.GetInt("MaxLevelReached", 0) },
                { "worlds_unlocked", PlayerPrefs.GetString("WorldsUnlocked", "") },
                { "retention_d1", PlayerPrefs.HasKey("Retention_d1") },
                { "retention_d7", PlayerPrefs.HasKey("Retention_d7") },
                { "retention_d30", PlayerPrefs.HasKey("Retention_d30") }
            };
        }

        #endregion

        #region Event Sending

        private void SendEventImmediate(string eventName, Dictionary<string, object> parameters)
        {
#if UNITY_ANALYTICS
            if (parameters != null)
            {
                Analytics.CustomEvent(eventName, parameters);
            }
            else
            {
                Analytics.CustomEvent(eventName);
            }
#endif
        }

        private void ProcessEventBatch()
        {
            if (eventQueue.Count == 0) return;

            lastBatchSendTime = Time.time;

            while (eventQueue.Count > 0)
            {
                var evt = eventQueue.Dequeue();
                SendEventImmediate(evt.EventName, evt.Parameters);
            }

            LogDebug($"Sent batch of events. Queue remaining: {eventQueue.Count}");
        }

        private void FlushEvents()
        {
            if (batchEvents)
            {
                ProcessEventBatch();
            }

#if UNITY_ANALYTICS
            Analytics.FlushEvents();
#endif
        }

        private void LogDebug(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[AnalyticsManager] {message}");
            }
        }

        #endregion

        #region Analytics Event Class

        private class AnalyticsEvent
        {
            public string EventName { get; }
            public Dictionary<string, object> Parameters { get; }
            public DateTime Timestamp { get; }

            public AnalyticsEvent(string eventName, Dictionary<string, object> parameters)
            {
                EventName = eventName;
                Parameters = parameters;
                Timestamp = DateTime.UtcNow;
            }
        }

        #endregion
    }
}
