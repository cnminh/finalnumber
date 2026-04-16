using System;
using UnityEngine;

namespace FinalNumber.Gameplay
{
    /// <summary>
    /// Direction of swipe/movement in the game
    /// </summary>
    public enum MoveDirection
    {
        Up,
        Down,
        Left,
        Right,
        None
    }

    /// <summary>
    /// Event arguments for tile movement
    /// </summary>
    public class TileMoveEventArgs : EventArgs
    {
        public Tile Tile { get; set; }
        public int FromRow { get; set; }
        public int FromColumn { get; set; }
        public int ToRow { get; set; }
        public int ToColumn { get; set; }
        public bool IsMerge { get; set; }
        public int ScoreGained { get; set; }
    }

    /// <summary>
    /// Event arguments for new tile spawn
    /// </summary>
    public class TileSpawnEventArgs : EventArgs
    {
        public Tile Tile { get; set; }
        public int Row { get; set; }
        public int Column { get; set; }
    }

    /// <summary>
    /// Manages the 4x4 game grid state and operations.
    /// Handles tile placement, movement, and grid queries.
    /// </summary>
    public class GridManager
    {
        public const int GridSize = 4;
        public const int MaxTiles = GridSize * GridSize;

        private Tile[,] _grid;
        private System.Random _random;

        // Events
        public event EventHandler<TileMoveEventArgs> OnTileMoved;
        public event EventHandler<TileSpawnEventArgs> OnTileSpawned;
        public event EventHandler OnGridChanged;

        public GridManager(int seed = 0)
        {
            _grid = new Tile[GridSize, GridSize];
            _random = seed == 0 ? new System.Random() : new System.Random(seed);
        }

        /// <summary>
        /// Internal constructor for creating a grid manager from an existing grid.
        /// Used for simulation/testing purposes.
        /// </summary>
        private GridManager(Tile[,] grid, int seed = 0)
        {
            _grid = grid ?? new Tile[GridSize, GridSize];
            _random = seed == 0 ? new System.Random() : new System.Random(seed);
        }

        /// <summary>
        /// Creates a GridManager from an existing grid (for simulation/testing).
        /// </summary>
        public static GridManager FromGrid(Tile[,] grid, int seed = 0)
        {
            return new GridManager(grid, seed);
        }

        /// <summary>
        /// Initialize the grid with empty cells
        /// </summary>
        public void InitializeGrid()
        {
            for (int row = 0; row < GridSize; row++)
            {
                for (int col = 0; col < GridSize; col++)
                {
                    _grid[row, col] = null;
                }
            }
        }

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

        /// <summary>
        /// Place a tile at specific position
        /// </summary>
        public bool PlaceTile(Tile tile, int row, int column)
        {
            if (!IsEmpty(row, column))
                return false;

            tile.SetPosition(row, column);
            _grid[row, column] = tile;
            
            OnTileSpawned?.Invoke(this, new TileSpawnEventArgs 
            { 
                Tile = tile, 
                Row = row, 
                Column = column 
            });
            
            OnGridChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        /// <summary>
        /// Remove a tile from specific position
        /// </summary>
        public Tile RemoveTile(int row, int column)
        {
            if (!IsValidPosition(row, column))
                return null;

            Tile tile = _grid[row, column];
            _grid[row, column] = null;
            return tile;
        }

        /// <summary>
        /// Move a tile from one position to another
        /// </summary>
        public bool MoveTile(int fromRow, int fromColumn, int toRow, int toColumn, bool isMerge = false, int scoreGained = 0)
        {
            if (!IsValidPosition(fromRow, fromColumn) || !IsValidPosition(toRow, toColumn))
                return false;

            Tile tile = _grid[fromRow, fromColumn];
            if (tile == null)
                return false;

            // Remove from source
            _grid[fromRow, fromColumn] = null;

            // Handle merge: remove the tile at destination if merging
            Tile mergedTile = null;
            if (isMerge && _grid[toRow, toColumn] != null)
            {
                mergedTile = _grid[toRow, toColumn];
                _grid[toRow, toColumn] = null;
            }

            // Place at destination
            tile.SetPosition(toRow, toColumn);
            _grid[toRow, toColumn] = tile;

            if (isMerge && mergedTile != null)
            {
                tile.Merge(mergedTile);
            }

            OnTileMoved?.Invoke(this, new TileMoveEventArgs
            {
                Tile = tile,
                FromRow = fromRow,
                FromColumn = fromColumn,
                ToRow = toRow,
                ToColumn = toColumn,
                IsMerge = isMerge,
                ScoreGained = scoreGained
            });

            OnGridChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        /// <summary>
        /// Spawn a new tile in a random empty cell
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
                            PlaceTile(tile, row, col);
                            return tile;
                        }
                        currentIndex++;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Reset all tile merge states (call after each move)
        /// </summary>
        public void ResetMergeStates()
        {
            for (int row = 0; row < GridSize; row++)
            {
                for (int col = 0; col < GridSize; col++)
                {
                    _grid[row, col]?.ResetMergeState();
                }
            }
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
        /// Check if 2048 tile exists (win condition)
        /// </summary>
        public bool HasWinningTile(int targetValue = 2048)
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
        /// Get current grid state as a 2D array
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
        /// Create a deep copy of the current grid state
        /// </summary>
        public Tile[,] CloneGrid()
        {
            Tile[,] clone = new Tile[GridSize, GridSize];
            for (int row = 0; row < GridSize; row++)
            {
                for (int col = 0; col < GridSize; col++)
                {
                    if (_grid[row, col] != null)
                    {
                        clone[row, col] = new Tile(_grid[row, col].Value, row, col);
                    }
                }
            }
            return clone;
        }

        /// <summary>
        /// Debug: Print grid state to console
        /// </summary>
        public void DebugPrintGrid()
        {
            Debug.Log("=== Grid State ===");
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
            Debug.Log("==================");
        }
    }
}
