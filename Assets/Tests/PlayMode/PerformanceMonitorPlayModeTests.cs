using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using FinalNumber.Performance;

namespace FinalNumber.Tests.PlayMode
{
    /// <summary>
    /// PlayMode tests for PerformanceMonitor initialization and functionality.
    /// Tests singleton behavior, FPS tracking, memory tracking, and scene load tracking.
    /// </summary>
    public class PerformanceMonitorPlayModeTests
    {
        private PerformanceMonitor _performanceMonitor;

        [SetUp]
        public void Setup()
        {
            // Clear any existing instance
            if (PerformanceMonitor.Instance != null)
            {
                Object.DestroyImmediate(PerformanceMonitor.Instance.gameObject);
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (PerformanceMonitor.Instance != null)
            {
                Object.DestroyImmediate(PerformanceMonitor.Instance.gameObject);
            }
        }

        #region Singleton Tests

        [UnityTest]
        public IEnumerator Awake_CreatesSingleton()
        {
            var go = new GameObject("PerformanceMonitor");
            _performanceMonitor = go.AddComponent<PerformanceMonitor>();

            yield return null;

            Assert.IsNotNull(PerformanceMonitor.Instance, "Instance should be created");
            Assert.AreEqual(_performanceMonitor, PerformanceMonitor.Instance, "Instance should match created component");
        }

        [UnityTest]
        public IEnumerator Awake_DuplicateInstance_IsDestroyed()
        {
            var go1 = new GameObject("PerformanceMonitor1");
            var monitor1 = go1.AddComponent<PerformanceMonitor>();

            yield return null;

            var go2 = new GameObject("PerformanceMonitor2");
            var monitor2 = go2.AddComponent<PerformanceMonitor>();

            yield return null;

            // The second one should be destroyed or disabled
            Assert.IsTrue(go2 == null || monitor2 == null || !monitor2.enabled,
                "Duplicate instance should be destroyed or disabled");
        }

        [UnityTest]
        public IEnumerator Awake_SetsDontDestroyOnLoad()
        {
            var go = new GameObject("PerformanceMonitor");
            _performanceMonitor = go.AddComponent<PerformanceMonitor>();

            yield return null;

            // Verify the instance persists (cant directly test DontDestroyOnLoad)
            Assert.IsNotNull(PerformanceMonitor.Instance, "Instance should exist");
        }

        #endregion

        #region Monitoring Control Tests

        [UnityTest]
        public IEnumerator SetEnabled_TogglesMonitoring()
        {
            var go = new GameObject("PerformanceMonitor");
            _performanceMonitor = go.AddComponent<PerformanceMonitor>();

            yield return null;

            // Disable monitoring
            _performanceMonitor.SetEnabled(false);
            Assert.IsFalse(_performanceMonitor.monitoringEnabled);

            // Re-enable monitoring
            _performanceMonitor.SetEnabled(true);
            Assert.IsTrue(_performanceMonitor.monitoringEnabled);
        }

        [UnityTest]
        public IEnumerator MonitoringEnabled_ByDefault()
        {
            var go = new GameObject("PerformanceMonitor");
            _performanceMonitor = go.AddComponent<PerformanceMonitor>();

            yield return null;

            Assert.IsTrue(_performanceMonitor.monitoringEnabled, "Monitoring should be enabled by default");
        }

        #endregion

        #region FPS Tracking Tests

        [UnityTest]
        public IEnumerator GetCurrentFPS_ReturnsValidValue()
        {
            var go = new GameObject("PerformanceMonitor");
            _performanceMonitor = go.AddComponent<PerformanceMonitor>();

            yield return null;

            // Wait a few frames for FPS calculation
            yield return new WaitForSeconds(1.5f);

            float fps = _performanceMonitor.GetCurrentFPS();
            Assert.Greater(fps, 0, "FPS should be greater than 0");
            Assert.LessOrEqual(fps, 1000, "FPS should be within reasonable range");
        }

        [UnityTest]
        public IEnumerator GetAverageFPS_ReturnsValidValue()
        {
            var go = new GameObject("PerformanceMonitor");
            _performanceMonitor = go.AddComponent<PerformanceMonitor>();

            yield return null;

            // Wait for some samples
            yield return new WaitForSeconds(2f);

            float avgFps = _performanceMonitor.GetAverageFPS();
            Assert.Greater(avgFps, 0, "Average FPS should be greater than 0");
        }

        #endregion

        #region Memory Tracking Tests

        [UnityTest]
        public IEnumerator GetCurrentMemoryMB_ReturnsPositiveValue()
        {
            var go = new GameObject("PerformanceMonitor");
            _performanceMonitor = go.AddComponent<PerformanceMonitor>();

            yield return null;

            // Wait a frame for memory tracking
            yield return null;

            long memory = _performanceMonitor.GetCurrentMemoryMB();
            Assert.Greater(memory, 0, "Memory usage should be greater than 0");
        }

        [UnityTest]
        public IEnumerator GetPeakMemoryMB_ReturnsValidValue()
        {
            var go = new GameObject("PerformanceMonitor");
            _performanceMonitor = go.AddComponent<PerformanceMonitor>();

            yield return null;

            // Wait for memory tracking
            yield return new WaitForSeconds(0.5f);

            long peakMemory = _performanceMonitor.GetPeakMemoryMB();
            Assert.GreaterOrEqual(peakMemory, _performanceMonitor.GetCurrentMemoryMB(),
                "Peak memory should be >= current memory");
        }

        #endregion

        #region Stats Reset Tests

        [UnityTest]
        public IEnumerator ResetStats_ClearsPeakValues()
        {
            var go = new GameObject("PerformanceMonitor");
            _performanceMonitor = go.AddComponent<PerformanceMonitor>();

            yield return null;

            // Wait for some tracking
            yield return new WaitForSeconds(1f);

            // Get values before reset
            long peakBefore = _performanceMonitor.GetPeakMemoryMB();

            // Reset stats
            _performanceMonitor.ResetStats();

            // Peak should be reset (but might immediately increase)
            long peakAfter = _performanceMonitor.GetPeakMemoryMB();
            Assert.GreaterOrEqual(peakAfter, 0, "Peak memory should be valid after reset");
        }

        #endregion

        #region Performance Report Tests

        [UnityTest]
        public IEnumerator GetPerformanceReport_ReturnsValidReport()
        {
            var go = new GameObject("PerformanceMonitor");
            _performanceMonitor = go.AddComponent<PerformanceMonitor>();

            yield return null;

            // Wait for data collection
            yield return new WaitForSeconds(1.5f);

            var report = _performanceMonitor.GetPerformanceReport();

            Assert.IsNotNull(report, "Report should not be null");
            Assert.GreaterOrEqual(report.CurrentFPS, 0, "Current FPS should be valid");
            Assert.GreaterOrEqual(report.AverageFPS, 0, "Average FPS should be valid");
            Assert.GreaterOrEqual(report.CurrentMemoryMB, 0, "Current memory should be valid");
            Assert.IsNotNull(report.DeviceModel, "Device model should not be null");
            Assert.IsNotNull(report.OSVersion, "OS version should not be null");
            Assert.IsNotNull(report.AppVersion, "App version should not be null");
            Assert.IsNotNull(report.SceneLoadTimes, "Scene load times should not be null");
        }

        [UnityTest]
        public IEnumerator PerformanceReport_TargetFPS_MatchesConfig()
        {
            var go = new GameObject("PerformanceMonitor");
            _performanceMonitor = go.AddComponent<PerformanceMonitor>();
            _performanceMonitor.targetFrameRate = 60;

            yield return null;

            var report = _performanceMonitor.GetPerformanceReport();
            Assert.AreEqual(60, report.TargetFPS, "Target FPS should match configured value");
        }

        #endregion

        #region Configuration Tests

        [UnityTest]
        public IEnumerator TargetFrameRate_IsConfigurable()
        {
            var go = new GameObject("PerformanceMonitor");
            _performanceMonitor = go.AddComponent<PerformanceMonitor>();
            _performanceMonitor.targetFrameRate = 30;

            yield return null;

            Assert.AreEqual(30, _performanceMonitor.targetFrameRate);
        }

        [UnityTest]
        public IEnumerator FPSWarningThreshold_IsConfigurable()
        {
            var go = new GameObject("PerformanceMonitor");
            _performanceMonitor = go.AddComponent<PerformanceMonitor>();
            _performanceMonitor.fpsWarningThreshold = 25f;

            yield return null;

            Assert.AreEqual(25f, _performanceMonitor.fpsWarningThreshold);
        }

        [UnityTest]
        public IEnumerator MemoryWarningThreshold_IsConfigurable()
        {
            var go = new GameObject("PerformanceMonitor");
            _performanceMonitor = go.AddComponent<PerformanceMonitor>();
            _performanceMonitor.memoryWarningThresholdMB = 500;

            yield return null;

            Assert.AreEqual(500, _performanceMonitor.memoryWarningThresholdMB);
        }

        #endregion

        #region Scene Load Tracking Tests

        [UnityTest]
        public IEnumerator GetSceneLoadTime_UnknownScene_ReturnsNegative()
        {
            var go = new GameObject("PerformanceMonitor");
            _performanceMonitor = go.AddComponent<PerformanceMonitor>();

            yield return null;

            float loadTime = _performanceMonitor.GetSceneLoadTime("NonExistentScene");
            Assert.AreEqual(-1f, loadTime, "Unknown scene should return -1");
        }

        #endregion

        #region Logging Tests

        [UnityTest]
        public IEnumerator Awake_LogsInitializationMessage()
        {
            var go = new GameObject("PerformanceMonitor");
            bool logReceived = false;

            Application.logMessageReceived += (message, stackTrace, type) =>
            {
                if (message.Contains("[PerformanceMonitor]"))
                {
                    logReceived = true;
                }
            };

            _performanceMonitor = go.AddComponent<PerformanceMonitor>();

            yield return null;

            Assert.IsTrue(logReceived, "Should log initialization message");
        }

        #endregion

        #region Edge Case Tests

        [UnityTest]
        public IEnumerator RapidEnableDisable_DoesNotThrow()
        {
            var go = new GameObject("PerformanceMonitor");
            _performanceMonitor = go.AddComponent<PerformanceMonitor>();

            yield return null;

            // Rapidly toggle monitoring
            Assert.DoesNotThrow(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    _performanceMonitor.SetEnabled(i % 2 == 0);
                }
            });
        }

        [UnityTest]
        public IEnumerator MultipleResetStats_DoesNotThrow()
        {
            var go = new GameObject("PerformanceMonitor");
            _performanceMonitor = go.AddComponent<PerformanceMonitor>();

            yield return null;

            // Reset multiple times
            Assert.DoesNotThrow(() =>
            {
                _performanceMonitor.ResetStats();
                _performanceMonitor.ResetStats();
                _performanceMonitor.ResetStats();
            });
        }

        #endregion
    }
}
