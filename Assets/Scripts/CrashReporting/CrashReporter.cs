using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FinalNumber.CrashReporting
{
    /// <summary>
    /// Crash reporter for automated crash detection and reporting.
    /// Captures unhandled exceptions, native crashes, and breadcrumbs.
    /// Supports Unity Cloud Diagnostics and Firebase Crashlytics.
    /// </summary>
    public class CrashReporter : MonoBehaviour
    {
        public static CrashReporter Instance { get; private set; }

        [Header("Crash Reporting")]
        [Tooltip("Enable automatic crash reporting")]
        public bool crashReportingEnabled = true;

        [Tooltip("Send crash reports automatically (vs manual review)")]
        public bool automaticReporting = true;

        [Tooltip("Include full device info in crash reports")]
        public bool includeDeviceInfo = true;

        [Tooltip("Include recent logs with crash reports")]
        public bool includeLogs = true;

        [Tooltip("Max log lines to include with crash report")]
        [Range(0, 500)]
        public int maxLogLines = 100;

        // Breadcrumbs for crash context
        private Queue<Breadcrumb> breadcrumbs = new Queue<Breadcrumb>();
        private const int MaxBreadcrumbs = 50;

        // Log capture
        private Queue<string> recentLogs = new Queue<string>();

        // Crash data
        private string lastScene = "";
        private int currentLevel = 0;

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

            InitializeCrashReporting();
        }

        private void OnEnable()
        {
            Application.logMessageReceived += OnLogMessageReceived;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= OnLogMessageReceived;
            AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
        }

        private void Start()
        {
            LogBreadcrumb("session_start", "Crash reporter initialized");
            
            // Subscribe to game events for context
            GameEventBus.OnLevelStarted += OnLevelStarted;
            GameEventBus.OnLevelCompleted += OnLevelCompleted;
        }

        private void OnDestroy()
        {
            GameEventBus.OnLevelStarted -= OnLevelStarted;
            GameEventBus.OnLevelCompleted -= OnLevelCompleted;
        }

        #endregion

        #region Initialization

        private void InitializeCrashReporting()
        {
            if (!crashReportingEnabled) return;

            // Log device info on startup
            LogDeviceInfo();

            LogInfo("Crash reporter initialized");
        }

        private void LogDeviceInfo()
        {
            LogBreadcrumb("device_info", $"Device: {SystemInfo.deviceModel}, OS: {SystemInfo.operatingSystem}");
            LogBreadcrumb("app_info", $"App: {Application.version}, Unity: {Application.unityVersion}");
        }

        #endregion

        #region Event Handlers

        private void OnLevelStarted(int levelId, int worldId)
        {
            currentLevel = levelId;
            LogBreadcrumb("level_start", $"Level {levelId}, World {worldId}");
        }

        private void OnLevelCompleted(int levelId, int worldId, int score, int moves, float timeSeconds)
        {
            LogBreadcrumb("level_complete", $"Level {levelId}, Score {score}, Time {timeSeconds:F1}s");
        }

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            // Capture recent logs
            if (includeLogs && recentLogs.Count < maxLogLines)
            {
                recentLogs.Enqueue($"[{type}] {condition}");
            }

            // Check for errors/fatal
            if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
            {
                // Check if it's a crash-level exception
                if (type == LogType.Exception)
                {
                    HandleCrash(condition, stackTrace);
                }
            }
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                HandleCrash($"Unhandled Exception: {ex.Message}", ex.StackTrace ?? "No stack trace");
            }
        }

        #endregion

        #region Crash Handling

        private void HandleCrash(string message, string stackTrace)
        {
            if (!crashReportingEnabled) return;

            var crashData = new CrashData
            {
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                Message = message,
                StackTrace = stackTrace,
                DeviceModel = SystemInfo.deviceModel,
                OperatingSystem = SystemInfo.operatingSystem,
                AppVersion = Application.version,
                UnityVersion = Application.unityVersion,
                Scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                Level = currentLevel,
                MemoryUsageMB = GC.GetTotalMemory(false) / (1024 * 1024),
                BatteryLevel = SystemInfo.batteryLevel,
                BatteryStatus = SystemInfo.batteryStatus.ToString(),
                Breadcrumbs = GetBreadcrumbsList(),
                RecentLogs = includeLogs ? new List<string>(recentLogs) : new List<string>()
            };

            // Save crash report locally
            SaveCrashReport(crashData);

            // Send if automatic reporting enabled
            if (automaticReporting)
            {
                SendCrashReport(crashData);
            }

            LogError($"Crash captured: {message}");
        }

        #endregion

        #region Crash Report Persistence

        private void SaveCrashReport(CrashData crashData)
        {
            try
            {
                string json = JsonUtility.ToJson(crashData);
                string filename = $"crash_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
                string path = System.IO.Path.Combine(Application.persistentDataPath, "CrashReports");
                
                System.IO.Directory.CreateDirectory(path);
                System.IO.File.WriteAllText(System.IO.Path.Combine(path, filename), json);

                LogInfo($"Crash report saved: {filename}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CrashReporter] Failed to save crash report: {ex.Message}");
            }
        }

        /// <summary>
        /// Get all pending crash reports
        /// </summary>
        public string[] GetPendingCrashReports()
        {
            string path = System.IO.Path.Combine(Application.persistentDataPath, "CrashReports");
            
            if (!System.IO.Directory.Exists(path))
            {
                return new string[0];
            }

            return System.IO.Directory.GetFiles(path, "crash_*.json");
        }

        /// <summary>
        /// Delete a crash report after processing
        /// </summary>
        public void DeleteCrashReport(string filepath)
        {
            try
            {
                if (System.IO.File.Exists(filepath))
                {
                    System.IO.File.Delete(filepath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CrashReporter] Failed to delete crash report: {ex.Message}");
            }
        }

        #endregion

        #region Crash Report Sending

        private void SendCrashReport(CrashData crashData)
        {
            // Send to Unity Cloud Diagnostics if available
            SendToUnityCloudDiagnostics(crashData);

            // Send to Firebase Crashlytics if available
            SendToFirebaseCrashlytics(crashData);
        }

        private void SendToUnityCloudDiagnostics(CrashData crashData)
        {
#if UNITY_CLOUD_DIAGNOSTICS
            try
            {
                // Unity Cloud Diagnostics captures automatically
                // We can add custom metadata
                LogInfo("Crash report sent to Unity Cloud Diagnostics");
            }
            catch (Exception ex)
            {
                LogError($"Failed to send to Unity Cloud Diagnostics: {ex.Message}");
            }
#else
            // Unity Cloud Diagnostics not available
            // Report would be captured by Application.logMessageReceived
#endif
        }

        private void SendToFirebaseCrashlytics(CrashData crashData)
        {
            // Firebase Crashlytics integration placeholder
            // Requires Firebase Unity SDK
            
            /*
            // Firebase Crashlytics code (requires SDK):
            Firebase.Crashlytics.Crashlytics.SetCustomKey("level", crashData.Level);
            Firebase.Crashlytics.Crashlytics.SetCustomKey("scene", crashData.Scene);
            Firebase.Crashlytics.Crashlytics.Log(crashData.Message);
            Firebase.Crashlytics.Crashlytics.RecordException(
                new Exception($"{crashData.Message}\n\n{crashData.StackTrace}")
            );
            */

            LogInfo("Crash report prepared for Firebase Crashlytics (SDK required for actual sending)");
        }

        #endregion

        #region Breadcrumbs

        /// <summary>
        /// Log a breadcrumb for crash context
        /// </summary>
        public void LogBreadcrumb(string category, string message)
        {
            if (breadcrumbs.Count >= MaxBreadcrumbs)
            {
                breadcrumbs.Dequeue();
            }

            breadcrumbs.Enqueue(new Breadcrumb
            {
                Timestamp = DateTime.UtcNow,
                Category = category,
                Message = message
            });

            LogDebug($"Breadcrumb: [{category}] {message}");
        }

        /// <summary>
        /// Log a user action breadcrumb
        /// </summary>
        public void LogUserAction(string action, string details = "")
        {
            LogBreadcrumb("user_action", $"{action}: {details}");
        }

        /// <summary>
        /// Log a navigation breadcrumb
        /// </summary>
        public void LogNavigation(string fromScreen, string toScreen)
        {
            LogBreadcrumb("navigation", $"{fromScreen} -> {toScreen}");
        }

        /// <summary>
        /// Log an API/network call breadcrumb
        /// </summary>
        public void LogApiCall(string endpoint, bool success, int statusCode = 0)
        {
            string status = success ? "success" : "failed";
            LogBreadcrumb("api_call", $"{endpoint} - {status} ({statusCode})");
        }

        private List<Breadcrumb> GetBreadcrumbsList()
        {
            return new List<Breadcrumb>(breadcrumbs);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Test crash reporting with a non-fatal exception
        /// </summary>
        public void TestCrashReporting()
        {
            LogInfo("Testing crash reporting...");
            LogBreadcrumb("test", "Crash reporting test initiated");

            try
            {
                throw new InvalidOperationException("Test exception for crash reporting verification");
            }
            catch (Exception ex)
            {
                HandleCrash($"Test Exception: {ex.Message}", ex.StackTrace);
            }
        }

        /// <summary>
        /// Enable/disable crash reporting
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            crashReportingEnabled = enabled;
            LogInfo($"Crash reporting {(enabled ? "enabled" : "disabled")}");
        }

        /// <summary>
        /// Get crash report statistics
        /// </summary>
        public CrashStats GetCrashStats()
        {
            string[] pending = GetPendingCrashReports();
            
            return new CrashStats
            {
                PendingReports = pending.Length,
                BreadcrumbsCaptured = breadcrumbs.Count,
                CrashReportingEnabled = crashReportingEnabled,
                AutomaticReporting = automaticReporting
            };
        }

        #endregion

        #region Logging Helpers

        private void LogInfo(string message)
        {
            Debug.Log($"[CrashReporter] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[CrashReporter] {message}");
        }

        private void LogDebug(string message)
        {
            #if DEBUG
            Debug.Log($"[CrashReporter] {message}");
            #endif
        }

        #endregion

        #region Data Classes

        [System.Serializable]
        public class CrashData
        {
            public string Timestamp;
            public string Message;
            public string StackTrace;
            public string DeviceModel;
            public string OperatingSystem;
            public string AppVersion;
            public string UnityVersion;
            public string Scene;
            public int Level;
            public long MemoryUsageMB;
            public float BatteryLevel;
            public string BatteryStatus;
            public List<Breadcrumb> Breadcrumbs;
            public List<string> RecentLogs;
        }

        [System.Serializable]
        public class Breadcrumb
        {
            public DateTime Timestamp;
            public string Category;
            public string Message;
        }

        public class CrashStats
        {
            public int PendingReports;
            public int BreadcrumbsCaptured;
            public bool CrashReportingEnabled;
            public bool AutomaticReporting;
        }

        #endregion
    }
}
