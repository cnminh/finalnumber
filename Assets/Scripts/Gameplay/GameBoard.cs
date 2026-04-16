using System;
using System.Collections.Generic;
using UnityEngine;

namespace FinalNumber.Gameplay
{
    /// <summary>
    /// Represents the current state of the game
    /// </summary>
    public enum GameState
    {
        Playing,
        Won,
        Lost
    }

    /// <summary>
    /// Manages the core 2048 game board logic including grid state,
    /// tile movement, merging, scoring, and win/lose conditions.
    /// Fires GameEventBus events for analytics integration.
    /// </summary>
    public class GameBoard
    {
        public const int GridSize = 4;
        public const int MaxTiles = GridSize * GridSize;
        public const int WinValue = 2048;
        private const string BestScoreKey = "FinalNumber_GameBoard_BestScore";

        // Core components
        private Tile[,] _grid;
        private System.Random _random;

        // Game state
        public GameState CurrentState { get; private set; }
        public int CurrentScore { get; private set; }
        public int BestScore { get; private set; }
        public int CurrentMoves { get; private set; }
        public int HighestTileValue { get; private set; }

        // Events
        public event EventHandler<TileMoveEventArgs> OnTileMoved;
        public event EventHandler<TileSpawnEventArgs> OnTileSpawned;
        public event EventHandler OnGridChanged;
        public event EventHandler<int> OnScoreChanged;
        public event EventHandler<GameState> OnGameStateChanged;
        public event EventHandler OnMoveExecuted;
        public event EventHandler OnInvalidMove;

        /// <summary>
        /// Create a new GameBoard with optional random seed
        /// </summary>
        public GameBoard(int seed = 0)
        {
            _grid = new Tile[GridSize, GridSize];
            _random = seed == 0 ? new System.Random() : new System.Random(seed);
            CurrentState = GameState.Playing;
            CurrentScore = 0;
            CurrentMoves = 0;
            HighestTileValue = 0;
            LoadBestScore();
        }

        #region Initialization

        /// <summary>
        /// Initialize a new game with 2 random tiles
        /// </summary>
        public void InitializeGame()
        {
            // Clear the grid
            for (int row = 0; row < GridSize; row++)
            {
                for (int col = 0; col < GridSize; col++)
                {
                    _grid[row, col] = null;
                }
            }

            // Reset game state
            CurrentState = GameState.Playing;
            CurrentScore = 0;
            CurrentMoves = 0;
            HighestTileValue = 0;

            // Spawn 2 starting tiles
            SpawnRandomTile();
            SpawnRandomTile();

            // Fire level started event
            GameEventBus.TriggerLevelStarted(1, 1);

            OnGridChanged?.Invoke(this, EventArgs.Empty);
            OnScoreChanged?.Invoke(this, CurrentScore);
            OnGameStateChanged?.Invoke(this, CurrentState);

            Debug.Log("[GameBoard] Game initialized with 2 tiles");
        }

        #endregion

        #region Grid Queries

        /// <summary>
        /// Get tile at specific position
        /// </summary>
        public Tile GetTile(int row, int column)
        {
            if (!IsValidPosition(row, column))
                return null;
            return _grid[row, column];
        }

        /// <summary>
        /// Check if position is within grid bounds
        /// </summary>
        public bool IsValidPosition(int row, int column)
        {
            return row >= 0 && row < GridSize && column >= 0 && column < GridSize;
        }

        /// <summary>
        /// Check if a cell is empty
        /// </summary>
        public bool IsEmpty(int row, int column)
        {
            return IsValidPosition(row, column) && _grid[row, column] == null;
        }

        /// <summary>
        /// Get count of empty cells
        /// </summary>
        public int GetEmptyCellCount()
        {
            int count = 0;
            for (int row = 0; row < GridSize; row++)
            {
                for (int col = 0; col < GridSize; col++)
                {
                    if (_grid[row, col] == null)
                        count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Check if grid has any empty cells
        /// </summary>
        public bool HasEmptyCells()
        {
            return GetEmptyCellCount() > 0;
        }

        #endregion

        #region Tile Spawning

        /// <summary>
        /// Spawn a new tile in a random empty cell
        /// 90% chance of value 2, 10% chance of value 4
        /// </summary>
        public Tile SpawnRandomTile()
        {
            if (!HasEmptyCells())
                return null;

            // 90% chance of value 2, 10% chance of value 4
            int value = _random.Next(100) < 90 ? 2 : 4;

            // Find random empty cell
            int emptyCount = GetEmptyCellCount();
            int targetIndex = _random.Next(emptyCount);
            int currentIndex = 0;

            for (int row = 0; row < GridSize; row++)
            {
                for (int col = 0; col < GridSize; col++)
                {
                    if (_grid[row, col] == null)
                    {
                        if (currentIndex == targetIndex)
                        {
                            Tile tile = new Tile(value, row, col);
                            _grid[row, col] = tile;

                            OnTileSpawned?.Invoke(this, new TileSpawnEventArgs
                            {
                                Tile = tile,
                                Row = row,
                                Column = col
                            });

                            OnGridChanged?.Invoke(this, EventArgs.Empty);

                            // Update highest tile tracking
                            if (value > HighestTileValue)
                            {
                                HighestTileValue = value;
                            }

                            return tile;
                        }
                        currentIndex++;
                    }
                }
            }

            return null;
        }

        #endregion

        #region Movement & Merging

        /// <summary>
        /// Execute a move in the specified direction
        /// Returns true if the move changed the grid state
        /// </summary>
        public bool ExecuteMove(MoveDirection direction)
        {
            if (CurrentState != GameState.Playing || direction == MoveDirection.None)
                return false;

            // Reset merge states before processing
            ResetMergeStates();

            bool moved = false;
            int scoreGained = 0;

            // Process move based on direction
            switch (direction)
            {
                case MoveDirection.Left:
                    (moved, scoreGained) = ProcessLeft();
                    break;
                case MoveDirection.Right:
                    (moved, scoreGained) = ProcessRight();
                    break;
                case MoveDirection.Up:
                    (moved, scoreGained) = ProcessUp();
                    break;
                case MoveDirection.Down:
                    (moved, scoreGained) = ProcessDown();
                    break;
            }

            if (moved)
            {
                CurrentMoves++;

                if (scoreGained > 0)
                {
                    CurrentScore += scoreGained;
                    OnScoreChanged?.Invoke(this, CurrentScore);
                    CheckAndUpdateBestScore();
                }

                OnMoveExecuted?.Invoke(this, EventArgs.Empty);

                // Spawn new tile after move
                SpawnRandomTile();

                // Check win/lose conditions
                CheckGameConditions();

                return true;
            }
            else
            {
                OnInvalidMove?.Invoke(this, EventArgs.Empty);
                return false;
            }
        }

        private (bool moved, int score) ProcessLeft()
        {
            bool moved = false;
            int totalScore = 0;

            for (int row = 0; row < GridSize; row++)
            {
                var (rowMoved, rowScore) = ProcessLineLeft(row);
                if (rowMoved) moved = true;
                totalScore += rowScore;
            }

            return (moved, totalScore);
        }

        private (bool moved, int score) ProcessLineLeft(int row)
        {
            bool moved = false;
            int score = 0;

            // Collect non-null tiles in this row
            List<Tile> tiles = new List<Tile>();
            for (int col = 0; col < GridSize; col++)
            {
                var tile = _grid[row, col];
                if (tile != null)
                    tiles.Add(tile);
            }

            if (tiles.Count == 0)
                return (false, 0);

            // Process merges and moves
            int targetPosition = 0;

            for (int i = 0; i < tiles.Count; i++)
            {
                Tile current = tiles[i];
                int originalCol = current.Column;

                // Check if we can merge with the previous tile
                if (i > 0 && !tiles[i - 1].HasMerged && tiles[i - 1].Value == current.Value)
                {
                    // Merge!
                    targetPosition = tiles[i - 1].Column;
                    int gainedScore = current.Value * 2;
                    score += gainedScore;

                    // Execute merge
                    _grid[row, originalCol] = null;
                    current.SetPosition(row, targetPosition);
                    current.Merge(tiles[i - 1]);
                    tiles[i - 1].HasMerged = true;

                    // The tile we merged "into" is removed from grid
                    _grid[row, targetPosition] = current;

                    OnTileMoved?.Invoke(this, new TileMoveEventArgs
                    {
                        Tile = current,
                        FromRow = row,
                        FromColumn = originalCol,
                        ToRow = row,
                        ToColumn = targetPosition,
                        IsMerge = true,
                        ScoreGained = gainedScore
                    });

                    moved = true;
                }
                else
                {
                    // Just move
                    if (originalCol != targetPosition)
                    {
                        _grid[row, originalCol] = null;
                        current.SetPosition(row, targetPosition);
                        _grid[row, targetPosition] = current;

                        OnTileMoved?.Invoke(this, new TileMoveEventArgs
                        {
                            Tile = current,
                            FromRow = row,
                            FromColumn = originalCol,
                            ToRow = row,
                            ToColumn = targetPosition,
                            IsMerge = false,
                            ScoreGained = 0
                        });

                        moved = true;
                    }
                    targetPosition++;
                }
            }

            return (moved, score);
        }

        private (bool moved, int score) ProcessRight()
        {
            bool moved = false;
            int totalScore = 0;

            for (int row = 0; row < GridSize; row++)
            {
                var (rowMoved, rowScore) = ProcessLineRight(row);
                if (rowMoved) moved = true;
                totalScore += rowScore;
            }

            return (moved, totalScore);
        }

        private (bool moved, int score) ProcessLineRight(int row)
        {
            bool moved = false;
            int score = 0;

            // Collect non-null tiles in this row (right to left)
            List<Tile> tiles = new List<Tile>();
            for (int col = GridSize - 1; col >= 0; col--)
            {
                var tile = _grid[row, col];
                if (tile != null)
                    tiles.Add(tile);
            }

            if (tiles.Count == 0)
                return (false, 0);

            // Process merges and moves
            int targetPosition = GridSize - 1;

            for (int i = 0; i < tiles.Count; i++)
            {
                Tile current = tiles[i];
                int originalCol = current.Column;

                // Check if we can merge with the previous tile
                if (i > 0 && !tiles[i - 1].HasMerged && tiles[i - 1].Value == current.Value)
                {
                    // Merge!
                    targetPosition = tiles[i - 1].Column;
                    int gainedScore = current.Value * 2;
                    score += gainedScore;

                    // Execute merge
                    _grid[row, originalCol] = null;
                    current.SetPosition(row, targetPosition);
                    current.Merge(tiles[i - 1]);
                    tiles[i - 1].HasMerged = true;
                    _grid[row, targetPosition] = current;

                    OnTileMoved?.Invoke(this, new TileMoveEventArgs
                    {
                        Tile = current,
                        FromRow = row,
                        FromColumn = originalCol,
                        ToRow = row,
                        ToColumn = targetPosition,
                        IsMerge = true,
                        ScoreGained = gainedScore
                    });

                    moved = true;
                }
                else
                {
                    // Just move
                    if (originalCol != targetPosition)
                    {
                        _grid[row, originalCol] = null;
                        current.SetPosition(row, targetPosition);
                        _grid[row, targetPosition] = current;

                        OnTileMoved?.Invoke(this, new TileMoveEventArgs
                        {
                            Tile = current,
                            FromRow = row,
                            FromColumn = originalCol,
                            ToRow = row,
                            ToColumn = targetPosition,
                            IsMerge = false,
                            ScoreGained = 0
                        });

                        moved = true;
                    }
                    targetPosition--;
                }
            }

            return (moved, score);
        }

        private (bool moved, int score) ProcessUp()
        {
            bool moved = false;
            int totalScore = 0;

            for (int col = 0; col < GridSize; col++)
            {
                var (colMoved, colScore) = ProcessColumnUp(col);
                if (colMoved) moved = true;
                totalScore += colScore;
            }

            return (moved, totalScore);
        }

        private (bool moved, int score) ProcessColumnUp(int col)
        {
            bool moved = false;
            int score = 0;

            // Collect non-null tiles in this column (top to bottom)
            List<Tile> tiles = new List<Tile>();
            for (int row = 0; row < GridSize; row++)
            {
                var tile = _grid[row, col];
                if (tile != null)
                    tiles.Add(tile);
            }

            if (tiles.Count == 0)
                return (false, 0);

            // Process merges and moves
            int targetPosition = 0;

            for (int i = 0; i < tiles.Count; i++)
            {
                Tile current = tiles[i];
                int originalRow = current.Row;

                // Check if we can merge with the previous tile
                if (i > 0 && !tiles[i - 1].HasMerged && tiles[i - 1].Value == current.Value)
                {
                    // Merge!
                    targetPosition = tiles[i - 1].Row;
                    int gainedScore = current.Value * 2;
                    score += gainedScore;

                    // Execute merge
                    _grid[originalRow, col] = null;
                    current.SetPosition(targetPosition, col);
                    current.Merge(tiles[i - 1]);
                    tiles[i - 1].HasMerged = true;
                    _grid[targetPosition, col] = current;

                    OnTileMoved?.Invoke(this, new TileMoveEventArgs
                    {
                        Tile = current,
                        FromRow = originalRow,
                        FromColumn = col,
                        ToRow = targetPosition,
                        ToColumn = col,
                        IsMerge = true,
                        ScoreGained = gainedScore
                    });

                    moved = true;
                }
                else
                {
                    // Just move
                    if (originalRow != targetPosition)
                    {
                        _grid[originalRow, col] = null;
                        current.SetPosition(targetPosition, col);
                        _grid[targetPosition, col] = current;

                        OnTileMoved?.Invoke(this, new TileMoveEventArgs
                        {
                            Tile = current,
                            FromRow = originalRow,
                            FromColumn = col,
                            ToRow = targetPosition,
                            ToColumn = col,
                            IsMerge = false,
                            ScoreGained = 0
                        });

                        moved = true;
                    }
                    targetPosition++;
                }
            }

            return (moved, score);
        }

        private (bool moved, int score) ProcessDown()
        {
            bool moved = false;
            int totalScore = 0;

            for (int col = 0; col < GridSize; col++)
            {
                var (colMoved, colScore) = ProcessColumnDown(col);
                if (colMoved) moved = true;
                totalScore += colScore;
            }

            return (moved, totalScore);
        }

        private (bool moved, int score) ProcessColumnDown(int col)
        {
            bool moved = false;
            int score = 0;

            // Collect non-null tiles in this column (bottom to top)
            List<Tile> tiles = new List<Tile>();
            for (int row = GridSize - 1; row >= 0; row--)
            {
                var tile = _grid[row, col];
                if (tile != null)
                    tiles.Add(tile);
            }

            if (tiles.Count == 0)
                return (false, 0);

            // Process merges and moves
            int targetPosition = GridSize - 1;

            for (int i = 0; i < tiles.Count; i++)
            {
                Tile current = tiles[i];
                int originalRow = current.Row;

                // Check if we can merge with the previous tile
                if (i > 0 && !tiles[i - 1].HasMerged && tiles[i - 1].Value == current.Value)
                {
                    // Merge!
                    targetPosition = tiles[i - 1].Row;
                    int gainedScore = current.Value * 2;
                    score += gainedScore;

                    // Execute merge
                    _grid[originalRow, col] = null;
                    current.SetPosition(targetPosition, col);
                    current.Merge(tiles[i - 1]);
                    tiles[i - 1].HasMerged = true;
                    _grid[targetPosition, col] = current;

                    OnTileMoved?.Invoke(this, new TileMoveEventArgs
                    {
                        Tile = current,
                        FromRow = originalRow,
                        FromColumn = col,
                        ToRow = targetPosition,
                        ToColumn = col,
                        IsMerge = true,
                        ScoreGained = gainedScore
                    });

                    moved = true;
                }
                else
                {
                    // Just move
                    if (originalRow != targetPosition)
                    {
                        _grid[originalRow, col] = null;
                        current.SetPosition(targetPosition, col);
                        _grid[targetPosition, col] = current;

                        OnTileMoved?.Invoke(this, new TileMoveEventArgs
                        {
                            Tile = current,
                            FromRow = originalRow,
                            FromColumn = col,
                            ToRow = targetPosition,
                            ToColumn = col,
                            IsMerge = false,
                            ScoreGained = 0
                        });

                        moved = true;
                    }
                    targetPosition--;
                }
            }

            return (moved, score);
        }

        private void ResetMergeStates()
        {
            for (int row = 0; row < GridSize; row++)
            {
                for (int col = 0; col < GridSize; col++)
                {
                    _grid[row, col]?.ResetMergeState();
                }
            }
        }

        #endregion

        #region Game State Management

        /// <summary>
        /// Check and update win/lose conditions
        /// </summary>
        private void CheckGameConditions()
        {
            // Check for win (2048 tile created)
            if (CurrentState == GameState.Playing && HasWinningTile())
            {
                WinGame();
            }

            // Check for lose (no valid moves)
            if (CurrentState == GameState.Playing && !HasValidMoves())
            {
                LoseGame();
            }
        }

        /// <summary>
        /// Check if 2048 tile exists (win condition)
        /// </summary>
        public bool HasWinningTile(int targetValue = WinValue)
        {
            for (int row = 0; row < GridSize; row++)
            {
                for (int col = 0; col < GridSize; col++)
                {
                    if (_grid[row, col]?.Value >= targetValue)
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Check if any valid moves are possible
        /// </summary>
        public bool HasValidMoves()
        {
            // If there are empty cells, moves are possible
            if (HasEmptyCells())
                return true;

            // Check for adjacent tiles with same value
            for (int row = 0; row < GridSize; row++)
            {
                for (int col = 0; col < GridSize; col++)
                {
                    Tile current = _grid[row, col];
                    if (current == null) continue;

                    // Check right neighbor
                    if (col < GridSize - 1 && _grid[row, col + 1]?.Value == current.Value)
                        return true;

                    // Check bottom neighbor
                    if (row < GridSize - 1 && _grid[row + 1, col]?.Value == current.Value)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Handle win condition
        /// </summary>
        private void WinGame()
        {
            CurrentState = GameState.Won;
            OnGameStateChanged?.Invoke(this, CurrentState);

            // Fire GameEventBus event
            GameEventBus.TriggerLevelCompleted(1, 1, CurrentScore, CurrentMoves, 0f);

            Debug.Log("[GameBoard] Game WON! 2048 tile achieved!");
        }

        /// <summary>
        /// Handle lose condition
        /// </summary>
        private void LoseGame()
        {
            CurrentState = GameState.Lost;
            OnGameStateChanged?.Invoke(this, CurrentState);

            // Fire GameEventBus event
            GameEventBus.TriggerLevelFailed(1, 1, CurrentScore, "No valid moves remaining");

            Debug.Log("[GameBoard] Game LOST! No valid moves remaining.");
        }

        #endregion

        #region Scoring & Persistence

        /// <summary>
        /// Check and update best score using PlayerPrefs
        /// </summary>
        private void CheckAndUpdateBestScore()
        {
            if (CurrentScore > BestScore)
            {
                BestScore = CurrentScore;
                SaveBestScore();
                Debug.Log($"[GameBoard] New best score: {BestScore}");
            }
        }

        /// <summary>
        /// Load best score from PlayerPrefs
        /// </summary>
        private void LoadBestScore()
        {
            BestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
        }

        /// <summary>
        /// Save best score to PlayerPrefs
        /// </summary>
        private void SaveBestScore()
        {
            PlayerPrefs.SetInt(BestScoreKey, BestScore);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Reset best score (for testing)
        /// </summary>
        public void ResetBestScore()
        {
            BestScore = 0;
            PlayerPrefs.DeleteKey(BestScoreKey);
            PlayerPrefs.Save();
        }

        #endregion

        #region Utility

        /// <summary>
        /// Get current grid state as a 2D array of values
        /// </summary>
        public int[,] GetGridState()
        {
            int[,] state = new int[GridSize, GridSize];
            for (int row = 0; row < GridSize; row++)
            {
                for (int col = 0; col < GridSize; col++)
                {
                    state[row, col] = _grid[row, col]?.Value ?? 0;
                }
            }
            return state;
        }

        /// <summary>
        /// Get the highest tile value on the grid
        /// </summary>
        public int GetHighestTileValue()
        {
            int highest = 0;
            for (int row = 0; row < GridSize; row++)
            {
                for (int col = 0; col < GridSize; col++)
                {
                    int value = _grid[row, col]?.Value ?? 0;
                    if (value > highest)
                        highest = value;
                }
            }
            return highest;
        }

        /// <summary>
        /// Debug: Print grid state to console
        /// </summary>
        public void DebugPrintGrid()
        {
            Debug.Log("=== GameBoard Grid State ===");
            for (int row = 0; row < GridSize; row++)
            {
                string line = "";
                for (int col = 0; col < GridSize; col++)
                {
                    int value = _grid[row, col]?.Value ?? 0;
                    line += value == 0 ? "[    ]" : $"[{value,4}]";
                }
                Debug.Log(line);
            }
            Debug.Log($"Score: {CurrentScore} | Best: {BestScore} | State: {CurrentState}");
            Debug.Log("============================");
        }

        #endregion
    }
}
