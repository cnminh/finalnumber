using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FinalNumber.Gameplay;

namespace FinalNumber.UI
{
    /// <summary>
    /// Main gameplay UI for the 2048 board.
    /// Handles visual rendering of the grid, tiles, input, and game state overlays.
    /// </summary>
    public class BoardUI : MonoBehaviour
    {
        [Header("Canvas & Containers")]
        public Canvas gameCanvas;
        public RectTransform boardContainer;
        public RectTransform hudContainer;
        public RectTransform overlayContainer;

        [Header("Tile Prefab & Settings")]
        public GameObject tilePrefab;
        public float tileSize = 100f;
        public float tileSpacing = 10f;
        public float animationDuration = 0.15f;

        [Header("Colors")]
        public Color boardBackgroundColor = new Color(0.18f, 0.16f, 0.15f, 1f);
        public Color emptyTileColor = new Color(0.24f, 0.22f, 0.20f, 1f);

        [Header("HUD Elements")]
        public TextMeshProUGUI scoreText;
        public TextMeshProUGUI bestScoreText;
        public TextMeshProUGUI highestTileText;
        public Button newGameButton;
        public Button undoButton;

        [Header("Game Over Overlay")]
        public GameObject gameOverPanel;
        public TextMeshProUGUI gameOverTitle;
        public TextMeshProUGUI gameOverScore;
        public Button gameOverNewGameButton;
        public Button gameOverMenuButton;

        [Header("Win Overlay")]
        public GameObject winPanel;
        public TextMeshProUGUI winTitle;
        public TextMeshProUGUI winScore;
        public Button winContinueButton;
        public Button winNewGameButton;

        [Header("Input")]
        public float minSwipeDistance = 50f;
        public float maxSwipeTime = 0.5f;

        // References
        private GameBoard _gameBoard;
        private InputHandler _inputHandler;

        // Visual state
        private Dictionary<(int row, int col), GameObject> _visualTiles = new Dictionary<(int, int), GameObject>();
        private Vector2 _touchStartPosition;
        private float _touchStartTime;
        private bool _isTrackingTouch;
        private bool _isAnimating;

        // Events
        public event Action OnNewGameRequested;
        public event Action OnMenuRequested;

        private void Awake()
        {
            EnsureUIExists();
            SetupButtonListeners();
        }

        private void Update()
        {
            if (!_isAnimating && _gameBoard != null && _gameBoard.CurrentState == GameState.Playing)
            {
                ProcessInput();
            }
        }

        /// <summary>
        /// Initialize the UI with a game board reference
        /// </summary>
        public void Initialize(GameBoard gameBoard)
        {
            _gameBoard = gameBoard;

            // Subscribe to board events
            _gameBoard.OnTileMoved += HandleTileMoved;
            _gameBoard.OnTileSpawned += HandleTileSpawned;
            _gameBoard.OnScoreChanged += HandleScoreChanged;
            _gameBoard.OnGameStateChanged += HandleGameStateChanged;
            _gameBoard.OnGridChanged += HandleGridChanged;

            // Create visual grid
            CreateVisualGrid();

            // Initial score update
            UpdateScoreDisplay(_gameBoard.CurrentScore);

            // Hide overlays
            HideOverlays();
        }

        private void OnDestroy()
        {
            if (_gameBoard != null)
            {
                _gameBoard.OnTileMoved -= HandleTileMoved;
                _gameBoard.OnTileSpawned -= HandleTileSpawned;
                _gameBoard.OnScoreChanged -= HandleScoreChanged;
                _gameBoard.OnGameStateChanged -= HandleGameStateChanged;
                _gameBoard.OnGridChanged -= HandleGridChanged;
            }
        }

        #region UI Creation

        /// <summary>
        /// Ensure all UI elements exist
        /// </summary>
        private void EnsureUIExists()
        {
            if (gameCanvas == null)
            {
                GameObject canvasGO = new GameObject("BoardCanvas");
                gameCanvas = canvasGO.AddComponent<Canvas>();
                gameCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                gameCanvas.sortingOrder = 1;

                CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);

                canvasGO.AddComponent<GraphicRaycaster>();
            }

            // Ensure EventSystem exists
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // Create board container if needed
            if (boardContainer == null)
            {
                GameObject boardGO = new GameObject("BoardContainer");
                boardGO.transform.SetParent(gameCanvas.transform, false);
                boardContainer = boardGO.AddComponent<RectTransform>();
                boardContainer.anchorMin = new Vector2(0.5f, 0.5f);
                boardContainer.anchorMax = new Vector2(0.5f, 0.5f);
                boardContainer.sizeDelta = new Vector2(500, 500);

                // Add background image
                Image bg = boardGO.AddComponent<Image>();
                bg.color = boardBackgroundColor;
            }
        }

        /// <summary>
        /// Create the visual 4x4 grid
        /// </summary>
        private void CreateVisualGrid()
        {
            // Clear existing tiles
            foreach (var tile in _visualTiles.Values)
            {
                if (tile != null)
                    Destroy(tile);
            }
            _visualTiles.Clear();

            // Create empty cell backgrounds
            float startOffset = -((GameBoard.GridSize - 1) * (tileSize + tileSpacing)) / 2f;

            for (int row = 0; row < GameBoard.GridSize; row++)
            {
                for (int col = 0; col < GameBoard.GridSize; col++)
                {
                    // Create empty cell background
                    GameObject cellGO = new GameObject($"Cell_{row}_{col}");
                    cellGO.transform.SetParent(boardContainer, false);

                    RectTransform cellRect = cellGO.AddComponent<RectTransform>();
                    cellRect.sizeDelta = new Vector2(tileSize, tileSize);
                    cellRect.anchoredPosition = new Vector2(
                        startOffset + col * (tileSize + tileSpacing),
                        startOffset + (GameBoard.GridSize - 1 - row) * (tileSize + tileSpacing)
                    );

                    Image cellImage = cellGO.AddComponent<Image>();
                    cellImage.color = emptyTileColor;
                }
            }
        }

        /// <summary>
        /// Create or update a visual tile
        /// </summary>
        private void CreateOrUpdateTile(int row, int col, int value, bool animate = false)
        {
            if (value == 0)
            {
                // Remove tile if it exists
                if (_visualTiles.TryGetValue((row, col), out GameObject existingTile))
                {
                    if (animate)
                    {
                        LeanTween.scale(existingTile, Vector3.zero, animationDuration * 0.5f)
                            .setOnComplete(() => Destroy(existingTile));
                    }
                    else
                    {
                        Destroy(existingTile);
                    }
                    _visualTiles.Remove((row, col));
                }
                return;
            }

            GameObject tileGO;
            bool isNew = false;

            if (_visualTiles.TryGetValue((row, col), out GameObject existing))
            {
                tileGO = existing;
            }
            else
            {
                // Create new tile
                if (tilePrefab != null)
                {
                    tileGO = Instantiate(tilePrefab, boardContainer);
                }
                else
                {
                    tileGO = CreateDefaultTile();
                }
                isNew = true;
                _visualTiles[(row, col)] = tileGO;
            }

            // Setup tile
            float startOffset = -((GameBoard.GridSize - 1) * (tileSize + tileSpacing)) / 2f;
            RectTransform tileRect = tileGO.GetComponent<RectTransform>();
            tileRect.sizeDelta = new Vector2(tileSize, tileSize);
            tileRect.anchoredPosition = new Vector2(
                startOffset + col * (tileSize + tileSpacing),
                startOffset + (GameBoard.GridSize - 1 - row) * (tileSize + tileSpacing)
            );

            // Update visual
            UpdateTileVisual(tileGO, value);

            // Animation
            if (isNew && animate)
            {
                tileGO.transform.localScale = Vector3.zero;
                LeanTween.scale(tileGO, Vector3.one, animationDuration)
                    .setEaseOutBack();
            }
        }

        /// <summary>
        /// Create a default tile GameObject
        /// </summary>
        private GameObject CreateDefaultTile()
        {
            GameObject tileGO = new GameObject("Tile");
            tileGO.layer = 5;

            RectTransform rect = tileGO.AddComponent<RectTransform>();

            Image image = tileGO.AddComponent<Image>();

            // Add text
            GameObject textGO = new GameObject("ValueText");
            textGO.transform.SetParent(tileGO.transform, false);
            TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 36;
            text.fontStyle = FontStyles.Bold;

            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return tileGO;
        }

        /// <summary>
        /// Update tile visual based on value
        /// </summary>
        private void UpdateTileVisual(GameObject tileGO, int value)
        {
            Image image = tileGO.GetComponent<Image>();
            TextMeshProUGUI text = tileGO.GetComponentInChildren<TextMeshProUGUI>();

            // Get color from Tile class
            var tempTile = new Tile(value, 0, 0);
            if (image != null)
                image.color = tempTile.GetTileColor();

            if (text != null)
            {
                text.text = value.ToString();
                text.color = tempTile.GetTextColor();
            }
        }

        #endregion

        #region Event Handlers

        private void HandleTileMoved(object sender, TileMoveEventArgs e)
        {
            _isAnimating = true;

            // Animate the tile movement
            if (_visualTiles.TryGetValue((e.FromRow, e.FromColumn), out GameObject tile))
            {
                float startOffset = -((GameBoard.GridSize - 1) * (tileSize + tileSpacing)) / 2f;
                Vector2 targetPos = new Vector2(
                    startOffset + e.ToColumn * (tileSize + tileSpacing),
                    startOffset + (GameBoard.GridSize - 1 - e.ToRow) * (tileSize + tileSpacing)
                );

                LeanTween.move(tile.GetComponent<RectTransform>(), targetPos, animationDuration)
                    .setEaseOutQuad()
                    .setOnComplete(() =>
                    {
                        if (e.IsMerge)
                        {
                            // Remove the moving tile (it's merged)
                            Destroy(tile);
                            _visualTiles.Remove((e.FromRow, e.FromColumn));

                            // Create merged tile at destination with animation
                            CreateOrUpdateTile(e.ToRow, e.ToColumn, e.Tile.Value, true);
                        }
                        else
                        {
                            // Just moved - update dictionary key
                            _visualTiles.Remove((e.FromRow, e.FromColumn));
                            _visualTiles[(e.ToRow, e.ToColumn)] = tile;
                        }

                        _isAnimating = false;
                    });
            }
        }

        private void HandleTileSpawned(object sender, TileSpawnEventArgs e)
        {
            CreateOrUpdateTile(e.Row, e.Column, e.Tile.Value, true);
        }

        private void HandleScoreChanged(object sender, int score)
        {
            UpdateScoreDisplay(score);
        }

        private void HandleGameStateChanged(object sender, GameState state)
        {
            switch (state)
            {
                case GameState.Won:
                    ShowWinOverlay();
                    break;
                case GameState.Lost:
                    ShowGameOverOverlay();
                    break;
            }
        }

        private void HandleGridChanged(object sender, EventArgs e)
        {
            // Sync all tiles
            for (int row = 0; row < GameBoard.GridSize; row++)
            {
                for (int col = 0; col < GameBoard.GridSize; col++)
                {
                    var tile = _gameBoard.GetTile(row, col);
                    int value = tile?.Value ?? 0;
                    CreateOrUpdateTile(row, col, value, false);
                }
            }

            // Update highest tile display
            if (highestTileText != null)
            {
                highestTileText.text = $"Best Tile: {_gameBoard.GetHighestTileValue()}";
            }
        }

        #endregion

        #region Input Handling

        private void ProcessInput()
        {
            // Touch/Mouse input
            if (Input.touchSupported && Input.touchCount > 0)
            {
                ProcessTouchInput();
            }
            else
            {
                ProcessMouseInput();
            }

            // Keyboard input
            ProcessKeyboardInput();
        }

        private void ProcessTouchInput()
        {
            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    _touchStartPosition = touch.position;
                    _touchStartTime = Time.time;
                    _isTrackingTouch = true;
                    break;

                case TouchPhase.Ended:
                    if (_isTrackingTouch)
                    {
                        EndTracking(touch.position);
                    }
                    break;
            }
        }

        private void ProcessMouseInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                _touchStartPosition = Input.mousePosition;
                _touchStartTime = Time.time;
                _isTrackingTouch = true;
            }

            if (Input.GetMouseButtonUp(0) && _isTrackingTouch)
            {
                EndTracking(Input.mousePosition);
            }
        }

        private void ProcessKeyboardInput()
        {
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                _gameBoard?.ExecuteMove(MoveDirection.Up);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                _gameBoard?.ExecuteMove(MoveDirection.Down);
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                _gameBoard?.ExecuteMove(MoveDirection.Left);
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                _gameBoard?.ExecuteMove(MoveDirection.Right);
            }
        }

        private void EndTracking(Vector2 endPosition)
        {
            _isTrackingTouch = false;

            Vector2 delta = endPosition - _touchStartPosition;
            float time = Time.time - _touchStartTime;

            if (time > maxSwipeTime || delta.magnitude < minSwipeDistance)
                return;

            MoveDirection direction;
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                direction = delta.x > 0 ? MoveDirection.Right : MoveDirection.Left;
            }
            else
            {
                direction = delta.y > 0 ? MoveDirection.Up : MoveDirection.Down;
            }

            _gameBoard?.ExecuteMove(direction);
        }

        #endregion

        #region HUD & Overlays

        private void SetupButtonListeners()
        {
            if (newGameButton != null)
                newGameButton.onClick.AddListener(() => OnNewGameRequested?.Invoke());

            if (undoButton != null)
                undoButton.onClick.AddListener(OnUndoClicked);

            if (gameOverNewGameButton != null)
                gameOverNewGameButton.onClick.AddListener(() => OnNewGameRequested?.Invoke());

            if (gameOverMenuButton != null)
                gameOverMenuButton.onClick.AddListener(() => OnMenuRequested?.Invoke());

            if (winContinueButton != null)
                winContinueButton.onClick.AddListener(OnContinueClicked);

            if (winNewGameButton != null)
                winNewGameButton.onClick.AddListener(() => OnNewGameRequested?.Invoke());
        }

        private void UpdateScoreDisplay(int score)
        {
            if (scoreText != null)
                scoreText.text = $"Score: {score}";

            if (bestScoreText != null && _gameBoard != null)
                bestScoreText.text = $"Best: {_gameBoard.BestScore}";
        }

        private void ShowGameOverOverlay()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);

                if (gameOverScore != null)
                    gameOverScore.text = $"Final Score: {_gameBoard.CurrentScore}";
            }
        }

        private void ShowWinOverlay()
        {
            if (winPanel != null)
            {
                winPanel.SetActive(true);

                if (winScore != null)
                    winScore.text = $"Score: {_gameBoard.CurrentScore}";
            }
        }

        private void HideOverlays()
        {
            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);

            if (winPanel != null)
                winPanel.SetActive(false);
        }

        private void OnUndoClicked()
        {
            // TODO: Implement undo functionality
            Debug.Log("[BoardUI] Undo requested (not yet implemented)");
        }

        private void OnContinueClicked()
        {
            if (winPanel != null)
                winPanel.SetActive(false);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Reset the board display for a new game
        /// </summary>
        public void ResetDisplay()
        {
            HideOverlays();

            // Clear all tiles
            foreach (var tile in _visualTiles.Values)
            {
                if (tile != null)
                    Destroy(tile);
            }
            _visualTiles.Clear();

            // Recreate grid
            CreateVisualGrid();
        }

        /// <summary>
        /// Update the entire board display
        /// </summary>
        public void RefreshDisplay()
        {
            HandleGridChanged(null, EventArgs.Empty);
        }

        #endregion
    }
}
