using System;
using UnityEngine;

namespace FinalNumber.Gameplay
{
    /// <summary>
    /// Manages score tracking, high scores, and move counting for the 2048 game.
    /// Persists high scores using PlayerPrefs.
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        [Header("Score Settings")]
        [Tooltip("Target score to achieve (win condition)")]
        public int targetScore = 2048;

        // Current game state
        public int CurrentScore { get; private set; }
        public int CurrentMoves { get; private set; }
        public float GameTime { get; private set; }
        public int HighestTileAchieved { get; private set; }
        public bool IsGameActive { get; private set; }

        // High scores (persisted)
        public int HighScore { get; private set; }
        public int BestTile { get; private set; }

        // Events
        public event EventHandler<int> OnScoreChanged;
        public event EventHandler<int> OnHighScoreChanged;
        public event EventHandler<int> OnMovesChanged;
        public event EventHandler<int> OnBestTileAchieved;
        public event EventHandler OnTargetScoreReached;

        // PlayerPrefs keys
        private const string HighScoreKey = "FinalNumber_HighScore";
        private const string BestTileKey = "FinalNumber_BestTile";

        private void Awake()
        {
            LoadHighScores();
        }

        private void Update()
        {
            if (IsGameActive)
            {
                GameTime += Time.deltaTime;
            }
        }

        /// <summary>
        /// Start a new game session
        /// </summary>
        public void StartNewGame()
        {
            CurrentScore = 0;
            CurrentMoves = 0;
            GameTime = 0f;
            HighestTileAchieved = 0;
            IsGameActive = true;

            OnScoreChanged?.Invoke(this, CurrentScore);
            OnMovesChanged?.Invoke(this, CurrentMoves);

            Debug.Log("[ScoreManager] New game started");
        }

        /// <summary>
        /// End the current game session
        /// </summary>
        public void EndGame()
        {
            IsGameActive = false;

            // Check and update high score
            if (CurrentScore > HighScore)
            {
                HighScore = CurrentScore;
                SaveHighScore();
                OnHighScoreChanged?.Invoke(this, HighScore);
                Debug.Log($"[ScoreManager] New high score: {HighScore}");
            }

            // Check and update best tile
            if (HighestTileAchieved > BestTile)
            {
                BestTile = HighestTileAchieved;
                SaveBestTile();
                OnBestTileAchieved?.Invoke(this, BestTile);
                Debug.Log($"[ScoreManager] New best tile: {BestTile}");
            }
        }

        /// <summary>
        /// Pause the game timer
        /// </summary>
        public void PauseGame()
        {
            IsGameActive = false;
        }

        /// <summary>
        /// Resume the game timer
        /// </summary>
        public void ResumeGame()
        {
            IsGameActive = true;
        }

        /// <summary>
        /// Add points to the current score
        /// </summary>
        public void AddScore(int points)
        {
            if (points <= 0)
                return;

            CurrentScore += points;
            OnScoreChanged?.Invoke(this, CurrentScore);

            // Check if target score reached
            if (CurrentScore >= targetScore && CurrentScore - points < targetScore)
            {
                OnTargetScoreReached?.Invoke(this, EventArgs.Empty);
            }

            Debug.Log($"[ScoreManager] Added {points} points. Total: {CurrentScore}");
        }

        /// <summary>
        /// Increment the move counter
        /// </summary>
        public void AddMove()
        {
            CurrentMoves++;
            OnMovesChanged?.Invoke(this, CurrentMoves);
        }

        /// <summary>
        /// Update the highest tile achieved
        /// </summary>
        public void UpdateHighestTile(int tileValue)
        {
            if (tileValue > HighestTileAchieved)
            {
                HighestTileAchieved = tileValue;
                Debug.Log($"[ScoreManager] New highest tile: {tileValue}");

                // Check if this is the best tile ever
                if (tileValue > BestTile)
                {
                    OnBestTileAchieved?.Invoke(this, tileValue);
                }
            }
        }

        /// <summary>
        /// Check if target score/tile has been reached
        /// </summary>
        public bool HasReachedTarget()
        {
            return HighestTileAchieved >= targetScore;
        }

        /// <summary>
        /// Get formatted game time as MM:SS
        /// </summary>
        public string GetFormattedTime()
        {
            int minutes = Mathf.FloorToInt(GameTime / 60f);
            int seconds = Mathf.FloorToInt(GameTime % 60f);
            return $"{minutes:00}:{seconds:00}";
        }

        /// <summary>
        /// Get the current rating based on score
        /// </summary>
        public string GetRating()
        {
            if (CurrentScore >= 50000) return "S";
            if (CurrentScore >= 30000) return "A";
            if (CurrentScore >= 15000) return "B";
            if (CurrentScore >= 5000) return "C";
            return "D";
        }

        #region Persistence

        private void LoadHighScores()
        {
            try
            {
                HighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
                BestTile = PlayerPrefs.GetInt(BestTileKey, 0);
                Debug.Log($"[ScoreManager] Loaded high scores - HighScore: {HighScore}, BestTile: {BestTile}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ScoreManager] Failed to load high scores: {e.Message}");
                HighScore = 0;
                BestTile = 0;
            }
        }

        private void SaveHighScore()
        {
            try
            {
                PlayerPrefs.SetInt(HighScoreKey, HighScore);
                PlayerPrefs.Save();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ScoreManager] Failed to save high score: {e.Message}");
            }
        }

        private void SaveBestTile()
        {
            try
            {
                PlayerPrefs.SetInt(BestTileKey, BestTile);
                PlayerPrefs.Save();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ScoreManager] Failed to save best tile: {e.Message}");
            }
        }

        /// <summary>
        /// Reset all high scores (for testing/debugging)
        /// </summary>
        public void ResetHighScores()
        {
            try
            {
                HighScore = 0;
                BestTile = 0;
                PlayerPrefs.DeleteKey(HighScoreKey);
                PlayerPrefs.DeleteKey(BestTileKey);
                PlayerPrefs.Save();

                OnHighScoreChanged?.Invoke(this, HighScore);
                OnBestTileAchieved?.Invoke(this, BestTile);

                Debug.Log("[ScoreManager] High scores reset");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ScoreManager] Failed to reset high scores: {e.Message}");
            }
        }

        #endregion

        /// <summary>
        /// Get a summary of the current game session
        /// </summary>
        public GameSessionSummary GetSessionSummary()
        {
            return new GameSessionSummary
            {
                Score = CurrentScore,
                Moves = CurrentMoves,
                TimeSeconds = GameTime,
                HighestTile = HighestTileAchieved,
                Rating = GetRating(),
                IsNewHighScore = CurrentScore > HighScore,
                IsNewBestTile = HighestTileAchieved > BestTile
            };
        }
    }

    /// <summary>
    /// Summary of a completed game session
    /// </summary>
    [Serializable]
    public class GameSessionSummary
    {
        public int Score;
        public int Moves;
        public float TimeSeconds;
        public int HighestTile;
        public string Rating;
        public bool IsNewHighScore;
        public bool IsNewBestTile;
    }
}
