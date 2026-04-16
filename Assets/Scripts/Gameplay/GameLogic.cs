using System;
using System.Collections.Generic;
using UnityEngine;

namespace FinalNumber.Gameplay
{
    /// <summary>
    /// Contains the core 2048 game logic for tile movement and merging.
    /// Processes moves in all four directions.
    /// </summary>
    public class GameLogic
    {
        private GridManager _gridManager;

        // Events
        public event EventHandler OnMoveExecuted;
        public event EventHandler OnInvalidMove;

        public GameLogic(GridManager gridManager)
        {
            _gridManager = gridManager ?? throw new ArgumentNullException(nameof(gridManager));
        }

        /// <summary>
        /// Execute a move in the specified direction.
        /// Returns true if the move changed the grid state.
        /// </summary>
        public bool ExecuteMove(MoveDirection direction)
        {
            if (direction == MoveDirection.None)
                return false;

            bool moved = false;
            int totalScoreGained = 0;

            // Reset merge states before processing
            _gridManager.ResetMergeStates();

            // Process each row/column based on direction
            switch (direction)
            {
                case MoveDirection.Left:
                    (moved, totalScoreGained) = ProcessLeft();
                    break;
                case MoveDirection.Right:
                    (moved, totalScoreGained) = ProcessRight();
                    break;
                case MoveDirection.Up:
                    (moved, totalScoreGained) = ProcessUp();
                    break;
                case MoveDirection.Down:
                    (moved, totalScoreGained) = ProcessDown();
                    break;
            }

            if (moved)
            {
                OnMoveExecuted?.Invoke(this, EventArgs.Empty);
                return true;
            }
            else
            {
                OnInvalidMove?.Invoke(this, EventArgs.Empty);
                return false;
            }
        }

        /// <summary>
        /// Process a leftward move
        /// </summary>
        private (bool moved, int score) ProcessLeft()
        {
            bool moved = false;
            int totalScore = 0;

            for (int row = 0; row < GridManager.GridSize; row++)
            {
                var (rowMoved, rowScore) = ProcessLineLeft(row);
                if (rowMoved) moved = true;
                totalScore += rowScore;
            }

            return (moved, totalScore);
        }

        /// <summary>
        /// Process a single row moving left
        /// </summary>
        private (bool moved, int score) ProcessLineLeft(int row)
        {
            bool moved = false;
            int score = 0;

            // Collect non-null tiles in this row
            List<Tile> tiles = new List<Tile>();
            for (int col = 0; col < GridManager.GridSize; col++)
            {
                var tile = _gridManager.GetTile(row, col);
                if (tile != null)
                    tiles.Add(tile);
            }

            if (tiles.Count == 0)
                return (false, 0);

            // Process merges and moves
            List<(Tile tile, int targetCol, bool isMerge, int scoreGained)> moves = new List<(Tile, int, bool, int)>();
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
                    moves.Add((current, targetPosition, true, gainedScore));
                    score += gainedScore;
                    tiles[i - 1].HasMerged = true; // Mark the merged tile
                }
                else
                {
                    // Just move
                    if (originalCol != targetPosition)
                    {
                        moves.Add((current, targetPosition, false, 0));
                    }
                    targetPosition++;
                }
            }

            // Execute the moves
            foreach (var (tile, targetCol, isMerge, gained) in moves)
            {
                int originalCol = tile.Column;
                _gridManager.MoveTile(row, originalCol, row, targetCol, isMerge, gained);
                moved = true;
            }

            return (moved, score);
        }

        /// <summary>
        /// Process a rightward move
        /// </summary>
        private (bool moved, int score) ProcessRight()
        {
            bool moved = false;
            int totalScore = 0;

            for (int row = 0; row < GridManager.GridSize; row++)
            {
                var (rowMoved, rowScore) = ProcessLineRight(row);
                if (rowMoved) moved = true;
                totalScore += rowScore;
            }

            return (moved, totalScore);
        }

        /// <summary>
        /// Process a single row moving right
        /// </summary>
        private (bool moved, int score) ProcessLineRight(int row)
        {
            bool moved = false;
            int score = 0;

            // Collect non-null tiles in this row (right to left)
            List<Tile> tiles = new List<Tile>();
            for (int col = GridManager.GridSize - 1; col >= 0; col--)
            {
                var tile = _gridManager.GetTile(row, col);
                if (tile != null)
                    tiles.Add(tile);
            }

            if (tiles.Count == 0)
                return (false, 0);

            // Process merges and moves
            List<(Tile tile, int targetCol, bool isMerge, int scoreGained)> moves = new List<(Tile, int, bool, int)>();
            int targetPosition = GridManager.GridSize - 1;

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
                    moves.Add((current, targetPosition, true, gainedScore));
                    score += gainedScore;
                    tiles[i - 1].HasMerged = true;
                }
                else
                {
                    // Just move
                    if (originalCol != targetPosition)
                    {
                        moves.Add((current, targetPosition, false, 0));
                    }
                    targetPosition--;
                }
            }

            // Execute the moves
            foreach (var (tile, targetCol, isMerge, gained) in moves)
            {
                int originalCol = tile.Column;
                _gridManager.MoveTile(row, originalCol, row, targetCol, isMerge, gained);
                moved = true;
            }

            return (moved, score);
        }

        /// <summary>
        /// Process an upward move
        /// </summary>
        private (bool moved, int score) ProcessUp()
        {
            bool moved = false;
            int totalScore = 0;

            for (int col = 0; col < GridManager.GridSize; col++)
            {
                var (colMoved, colScore) = ProcessColumnUp(col);
                if (colMoved) moved = true;
                totalScore += colScore;
            }

            return (moved, totalScore);
        }

        /// <summary>
        /// Process a single column moving up
        /// </summary>
        private (bool moved, int score) ProcessColumnUp(int col)
        {
            bool moved = false;
            int score = 0;

            // Collect non-null tiles in this column (top to bottom)
            List<Tile> tiles = new List<Tile>();
            for (int row = 0; row < GridManager.GridSize; row++)
            {
                var tile = _gridManager.GetTile(row, col);
                if (tile != null)
                    tiles.Add(tile);
            }

            if (tiles.Count == 0)
                return (false, 0);

            // Process merges and moves
            List<(Tile tile, int targetRow, bool isMerge, int scoreGained)> moves = new List<(Tile, int, bool, int)>();
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
                    moves.Add((current, targetPosition, true, gainedScore));
                    score += gainedScore;
                    tiles[i - 1].HasMerged = true;
                }
                else
                {
                    // Just move
                    if (originalRow != targetPosition)
                    {
                        moves.Add((current, targetPosition, false, 0));
                    }
                    targetPosition++;
                }
            }

            // Execute the moves
            foreach (var (tile, targetRow, isMerge, gained) in moves)
            {
                int originalRow = tile.Row;
                _gridManager.MoveTile(originalRow, col, targetRow, col, isMerge, gained);
                moved = true;
            }

            return (moved, score);
        }

        /// <summary>
        /// Process a downward move
        /// </summary>
        private (bool moved, int score) ProcessDown()
        {
            bool moved = false;
            int totalScore = 0;

            for (int col = 0; col < GridManager.GridSize; col++)
            {
                var (colMoved, colScore) = ProcessColumnDown(col);
                if (colMoved) moved = true;
                totalScore += colScore;
            }

            return (moved, totalScore);
        }

        /// <summary>
        /// Process a single column moving down
        /// </summary>
        private (bool moved, int score) ProcessColumnDown(int col)
        {
            bool moved = false;
            int score = 0;

            // Collect non-null tiles in this column (bottom to top)
            List<Tile> tiles = new List<Tile>();
            for (int row = GridManager.GridSize - 1; row >= 0; row--)
            {
                var tile = _gridManager.GetTile(row, col);
                if (tile != null)
                    tiles.Add(tile);
            }

            if (tiles.Count == 0)
                return (false, 0);

            // Process merges and moves
            List<(Tile tile, int targetRow, bool isMerge, int scoreGained)> moves = new List<(Tile, int, bool, int)>();
            int targetPosition = GridManager.GridSize - 1;

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
                    moves.Add((current, targetPosition, true, gainedScore));
                    score += gainedScore;
                    tiles[i - 1].HasMerged = true;
                }
                else
                {
                    // Just move
                    if (originalRow != targetPosition)
                    {
                        moves.Add((current, targetPosition, false, 0));
                    }
                    targetPosition--;
                }
            }

            // Execute the moves
            foreach (var (tile, targetRow, isMerge, gained) in moves)
            {
                int originalRow = tile.Row;
                _gridManager.MoveTile(originalRow, col, targetRow, col, isMerge, gained);
                moved = true;
            }

            return (moved, score);
        }

        /// <summary>
        /// Simulate a move without actually executing it.
        /// Useful for AI hints or checking if any move is possible.
        /// </summary>
        public (bool canMove, int potentialScore) SimulateMove(MoveDirection direction)
        {
            // Clone the current grid state
            var clonedGrid = _gridManager.CloneGrid();

            // Create a temporary grid manager with the clone using factory method
            var tempGrid = GridManager.FromGrid(clonedGrid);
            var tempLogic = new GameLogic(tempGrid);

            // Subscribe to events to capture score from the TEMP grid (not original)
            int capturedScore = 0;
            tempGrid.OnTileMoved += (s, e) =>
            {
                if (e is TileMoveEventArgs args)
                    capturedScore += args.ScoreGained;
            };

            bool canMove = tempLogic.ExecuteMove(direction);

            return (canMove, capturedScore);
        }

        /// <summary>
        /// Check if a specific move would change the grid state
        /// </summary>
        public bool CanMove(MoveDirection direction)
        {
            var (canMove, _) = SimulateMove(direction);
            return canMove;
        }

        /// <summary>
        /// Get all valid moves for the current grid state
        /// </summary>
        public List<MoveDirection> GetValidMoves()
        {
            var validMoves = new List<MoveDirection>();
            
            foreach (MoveDirection dir in Enum.GetValues(typeof(MoveDirection)))
            {
                if (dir != MoveDirection.None && CanMove(dir))
                    validMoves.Add(dir);
            }
            
            return validMoves;
        }
    }
}
