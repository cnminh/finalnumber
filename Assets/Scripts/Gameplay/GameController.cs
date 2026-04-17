using System;
using UnityEngine;
using FinalNumber.Gameplay;

namespace FinalNumber
{
    /// <summary>
    /// Main game controller that orchestrates the 2048 gameplay.
    /// Connects GridManager, GameLogic, InputHandler, and ScoreManager.
    /// Integrates with GameEventBus for analytics and event tracking.
    /// </summary>
    public class GameController : MonoBehaviour
    {
        [Header("Components")]
        public GridManager GridManager { get; private set; }
        public GameLogic GameLogic { get; private set; }
        public InputHandler InputHandler { get; private set; }
        public ScoreManager ScoreManager { get; private set; }

        [Header("Game Settings")]
        [Tooltip("Number of tiles to spawn at game start")]
        public int startingTiles = 2;

        [Tooltip("Target tile value to win")]
        public int targetTileValue = 2048;

        // Game state
        public bool IsGameOver { get; private set; }
        public bool IsGameWon { get; private set; }
        public bool IsPaused { get; private set; }

        // Events
        public event EventHandler OnGameStarted;
        public event EventHandler OnGameOverEvent;
        public event EventHandler OnGameWonEvent;
        public event EventHandler OnGamePaused;
        public event EventHandler OnGameResumed;

        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            StartNewGame();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        /// <summary>
        /// Initialize all game components
        /// </summary>
        private void InitializeComponents()
        {
            // Create GridManager
            GridManager = new GridManager();
            
            // Create GameLogic
            GameLogic = new GameLogic(GridManager);

            // Get or create InputHandler
            InputHandler = GetComponent<InputHandler>();
            if (InputHandler == null)
            {
                InputHandler = gameObject.AddComponent<InputHandler>();
            }

            // Get or create ScoreManager
            ScoreManager = GetComponent<ScoreManager>();
            if (ScoreManager == null)
            {
                ScoreManager = gameObject.AddComponent<ScoreManager>();
            }

            // Subscribe to events
            SubscribeToEvents();
        }

        /// <summary>
        /// Subscribe to all component events
        /// </summary>
        private void SubscribeToEvents()
        {
            if (InputHandler != null)
            {
                InputHandler.OnSwipeDetected += HandleSwipe;
            }

            if (GridManager != null)
            {
                GridManager.OnTileMoved += HandleTileMoved;
                GridManager.OnTileSpawned += HandleTileSpawned;
            }

            if (ScoreManager != null)
            {
                ScoreManager.OnTargetScoreReached += HandleTargetReached;
            }
        }

        /// <summary>
        /// Unsubscribe from all events
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (InputHandler != null)
            {
                InputHandler.OnSwipeDetected -= HandleSwipe;
            }

            if (GridManager != null)
            {
                GridManager.OnTileMoved -= HandleTileMoved;
                GridManager.OnTileSpawned -= HandleTileSpawned;
            }

            if (ScoreManager != null)
            {
                ScoreManager.OnTargetScoreReached -= HandleTargetReached;
            }
        }

        /// <summary>
        /// Start a new game session
        /// </summary>
        public void StartNewGame()
        {
            // Reset state
            IsGameOver = false;
            IsGameWon = false;
            IsPaused = false;

            // Initialize grid
            GridManager.InitializeGrid();

            // Spawn starting tiles
            for (int i = 0; i < startingTiles; i++)
            {
                GridManager.SpawnRandomTile();
            }

            // Start score tracking
            ScoreManager.StartNewGame();
            ScoreManager.targetScore = targetTileValue;

            // Enable input
            InputHandler.SetInputEnabled(true);

            // Trigger events
            OnGameStarted?.Invoke(this, EventArgs.Empty);
            GameEventBus.TriggerGameStarted();

            Debug.Log("[GameController] Game started");
        }

        /// <summary>
        /// Handle swipe input from InputHandler
        /// </summary>
        private void HandleSwipe(object sender, MoveDirection direction)
        {
            if (IsGameOver || IsGameWon || IsPaused)
                return;

            ExecuteMove(direction);
        }

        /// <summary>
        /// Execute a move in the specified direction
        /// </summary>
        public void ExecuteMove(MoveDirection direction)
        {
            if (IsGameOver || IsGameWon || IsPaused)
                return;

            // Execute the move
            bool moved = GameLogic.ExecuteMove(direction);

            if (moved)
            {
                // Increment move counter
                ScoreManager.AddMove();

                // Spawn a new tile
                GridManager.SpawnRandomTile();

                // Check win condition
                if (!IsGameWon && GridManager.HasWinningTile(targetTileValue))
                {
                    WinGame();
                }

                // Check game over
                if (!IsGameOver && !GridManager.HasValidMoves())
                {
                    EndGame("No valid moves remaining");
                }
            }
        }

        /// <summary>
        /// Handle tile moved event
        /// </summary>
        private void HandleTileMoved(object sender, TileMoveEventArgs e)
        {
            // Add score for merges
            if (e.IsMerge && e.ScoreGained > 0)
            {
                ScoreManager.AddScore(e.ScoreGained);
            }
        }

        /// <summary>
        /// Handle tile spawned event
        /// </summary>
        private void HandleTileSpawned(object sender, TileSpawnEventArgs e)
        {
            // Track highest tile
            ScoreManager.UpdateHighestTile(e.Tile.Value);
        }

        /// <summary>
        /// Handle target score reached
        /// </summary>
        private void HandleTargetReached(object sender, EventArgs e)
        {
            // This is triggered when score reaches target, but win is based on tile value
            Debug.Log("[GameController] Target score reached!");
        }

        /// <summary>
        /// Win the game (reached 2048 tile)
        /// </summary>
        private void WinGame()
        {
            IsGameWon = true;
            ScoreManager.EndGame();

            OnGameWonEvent?.Invoke(this, EventArgs.Empty);

            // Trigger level completed event via GameEventBus
            var summary = ScoreManager.GetSessionSummary();
            GameEventBus.TriggerLevelCompleted(1, 1, summary.Score, summary.Moves, summary.TimeSeconds);

            Debug.Log("[GameController] Game WON!");
        }

        /// <summary>
        /// End the game (game over)
        /// </summary>
        private void EndGame(string reason)
        {
            IsGameOver = true;
            ScoreManager.EndGame();

            OnGameOverEvent?.Invoke(this, EventArgs.Empty);

            // Trigger level failed event via GameEventBus
            var summary = ScoreManager.GetSessionSummary();
            GameEventBus.TriggerLevelFailed(1, 1, summary.Score, reason);

            Debug.Log($"[GameController] Game Over: {reason}");
        }

        /// <summary>
        /// Pause the game
        /// </summary>
        public void PauseGame()
        {
            if (IsPaused)
                return;

            IsPaused = true;
            ScoreManager.PauseGame();
            InputHandler.SetInputEnabled(false);

            OnGamePaused?.Invoke(this, EventArgs.Empty);
            GameEventBus.TriggerGamePaused();

            Debug.Log("[GameController] Game paused");
        }

        /// <summary>
        /// Resume the game
        /// </summary>
        public void ResumeGame()
        {
            if (!IsPaused)
                return;

            IsPaused = false;
            ScoreManager.ResumeGame();
            InputHandler.SetInputEnabled(true);

            OnGameResumed?.Invoke(this, EventArgs.Empty);
            GameEventBus.TriggerGameResumed();

            Debug.Log("[GameController] Game resumed");
        }

        /// <summary>
        /// Quit the current game
        /// </summary>
        public void QuitGame()
        {
            ScoreManager.EndGame();
            InputHandler.SetInputEnabled(false);

            GameEventBus.TriggerGameQuit();

            Debug.Log("[GameController] Game quit");
        }

        /// <summary>
        /// Get current game state for saving
        /// </summary>
        public SerializableGameState GetCurrentGameState()
        {
            return new SerializableGameState
            {
                GridState = GridManager.GetGridState(),
                Score = ScoreManager.CurrentScore,
                Moves = ScoreManager.CurrentMoves,
                Time = ScoreManager.GameTime,
                IsGameOver = IsGameOver,
                IsGameWon = IsGameWon
            };
        }

        /// <summary>
        /// Restore game from saved state
        /// </summary>
        public void RestoreGameState(SerializableGameState state)
        {
            // TODO: Implement game state restoration
            Debug.Log("[GameController] Restore game state - Not yet implemented");
        }
    }

    /// <summary>
    /// Serializable game state for saving/loading
    /// </summary>
    [Serializable]
    public class SerializableGameState
    {
        public int[,] GridState;
        public int Score;
        public int Moves;
        public float Time;
        public bool IsGameOver;
        public bool IsGameWon;
    }
}
