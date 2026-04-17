using NUnit.Framework;
using System;

namespace FinalNumber.Tests.EditMode
{
    /// <summary>
    /// Unit tests for the GameEventBus system.
    /// Tests event subscription, triggering, and removal.
    /// </summary>
    public class GameEventBusTests
    {
        private int _levelStartedCount;
        private int _levelCompletedCount;
        private int _gameStartedCount;
        private int _lastLevelId;
        private int _lastWorldId;
        private int _lastScore;

        [SetUp]
        public void Setup()
        {
            _levelStartedCount = 0;
            _levelCompletedCount = 0;
            _gameStartedCount = 0;
            _lastLevelId = 0;
            _lastWorldId = 0;
            _lastScore = 0;
        }

        #region Level Event Tests

        [Test]
        public void TriggerLevelStarted_NoSubscribers_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => GameEventBus.TriggerLevelStarted(1, 1));
        }

        [Test]
        public void TriggerLevelStarted_WithSubscriber_CallsSubscriber()
        {
            GameEventBus.OnLevelStarted += OnLevelStartedHandler;
            
            GameEventBus.TriggerLevelStarted(5, 2);
            
            Assert.AreEqual(1, _levelStartedCount);
            Assert.AreEqual(5, _lastLevelId);
            Assert.AreEqual(2, _lastWorldId);
        }

        [Test]
        public void TriggerLevelStarted_MultipleSubscribers_CallsAll()
        {
            int count1 = 0;
            int count2 = 0;
            
            GameEventBus.OnLevelStarted += (levelId, worldId) => count1++;
            GameEventBus.OnLevelStarted += (levelId, worldId) => count2++;
            
            GameEventBus.TriggerLevelStarted(1, 1);
            
            Assert.AreEqual(1, count1);
            Assert.AreEqual(1, count2);
        }

        [Test]
        public void TriggerLevelStarted_AfterUnsubscribe_DoesNotCall()
        {
            GameEventBus.OnLevelStarted += OnLevelStartedHandler;
            GameEventBus.OnLevelStarted -= OnLevelStartedHandler;
            
            GameEventBus.TriggerLevelStarted(1, 1);
            
            Assert.AreEqual(0, _levelStartedCount);
        }

        [Test]
        public void TriggerLevelCompleted_WithSubscriber_PassesCorrectParameters()
        {
            GameEventBus.OnLevelCompleted += OnLevelCompletedHandler;
            
            GameEventBus.TriggerLevelCompleted(3, 1, 1500, 25, 45.5f);
            
            Assert.AreEqual(1, _levelCompletedCount);
        }

        [Test]
        public void TriggerLevelCompleted_MultipleTimes_CountsCorrectly()
        {
            GameEventBus.OnLevelCompleted += OnLevelCompletedHandler;
            
            GameEventBus.TriggerLevelCompleted(1, 1, 100, 10, 10f);
            GameEventBus.TriggerLevelCompleted(2, 1, 200, 15, 20f);
            GameEventBus.TriggerLevelCompleted(3, 1, 300, 20, 30f);
            
            Assert.AreEqual(3, _levelCompletedCount);
        }

        [Test]
        public void TriggerLevelFailed_WithReason_PassesCorrectly()
        {
            string receivedReason = "";
            GameEventBus.OnLevelFailed += (levelId, worldId, score, reason) => receivedReason = reason;
            
            GameEventBus.TriggerLevelFailed(5, 2, 1000, "Out of moves");
            
            Assert.AreEqual("Out of moves", receivedReason);
        }

        #endregion

        #region Game State Event Tests

        [Test]
        public void TriggerGameStarted_NoSubscribers_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => GameEventBus.TriggerGameStarted());
        }

        [Test]
        public void TriggerGameStarted_WithSubscriber_CallsSubscriber()
        {
            GameEventBus.OnGameStarted += OnGameStartedHandler;
            
            GameEventBus.TriggerGameStarted();
            
            Assert.AreEqual(1, _gameStartedCount);
        }

        [Test]
        public void TriggerGamePaused_WithSubscriber_CallsSubscriber()
        {
            bool called = false;
            GameEventBus.OnGamePaused += () => called = true;
            
            GameEventBus.TriggerGamePaused();
            
            Assert.IsTrue(called);
        }

        [Test]
        public void TriggerGameResumed_WithSubscriber_CallsSubscriber()
        {
            bool called = false;
            GameEventBus.OnGameResumed += () => called = true;
            
            GameEventBus.TriggerGameResumed();
            
            Assert.IsTrue(called);
        }

        [Test]
        public void TriggerGameQuit_WithSubscriber_CallsSubscriber()
        {
            bool called = false;
            GameEventBus.OnGameQuit += () => called = true;
            
            GameEventBus.TriggerGameQuit();
            
            Assert.IsTrue(called);
        }

        #endregion

        #region Progression Event Tests

        [Test]
        public void TriggerWorldUnlocked_PassesCorrectWorldInfo()
        {
            int receivedWorldId = 0;
            string receivedWorldName = "";
            
            GameEventBus.OnWorldUnlocked += (worldId, worldName) =>
            {
                receivedWorldId = worldId;
                receivedWorldName = worldName;
            };
            
            GameEventBus.TriggerWorldUnlocked(3, "Crystal Caves");
            
            Assert.AreEqual(3, receivedWorldId);
            Assert.AreEqual("Crystal Caves", receivedWorldName);
        }

        [Test]
        public void TriggerPowerUpUsed_PassesCorrectPowerUpType()
        {
            string receivedType = "";
            int receivedLevelId = 0;
            
            GameEventBus.OnPowerUpUsed += (powerUpType, levelId) =>
            {
                receivedType = powerUpType;
                receivedLevelId = levelId;
            };
            
            GameEventBus.TriggerPowerUpUsed("Undo", 15);
            
            Assert.AreEqual("Undo", receivedType);
            Assert.AreEqual(15, receivedLevelId);
        }

        [Test]
        public void TriggerAchievementUnlocked_PassesCorrectAchievement()
        {
            string receivedId = "";
            string receivedName = "";
            
            GameEventBus.OnAchievementUnlocked += (achievementId, achievementName) =>
            {
                receivedId = achievementId;
                receivedName = achievementName;
            };
            
            GameEventBus.TriggerAchievementUnlocked("ach_first_win", "First Victory");
            
            Assert.AreEqual("ach_first_win", receivedId);
            Assert.AreEqual("First Victory", receivedName);
        }

        #endregion

        #region UI Event Tests

        [Test]
        public void TriggerScreenOpened_PassesScreenName()
        {
            string receivedScreen = "";
            GameEventBus.OnScreenOpened += screenName => receivedScreen = screenName;
            
            GameEventBus.TriggerScreenOpened("LevelSelect");
            
            Assert.AreEqual("LevelSelect", receivedScreen);
        }

        [Test]
        public void TriggerScreenClosed_PassesScreenName()
        {
            string receivedScreen = "";
            GameEventBus.OnScreenClosed += screenName => receivedScreen = screenName;
            
            GameEventBus.TriggerScreenClosed("MainMenu");
            
            Assert.AreEqual("MainMenu", receivedScreen);
        }

        #endregion

        #region Edge Case Tests

        [Test]
        public void MultipleEvents_TriggeredIndependently()
        {
            bool levelStarted = false;
            bool gameStarted = false;
            bool screenOpened = false;
            
            GameEventBus.OnLevelStarted += (id, world) => levelStarted = true;
            GameEventBus.OnGameStarted += () => gameStarted = true;
            GameEventBus.OnScreenOpened += name => screenOpened = true;
            
            GameEventBus.TriggerGameStarted();
            
            Assert.IsFalse(levelStarted); // Should not be triggered
            Assert.IsTrue(gameStarted);
            Assert.IsFalse(screenOpened); // Should not be triggered
        }

        [Test]
        public void EventHandlers_CanBeAddedAndRemovedMultipleTimes()
        {
            int count = 0;
            Action<int, int> handler = (id, world) => count++;
            
            GameEventBus.OnLevelStarted += handler;
            GameEventBus.OnLevelStarted += handler; // Added twice
            
            GameEventBus.TriggerLevelStarted(1, 1);
            
            Assert.AreEqual(2, count); // Called twice
            
            GameEventBus.OnLevelStarted -= handler;
            GameEventBus.TriggerLevelStarted(1, 1);
            
            Assert.AreEqual(3, count); // One handler removed, one remains
            
            GameEventBus.OnLevelStarted -= handler;
            GameEventBus.TriggerLevelStarted(1, 1);
            
            Assert.AreEqual(3, count); // Both removed, no more calls
        }

        [Test]
        public void TriggerEvents_WithZeroOrNegativeIds_Works()
        {
            int receivedLevelId = -1;
            int receivedWorldId = -1;
            
            GameEventBus.OnLevelStarted += (levelId, worldId) =>
            {
                receivedLevelId = levelId;
                receivedWorldId = worldId;
            };
            
            GameEventBus.TriggerLevelStarted(0, 0);
            
            Assert.AreEqual(0, receivedLevelId);
            Assert.AreEqual(0, receivedWorldId);
        }

        [Test]
        public void TriggerLevelCompleted_WithZeroScore_Works()
        {
            int receivedScore = -1;
            GameEventBus.OnLevelCompleted += (levelId, worldId, score, moves, time) => receivedScore = score;
            
            GameEventBus.TriggerLevelCompleted(1, 1, 0, 0, 0f);
            
            Assert.AreEqual(0, receivedScore);
        }

        [Test]
        public void TriggerLevelCompleted_WithMaxIntScore_Works()
        {
            int receivedScore = 0;
            GameEventBus.OnLevelCompleted += (levelId, worldId, score, moves, time) => receivedScore = score;
            
            GameEventBus.TriggerLevelCompleted(1, 1, int.MaxValue, 0, 0f);
            
            Assert.AreEqual(int.MaxValue, receivedScore);
        }

        #endregion

        #region Handler Methods

        private void OnLevelStartedHandler(int levelId, int worldId)
        {
            _levelStartedCount++;
            _lastLevelId = levelId;
            _lastWorldId = worldId;
        }

        private void OnLevelCompletedHandler(int levelId, int worldId, int score, int moves, float timeSeconds)
        {
            _levelCompletedCount++;
            _lastLevelId = levelId;
            _lastWorldId = worldId;
            _lastScore = score;
        }

        private void OnGameStartedHandler()
        {
            _gameStartedCount++;
        }

        #endregion
    }
}
