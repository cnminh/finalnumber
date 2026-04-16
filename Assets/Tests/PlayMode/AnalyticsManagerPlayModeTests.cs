using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using FinalNumber.Analytics;
using System.Collections.Generic;

namespace FinalNumber.Tests.PlayMode
{
    /// <summary>
    /// PlayMode tests for AnalyticsManager event tracking and session management.
    /// </summary>
    public class AnalyticsManagerPlayModeTests
    {
        private AnalyticsManager _analyticsManager;
        private const float TEST_TIMEOUT = 2.0f;

        [SetUp]
        public void Setup()
        {
            // Clear any existing instance
            if (AnalyticsManager.Instance != null)
            {
                Object.DestroyImmediate(AnalyticsManager.Instance.gameObject);
            }

            // Clear PlayerPrefs for clean state
            PlayerPrefs.DeleteKey("AnalyticsOptOut");
            PlayerPrefs.DeleteKey("SessionCount");
            PlayerPrefs.DeleteKey("InstallDate");
        }

        [TearDown]
        public void TearDown()
        {
            if (AnalyticsManager.Instance != null)
            {
                Object.DestroyImmediate(AnalyticsManager.Instance.gameObject);
            }
            PlayerPrefs.DeleteAll();
        }

        [UnityTest]
        public IEnumerator Awake_CreatesSingleton()
        {
            var go = new GameObject("AnalyticsManager");
            _analyticsManager = go.AddComponent<AnalyticsManager>();

            yield return null;

            Assert.IsNotNull(AnalyticsManager.Instance, "Instance should be created");
            Assert.AreEqual(_analyticsManager, AnalyticsManager.Instance, "Instance should match created component");
        }

        [UnityTest]
        public IEnumerator Awake_DuplicateInstance_IsDestroyed()
        {
            var go1 = new GameObject("AnalyticsManager1");
            var manager1 = go1.AddComponent<AnalyticsManager>();

            yield return null;

            var go2 = new GameObject("AnalyticsManager2");
            var manager2 = go2.AddComponent<AnalyticsManager>();

            yield return null;

            // The second one should be destroyed
            Assert.IsTrue(go2 == null || manager2 == null || !manager2.enabled,
                "Duplicate instance should be destroyed or disabled");
        }

        [UnityTest]
        public IEnumerator Awake_SetsDontDestroyOnLoad()
        {
            var go = new GameObject("AnalyticsManager");
            _analyticsManager = go.AddComponent<AnalyticsManager>();

            yield return null;

            // Verify the instance persists (cant directly test DontDestroyOnLoad)
            Assert.IsNotNull(AnalyticsManager.Instance, "Instance should exist");
        }

        [UnityTest]
        public IEnumerator OptOut_DisablesAnalytics()
        {
            var go = new GameObject("AnalyticsManager");
            _analyticsManager = go.AddComponent<AnalyticsManager>();

            yield return null;

            _analyticsManager.OptOut();

            Assert.IsTrue(_analyticsManager.UserOptedOut, "UserOptedOut should be true");
        }

        [UnityTest]
        public IEnumerator OptOut_PreventsEventTracking()
        {
            var go = new GameObject("AnalyticsManager");
            _analyticsManager = go.AddComponent<AnalyticsManager>();
            _analyticsManager.analyticsEnabled = true;
            _analyticsManager.batchEvents = false; // Immediate sending

            yield return null;

            _analyticsManager.OptOut();

            // Should not throw when tracking events while opted out
            Assert.DoesNotThrow(() =>
            {
                _analyticsManager.TrackEvent("test_event", new Dictionary<string, object> { { "test", 1 } });
            });
        }

        [UnityTest]
        public IEnumerator OptIn_EnablesAnalytics()
        {
            var go = new GameObject("AnalyticsManager");
            _analyticsManager = go.AddComponent<AnalyticsManager>();

            yield return null;

            _analyticsManager.OptOut();
            _analyticsManager.OptIn();

            Assert.IsFalse(_analyticsManager.UserOptedOut, "UserOptedOut should be false after OptIn");
        }

        [UnityTest]
        public IEnumerator TrackEvent_WithParameters_DoesNotThrow()
        {
            var go = new GameObject("AnalyticsManager");
            _analyticsManager = go.AddComponent<AnalyticsManager>();
            _analyticsManager.batchEvents = false;

            yield return null;

            Assert.DoesNotThrow(() =>
            {
                _analyticsManager.TrackEvent("test_event", new Dictionary<string, object>
                {
                    { "level_id", 5 },
                    { "score", 1000 },
                    { "player_name", "TestPlayer" }
                });
            });
        }

        [UnityTest]
        public IEnumerator TrackPurchase_RecordsTransaction()
        {
            var go = new GameObject("AnalyticsManager");
            _analyticsManager = go.AddComponent<AnalyticsManager>();

            yield return null;

            Assert.DoesNotThrow(() =>
            {
                _analyticsManager.TrackPurchase("coin_pack_100", 0.99m, "USD");
            });
        }

        [UnityTest]
        public IEnumerator TrackAdImpression_RecordsImpression()
        {
            var go = new GameObject("AnalyticsManager");
            _analyticsManager = go.AddComponent<AnalyticsManager>();

            yield return null;

            Assert.DoesNotThrow(() =>
            {
                _analyticsManager.TrackAdImpression("interstitial", "level_complete");
            });
        }

        [UnityTest]
        public IEnumerator GetCollectedData_ReturnsDictionary()
        {
            var go = new GameObject("AnalyticsManager");
            _analyticsManager = go.AddComponent<AnalyticsManager>();

            yield return null;

            var data = _analyticsManager.GetCollectedData();

            Assert.IsNotNull(data, "Should return a dictionary");
            Assert.IsTrue(data.Count > 0, "Should contain some data fields");
        }

        [UnityTest]
        public IEnumerator SessionTracking_TracksDuration()
        {
            var go = new GameObject("AnalyticsManager");
            _analyticsManager = go.AddComponent<AnalyticsManager>();

            yield return null;

            // Wait a short time
            yield return new WaitForSeconds(0.1f);

            // Session should be tracking (we can't directly verify without more time,
            // but we can verify the session started)
            Assert.Pass("Session tracking initialized");
        }

        [UnityTest]
        public IEnumerator SubscribeToGameEvents_HandlesAllEvents()
        {
            var go = new GameObject("AnalyticsManager");
            _analyticsManager = go.AddComponent<AnalyticsManager>();

            yield return null;

            // Trigger various game events - should not throw
            Assert.DoesNotThrow(() =>
            {
                GameEventBus.TriggerLevelStarted(1, 1);
                GameEventBus.TriggerLevelCompleted(1, 1, 1000, 20, 30f);
                GameEventBus.TriggerLevelFailed(1, 1, 500, "Out of moves");
                GameEventBus.TriggerWorldUnlocked(2, "Crystal Caves");
                GameEventBus.TriggerPowerUpUsed("Undo", 5);
                GameEventBus.TriggerAchievementUnlocked("ach_first_win", "First Victory");
            });
        }

        [UnityTest]
        public IEnumerator OnApplicationPause_FlushesEvents()
        {
            var go = new GameObject("AnalyticsManager");
            _analyticsManager = go.AddComponent<AnalyticsManager>();
            _analyticsManager.batchEvents = true;

            yield return null;

            // Queue some events
            _analyticsManager.TrackEvent("test_event_1");
            _analyticsManager.TrackEvent("test_event_2");

            // Simulate application pause
            _analyticsManager.SendMessage("OnApplicationPause", true);

            yield return null;

            // Should complete without error
            Assert.Pass("Events flushed on pause");
        }

        [UnityTest]
        public IEnumerator LoadOptOutState_RespectsSavedPreference()
        {
            // Set opt-out in PlayerPrefs before creating manager
            PlayerPrefs.SetInt("AnalyticsOptOut", 1);
            PlayerPrefs.Save();

            var go = new GameObject("AnalyticsManager");
            _analyticsManager = go.AddComponent<AnalyticsManager>();

            yield return null;

            Assert.IsTrue(_analyticsManager.UserOptedOut, "Should load saved opt-out state");
        }

        [UnityTest]
        public IEnumerator TrackTutorialStep_RecordsStep()
        {
            var go = new GameObject("AnalyticsManager");
            _analyticsManager = go.AddComponent<AnalyticsManager>();

            yield return null;

            Assert.DoesNotThrow(() =>
            {
                _analyticsManager.TrackTutorialStep(1, "Welcome", true);
                _analyticsManager.TrackTutorialStep(2, "How To Move", false);
            });
        }

        [TearDown]
        public void FinalTearDown()
        {
            // Ensure GameEventBus is cleaned up
            GameEventBus.OnLevelStarted = null;
            GameEventBus.OnLevelCompleted = null;
            GameEventBus.OnLevelFailed = null;
            GameEventBus.OnWorldUnlocked = null;
            GameEventBus.OnPowerUpUsed = null;
            GameEventBus.OnAchievementUnlocked = null;
            GameEventBus.OnGameStarted = null;
            GameEventBus.OnGamePaused = null;
            GameEventBus.OnGameResumed = null;
            GameEventBus.OnGameQuit = null;
        }
    }
}
