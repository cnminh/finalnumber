using NUnit.Framework;
using FinalNumber.Gameplay;
using System.Collections.Generic;

namespace FinalNumber.Tests.EditMode
{
    /// <summary>
    /// Unit tests for the GameLogic class
    /// </summary>
    public class GameLogicTests
    {
        private GridManager _gridManager;
        private GameLogic _gameLogic;

        [SetUp]
        public void Setup()
        {
            _gridManager = new GridManager(seed: 12345);
            _gridManager.InitializeGrid();
            _gameLogic = new GameLogic(_gridManager);
        }

        [Test]
        public void ExecuteMove_Left_MovesTiles()
        {
            // Arrange
            _gridManager.PlaceTile(new Tile(2, 0, 1), 0, 1);
            _gridManager.PlaceTile(new Tile(4, 0, 3), 0, 3);

            // Act
            bool moved = _gameLogic.ExecuteMove(MoveDirection.Left);

            // Assert
            Assert.IsTrue(moved, "Move should succeed");
            Assert.AreEqual(2, _gridManager.GetTile(0, 0)?.Value, "Tile 2 should be at column 0");
            Assert.AreEqual(4, _gridManager.GetTile(0, 1)?.Value, "Tile 4 should be at column 1");
        }

        [Test]
        public void ExecuteMove_Left_MergesMatchingTiles()
        {
            // Arrange
            _gridManager.PlaceTile(new Tile(2, 0, 0), 0, 0);
            _gridManager.PlaceTile(new Tile(2, 0, 1), 0, 1);

            // Act
            bool moved = _gameLogic.ExecuteMove(MoveDirection.Left);

            // Assert
            Assert.IsTrue(moved, "Move should succeed");
            Assert.AreEqual(4, _gridManager.GetTile(0, 0)?.Value, "Merged tile should be 4");
            Assert.IsNull(_gridManager.GetTile(0, 1), "Second position should be empty");
        }

        [Test]
        public void ExecuteMove_Right_MovesAndMerges()
        {
            // Arrange
            _gridManager.PlaceTile(new Tile(2, 0, 2), 0, 2);
            _gridManager.PlaceTile(new Tile(2, 0, 3), 0, 3);

            // Act
            bool moved = _gameLogic.ExecuteMove(MoveDirection.Right);

            // Assert
            Assert.IsTrue(moved, "Move should succeed");
            Assert.AreEqual(4, _gridManager.GetTile(0, 3)?.Value, "Merged tile should be at column 3");
            Assert.IsNull(_gridManager.GetTile(0, 2), "Position 2 should be empty");
        }

        [Test]
        public void ExecuteMove_Up_MovesAndMerges()
        {
            // Arrange
            _gridManager.PlaceTile(new Tile(4, 2, 0), 2, 0);
            _gridManager.PlaceTile(new Tile(4, 3, 0), 3, 0);

            // Act
            bool moved = _gameLogic.ExecuteMove(MoveDirection.Up);

            // Assert
            Assert.IsTrue(moved, "Move should succeed");
            Assert.AreEqual(8, _gridManager.GetTile(0, 0)?.Value, "Merged tile should be at row 0");
            Assert.IsNull(_gridManager.GetTile(1, 0), "Position (1,0) should be empty");
            Assert.IsNull(_gridManager.GetTile(2, 0), "Position (2,0) should be empty");
            Assert.IsNull(_gridManager.GetTile(3, 0), "Position (3,0) should be empty");
        }

        [Test]
        public void ExecuteMove_Down_MovesAndMerges()
        {
            // Arrange
            _gridManager.PlaceTile(new Tile(8, 0, 0), 0, 0);
            _gridManager.PlaceTile(new Tile(8, 1, 0), 1, 0);

            // Act
            bool moved = _gameLogic.ExecuteMove(MoveDirection.Down);

            // Assert
            Assert.IsTrue(moved, "Move should succeed");
            Assert.AreEqual(16, _gridManager.GetTile(3, 0)?.Value, "Merged tile should be at row 3");
        }

        [Test]
        public void ExecuteMove_NoChange_ReturnsFalse()
        {
            // Arrange - Tiles already against the left wall
            _gridManager.PlaceTile(new Tile(2, 0, 0), 0, 0);
            _gridManager.PlaceTile(new Tile(4, 0, 1), 0, 1);

            // Act
            bool moved = _gameLogic.ExecuteMove(MoveDirection.Left);

            // Assert
            Assert.IsFalse(moved, "Move should not change anything");
        }

        [Test]
        public void ExecuteMove_NoMergeWhenValuesDiffer()
        {
            // Arrange
            _gridManager.PlaceTile(new Tile(2, 0, 0), 0, 0);
            _gridManager.PlaceTile(new Tile(4, 0, 1), 0, 1);

            // Act
            bool moved = _gameLogic.ExecuteMove(MoveDirection.Left);

            // Assert
            Assert.IsFalse(moved, "Should not move when blocked by different value");
            Assert.AreEqual(2, _gridManager.GetTile(0, 0)?.Value, "Tile 2 should stay at position 0");
            Assert.AreEqual(4, _gridManager.GetTile(0, 1)?.Value, "Tile 4 should stay at position 1");
        }

        [Test]
        public void ExecuteMove_ChainMerge_MergesOnce()
        {
            // Arrange - Three 2s in a row should merge into one 4 and one 2
            _gridManager.PlaceTile(new Tile(2, 0, 0), 0, 0);
            _gridManager.PlaceTile(new Tile(2, 0, 1), 0, 1);
            _gridManager.PlaceTile(new Tile(2, 0, 2), 0, 2);

            // Act
            bool moved = _gameLogic.ExecuteMove(MoveDirection.Left);

            // Assert
            Assert.IsTrue(moved, "Move should succeed");
            Assert.AreEqual(4, _gridManager.GetTile(0, 0)?.Value, "First two 2s should merge into 4");
            Assert.AreEqual(2, _gridManager.GetTile(0, 1)?.Value, "Third 2 should move to position 1");
            Assert.IsNull(_gridManager.GetTile(0, 2), "Position 2 should be empty");
        }

        [Test]
        public void ExecuteMove_ComplexScenario_MultipleMerges()
        {
            // Arrange: Row with [2, 2, 4, 4]
            _gridManager.PlaceTile(new Tile(2, 0, 0), 0, 0);
            _gridManager.PlaceTile(new Tile(2, 0, 1), 0, 1);
            _gridManager.PlaceTile(new Tile(4, 0, 2), 0, 2);
            _gridManager.PlaceTile(new Tile(4, 0, 3), 0, 3);

            // Act
            bool moved = _gameLogic.ExecuteMove(MoveDirection.Left);

            // Assert: Result should be [4, 8, empty, empty]
            Assert.IsTrue(moved, "Move should succeed");
            Assert.AreEqual(4, _gridManager.GetTile(0, 0)?.Value, "Position 0 should be 4");
            Assert.AreEqual(8, _gridManager.GetTile(0, 1)?.Value, "Position 1 should be 8");
            Assert.IsNull(_gridManager.GetTile(0, 2), "Position 2 should be empty");
            Assert.IsNull(_gridManager.GetTile(0, 3), "Position 3 should be empty");
        }

        [Test]
        public void CanMove_ReturnsTrueWhenMovePossible()
        {
            // Arrange
            _gridManager.PlaceTile(new Tile(2, 0, 1), 0, 1);

            // Act & Assert
            Assert.IsTrue(_gameLogic.CanMove(MoveDirection.Left), "Should be able to move left");
            Assert.IsFalse(_gameLogic.CanMove(MoveDirection.Right), "Should not be able to move right (blocked by wall)");
        }

        [Test]
        public void GetValidMoves_ReturnsAllPossibleMoves()
        {
            // Arrange - Tile in center
            _gridManager.PlaceTile(new Tile(2, 1, 1), 1, 1);

            // Act
            var validMoves = _gameLogic.GetValidMoves();

            // Assert
            Assert.AreEqual(4, validMoves.Count, "All 4 directions should be valid");
            Assert.Contains(MoveDirection.Up, validMoves);
            Assert.Contains(MoveDirection.Down, validMoves);
            Assert.Contains(MoveDirection.Left, validMoves);
            Assert.Contains(MoveDirection.Right, validMoves);
        }

        [Test]
        public void GetValidMoves_ReturnsEmptyWhenGridFull()
        {
            // Arrange - Full grid with no mergeable tiles
            int[] values = { 2, 4, 2, 4, 4, 2, 4, 2, 2, 4, 2, 4, 4, 2, 4, 2 };
            int index = 0;
            for (int row = 0; row < GridManager.GridSize; row++)
            {
                for (int col = 0; col < GridManager.GridSize; col++)
                {
                    _gridManager.PlaceTile(new Tile(values[index++], row, col), row, col);
                }
            }

            // Act
            var validMoves = _gameLogic.GetValidMoves();

            // Assert
            Assert.AreEqual(0, validMoves.Count, "No valid moves should exist");
        }

        [Test]
        public void ExecuteMove_None_ReturnsFalse()
        {
            // Act
            bool moved = _gameLogic.ExecuteMove(MoveDirection.None);

            // Assert
            Assert.IsFalse(moved, "None direction should not cause a move");
        }

        [Test]
        public void MultipleMoves_ResetMergeStatesCorrectly()
        {
            // Arrange
            _gridManager.PlaceTile(new Tile(2, 0, 0), 0, 0);
            _gridManager.PlaceTile(new Tile(2, 0, 1), 0, 1);

            // Act - First merge
            _gameLogic.ExecuteMove(MoveDirection.Left);

            // The 4 should be able to merge again if we add another 4
            _gridManager.PlaceTile(new Tile(4, 0, 1), 0, 1);
            bool secondMerge = _gameLogic.ExecuteMove(MoveDirection.Left);

            // Assert
            Assert.IsTrue(secondMerge, "Second merge should be possible");
            Assert.AreEqual(8, _gridManager.GetTile(0, 0)?.Value, "Should have 8 after two merges");
        }

        #region Simulation Tests

        [Test]
        public void SimulateMove_ReturnsCorrectScore()
        {
            // Arrange
            _gridManager.PlaceTile(new Tile(2, 0, 0), 0, 0);
            _gridManager.PlaceTile(new Tile(2, 0, 1), 0, 1);

            // Act
            var (canMove, potentialScore) = _gameLogic.SimulateMove(MoveDirection.Left);

            // Assert
            Assert.IsTrue(canMove, "Should be able to move");
            Assert.AreEqual(4, potentialScore, "Should predict 4 points from merging two 2s");

            // Verify original grid is unchanged
            Assert.AreEqual(2, _gridManager.GetTile(0, 0)?.Value, "Original grid should be unchanged");
            Assert.AreEqual(2, _gridManager.GetTile(0, 1)?.Value, "Original grid should be unchanged");
        }

        [Test]
        public void SimulateMove_NoChange_ReturnsFalse()
        {
            // Arrange - Tiles already against the left wall
            _gridManager.PlaceTile(new Tile(2, 0, 0), 0, 0);
            _gridManager.PlaceTile(new Tile(4, 0, 1), 0, 1);

            // Act
            var (canMove, potentialScore) = _gameLogic.SimulateMove(MoveDirection.Left);

            // Assert
            Assert.IsFalse(canMove, "Should not be able to move when blocked");
            Assert.AreEqual(0, potentialScore, "No score should be gained");
        }

        [Test]
        public void CanMove_ValidMove_ReturnsTrue()
        {
            // Arrange
            _gridManager.PlaceTile(new Tile(2, 0, 1), 0, 1);

            // Act & Assert
            Assert.IsTrue(_gameLogic.CanMove(MoveDirection.Left), "Should be able to move left");
            Assert.IsFalse(_gameLogic.CanMove(MoveDirection.Right), "Should not be able to move right (blocked by wall)");
        }

        [Test]
        public void GetValidMoves_ReturnsAllPossibleMoves()
        {
            // Arrange - Tile in center
            _gridManager.PlaceTile(new Tile(2, 1, 1), 1, 1);

            // Act
            var validMoves = _gameLogic.GetValidMoves();

            // Assert
            Assert.AreEqual(4, validMoves.Count, "All 4 directions should be valid");
            Assert.Contains(MoveDirection.Up, validMoves);
            Assert.Contains(MoveDirection.Down, validMoves);
            Assert.Contains(MoveDirection.Left, validMoves);
            Assert.Contains(MoveDirection.Right, validMoves);
        }

        [Test]
        public void GetValidMoves_NoValidMoves_ReturnsEmptyList()
        {
            // Arrange - Full grid with no mergeable tiles
            int[] values = { 2, 4, 2, 4, 4, 2, 4, 2, 2, 4, 2, 4, 4, 2, 4, 2 };
            int index = 0;
            for (int row = 0; row < GridManager.GridSize; row++)
            {
                for (int col = 0; col < GridManager.GridSize; col++)
                {
                    _gridManager.PlaceTile(new Tile(values[index++], row, col), row, col);
                }
            }

            // Act
            var validMoves = _gameLogic.GetValidMoves();

            // Assert
            Assert.AreEqual(0, validMoves.Count, "No valid moves should exist");
        }

        #endregion

        #region GridManager.FromGrid Tests

        [Test]
        public void GridManager_FromGrid_CreatesManagerWithExistingGrid()
        {
            // Arrange
            _gridManager.PlaceTile(new Tile(2, 0, 0), 0, 0);
            _gridManager.PlaceTile(new Tile(4, 1, 1), 1, 1);

            var clonedGrid = _gridManager.CloneGrid();

            // Act
            var newManager = GridManager.FromGrid(clonedGrid);

            // Assert
            Assert.AreEqual(2, newManager.GetTile(0, 0)?.Value, "Tile should be cloned");
            Assert.AreEqual(4, newManager.GetTile(1, 1)?.Value, "Tile should be cloned");
            Assert.IsNull(newManager.GetTile(0, 1), "Empty cells should be null");
        }

        [Test]
        public void GridManager_FromGrid_ModificationsDoNotAffectOriginal()
        {
            // Arrange
            _gridManager.PlaceTile(new Tile(2, 0, 0), 0, 0);
            var clonedGrid = _gridManager.CloneGrid();
            var newManager = GridManager.FromGrid(clonedGrid);

            // Act - modify the new manager
            newManager.MoveTile(0, 0, 0, 1);

            // Assert
            Assert.AreEqual(2, _gridManager.GetTile(0, 0)?.Value, "Original should be unchanged");
            Assert.IsNull(_gridManager.GetTile(0, 1), "Original should be unchanged");
        }

        #endregion
    }
}
