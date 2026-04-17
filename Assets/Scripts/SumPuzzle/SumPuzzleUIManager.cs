using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FinalNumber.SumPuzzle
{
    /// <summary>
    /// UI Manager for the Sum Puzzle game.
    /// Handles all UI elements including the tile grid, number selector,
    /// target displays, and game state screens.
    /// </summary>
    public class SumPuzzleUIManager : MonoBehaviour
    {
        [Header("Canvas References")]
        public Canvas gameCanvas;
        public Canvas pauseCanvas;
        public Canvas completionCanvas;

        [Header("Game UI")]
        public GameObject gameUIPanel;
        public RectTransform gridContainer;
        public GameObject tilePrefab;

        [Header("Target Displays")]
        public TextMeshProUGUI[] rowTargetTexts;
        public TextMeshProUGUI[] columnTargetTexts;

        [Header("Number Selector")]
        public GameObject numberSelectorPanel;
        public Button[] numberButtons;
        public Button clearButton;
        public Button hintButton;

        [Header("HUD")]
        public TextMeshProUGUI levelText;
        public TextMeshProUGUI timerText;
        public TextMeshProUGUI movesText;
        public TextMeshProUGUI progressText;
        public Slider progressSlider;

        [Header("Pause Menu")]
        public Button resumeButton;
        public Button quitButton;

        [Header("Completion Screen")]
        public GameObject completionPanel;
        public TextMeshProUGUI completionTitle;
        public TextMeshProUGUI completionStats;
        public Button nextLevelButton;
        public Button retryButton;
        public Button menuButton;

        [Header("Failure Screen")]
        public GameObject failurePanel;
        public TextMeshProUGUI failureReasonText;

        [Header("Control Buttons")]
        public Button pauseButton;

        // Events
        public event Action<int> OnNumberSelected;
        public event Action OnClearSelected;
        public event Action OnHintRequested;
        public event Action OnPauseRequested;
        public event Action OnResumeRequested;
        public event Action OnQuitRequested;

        private SumPuzzleTile[,] _tiles;
        private SumPuzzleBoard _board;
        private int _currentLevelId;

        private void Awake()
        {
            EnsureUIExists();
            SetupButtonListeners();
        }

        /// <summary>
        /// Ensure all UI elements exist
        /// </summary>
        private void EnsureUIExists()
        {
            if (gameCanvas == null)
            {
                GameObject canvasGO = new GameObject("SumPuzzleCanvas");
                gameCanvas = canvasGO.AddComponent<Canvas>();
                gameCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                gameCanvas.sortingOrder = 0;

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
        }

        /// <summary>
        /// Setup button click listeners
        /// </summary>
        private void SetupButtonListeners()
        {
            // Number buttons (1-9)
            if (numberButtons != null)
            {
                for (int i = 0; i < numberButtons.Length && i < 9; i++)
                {
                    int number = i + 1;
                    if (numberButtons[i] != null)
                    {
                        numberButtons[i].onClick.AddListener(() => OnNumberSelected?.Invoke(number));
                    }
                }
            }

            // Control buttons
            if (clearButton != null)
                clearButton.onClick.AddListener(() => OnClearSelected?.Invoke());

            if (hintButton != null)
                hintButton.onClick.AddListener(() => OnHintRequested?.Invoke());

            if (pauseButton != null)
                pauseButton.onClick.AddListener(() => OnPauseRequested?.Invoke());

            if (resumeButton != null)
                resumeButton.onClick.AddListener(() => OnResumeRequested?.Invoke());

            if (quitButton != null)
                quitButton.onClick.AddListener(() => OnQuitRequested?.Invoke());

            // Completion screen buttons
            if (nextLevelButton != null)
                nextLevelButton.onClick.AddListener(() => OnQuitRequested?.Invoke());

            if (retryButton != null)
                retryButton.onClick.AddListener(() => OnQuitRequested?.Invoke());

            if (menuButton != null)
                menuButton.onClick.AddListener(() => OnQuitRequested?.Invoke());
        }

        /// <summary>
        /// Setup the UI for a level
        /// </summary>
        public void SetupUI(SumPuzzleLevel level, SumPuzzleBoard board)
        {
            _board = board;
            _currentLevelId = level.LevelId;

            // Update level text
            if (levelText != null)
                levelText.text = $"Level {level.LevelId}";

            // Setup target displays
            SetupTargetDisplays(level);

            // Clear previous completion/failure screens
            if (completionPanel != null)
                completionPanel.SetActive(false);

            if (failurePanel != null)
                failurePanel.SetActive(false);

            // Show game UI
            ShowGameUI();
        }

        /// <summary>
        /// Create the visual tile grid
        /// </summary>
        public SumPuzzleTile[,] CreateTileGrid(int gridSize, SumPuzzleBoard board)
        {
            // Clean up existing tiles
            if (_tiles != null)
            {
                foreach (var tile in _tiles)
                {
                    if (tile != null)
                        Destroy(tile.gameObject);
                }
            }

            _tiles = new SumPuzzleTile[gridSize, gridSize];

            // Ensure grid container exists
            if (gridContainer == null)
            {
                GameObject containerGO = new GameObject("GridContainer");
                containerGO.transform.SetParent(gameCanvas.transform, false);
                gridContainer = containerGO.AddComponent<RectTransform>();
                gridContainer.anchorMin = new Vector2(0.5f, 0.5f);
                gridContainer.anchorMax = new Vector2(0.5f, 0.5f);
                gridContainer.sizeDelta = new Vector2(600, 600);
            }

            // Clear existing children
            foreach (Transform child in gridContainer)
            {
                Destroy(child.gameObject);
            }

            // Create tiles
            float tileSize = 100f;
            float spacing = 10f;
            float startOffset = -((gridSize - 1) * (tileSize + spacing)) / 2f;

            for (int row = 0; row < gridSize; row++)
            {
                for (int col = 0; col < gridSize; col++)
                {
                    GameObject tileGO;

                    if (tilePrefab != null)
                    {
                        tileGO = Instantiate(tilePrefab, gridContainer);
                    }
                    else
                    {
                        tileGO = CreateDefaultTile();
                        tileGO.transform.SetParent(gridContainer, false);
                    }

                    RectTransform tileRect = tileGO.GetComponent<RectTransform>();
                    tileRect.sizeDelta = new Vector2(tileSize, tileSize);
                    tileRect.anchoredPosition = new Vector2(
                        startOffset + col * (tileSize + spacing),
                        startOffset + (gridSize - 1 - row) * (tileSize + spacing)
                    );

                    SumPuzzleTile tile = tileGO.GetComponent<SumPuzzleTile>();
                    if (tile == null)
                        tile = tileGO.AddComponent<SumPuzzleTile>();

                    tile.EnsureComponents();
                    tile.Initialize(row, col);

                    _tiles[row, col] = tile;
                }
            }

            return _tiles;
        }

        /// <summary>
        /// Create a default tile GameObject
        /// </summary>
        private GameObject CreateDefaultTile()
        {
            GameObject tileGO = new GameObject("SumPuzzleTile");
            tileGO.layer = 5; // UI layer

            RectTransform rect = tileGO.AddComponent<RectTransform>();

            Image image = tileGO.AddComponent<Image>();
            image.color = new Color(0.9f, 0.9f, 0.9f, 1f);

            Button button = tileGO.AddComponent<Button>();
            button.targetGraphic = image;

            // Add outline/background
            GameObject bgGO = new GameObject("Background");
            bgGO.transform.SetParent(tileGO.transform, false);
            Image bgImage = bgGO.AddComponent<Image>();
            bgImage.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            RectTransform bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            return tileGO;
        }

        /// <summary>
        /// Setup row and column target displays
        /// </summary>
        private void SetupTargetDisplays(SumPuzzleLevel level)
        {
            // Row targets (right side)
            for (int i = 0; i < level.GridSize && i < rowTargetTexts.Length; i++)
            {
                if (rowTargetTexts[i] != null)
                {
                    rowTargetTexts[i].text = level.RowTargets[i].ToString();
                    rowTargetTexts[i].gameObject.SetActive(true);
                }
            }

            // Hide unused
            for (int i = level.GridSize; i < rowTargetTexts.Length; i++)
            {
                if (rowTargetTexts[i] != null)
                    rowTargetTexts[i].gameObject.SetActive(false);
            }

            // Column targets (bottom)
            for (int i = 0; i < level.GridSize && i < columnTargetTexts.Length; i++)
            {
                if (columnTargetTexts[i] != null)
                {
                    columnTargetTexts[i].text = level.ColumnTargets[i].ToString();
                    columnTargetTexts[i].gameObject.SetActive(true);
                }
            }

            // Hide unused
            for (int i = level.GridSize; i < columnTargetTexts.Length; i++)
            {
                if (columnTargetTexts[i] != null)
                    columnTargetTexts[i].gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Update the progress display
        /// </summary>
        public void UpdateProgress(SumPuzzleBoard board)
        {
            if (board == null)
                return;

            // Update timer
            if (timerText != null)
            {
                int minutes = Mathf.FloorToInt(Time.time / 60f);
                int seconds = Mathf.FloorToInt(Time.time % 60f);
                timerText.text = $"{minutes:00}:{seconds:00}";
            }

            // Update moves
            if (movesText != null)
            {
                movesText.text = $"Moves: {board.GetMoveCount()}";
            }

            // Update progress bar
            float completion = board.GetCompletionPercentage();
            if (progressSlider != null)
            {
                progressSlider.value = completion / 100f;
            }

            if (progressText != null)
            {
                progressText.text = $"{completion:0}%";
            }
        }

        /// <summary>
        /// Show/hide the number selector
        /// </summary>
        public void ShowNumberSelector(bool show)
        {
            if (numberSelectorPanel != null)
            {
                numberSelectorPanel.SetActive(show);
            }

            // Enable/disable number buttons
            if (numberButtons != null)
            {
                foreach (var btn in numberButtons)
                {
                    if (btn != null)
                        btn.interactable = show;
                }
            }
        }

        /// <summary>
        /// Show the main game UI
        /// </summary>
        public void ShowGameUI()
        {
            if (gameCanvas != null)
                gameCanvas.gameObject.SetActive(true);

            if (gameUIPanel != null)
                gameUIPanel.SetActive(true);

            if (pauseCanvas != null)
                pauseCanvas.gameObject.SetActive(false);

            if (completionCanvas != null)
                completionCanvas.gameObject.SetActive(false);

            ShowNumberSelector(true);
        }

        /// <summary>
        /// Show pause menu
        /// </summary>
        public void ShowPauseMenu()
        {
            if (pauseCanvas != null)
                pauseCanvas.gameObject.SetActive(true);

            if (gameUIPanel != null)
                gameUIPanel.SetActive(false);
        }

        /// <summary>
        /// Hide pause menu
        /// </summary>
        public void HidePauseMenu()
        {
            if (pauseCanvas != null)
                pauseCanvas.gameObject.SetActive(false);

            if (gameUIPanel != null)
                gameUIPanel.SetActive(true);
        }

        /// <summary>
        /// Show level completion screen
        /// </summary>
        public void ShowCompletionScreen(int moves, float time)
        {
            if (gameUIPanel != null)
                gameUIPanel.SetActive(false);

            if (completionPanel != null)
                completionPanel.SetActive(true);

            if (completionTitle != null)
                completionTitle.text = "Level Complete!";

            if (completionStats != null)
            {
                int minutes = Mathf.FloorToInt(time / 60f);
                int seconds = Mathf.FloorToInt(time % 60f);
                completionStats.text = $"Time: {minutes:00}:{seconds:00}\nMoves: {moves}";
            }

            // Hide next level button if this was the last level
            if (nextLevelButton != null)
            {
                nextLevelButton.gameObject.SetActive(_currentLevelId < 5);
            }
        }

        /// <summary>
        /// Show failure screen
        /// </summary>
        public void ShowFailureScreen(string reason)
        {
            if (gameUIPanel != null)
                gameUIPanel.SetActive(false);

            if (failurePanel != null)
                failurePanel.SetActive(true);

            if (failureReasonText != null)
                failureReasonText.text = reason;
        }
    }
}
