using System;
using System.Collections.Generic;
using UnityEngine;

namespace FinalNumber.SumPuzzle
{
    /// <summary>
    /// Represents a cell in the sum puzzle grid
    /// </summary>
    public class SumPuzzleCell
    {
        public int Row { get; set; }
        public int Column { get; set; }
        public int Value { get; set; }
        public bool IsFixed { get; set; }  // Pre-filled cells that cannot be changed
        public bool IsValid { get; set; }  // Whether current value satisfies constraints

        public SumPuzzleCell(int row, int column)
        {
            Row = row;
            Column = column;
            Value = 0;
            IsFixed = false;
            IsValid = true;
        }
    }

    /// <summary>
    /// Level configuration for sum puzzle
    /// </summary>
    [Serializable]
    public class SumPuzzleLevel
    {
        public int LevelId;
        public int GridSize;  // 3x3, 4x4, or 5x5
        public int[] RowTargets;  // Target sum for each row
        public int[] ColumnTargets;  // Target sum for each column
        public int[] FixedCells;  // Pre-filled cells [row, col, value, ...]
        public int TimeLimit;  // Seconds, 0 = no limit
        public int MoveLimit;  // 0 = no limit
    }

    /// <summary>
    /// Manages the sum puzzle grid state and validation.
    /// Each row and column has a target sum that must be achieved.
    /// </summary>
    public class SumPuzzleBoard
    {
        public int GridSize { get; private set; }
        public int CurrentLevel { get; private set; }

        private SumPuzzleCell[,] _grid;
        private int[] _rowTargets;
        private int[] _columnTargets;
        private int _moves;

        // Events
        public event EventHandler OnGridChanged;
        public event EventHandler<int> OnNumberPlaced;
        public event EventHandler<int> OnNumberRemoved;
        public event EventHandler OnRowCompleted;
        public event EventHandler OnColumnCompleted;
        public event EventHandler OnBoardCompleted;

        public SumPuzzleBoard(int gridSize = 4)
        {
            GridSize = gridSize;
            _moves = 0;
            InitializeGrid();
        }

        /// <summary>
        /// Initialize empty grid
        /// </summary>
        private void InitializeGrid()
        {
            _grid = new SumPuzzleCell[GridSize, GridSize];
            for (int row = 0; row < GridSize; row++)
            {
                for (int col = 0; col < GridSize; col++)
                {
                    _grid[row, col] = new SumPuzzleCell(row, col);
                }
            }
        }

        /// <summary>
        /// Set up a level with targets
        /// </summary>
        public void SetupLevel(SumPuzzleLevel level)
        {
            CurrentLevel = level.LevelId;

            // Resize grid if needed
            if (level.GridSize != GridSize)
            {
                GridSize = level.GridSize;
                InitializeGrid();
            }
            else
            {
                ClearBoard();
            }

            // Set targets
            _rowTargets = new int[GridSize];
            _columnTargets = new int[GridSize];
            Array.Copy(level.RowTargets, _rowTargets, GridSize);
            Array.Copy(level.ColumnTargets, _columnTargets, GridSize);

            // Set fixed cells
            for (int i = 0; i < level.FixedCells.Length; i += 3)
            {
                int row = level.FixedCells[i];
                int col = level.FixedCells[i + 1];
                int value = level.FixedCells[i + 2];
                PlaceNumber(value, row, col, true);
            }

            _moves = 0;
            ValidateBoard();
            OnGridChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Clear the board
        /// </summary>
        public void ClearBoard()
        {
            for (int row = 0; row < GridSize; row++)
            {
                for (int col = 0; col < GridSize; col++)
                {
                    _grid[row, col].Value = 0;
                    _grid[row, col].IsFixed = false;
                    _grid[row, col].IsValid = true;
                }
            }
            _moves = 0;
        }

        /// <summary>
        /// Place a number on the grid
        /// </summary>
        public bool PlaceNumber(int number, int row, int column, bool isFixed = false)
        {
            if (!IsValidPosition(row, column))
                return false;

            if (number < 1 || number > 9)
                return false;

            var cell = _grid[row, column];
            if (cell.IsFixed)
                return false;  // Cannot change fixed cells

            bool isNewPlacement = cell.Value == 0;
            cell.Value = number;
            cell.IsFixed = isFixed;

            if (isNewPlacement && !isFixed)
            {
                _moves++;
                OnNumberPlaced?.Invoke(this, number);
            }

            // Validate the affected row and column
            ValidateRow(row);
            ValidateColumn(column);

            OnGridChanged?.Invoke(this, EventArgs.Empty);

            // Check if board is complete
            if (isNewPlacement && !isFixed && IsBoardComplete())
            {
                OnBoardCompleted?.Invoke(this, EventArgs.Empty);
            }

            return true;
        }

        /// <summary>
        /// Remove a number from the grid
        /// </summary>
        public bool RemoveNumber(int row, int column)
        {
            if (!IsValidPosition(row, column))
                return false;

            var cell = _grid[row, column];
            if (cell.IsFixed || cell.Value == 0)
                return false;

            int removedValue = cell.Value;
            cell.Value = 0;

            _moves++;
            OnNumberRemoved?.Invoke(this, removedValue);

            // Validate the affected row and column
            ValidateRow(row);
            ValidateColumn(column);

            OnGridChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        /// <summary>
        /// Get cell at position
        /// </summary>
        public SumPuzzleCell GetCell(int row, int column)
        {
            if (!IsValidPosition(row, column))
                return null;
            return _grid[row, column];
        }

        /// <summary>
        /// Get target sum for a row
        /// </summary>
        public int GetRowTarget(int row)
        {
            if (row < 0 || row >= GridSize)
                return 0;
            return _rowTargets[row];
        }

        /// <summary>
        /// Get target sum for a column
        /// </summary>
        public int GetColumnTarget(int column)
        {
            if (column < 0 || column >= GridSize)
                return 0;
            return _columnTargets[column];
        }

        /// <summary>
        /// Calculate current sum of a row
        /// </summary>
        public int GetRowSum(int row)
        {
            if (row < 0 || row >= GridSize)
                return 0;

            int sum = 0;
            for (int col = 0; col < GridSize; col++)
            {
                sum += _grid[row, col].Value;
            }
            return sum;
        }

        /// <summary>
        /// Calculate current sum of a column
        /// </summary>
        public int GetColumnSum(int column)
        {
            if (column < 0 || column >= GridSize)
                return 0;

            int sum = 0;
            for (int row = 0; row < GridSize; row++)
            {
                sum += _grid[row, column].Value;
            }
            return sum;
        }

        /// <summary>
        /// Get remaining sum needed for a row
        /// </summary>
        public int GetRowRemaining(int row)
        {
            return GetRowTarget(row) - GetRowSum(row);
        }

        /// <summary>
        /// Get remaining sum needed for a column
        /// </summary>
        public int GetColumnRemaining(int column)
        {
            return GetColumnTarget(column) - GetColumnSum(column);
        }

        /// <summary>
        /// Check if a row is complete (matches target)
        /// </summary>
        public bool IsRowComplete(int row)
        {
            if (row < 0 || row >= GridSize)
                return false;

            // Row is complete if all cells are filled and sum matches target
            for (int col = 0; col < GridSize; col++)
            {
                if (_grid[row, col].Value == 0)
                    return false;
            }

            return GetRowSum(row) == GetRowTarget(row);
        }

        /// <summary>
        /// Check if a column is complete
        /// </summary>
        public bool IsColumnComplete(int column)
        {
            if (column < 0 || column >= GridSize)
                return false;

            for (int row = 0; row < GridSize; row++)
            {
                if (_grid[row, column].Value == 0)
                    return false;
            }

            return GetColumnSum(column) == GetColumnTarget(column);
        }

        /// <summary>
        /// Check if entire board is complete
        /// </summary>
        public bool IsBoardComplete()
        {
            for (int i = 0; i < GridSize; i++)
            {
                if (!IsRowComplete(i) || !IsColumnComplete(i))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Validate all rows and columns
        /// </summary>
        public void ValidateBoard()
        {
            for (int i = 0; i < GridSize; i++)
            {
                ValidateRow(i);
                ValidateColumn(i);
            }
        }

        /// <summary>
        /// Validate a row - check if current values can still reach target
        /// </summary>
        private void ValidateRow(int row)
        {
            int currentSum = GetRowSum(row);
            int target = GetRowTarget(row);
            int remaining = target - currentSum;
            int emptyCells = GetEmptyCellCountInRow(row);

            bool canStillReachTarget = true;
            if (emptyCells == 0)
            {
                canStillReachTarget = currentSum == target;
            }
            else
            {
                // Check if remaining sum is achievable with empty cells (1-9 each)
                int minPossible = emptyCells * 1;
                int maxPossible = emptyCells * 9;
                canStillReachTarget = remaining >= minPossible && remaining <= maxPossible;
            }

            for (int col = 0; col < GridSize; col++)
            {
                if (!_grid[row, col].IsFixed)
                {
                    _grid[row, col].IsValid = canStillReachTarget;
                }
            }
        }

        /// <summary>
        /// Validate a column
        /// </summary>
        private void ValidateColumn(int column)
        {
            int currentSum = GetColumnSum(column);
            int target = GetColumnTarget(column);
            int remaining = target - currentSum;
            int emptyCells = GetEmptyCellCountInColumn(column);

            bool canStillReachTarget = true;
            if (emptyCells == 0)
            {
                canStillReachTarget = currentSum == target;
            }
            else
            {
                int minPossible = emptyCells * 1;
                int maxPossible = emptyCells * 9;
                canStillReachTarget = remaining >= minPossible && remaining <= maxPossible;
            }

            for (int row = 0; row < GridSize; row++)
            {
                if (!_grid[row, column].IsFixed)
                {
                    // Preserve validity if already invalid from row check
                    _grid[row, column].IsValid = _grid[row, column].IsValid && canStillReachTarget;
                }
            }
        }

        /// <summary>
        /// Count empty cells in a row
        /// </summary>
        private int GetEmptyCellCountInRow(int row)
        {
            int count = 0;
            for (int col = 0; col < GridSize; col++)
            {
                if (_grid[row, col].Value == 0)
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Count empty cells in a column
        /// </summary>
        private int GetEmptyCellCountInColumn(int column)
        {
            int count = 0;
            for (int row = 0; row < GridSize; row++)
            {
                if (_grid[row, column].Value == 0)
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Check if position is valid
        /// </summary>
        public bool IsValidPosition(int row, int column)
        {
            return row >= 0 && row < GridSize && column >= 0 && column < GridSize;
        }

        /// <summary>
        /// Get current move count
        /// </summary>
        public int GetMoveCount()
        {
            return _moves;
        }

        /// <summary>
        /// Get completion percentage (0-100)
        /// </summary>
        public float GetCompletionPercentage()
        {
            int filledCells = 0;
            for (int row = 0; row < GridSize; row++)
            {
                for (int col = 0; col < GridSize; col++)
                {
                    if (_grid[row, col].Value > 0)
                        filledCells++;
                }
            }
            return (float)filledCells / (GridSize * GridSize) * 100f;
        }

        /// <summary>
        /// Get hint - suggests a valid placement
        /// </summary>
        public (int row, int column, int number)? GetHint()
        {
            // Find a cell where we can determine the value
            for (int row = 0; row < GridSize; row++)
            {
                int emptyInRow = GetEmptyCellCountInRow(row);
                if (emptyInRow == 1)
                {
                    // Can calculate the exact value needed
                    int remaining = GetRowRemaining(row);
                    if (remaining >= 1 && remaining <= 9)
                    {
                        // Find the empty cell in this row
                        for (int col = 0; col < GridSize; col++)
                        {
                            if (_grid[row, col].Value == 0)
                            {
                                // Check if this value also works for the column
                                int colRemaining = GetColumnRemaining(col);
                                if (colRemaining == remaining)
                                {
                                    return (row, col, remaining);
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Debug: Print board state
        /// </summary>
        public void DebugPrintBoard()
        {
            Debug.Log("=== SumPuzzle Board ===");
            Debug.Log("Row targets: " + string.Join(", ", _rowTargets));
            Debug.Log("Col targets: " + string.Join(", ", _columnTargets));

            for (int row = 0; row < GridSize; row++)
            {
                string line = "";
                for (int col = 0; col < GridSize; col++)
                {
                    int value = _grid[row, col].Value;
                    bool isFixed = _grid[row, col].IsFixed;
                    line += value == 0 ? "[   ]" : $"[{value}{(isFixed ? "F" : " ")}]";
                }
                line += $" = {_rowTargets[row]}";
                Debug.Log(line);
            }

            string colSums = "";
            for (int col = 0; col < GridSize; col++)
            {
                colSums += $" {_columnTargets[col]} ";
            }
            Debug.Log("  " + colSums);
            Debug.Log("======================");
        }
    }
}
