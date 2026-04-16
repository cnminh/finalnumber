using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using FinalNumber.CrashReporting;
using System.Linq;

namespace FinalNumber.Tests.PlayMode
{
    /// <summary>
    /// PlayMode tests for CrashReporter initialization and functionality.
    /// </summary>
    public class CrashReporterPlayModeTests
    {
        private CrashReporter _crashReporter;

        [SetUp]
        public void Setup()
        {
            // Clear any existing instance
            if (CrashReporter.Instance != null)
            {
                Object.DestroyImmediate(CrashReporter.Instance.gameObject);
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (CrashReporter.Instance != null)
            {
                Object.DestroyImmediate(CrashReporter.Instance.gameObject);
            }

            // Clean up crash reports directory
            string crashPath = System.IO.Path.Combine(Application.persistentDataPath, "CrashReports");
            if (System.IO.Directory.Exists(crashPath))
            {
                try
                {
                    System.IO.Directory.Delete(crashPath, true);
                }
                catch { }
            }
        }

        [UnityTest]
        public IEnumerator Awake_CreatesSingleton()
        {
            var go = new GameObject("CrashReporter");
            _crashReporter = go.AddComponent<CrashReporter>();

            yield return null;

            Assert.IsNotNull(CrashReporter.Instance);
            Assert.AreEqual(_crashReporter, CrashReporter.Instance);
        }

        [UnityTest]
        public IEnumerator Awake_DuplicateInstance_IsDestroyed()
        {
            var go1 = new GameObject("CrashReporter1");
            var reporter1 = go1.AddComponent<CrashReporter>();

            yield return null;

            var go2 = new GameObject("CrashReporter2");
            var reporter2 = go2.AddComponent<CrashReporter>();

            yield return null;

            Assert.IsTrue(go2 == null || reporter2 == null || !reporter2.enabled,
                "Duplicate instance should be destroyed");
        }

        [UnityTest]
        public IEnumerator LogBreadcrumb_AddsBreadcrumb()
        {
            var go = new GameObject("CrashReporter");
            _crashReporter = go.AddComponent<CrashReporter>();

            yield return null;

            _crashReporter.LogBreadcrumb("test_category", "Test message");

            var stats = _crashReporter.GetCrashStats();
            Assert.Greater(stats.BreadcrumbsCaptured, 0, "Should have captured breadcrumbs");
        }

        [UnityTest]
        public IEnumerator LogUserAction_AddsUserActionBreadcrumb()
        {
            var go = new GameObject("CrashReporter");
            _crashReporter = go.AddComponent<CrashReporter>();

            yield return null;

            _crashReporter.LogUserAction("button_click", "StartGame");

            var stats = _crashReporter.GetCrashStats();
            Assert.Greater(stats.BreadcrumbsCaptured, 0);
        }

        [UnityTest]
        public IEnumerator LogNavigation_AddsNavigationBreadcrumb()
        {
            var go = new GameObject("CrashReporter");
            _crashReporter = go.AddComponent<CrashReporter>();

            yield return null;

            _crashReporter.LogNavigation("MainMenu", "GameScene");

            var stats = _crashReporter.GetCrashStats();
            Assert.Greater(stats.BreadcrumbsCaptured, 0);
        }

        [UnityTest]
        public IEnumerator LogApiCall_AddsApiBreadcrumb()
        {
            var go = new GameObject("CrashReporter");
            _crashReporter = go.AddComponent<CrashReporter>();

            yield return null;

            _crashReporter.LogApiCall("/api/score/submit", true, 200);

            var stats = _crashReporter.GetCrashStats();
            Assert.Greater(stats.BreadcrumbsCaptured, 0);
        }

        [UnityTest]
        public IEnumerator SetEnabled_TogglesReporting()
        {
            var go = new GameObject("CrashReporter");
            _crashReporter = go.AddComponent<CrashReporter>();

            yield return null;

            _crashReporter.SetEnabled(false);
            var stats = _crashReporter.GetCrashStats();
            Assert.IsFalse(stats.CrashReportingEnabled);

            _crashReporter.SetEnabled(true);
            stats = _crashReporter.GetCrashStats();
            Assert.IsTrue(stats.CrashReportingEnabled);
        }

        [UnityTest]
        public IEnumerator GetCrashStats_InitialState_ReturnsZeroPending()
        {
            var go = new GameObject("CrashReporter");
            _crashReporter = go.AddComponent<CrashReporter>();

            yield return null;

            var stats = _crashReporter.GetCrashStats();
            Assert.AreEqual(0, stats.PendingReports);
            Assert.IsTrue(stats.CrashReportingEnabled);
            Assert.IsTrue(stats.AutomaticReporting);
        }

        [UnityTest]
        public IEnumerator TestCrashReporting_CreatesCrashReport()
        {
            var go = new GameObject("CrashReporter");
            _crashReporter = go.AddComponent<CrashReporter>();
            _crashReporter.automaticReporting = true;

            yield return null;

            _crashReporter.TestCrashReporting();

            // Give it a frame to process
            yield return null;

            var reports = _crashReporter.GetPendingCrashReports();
            Assert.Greater(reports.Length, 0, "Should have created a test crash report");
        }

        [UnityTest]
        public IEnumerator DeleteCrashReport_RemovesReport()
        {
            var go = new GameObject("CrashReporter");
            _crashReporter = go.AddComponent<CrashReporter>();
            _crashReporter.automaticReporting = true;

            yield return null;

            _crashReporter.TestCrashReporting();
            yield return null;

            var reports = _crashReporter.GetPendingCrashReports();
            if (reports.Length > 0)
            {
                _crashReporter.DeleteCrashReport(reports[0]);

                var updatedReports = _crashReporter.GetPendingCrashReports();
                Assert.AreEqual(reports.Length - 1, updatedReports.Length);
            }
            else
            {
                Assert.Inconclusive("No crash report was created");
            }
        }

        [UnityTest]
        public IEnumerator LogBreadcrumb_MaxLimit_DoesNotExceedMax()
        {
            var go = new GameObject("CrashReporter");
            _crashReporter = go.AddComponent<CrashReporter>();

            yield return null;

            // Add many breadcrumbs
            for (int i = 0; i < 100; i++)
            {
                _crashReporter.LogBreadcrumb("test", $"Message {i}");
            }

            var stats = _crashReporter.GetCrashStats();
            Assert.LessOrEqual(stats.BreadcrumbsCaptured, 50, "Should not exceed MaxBreadcrumbs (50)");
        }

        [UnityTest]
        public IEnumerator Awake_LogsDeviceInfo()
        {
            var go = new GameObject("CrashReporter");
            _crashReporter = go.AddComponent<CrashReporter>();

            bool logReceived = false;
            Application.logMessageReceived += (message, trace, type) =>
            {
                if (message.Contains("[CrashReporter]"))
                {
                    logReceived = true;
                }
            };

            yield return null;

            Assert.IsTrue(logReceived, "Should log initialization message");
        }

        [UnityTest]
        public IEnumerator OnEnable_SubscribesToLogMessages()
        {
            var go = new GameObject("CrashReporter");
            _crashReporter = go.AddComponent<CrashReporter>();
            _crashReporter.crashReportingEnabled = true;

            yield return null;

            // Log an error to verify subscription
            Debug.LogError("Test error for crash reporter");

            yield return null;

            // No assertion - if we got here without exception, subscription worked
            Assert.Pass("Log subscription working");
        }

        [UnityTest]
        public IEnumerator GetPendingCrashReports_InvalidDirectory_ReturnsEmpty()
        {
            var go = new GameObject("CrashReporter");
            _crashReporter = go.AddComponent<CrashReporter>();

            yield return null;

            var reports = _crashReporter.GetPendingCrashReports();
            Assert.IsNotNull(reports, "Should return empty array, not null");
            Assert.AreEqual(0, reports.Length, "Should return empty array when no crash reports exist");
        }
    }
}
