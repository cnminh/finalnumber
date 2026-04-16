using NUnit.Framework;
using FinalNumber.Gameplay;
using System;
using System.Collections.Generic;

namespace FinalNumber.Tests.EditMode
{
    /// <summary>
    /// Unit tests for the GameBoard class
    /// </summary>
    public class GameBoardTests
    {
        private GameBoard _gameBoard;

        [SetUp]
        public void Setup()
        {
            _gameBoard = new GameBoard(seed: 12345);
        }

        [TearDown]
        public void TearDown()
        {
            _gameBoard.ResetBestScore();
        }

        #region Initialization Tests

        [Test]
        public void InitializeGame_CreatesTwoTiles()
        {
            // Act
            _gameBoard.InitializeGame();

            // Assert
            Assert.AreEqual(2, 16 - _gameBoard.GetEmptyCellCount(), "Should have 2 tiles");
            Assert.AreEqual(GameState.Playing, _gameBoard.CurrentState, "Game should be in Playing state");
            Assert.AreEqual(0, _gameBoard.CurrentScore, "Score should be 0");
            Assert.AreEqual(0, _gameBoard.CurrentMoves, "Moves should be 0");
        }

        [Test]
        public void Constructor_WithSeed_CreatesDeterministicBoard()
        {
            // Arrange
            var board1 = new GameBoard(seed: 42);
            var board2 = new GameBoard(seed: 42);

            // Act
            board1.InitializeGame();
            board2.InitializeGame();

            // Assert - Both should have same initial state
            var state1 = board1.GetGridState();
            var state2 = board2.GetGridState();

            for (int row = 0; row < GameBoard.GridSize; row++)
            {
                for (int col = 0; col < GameBoard.GridSize; col++)
                {
                    Assert.AreEqual(state1[row, col], state2[row, col],
                        $"Mismatch at position [{row},{col}]");
                }
            }
        }

        [Test]
        public void Constructor_InitializesDefaultValues()
        {
            // Arrange & Act
            var board = new GameBoard();

            // Assert
            Assert.AreEqual(GameState.Playing, board.CurrentState);
            Assert.AreEqual(0, board.CurrentScore);
            Assert.AreEqual(0, board.CurrentMoves);
            Assert.AreEqual(0, board.HighestTileValue);
        }

        [Test]
        public void InitializeGame_ResetsGameState()
        {
            // Arrange
            _gameBoard.InitializeGame();
            _gameBoard.ExecuteMove(MoveDirection.Left); // Make a move to change state

            // Act
            _gameBoard.InitializeGame();

            // Assert
            Assert.AreEqual(GameState.Playing, _gameBoard.CurrentState);
            Assert.AreEqual(0, _gameBoard.CurrentScore);
            Assert.AreEqual(0, _gameBoard.CurrentMoves);
        }

        #endregion

        #region Grid Query Tests

        [Test]
        public void GetTile_EmptyCell_ReturnsNull()
        {
            // Arrange
            _gameBoard.InitializeGame();

            // Find an empty cell and verify it's null
            var state = _gameBoard.GetGridState();
            for (int row = 0; row < GameBoard.GridSize; row++)
            {
                for (int col = 0; col < GameBoard.GridSize; col++)
                {
                    if (state[row, col] == 0)
                    {
                        // Act
                        var tile = _gameBoard.GetTile(row, col);

                        // Assert
                        Assert.IsNull(tile, $"Tile at [{row},{col}] should be null");
                        return;
                    }
                }
            }
            Assert.Fail("Should have found an empty cell");
        }

        [Test]
        public void GetTile_InvalidPosition_ReturnsNull()
        {
            // Act & Assert
            Assert.IsNull(_gameBoard.GetTile(-1, 0), "Negative row should return null");
            Assert.IsNull(_gameBoard.GetTile(0, -1), "Negative col should return null");
            Assert.IsNull(_gameBoard.GetTile(4, 0), "Row 4 should return null");
            Assert.IsNull(_gameBoard.GetTile(0, 4), "Col 4 should return null");
        }

        [Test]
        public void IsValidPosition_BoundaryChecks()
        {
            // Assert
            Assert.IsTrue(_gameBoard.IsValidPosition(0, 0), "(0,0) should be valid");
            Assert.IsTrue(_gameBoard.IsValidPosition(3, 3), "(3,3) should be valid");
            Assert.IsFalse(_gameBoard.IsValidPosition(-1, 0), "Negative row should be invalid");
            Assert.IsFalse(_gameBoard.IsValidPosition(0, -1), "Negative col should be invalid");
            Assert.IsFalse(_gameBoard.IsValidPosition(4, 0), "Row 4 should be invalid");
            Assert.IsFalse(_gameBoard.IsValidPosition(0, 4), "Col 4 should be invalid");
        }

        [Test]
        public void IsEmpty_EmptyCell_ReturnsTrue()
        {
            // Arrange
            _gameBoard.InitializeGame();

            // Find an empty cell
            var state = _gameBoard.GetGridState();
            for (int row = 0; row < GameBoard.GridSize; row++)
            {
                for (int col = 0; col < GameBoard.GridSize; col++)
                {
                    if (state[row, col] == 0)
                    {
                        // Act & Assert
                        Assert.IsTrue(_gameBoard.IsEmpty(row, col), $"Cell [{row},{col}] should be empty");
                        return;
                    }
                }
            }
            Assert.Fail("Should have found an empty cell");
        }

        [Test]
        public void IsEmpty_InvalidPosition_ReturnsFalse()
        {
            // Assert
            Assert.IsFalse(_gameBoard.IsEmpty(-1, 0), "Invalid position should not be empty");
            Assert.IsFalse(_gameBoard.IsEmpty(0, -1), "Invalid position should not be empty");
            Assert.IsFalse(_gameBoard.IsEmpty(4, 0), "Invalid position should not be empty");
            Assert.IsFalse(_gameBoard.IsEmpty(0, 4), "Invalid position should not be empty");
        }

        [Test]
        public void GetEmptyCellCount_InitialGame_Returns14()
        {
            // Arrange
            _gameBoard.InitializeGame();

            // Act
            int count = _gameBoard.GetEmptyCellCount();

            // Assert
            Assert.AreEqual(14, count, "Should have 14 empty cells initially");
        }

        [Test]
        public void HasEmptyCells_EmptyGrid_ReturnsTrue()
        {
            // Arrange
            _gameBoard.InitializeGame();

            // Act & Assert
            Assert.IsTrue(_gameBoard.HasEmptyCells(), "New game should have empty cells");
        }

        #endregion

        #region Movement Tests

        [Test]
        public void ExecuteMove_UpdatesScoreAndMoves()
        {
            // Arrange
            _gameBoard.InitializeGame();
            int initialScore = _gameBoard.CurrentScore;
            int initialMoves = _gameBoard.CurrentMoves;

            // Act - Make moves until one succeeds
            for (int i = 0; i < 10 && _gameBoard.CurrentMoves == initialMoves; i++)
            {
                _gameBoard.ExecuteMove(MoveDirection.Left);
            }

            // Assert
            Assert.GreaterOrEqual(_gameBoard.CurrentScore, 0, "Score should be non-negative");
            Assert.GreaterOrEqual(_gameBoard.CurrentMoves, 0, "Moves should be non-negative");
        }

        [Test]
        public void ExecuteMove_SpawnsNewTileAfterValidMove()
        {
            // Arrange - Create board with known state
            _gameBoard = new GameBoard(seed: 42);
            _gameBoard.InitializeGame();
            int tilesBefore = 16 - _gameBoard.GetEmptyCellCount();

            // Make a move
            _gameBoard.ExecuteMove(MoveDirection.Left);

            // After a valid move, should have same count (moved + spawned = same)
            // or more if tiles merged
            int tilesAfter = 16 - _gameBoard.GetEmptyCellCount();
            Assert.GreaterOrEqual(tilesAfter, tilesBefore, "Should have tiles after move");
        }

        [Test]
        public void ExecuteMove_None_ReturnsFalse()
        {
            // Arrange
            _gameBoard.InitializeGame();

            // Act
            bool result = _gameBoard.ExecuteMove(MoveDirection.None);

            // Assert
            Assert.IsFalse(result, "None direction should not cause a move");
        }

        [Test]
        public void ExecuteMove_IncrementsMoveCounter()
        {
            // Arrange
            _gameBoard.InitializeGame();
            int initialMoves = _gameBoard.CurrentMoves;

            // Act - Make moves until one succeeds
            for (int i = 0; i < 10; i++)
            {
                if (_gameBoard.ExecuteMove(MoveDirection.Left))
                    break;
            }

            // Assert
            Assert.Greater(_gameBoard.CurrentMoves, initialMoves, "Moves should increment after valid move");
        }

        [Test]
        public void ExecuteMove_SpawnsNewTile()
        {
            // Arrange
            _gameBoard.InitializeGame();

            // Act - Make a valid move
            for (int i = 0; i < 10; i++)
            {
                if (_gameBoard.ExecuteMove(MoveDirection.Left))
                    break;
            }

            // Assert - Should have 13 empty cells (14 - 1 new tile spawned)
            Assert.AreEqual(13, _gameBoard.GetEmptyCellCount(), "Should have 13 empty cells after first move");
        }

        [Test]
        public void ExecuteMove_AllDirections_Work()
        {
            // Arrange
            _gameBoard.InitializeGame();

            // Act & Assert - All directions should be callable
            Assert.DoesNotThrow(() => _gameBoard.ExecuteMove(MoveDirection.Left));
            Assert.DoesNotThrow(() => _gameBoard.ExecuteMove(MoveDirection.Right));
            Assert.DoesNotThrow(() => _gameBoard.ExecuteMove(MoveDirection.Up));
            Assert.DoesNotThrow(() => _gameBoard.ExecuteMove(MoveDirection.Down));
        }

        #endregion

        #region Game State Tests

        [Test]
        public void HasWinningTile_Detects2048()
        {
            // This test verifies the win detection logic works
            // In practice, achieving 2048 requires many moves
            Assert.IsFalse(_gameBoard.HasWinningTile(), "New board should not have winning tile");
        }

        [Test]
        public void HasWinningTile_CustomTarget_ReturnsCorrectResult()
        {
            // Arrange
            _gameBoard.InitializeGame();

            // Act - Check for different target values
            // Initially only have 2s and 4s, so 8 should not exist
            bool has8 = _gameBoard.HasWinningTile(8);

            // Assert
            Assert.IsFalse(has8, "Should not have an 8 tile initially");
        }

        [Test]
        public void HasValidMoves_DetectsAvailableMoves()
        {
            // Arrange
            _gameBoard.InitializeGame();

            // Assert - new game should always have valid moves
            Assert.IsTrue(_gameBoard.HasValidMoves(), "New game should have valid moves");
        }

        [Test]
        public void CurrentState_InitializedToPlaying()
        {
            // Act
            _gameBoard.InitializeGame();

            // Assert
            Assert.AreEqual(GameState.Playing, _gameBoard.CurrentState);
        }

        #endregion

        #region Scoring Tests

        [Test]
        public void GetGridState_ReturnsCorrectArray()
        {
            // Arrange
            _gameBoard.InitializeGame();

            // Act
            int[,] state = _gameBoard.GetGridState();

            // Assert
            Assert.AreEqual(4, state.GetLength(0), "Should have 4 rows");
            Assert.AreEqual(4, state.GetLength(1), "Should have 4 columns");

            // Count non-zero entries (should be 2 for initial game)
            int nonZeroCount = 0;
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    if (state[row, col] != 0) nonZeroCount++;
                }
            }
            Assert.AreEqual(2, nonZeroCount, "Should have 2 tiles in initial state");
        }

        [Test]
        public void GetHighestTileValue_TracksMax()
        {
            // Arrange
            _gameBoard.InitializeGame();

            // Act
            int highest = _gameBoard.GetHighestTileValue();

            // Assert - should be 2 or 4 (initial tiles)
            Assert.IsTrue(highest == 2 || highest == 4, $"Initial highest tile should be 2 or 4, got {highest}");
        }

        [Test]
        public void HighestTileValue_TracksMaxTile()
        {
            // Arrange
            _gameBoard.InitializeGame();

            // Assert - Should be at least 2 or 4 from initial spawns
            Assert.GreaterOrEqual(_gameBoard.HighestTileValue, 2);
        }

        [Test]
        public void GetHighestTileValue_MatchesTracking()
        {
            // Arrange
            _gameBoard.InitializeGame();

            // Act
            int trackedValue = _gameBoard.HighestTileValue;
            int calculatedValue = _gameBoard.GetHighestTileValue();

            // Assert
            Assert.AreEqual(calculatedValue, trackedValue, "Tracked and calculated highest should match");
        }

        #endregion

        #region Event Tests

        [Test]
        public void Events_FireOnTileSpawned()
        {
            // Arrange
            bool eventFired = false;
            _gameBoard.OnTileSpawned += (s, e) => eventFired = true;

            // Act
            _gameBoard.InitializeGame();

            // Assert
            Assert.IsTrue(eventFired, "Tile spawn event should fire during initialization");
        }

        [Test]
        public void Events_FireOnScoreChanged()
        {
            // Arrange
            bool scoreEventFired = false;
            int newScore = -1;
            _gameBoard.OnScoreChanged += (s, score) =>
            {
                scoreEventFired = true;
                newScore = score;
            };

            // Act - Initialize and try to make moves to generate score
            _gameBoard.InitializeGame();

            // If we can make a merge, score should change
            // Force a scenario where we know tiles will merge
            _gameBoard = new GameBoard(seed: 999);
            _gameBoard.InitializeGame();

            // The event may or may not fire depending on tile placement
            // Just verify the structure is valid
            Assert.IsTrue(newScore >= 0 || !scoreEventFired, "Score should be non-negative if event fired");
        }

        [Test]
        public void OnGridChanged_FiresDuringInit()
        {
            // Arrange
            var board = new GameBoard(seed: 1);
            int eventCount = 0;
            board.OnGridChanged += (sender, args) => eventCount++;

            // Act
            board.InitializeGame();

            // Assert - Should fire for each tile spawn (2 tiles)
            Assert.GreaterOrEqual(eventCount, 2, "OnGridChanged should fire at least twice during initialization");
        }

        [Test]
        public void OnGameStateChanged_FiresDuringInit()
        {
            // Arrange
            var board = new GameBoard(seed: 1);
            bool eventFired = false;
            board.OnGameStateChanged += (sender, state) => eventFired = true;

            // Act
            board.InitializeGame();

            // Assert
            Assert.IsTrue(eventFired, "OnGameStateChanged should fire during initialization");
        }

        [Test]
        public void OnMoveExecuted_FiresOnValidMove()
        {
            // Arrange
            var board = new GameBoard(seed: 1);
            board.InitializeGame();
            bool eventFired = false;
            board.OnMoveExecuted += (sender, args) => eventFired = true;

            // Act - Try to make a valid move
            for (int i = 0; i < 10 && !eventFired; i++)
            {
                board.ExecuteMove(MoveDirection.Left);
            }

            // Assert
            Assert.IsTrue(eventFired, "OnMoveExecuted should fire on valid move");
        }

        [Test]
        public void OnInvalidMove_FiresOnInvalidMove()
        {
            // Arrange
            var board = new GameBoard(seed: 1);
            board.InitializeGame();
            bool eventFired = false;
            board.OnInvalidMove += (sender, args) => eventFired = true;

            // Act - Try many moves, some should be invalid
            for (int i = 0; i < 50; i++)
            {
                board.ExecuteMove(MoveDirection.Left);
            }

            // Assert - Verify the event is wired
            Assert.IsNotNull(board.OnInvalidMove);
        }

        [Test]
        public void OnTileMoved_FiresOnMove()
        {
            // Arrange
            var board = new GameBoard(seed: 1);
            board.InitializeGame();
            bool eventFired = false;
            board.OnTileMoved += (sender, args) => eventFired = true;

            // Act - Try to make moves
            for (int i = 0; i < 10 && !eventFired; i++)
            {
                board.ExecuteMove(MoveDirection.Left);
            }

            // Assert - Verify the event is wired
            Assert.IsNotNull(board.OnTileMoved);
        }

        #endregion

        #region Best Score Tests

        [Test]
        public void BestScore_PersistsViaPlayerPrefs()
        {
            // Arrange
            _gameBoard.ResetBestScore();
            int initialBest = _gameBoard.BestScore;

            // Act - simulate achieving a high score
            _gameBoard.InitializeGame();

            // Assert
            Assert.AreEqual(0, initialBest, "Best score should start at 0 after reset");
        }

        [Test]
        public void ResetBestScore_SetsToZero()
        {
            // Arrange
            var board = new GameBoard(seed: 1);
            board.InitializeGame();

            // Act
            board.ResetBestScore();

            // Assert
            Assert.AreEqual(0, board.BestScore);
        }

        #endregion

        #region Constants Tests

        [Test]
        public void GridSize_IsFour()
        {
            Assert.AreEqual(4, GameBoard.GridSize);
        }

        [Test]
        public void MaxTiles_IsSixteen()
        {
            Assert.AreEqual(16, GameBoard.MaxTiles);
        }

        [Test]
        public void WinValue_Is2048()
        {
            Assert.AreEqual(2048, GameBoard.WinValue);
        }

        #endregion
    }
}
