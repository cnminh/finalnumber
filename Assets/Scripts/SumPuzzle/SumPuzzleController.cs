using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FinalNumber.SumPuzzle
{
    /// <summary>
    /// Main controller for the Sum Puzzle game mode.
    /// Orchestrates the board, UI, input, and game flow.
    /// Integrates with GameEventBus for analytics.
    /// </summary>
    public class SumPuzzleController : MonoBehaviour
    {
        [Header("Game Components")]
        public SumPuzzleBoard Board { get; private set; }
        public SumPuzzleUIManager UIManager { get; private set; }

        [Header("Level Settings")]
        public int currentLevelId = 1;
        public List<SumPuzzleLevel> levels;

        [Header("Game State")]
        public bool IsGameActive { get; private set; }
        public bool IsPaused { get; private set; }
        public float GameTime { get; private set; }
        public int MoveCount { get; private set; }

        // Events
        public event EventHandler OnGameStarted;
        public event EventHandler OnGameCompleted;
        public event EventHandler OnGameFailed;
        public event EventHandler OnGamePaused;
        public event EventHandler OnGameResumed;

        private SumPuzzleTile[,] _visualTiles;
        private int _selectedRow = -1;
        private int _selectedColumn = -1;

        private void Awake()
        {
            InitializeComponents();
            LoadLevels();
        }

        private void Start()
        {
            StartGame(currentLevelId);
        }

        private void Update()
        {
            if (IsGameActive && !IsPaused)
            {
                GameTime += Time.deltaTime;

                // Check time limit
                var level = GetCurrentLevel();
                if (level != null && level.TimeLimit > 0 && GameTime >= level.TimeLimit)
                {
                    FailGame("Time limit exceeded");
                }
            }
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
            // Create board
            Board = new SumPuzzleBoard(4);

            // Subscribe to board events
            Board.OnNumberPlaced += HandleNumberPlaced;
            Board.OnNumberRemoved += HandleNumberRemoved;
            Board.OnBoardCompleted += HandleBoardCompleted;
            Board.OnGridChanged += HandleGridChanged;

            // Get UI Manager
            UIManager = GetComponent<SumPuzzleUIManager>();
            if (UIManager != null)
            {
                UIManager.OnNumberSelected += HandleNumberButtonClicked;
                UIManager.OnClearSelected += HandleClearClicked;
                UIManager.OnHintRequested += HandleHintRequested;
                UIManager.OnPauseRequested += HandlePauseRequested;
                UIManager.OnResumeRequested += HandleResumeRequested;
                UIManager.OnQuitRequested += HandleQuitRequested;
            }
        }

        /// <summary>
        /// Unsubscribe from all events
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (Board != null)
            {
                Board.OnNumberPlaced -= HandleNumberPlaced;
                Board.OnNumberRemoved -= HandleNumberRemoved;
                Board.OnBoardCompleted -= HandleBoardCompleted;
                Board.OnGridChanged -= HandleGridChanged;
            }

            if (UIManager != null)
            {
                UIManager.OnNumberSelected -= HandleNumberButtonClicked;
                UIManager.OnClearSelected -= HandleClearClicked;
                UIManager.OnHintRequested -= HandleHintRequested;
                UIManager.OnPauseRequested -= HandlePauseRequested;
                UIManager.OnResumeRequested -= HandleResumeRequested;
                UIManager.OnQuitRequested -= HandleQuitRequested;
            }
        }

        /// <summary>
        /// Load level definitions
        /// </summary>
        private void LoadLevels()
        {
            levels = new List<SumPuzzleLevel>
            {
                // Level 1: Simple 3x3 introduction
                new SumPuzzleLevel
                {
                    LevelId = 1,
                    GridSize = 3,
                    RowTargets = new int[] { 6, 9, 6 },
                    ColumnTargets = new int[] { 6, 9, 6 },
                    FixedCells = new int[] { 0, 0, 1, 2, 2, 1 },  // Two corners
                    TimeLimit = 0,
                    MoveLimit = 0
                },

                // Level 2: 3x3 with one empty center
                new SumPuzzleLevel
                {
                    LevelId = 2,
                    GridSize = 3,
                    RowTargets = new int[] { 10, 15, 11 },
                    ColumnTargets = new int[] { 10, 15, 11 },
                    FixedCells = new int[] { 0, 0, 2, 0, 2, 9, 2, 0, 2, 2, 2, 9 },
                    TimeLimit = 0,
                    MoveLimit = 0
                },

                // Level 3: 4x4 introduction
                new SumPuzzleLevel
                {
                    LevelId = 3,
                    GridSize = 4,
                    RowTargets = new int[] { 10, 20, 20, 10 },
                    ColumnTargets = new int[] { 10, 20, 20, 10 },
                    FixedCells = new int[] { 0, 0, 1, 0, 3, 9, 3, 0, 1, 3, 3, 9 },
                    TimeLimit = 180,  // 3 minutes
                    MoveLimit = 0
                },

                // Level 4: 4x4 challenge
                new SumPuzzleLevel
                {
                    LevelId = 4,
                    GridSize = 4,
                    RowTargets = new int[] { 15, 25, 20, 10 },
                    ColumnTargets = new int[] { 12, 22, 18, 18 },
                    FixedCells = new int[] { 0, 0, 3, 0, 2, 5, 3, 1, 7, 3, 3, 4 },
                    TimeLimit = 240,  // 4 minutes
                    MoveLimit = 0
                },

                // Level 5: 5x5 expert
                new SumPuzzleLevel
                {
                    LevelId = 5,
                    GridSize = 5,
                    RowTargets = new int[] { 15, 25, 30, 20, 10 },
                    ColumnTargets = new int[] { 15, 20, 25, 25, 15 },
                    FixedCells = new int[] { 0, 0, 5, 0, 4, 1, 4, 0, 5, 4, 4, 1, 2, 2, 9 },
                    TimeLimit = 300,  // 5 minutes
                    MoveLimit = 0
                }
            };
        }

        /// <summary>
        /// Start a new game
        /// </summary>
        public void StartGame(int levelId)
        {
            currentLevelId = levelId;
            var level = GetCurrentLevel();

            if (level == null)
            {
                Debug.LogError($"[SumPuzzleController] Level {levelId} not found!");
                return;
            }

            // Reset state
            IsGameActive = true;
            IsPaused = false;
            GameTime = 0f;
            MoveCount = 0;
            _selectedRow = -1;
            _selectedColumn = -1;

            // Setup board
            Board.SetupLevel(level);

            // Create visual tiles
            CreateVisualTiles(level.GridSize);

            // Update UI
            if (UIManager != null)
            {
                UIManager.SetupUI(level, Board);
                UIManager.ShowGameUI();
            }

            // Fire analytics event
            GameEventBus.TriggerLevelStarted(levelId, 1);

            OnGameStarted?.Invoke(this, EventArgs.Empty);

            Debug.Log($"[SumPuzzleController] Started Level {levelId}");
        }

        /// <summary>
        /// Get current level definition
        /// </summary>
        private SumPuzzleLevel GetCurrentLevel()
        {
            return levels.Find(l => l.LevelId == currentLevelId);
        }

        /// <summary>
        /// Create visual tile objects
        /// </summary>
        private void CreateVisualTiles(int gridSize)
        {
            if (UIManager != null)
            {
                _visualTiles = UIManager.CreateTileGrid(gridSize, Board);

                // Subscribe to tile clicks
                for (int row = 0; row < gridSize; row++)
                {
                    for (int col = 0; col < gridSize; col++)
                    {
                        int r = row;  // Capture for closure
                        int c = col;
                        _visualTiles[row, col].OnTileClicked += (clickedRow, clickedCol) =>
                        {
                            HandleTileClicked(r, c);
                        };
                    }
                }
            }
        }

        /// <summary>
        /// Handle tile click
        /// </summary>
        private void HandleTileClicked(int row, int column)
        {
            // Deselect previous
            if (_selectedRow >= 0 && _selectedColumn >= 0)
            {
                _visualTiles[_selectedRow, _selectedColumn].SetSelected(false);
            }

            // Select new tile
            _selectedRow = row;
            _selectedColumn = column;
            _visualTiles[row, column].SetSelected(true);

            // Update UI
            var cell = Board.GetCell(row, column);
            if (UIManager != null)
            {
                UIManager.ShowNumberSelector(!cell.IsFixed);
            }

            Debug.Log($"[SumPuzzleController] Selected tile ({row}, {column})");
        }

        /// <summary>
        /// Handle number button click from UI
        /// </summary>
        private void HandleNumberButtonClicked(int number)
        {
            if (_selectedRow < 0 || _selectedColumn < 0)
            {
                Debug.LogWarning("[SumPuzzleController] No tile selected!");
                return;
            }

            // Try to place the number
            bool placed = Board.PlaceNumber(number, _selectedRow, _selectedColumn);

            if (placed)
            {
                MoveCount++;

                // Check move limit
                var level = GetCurrentLevel();
                if (level != null && level.MoveLimit > 0 && MoveCount >= level.MoveLimit)
                {
                    FailGame("Move limit reached");
                }
            }
            else
            {
                // Play error feedback
                _visualTiles[_selectedRow, _selectedColumn].PlayErrorAnimation();
            }
        }

        /// <summary>
        /// Handle clear button click
        /// </summary>
        private void HandleClearClicked()
        {
            if (_selectedRow < 0 || _selectedColumn < 0)
                return;

            Board.RemoveNumber(_selectedRow, _selectedColumn);
        }

        /// <summary>
        /// Handle hint request
        /// </summary>
        private void HandleHintRequested()
        {
            var hint = Board.GetHint();
            if (hint.HasValue)
            {
                // Highlight the hint tile
                _visualTiles[hint.Value.row, hint.Value.column].ShowHint();

                // Auto-select the hint tile
                HandleTileClicked(hint.Value.row, hint.Value.column);

                Debug.Log($"[SumPuzzleController] Hint: Place {hint.Value.number} at ({hint.Value.row}, {hint.Value.column})");
            }
            else
            {
                Debug.Log("[SumPuzzleController] No hint available");
            }
        }

        /// <summary>
        /// Handle pause request
        /// </summary>
        private void HandlePauseRequested()
        {
            PauseGame();
        }

        /// <summary>
        /// Handle resume request
        /// </summary>
        private void HandleResumeRequested()
        {
            ResumeGame();
        }

        /// <summary>
        /// Handle quit request
        /// </summary>
        private void HandleQuitRequested()
        {
            QuitGame();
        }

        /// <summary>
        /// Handle number placed event from board
        /// </summary>
        private void HandleNumberPlaced(object sender, int number)
        {
            UpdateVisualTile(_selectedRow, _selectedColumn);

            // Analytics event
            GameEventBus.TriggerLevelCompleted(currentLevelId, 1, 0, MoveCount, GameTime);
        }

        /// <summary>
        /// Handle number removed event
        /// </summary>
        private void HandleNumberRemoved(object sender, int number)
        {
            UpdateVisualTile(_selectedRow, _selectedColumn);
        }

        /// <summary>
        /// Handle board completed event
        /// </summary>
        private void HandleBoardCompleted(object sender, EventArgs e)
        {
            CompleteGame();
        }

        /// <summary>
        /// Handle grid changed event
        /// </summary>
        private void HandleGridChanged(object sender, EventArgs e)
        {
            // Update all visual tiles
            for (int row = 0; row < Board.GridSize; row++)
            {
                for (int col = 0; col < Board.GridSize; col++)
                {
                    UpdateVisualTile(row, col);
                }
            }

            // Update UI
            if (UIManager != null)
            {
                UIManager.UpdateProgress(Board);
            }
        }

        /// <summary>
        /// Update a single visual tile
        /// </summary>
        private void UpdateVisualTile(int row, int column)
        {
            if (_visualTiles == null)
                return;

            var cell = Board.GetCell(row, column);
            var tile = _visualTiles[row, column];

            tile.SetValue(cell.Value, cell.IsFixed);
            tile.SetValid(cell.IsValid);
        }

        /// <summary>
        /// Complete the game successfully
        /// </summary>
        private void CompleteGame()
        {
            IsGameActive = false;

            OnGameCompleted?.Invoke(this, EventArgs.Empty);

            // Fire analytics event
            GameEventBus.TriggerLevelCompleted(currentLevelId, 1, 0, MoveCount, GameTime);

            // Show completion UI
            if (UIManager != null)
            {
                UIManager.ShowCompletionScreen(MoveCount, GameTime);
            }

            Debug.Log($"[SumPuzzleController] Level {currentLevelId} completed!");
        }

        /// <summary>
        /// Fail the game (time out, moves exceeded, etc.)
        /// </summary>
        private void FailGame(string reason)
        {
            IsGameActive = false;

            OnGameFailed?.Invoke(this, EventArgs.Empty);

            // Fire analytics event
            GameEventBus.TriggerLevelFailed(currentLevelId, 1, 0, reason);

            // Show failure UI
            if (UIManager != null)
            {
                UIManager.ShowFailureScreen(reason);
            }

            Debug.Log($"[SumPuzzleController] Game failed: {reason}");
        }

        /// <summary>
        /// Pause the game
        /// </summary>
        public void PauseGame()
        {
            if (!IsPaused)
            {
                IsPaused = true;
                Time.timeScale = 0f;

                OnGamePaused?.Invoke(this, EventArgs.Empty);
                GameEventBus.TriggerGamePaused();

                if (UIManager != null)
                {
                    UIManager.ShowPauseMenu();
                }
            }
        }

        /// <summary>
        /// Resume the game
        /// </summary>
        public void ResumeGame()
        {
            if (IsPaused)
            {
                IsPaused = false;
                Time.timeScale = 1f;

                OnGameResumed?.Invoke(this, EventArgs.Empty);
                GameEventBus.TriggerGameResumed();

                if (UIManager != null)
                {
                    UIManager.HidePauseMenu();
                }
            }
        }

        /// <summary>
        /// Quit the current game
        /// </summary>
        public void QuitGame()
        {
            IsGameActive = false;
            Time.timeScale = 1f;

            GameEventBus.TriggerGameQuit();

            // Return to main menu
            SceneManager.LoadScene("MainScene");
        }

        /// <summary>
        /// Go to next level
        /// </summary>
        public void NextLevel()
        {
            if (currentLevelId < levels.Count)
            {
                StartGame(currentLevelId + 1);
            }
            else
            {
                // All levels complete - return to main menu
                QuitGame();
            }
        }

        /// <summary>
        /// Retry current level
        /// </summary>
        public void RetryLevel()
        {
            StartGame(currentLevelId);
        }

        /// <summary>
        /// Get game progress percentage
        /// </summary>
        public float GetProgressPercentage()
        {
            return Board.GetCompletionPercentage();
        }
    }
}
