using NUnit.Framework;
using FinalNumber.Gameplay;
using UnityEngine;

namespace FinalNumber.Tests.EditMode
{
    /// <summary>
    /// Unit tests for the GridManager class
    /// </summary>
    public class GridManagerTests
    {
        private GridManager _gridManager;

        [SetUp]
        public void Setup()
        {
            _gridManager = new GridManager(seed: 12345);
            _gridManager.InitializeGrid();
        }

        [Test]
        public void InitializeGrid_CreatesEmptyGrid()
        {
            // Assert
            Assert.AreEqual(16, _gridManager.GetEmptyCellCount(), "Grid should be empty after initialization");
            Assert.IsFalse(_gridManager.HasWinningTile(), "No winning tile should exist");
        }

        [Test]
        public void PlaceTile_SuccessfullyPlacesTile()
        {
            // Arrange
            var tile = new Tile(2, 0, 0);

            // Act
            bool result = _gridManager.PlaceTile(tile, 0, 0);

            // Assert
            Assert.IsTrue(result, "Tile should be placed successfully");
            Assert.AreEqual(1, _gridManager.GetEmptyCellCount(), "One cell should be occupied");
            Assert.AreSame(tile, _gridManager.GetTile(0, 0), "Tile should be at position (0,0)");
        }

        [Test]
        public void PlaceTile_FailsWhenCellOccupied()
        {
            // Arrange
            var tile1 = new Tile(2, 0, 0);
            var tile2 = new Tile(4, 0, 0);
            _gridManager.PlaceTile(tile1, 0, 0);

            // Act
            bool result = _gridManager.PlaceTile(tile2, 0, 0);

            // Assert
            Assert.IsFalse(result, "Should fail to place tile on occupied cell");
        }

        [Test]
        public void PlaceTile_FailsWhenOutOfBounds()
        {
            // Arrange
            var tile = new Tile(2, 0, 0);

            // Act & Assert
            Assert.IsFalse(_gridManager.IsValidPosition(-1, 0), "Negative row should be invalid");
            Assert.IsFalse(_gridManager.IsValidPosition(0, -1), "Negative column should be invalid");
            Assert.IsFalse(_gridManager.IsValidPosition(4, 0), "Row 4 should be invalid");
            Assert.IsFalse(_gridManager.IsValidPosition(0, 4), "Column 4 should be invalid");
        }

        [Test]
        public void SpawnRandomTile_CreatesTileInEmptyCell()
        {
            // Act
            var tile = _gridManager.SpawnRandomTile();

            // Assert
            Assert.IsNotNull(tile, "Should spawn a tile");
            Assert.IsTrue(tile.Value == 2 || tile.Value == 4, "Tile value should be 2 or 4");
            Assert.AreEqual(15, _gridManager.GetEmptyCellCount(), "Should have 15 empty cells");
        }

        [Test]
        public void SpawnRandomTile_ReturnsNullWhenGridFull()
        {
            // Arrange - Fill the grid
            for (int row = 0; row < GridManager.GridSize; row++)
            {
                for (int col = 0; col < GridManager.GridSize; col++)
                {
                    _gridManager.PlaceTile(new Tile(2, row, col), row, col);
                }
            }

            // Act
            var tile = _gridManager.SpawnRandomTile();

            // Assert
            Assert.IsNull(tile, "Should return null when grid is full");
        }

        [Test]
        public void MoveTile_MovesTileToEmptyCell()
        {
            // Arrange
            var tile = new Tile(2, 0, 0);
            _gridManager.PlaceTile(tile, 0, 0);

            // Act
            bool result = _gridManager.MoveTile(0, 0, 0, 1);

            // Assert
            Assert.IsTrue(result, "Move should succeed");
            Assert.IsNull(_gridManager.GetTile(0, 0), "Source cell should be empty");
            Assert.AreSame(tile, _gridManager.GetTile(0, 1), "Tile should be at destination");
            Assert.AreEqual(0, tile.Row);
            Assert.AreEqual(1, tile.Column);
        }

        [Test]
        public void HasValidMoves_ReturnsTrueWhenEmptyCellsExist()
        {
            // Arrange
            _gridManager.SpawnRandomTile();

            // Act & Assert
            Assert.IsTrue(_gridManager.HasValidMoves(), "Should have valid moves when empty cells exist");
        }

        [Test]
        public void HasValidMoves_ReturnsTrueWhenMergesPossible()
        {
            // Arrange - Full grid with some mergeable tiles
            _gridManager.PlaceTile(new Tile(2, 0, 0), 0, 0);
            _gridManager.PlaceTile(new Tile(2, 0, 1), 0, 1);
            _gridManager.PlaceTile(new Tile(4, 0, 2), 0, 2);
            _gridManager.PlaceTile(new Tile(4, 0, 3), 0, 3);
            
            _gridManager.PlaceTile(new Tile(8, 1, 0), 1, 0);
            _gridManager.PlaceTile(new Tile(8, 1, 1), 1, 1);
            _gridManager.PlaceTile(new Tile(16, 1, 2), 1, 2);
            _gridManager.PlaceTile(new Tile(16, 1, 3), 1, 3);
            
            // Fill remaining cells
            for (int row = 2; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    _gridManager.PlaceTile(new Tile(32, row, col), row, col);
                }
            }

            // Act & Assert
            Assert.IsTrue(_gridManager.HasValidMoves(), "Should have valid moves due to mergeable tiles");
        }

        [Test]
        public void HasValidMoves_ReturnsFalseWhenNoMovesPossible()
        {
            // Arrange - Full grid with no matching adjacent tiles
            int[] values = { 2, 4, 2, 4, 4, 2, 4, 2, 2, 4, 2, 4, 4, 2, 4, 2 };
            int index = 0;
            for (int row = 0; row < GridManager.GridSize; row++)
            {
                for (int col = 0; col < GridManager.GridSize; col++)
                {
                    _gridManager.PlaceTile(new Tile(values[index++], row, col), row, col);
                }
            }

            // Act & Assert
            Assert.IsFalse(_gridManager.HasValidMoves(), "Should have no valid moves");
        }

        [Test]
        public void HasWinningTile_DetectsTargetTile()
        {
            // Arrange
            _gridManager.PlaceTile(new Tile(1024, 0, 0), 0, 0);
            _gridManager.PlaceTile(new Tile(2048, 0, 1), 0, 1);

            // Act & Assert
            Assert.IsTrue(_gridManager.HasWinningTile(2048), "Should detect 2048 tile");
            Assert.IsFalse(_gridManager.HasWinningTile(4096), "Should not detect 4096 tile");
        }

        [Test]
        public void GetHighestTileValue_ReturnsMaxValue()
        {
            // Arrange
            _gridManager.PlaceTile(new Tile(2, 0, 0), 0, 0);
            _gridManager.PlaceTile(new Tile(8, 0, 1), 0, 1);
            _gridManager.PlaceTile(new Tile(4, 0, 2), 0, 2);

            // Act
            int highest = _gridManager.GetHighestTileValue();

            // Assert
            Assert.AreEqual(8, highest, "Should return highest tile value");
        }

        [Test]
        public void GetGridState_ReturnsCorrectValues()
        {
            // Arrange
            _gridManager.PlaceTile(new Tile(2, 0, 0), 0, 0);
            _gridManager.PlaceTile(new Tile(4, 1, 1), 1, 1);

            // Act
            int[,] state = _gridManager.GetGridState();

            // Assert
            Assert.AreEqual(2, state[0, 0], "State should have 2 at (0,0)");
            Assert.AreEqual(4, state[1, 1], "State should have 4 at (1,1)");
            Assert.AreEqual(0, state[2, 2], "State should have 0 at (2,2)");
        }

        [Test]
        public void TileMerge_DoublesValueAndSetsFlag()
        {
            // Arrange
            var tile1 = new Tile(2, 0, 0);
            var tile2 = new Tile(2, 0, 1);

            // Act
            tile1.Merge(tile2);

            // Assert
            Assert.AreEqual(4, tile1.Value, "Tile value should be doubled");
            Assert.IsTrue(tile1.HasMerged, "Merge flag should be set");
        }

        [Test]
        public void TileMerge_ThrowsOnDifferentValues()
        {
            // Arrange
            var tile1 = new Tile(2, 0, 0);
            var tile2 = new Tile(4, 0, 1);

            // Act & Assert
            Assert.Throws<System.InvalidOperationException>(() => tile1.Merge(tile2));
        }

        [Test]
        public void TileResetMergeState_ClearsFlag()
        {
            // Arrange
            var tile = new Tile(4, 0, 0);
            tile.HasMerged = true;
            tile.IsNew = true;

            // Act
            tile.ResetMergeState();

            // Assert
            Assert.IsFalse(tile.HasMerged, "Merge flag should be cleared");
            Assert.IsFalse(tile.IsNew, "IsNew flag should be cleared");
        }
    }
}
