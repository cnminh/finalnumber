using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FinalNumber.Performance
{
    /// <summary>
    /// Performance monitor for tracking FPS, memory usage, and load times.
    /// Helps identify performance issues on low-end devices.
    /// </summary>
    public class PerformanceMonitor : MonoBehaviour
    {
        public static PerformanceMonitor Instance { get; private set; }

        [Header("Performance Tracking")]
        [Tooltip("Enable performance monitoring")]
        public bool monitoringEnabled = true;

        [Tooltip("Target frame rate for FPS tracking")]
        public int targetFrameRate = 60;

        [Tooltip("FPS threshold for warnings")]
        public float fpsWarningThreshold = 30f;

        [Tooltip("Severe FPS threshold for critical warnings")]
        public float fpsCriticalThreshold = 15f;

        [Tooltip("Memory warning threshold in MB")]
        public long memoryWarningThresholdMB = 512;

        [Header("Tracking Intervals")]
        [Tooltip("How often to capture performance metrics (seconds)")]
        public float captureInterval = 5f;

        [Tooltip("FPS averaging window (seconds)")]
        public float fpsWindow = 1f;

        // Performance data
        private float currentFPS;
        private float averageFPS;
        private float minFPS = float.MaxValue;
        private float maxFPS = 0f;
        private long currentMemoryMB;
        private long peakMemoryMB;

        // FPS tracking
        private List<float> fpsSamples = new List<float>();
        private float fpsTimer = 0f;
        private int frameCount = 0;

        // FPS drop tracking
        private int fpsDropCount = 0;
        private float consecutiveLowFpsTime = 0f;

        // Scene load tracking
        private Dictionary<string, float> sceneLoadTimes = new Dictionary<string, float>();
        private string currentScene;
        private float sceneStartTime;

        // Session metrics
        private float sessionStartTime;
        private int totalFrameTimeSpikes = 0;

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

            InitializeMonitoring();
        }

        private void Start()
        {
            sessionStartTime = Time.unscaledTime;
            StartCoroutine(PerformanceCaptureLoop());
        }

        private void Update()
        {
            if (!monitoringEnabled) return;

            // Track FPS
            frameCount++;
            fpsTimer += Time.unscaledDeltaTime;

            if (fpsTimer >= fpsWindow)
            {
                CalculateFPS();
            }

            // Check for FPS drops
            if (currentFPS < fpsWarningThreshold)
            {
                consecutiveLowFpsTime += Time.unscaledDeltaTime;
                
                if (consecutiveLowFpsTime > 2f) // Sustained low FPS
                {
                    OnFpsDrop(currentFPS);
                }
            }
            else
            {
                consecutiveLowFpsTime = 0f;
            }

            // Memory tracking
            TrackMemory();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        private void OnDestroy()
        {
            ReportSessionMetrics();
        }

        #endregion

        #region Initialization

        private void InitializeMonitoring()
        {
            Application.targetFrameRate = targetFrameRate;
            QualitySettings.vSyncCount = 0; // Let us manage frame rate

            LogInfo("Performance monitor initialized");
        }

        #endregion

        #region FPS Tracking

        private void CalculateFPS()
        {
            currentFPS = frameCount / fpsTimer;
            fpsSamples.Add(currentFPS);

            // Keep samples within reasonable window
            if (fpsSamples.Count > 60) // Keep last 60 windows
            {
                fpsSamples.RemoveAt(0);
            }

            // Update stats
            averageFPS = CalculateAverageFPS();
            minFPS = Mathf.Min(minFPS, currentFPS);
            maxFPS = Mathf.Max(maxFPS, currentFPS);

            // Reset counters
            frameCount = 0;
            fpsTimer = 0f;
        }

        private float CalculateAverageFPS()
        {
            if (fpsSamples.Count == 0) return 0f;

            float sum = 0f;
            foreach (var fps in fpsSamples)
            {
                sum += fps;
            }
            return sum / fpsSamples.Count;
        }

        private void OnFpsDrop(float fps)
        {
            fpsDropCount++;
            totalFrameTimeSpikes++;

            LogWarning($"FPS drop detected: {fps:F1} FPS (below {fpsWarningThreshold} threshold)");

            // Send analytics event
            Analytics.AnalyticsManager.Instance?.TrackEvent("fps_drop", new Dictionary<string, object>
            {
                { "fps", Mathf.RoundToInt(fps) },
                { "threshold", fpsWarningThreshold },
                { "scene", currentScene },
                { "memory_mb", currentMemoryMB }
            });

            consecutiveLowFpsTime = 0f; // Reset to avoid spam
        }

        #endregion

        #region Memory Tracking

        private void TrackMemory()
        {
            long memoryBytes = GC.GetTotalMemory(false);
            currentMemoryMB = memoryBytes / (1024 * 1024);
            peakMemoryMB = Mathf.Max(peakMemoryMB, currentMemoryMB);

            if (currentMemoryMB > memoryWarningThresholdMB)
            {
                LogWarning($"High memory usage: {currentMemoryMB} MB (above {memoryWarningThresholdMB} MB threshold)");

                Analytics.AnalyticsManager.Instance?.TrackEvent("memory_warning", new Dictionary<string, object>
                {
                    { "memory_mb", (int)currentMemoryMB },
                    { "threshold_mb", memoryWarningThresholdMB },
                    { "scene", currentScene }
                });
            }
        }

        #endregion

        #region Scene Load Tracking

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            float loadTime = Time.unscaledTime - sceneStartTime;
            currentScene = scene.name;

            sceneLoadTimes[scene.name] = loadTime;

            LogInfo($"Scene '{scene.name}' loaded in {loadTime:F2}s");

            // Track in analytics
            Analytics.AnalyticsManager.Instance?.TrackEvent("scene_loaded", new Dictionary<string, object>
            {
                { "scene_name", scene.name },
                { "load_time_seconds", Mathf.RoundToInt(loadTime) },
                { "load_mode", mode.ToString() }
            });

            sceneStartTime = Time.unscaledTime;
        }

        private void OnSceneUnloaded(Scene scene)
        {
            // Track scene duration for long-running scenes
            float sceneDuration = Time.unscaledTime - sceneStartTime;
            
            LogInfo($"Scene '{scene.name}' unloaded after {sceneDuration:F2}s");
        }

        #endregion

        #region Performance Capture Loop

        private IEnumerator PerformanceCaptureLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(captureInterval);

                if (!monitoringEnabled) continue;

                CapturePerformanceSnapshot();
            }
        }

        private void CapturePerformanceSnapshot()
        {
            var snapshot = new PerformanceSnapshot
            {
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                Scene = currentScene,
                FPS = Mathf.RoundToInt(currentFPS),
                AverageFPS = Mathf.RoundToInt(averageFPS),
                MemoryMB = (int)currentMemoryMB,
                PeakMemoryMB = (int)peakMemoryMB,
                DeviceModel = SystemInfo.deviceModel,
                BatteryLevel = SystemInfo.batteryLevel,
                TemperatureState = GetTemperatureState()
            };

            // Log to analytics
            Analytics.AnalyticsManager.Instance?.TrackEvent("performance_snapshot", new Dictionary<string, object>
            {
                { "fps", snapshot.FPS },
                { "avg_fps", snapshot.AverageFPS },
                { "memory_mb", snapshot.MemoryMB },
                { "scene", snapshot.Scene }
            });

            // Check for thermal throttling
            if (snapshot.TemperatureState == DeviceTemperatureState.Throttling ||
                snapshot.TemperatureState == DeviceTemperatureState.ShutdownImminent)
            {
                LogWarning($"Device thermal state: {snapshot.TemperatureState}");

                Analytics.AnalyticsManager.Instance?.TrackEvent("thermal_warning", new Dictionary<string, object>
                {
                    { "temperature_state", snapshot.TemperatureState.ToString() },
                    { "fps", snapshot.FPS },
                    { "scene", snapshot.Scene }
                });
            }
        }

        private DeviceTemperatureState GetTemperatureState()
        {
            #if UNITY_IOS && !UNITY_EDITOR
            // iOS specific thermal state
            return (DeviceTemperatureState)UnityEngine.iOS.Device.GetTemperatureLevel();
            #else
            // Default to nominal for other platforms
            return DeviceTemperatureState.Nominal;
            #endif
        }

        #endregion

        #region Session Reporting

        private void ReportSessionMetrics()
        {
            float sessionDuration = Time.unscaledTime - sessionStartTime;

            var metrics = new SessionPerformanceMetrics
            {
                SessionDurationSeconds = Mathf.RoundToInt(sessionDuration),
                AverageFPS = Mathf.RoundToInt(averageFPS),
                MinFPS = Mathf.RoundToInt(minFPS),
                MaxFPS = Mathf.RoundToInt(maxFPS),
                PeakMemoryMB = (int)peakMemoryMB,
                FpsDropCount = fpsDropCount,
                FrameTimeSpikes = totalFrameTimeSpikes,
                ScenesLoaded = sceneLoadTimes.Count
            };

            // Log to analytics
            Analytics.AnalyticsManager.Instance?.TrackEvent("session_performance", new Dictionary<string, object>
            {
                { "duration_seconds", metrics.SessionDurationSeconds },
                { "avg_fps", metrics.AverageFPS },
                { "min_fps", metrics.MinFPS },
                { "peak_memory_mb", metrics.PeakMemoryMB },
                { "fps_drops", metrics.FpsDropCount }
            });

            LogInfo($"Session performance: Avg {metrics.AverageFPS} FPS, Peak {metrics.PeakMemoryMB} MB, {metrics.FpsDropCount} drops");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get current FPS
        /// </summary>
        public float GetCurrentFPS() => currentFPS;

        /// <summary>
        /// Get average FPS
        /// </summary>
        public float GetAverageFPS() => averageFPS;

        /// <summary>
        /// Get current memory usage in MB
        /// </summary>
        public long GetCurrentMemoryMB() => currentMemoryMB;

        /// <summary>
        /// Get peak memory usage in MB
        /// </summary>
        public long GetPeakMemoryMB() => peakMemoryMB;

        /// <summary>
        /// Get scene load time
        /// </summary>
        public float GetSceneLoadTime(string sceneName)
        {
            return sceneLoadTimes.TryGetValue(sceneName, out float time) ? time : -1f;
        }

        /// <summary>
        /// Get complete performance report
        /// </summary>
        public PerformanceReport GetPerformanceReport()
        {
            return new PerformanceReport
            {
                CurrentFPS = Mathf.RoundToInt(currentFPS),
                AverageFPS = Mathf.RoundToInt(averageFPS),
                MinFPS = Mathf.RoundToInt(minFPS),
                MaxFPS = Mathf.RoundToInt(maxFPS),
                CurrentMemoryMB = (int)currentMemoryMB,
                PeakMemoryMB = (int)peakMemoryMB,
                FpsDropCount = fpsDropCount,
                TargetFPS = targetFrameRate,
                DeviceModel = SystemInfo.deviceModel,
                OSVersion = SystemInfo.operatingSystem,
                AppVersion = Application.version,
                SceneLoadTimes = new Dictionary<string, float>(sceneLoadTimes)
            };
        }

        /// <summary>
        /// Enable/disable monitoring
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            monitoringEnabled = enabled;
            LogInfo($"Performance monitoring {(enabled ? "enabled" : "disabled")}");
        }

        /// <summary>
        /// Reset performance stats
        /// </summary>
        public void ResetStats()
        {
            minFPS = float.MaxValue;
            maxFPS = 0f;
            peakMemoryMB = 0;
            fpsDropCount = 0;
            fpsSamples.Clear();

            LogInfo("Performance stats reset");
        }

        #endregion

        #region Logging

        private void LogInfo(string message)
        {
            Debug.Log($"[PerformanceMonitor] {message}");
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[PerformanceMonitor] {message}");
        }

        #endregion

        #region Data Classes

        [System.Serializable]
        public class PerformanceSnapshot
        {
            public string Timestamp;
            public string Scene;
            public int FPS;
            public int AverageFPS;
            public int MemoryMB;
            public int PeakMemoryMB;
            public string DeviceModel;
            public float BatteryLevel;
            public DeviceTemperatureState TemperatureState;
        }

        [System.Serializable]
        public class PerformanceReport
        {
            public int CurrentFPS;
            public int AverageFPS;
            public int MinFPS;
            public int MaxFPS;
            public int CurrentMemoryMB;
            public int PeakMemoryMB;
            public int FpsDropCount;
            public int TargetFPS;
            public string DeviceModel;
            public string OSVersion;
            public string AppVersion;
            public Dictionary<string, float> SceneLoadTimes;
        }

        [System.Serializable]
        public class SessionPerformanceMetrics
        {
            public int SessionDurationSeconds;
            public int AverageFPS;
            public int MinFPS;
            public int MaxFPS;
            public int PeakMemoryMB;
            public int FpsDropCount;
            public int FrameTimeSpikes;
            public int ScenesLoaded;
        }

        public enum DeviceTemperatureState
        {
            Unknown = -1,
            Nominal = 0,
            Fair = 1,
            Serious = 2,
            Critical = 3,
            Throttling = 2,  // Alias for Serious
            ShutdownImminent = 3 // Alias for Critical
        }

        #endregion
    }
}
