using System;

namespace FinalNumber
{
    /// <summary>
    /// Global event bus for decoupled communication between game systems.
    /// Used by AnalyticsManager, CrashReporter, and PerformanceMonitor.
    /// </summary>
    public static class GameEventBus
    {
        // Level Events
        public static event Action<int, int> OnLevelStarted; // levelId, worldId
        public static event Action<int, int, int, int, float> OnLevelCompleted; // levelId, worldId, score, moves, timeSeconds
        public static event Action<int, int, int, string> OnLevelFailed; // levelId, worldId, score, reason

        // Progression Events
        public static event Action<int, string> OnWorldUnlocked; // worldId, worldName
        public static event Action<string, int> OnPowerUpUsed; // powerUpType, levelId
        public static event Action<string, string> OnAchievementUnlocked; // achievementId, achievementName

        // Game State Events
        public static event Action OnGameStarted;
        public static event Action OnGamePaused;
        public static event Action OnGameResumed;
        public static event Action OnGameQuit;

        // UI Events
        public static event Action<string> OnScreenOpened; // screenName
        public static event Action<string> OnScreenClosed; // screenName

        #region Event Triggers

        public static void TriggerLevelStarted(int levelId, int worldId)
        {
            OnLevelStarted?.Invoke(levelId, worldId);
        }

        public static void TriggerLevelCompleted(int levelId, int worldId, int score, int moves, float timeSeconds)
        {
            OnLevelCompleted?.Invoke(levelId, worldId, score, moves, timeSeconds);
        }

        public static void TriggerLevelFailed(int levelId, int worldId, int score, string reason)
        {
            OnLevelFailed?.Invoke(levelId, worldId, score, reason);
        }

        public static void TriggerWorldUnlocked(int worldId, string worldName)
        {
            OnWorldUnlocked?.Invoke(worldId, worldName);
        }

        public static void TriggerPowerUpUsed(string powerUpType, int levelId)
        {
            OnPowerUpUsed?.Invoke(powerUpType, levelId);
        }

        public static void TriggerAchievementUnlocked(string achievementId, string achievementName)
        {
            OnAchievementUnlocked?.Invoke(achievementId, achievementName);
        }

        public static void TriggerGameStarted()
        {
            OnGameStarted?.Invoke();
        }

        public static void TriggerGamePaused()
        {
            OnGamePaused?.Invoke();
        }

        public static void TriggerGameResumed()
        {
            OnGameResumed?.Invoke();
        }

        public static void TriggerGameQuit()
        {
            OnGameQuit?.Invoke();
        }

        public static void TriggerScreenOpened(string screenName)
        {
            OnScreenOpened?.Invoke(screenName);
        }

        public static void TriggerScreenClosed(string screenName)
        {
            OnScreenClosed?.Invoke(screenName);
        }

        #endregion
    }
}
