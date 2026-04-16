using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FinalNumber.UI
{
    /// <summary>
    /// Main menu / homescreen UI controller.
    /// Handles the start game button and other main menu interactions.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Canvas containing the main menu UI")]
        public Canvas mainMenuCanvas;

        [Tooltip("Button to start the game")]
        public Button startGameButton;

        [Tooltip("Button to open settings")]
        public Button settingsButton;

        [Tooltip("Title text display")]
        public TextMeshProUGUI titleText;

        [Tooltip("Subtitle or tagline text")]
        public TextMeshProUGUI subtitleText;

        [Header("Optional UI Elements")]
        [Tooltip("Panel containing the main menu buttons")]
        public GameObject menuPanel;

        [Tooltip("Loading indicator shown when starting game")]
        public GameObject loadingIndicator;

        [Tooltip("Button to quit the game (desktop builds)")]
        public Button quitButton;

        [Header("Settings UI")]
        [Tooltip("Settings panel (optional, references PrivacySettingsUI)")]
        public GameObject settingsPanel;

        [Tooltip("Privacy settings UI component")]
        public PrivacySettingsUI privacySettingsUI;

        [Header("Audio")]
        [Tooltip("Audio source for button clicks")]
        public AudioSource buttonClickSound;

        private bool isGameStarting = false;

        private void Awake()
        {
            // Ensure UI is created early, before Start()
            EnsureUIExists();
        }

        private void Start()
        {
            InitializeUI();
            SubscribeToEvents();
            ShowMainMenu();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void InitializeUI()
        {
            // Log UI configuration for debugging
            Debug.Log($"[MainMenuUI] InitializeUI - Canvas: {(mainMenuCanvas != null ? "OK" : "MISSING")}, " +
                      $"StartButton: {(startGameButton != null ? "OK" : "MISSING")}, " +
                      $"SettingsButton: {(settingsButton != null ? "OK" : "MISSING")}, " +
                      $"QuitButton: {(quitButton != null ? "OK" : "MISSING")}");

            // Ensure we have an EventSystem for UI input handling
            EnsureEventSystemExists();

            // Setup button listeners
            if (startGameButton != null)
            {
                startGameButton.onClick.RemoveAllListeners();
                startGameButton.onClick.AddListener(OnStartGameClicked);
                Debug.Log("[MainMenuUI] Start Game button listener attached.");
            }
            else
            {
                Debug.LogError("[MainMenuUI] Start Game button is null! Button clicks will not work.");
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveAllListeners();
                settingsButton.onClick.AddListener(OnSettingsClicked);
                Debug.Log("[MainMenuUI] Settings button listener attached.");
            }
            else
            {
                Debug.LogWarning("[MainMenuUI] Settings button is null.");
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveAllListeners();
                quitButton.onClick.AddListener(OnQuitClicked);
#if UNITY_ANDROID || UNITY_IOS || UNITY_WEBGL
                // Hide quit button on mobile/web platforms
                quitButton.gameObject.SetActive(false);
#endif
                Debug.Log("[MainMenuUI] Quit button listener attached.");
            }

            // Set default text if not configured
            if (titleText != null && string.IsNullOrEmpty(titleText.text))
            {
                titleText.text = "Final Number";
            }

            if (subtitleText != null && string.IsNullOrEmpty(subtitleText.text))
                subtitleText.text = "A Number Puzzle Game";

            // Hide loading indicator
            if (loadingIndicator != null)
                loadingIndicator.SetActive(false);

            // Hide settings panel initially
            if (settingsPanel != null)
                settingsPanel.SetActive(false);

            // Ensure canvas is enabled
            if (mainMenuCanvas != null)
            {
                mainMenuCanvas.enabled = true;
                mainMenuCanvas.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogError("[MainMenuUI] Main menu canvas is null! UI will not be visible.");
            }
        }

        private void EnsureEventSystemExists()
        {
            // Check if an EventSystem already exists in the scene
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                // Create a new EventSystem GameObject
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

                // For Unity 2020.3+ with new Input System, add the appropriate input module
#if ENABLE_INPUT_SYSTEM
                var inputModule = eventSystem.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                if (inputModule != null)
                {
                    DestroyImmediate(inputModule);
                }
                eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#endif
            }
        }

        /// <summary>
        /// Creates the entire UI hierarchy at runtime if it doesn't exist.
        /// This ensures the game is playable even if scene setup is incomplete.
        /// </summary>
        private void EnsureUIExists()
        {
            // If we already have a canvas assigned, skip runtime creation
            if (mainMenuCanvas != null)
            {
                Debug.Log("[MainMenuUI] Using existing canvas from inspector.");
                return;
            }

            Debug.Log("[MainMenuUI] No canvas assigned - creating UI at runtime.");

            // Create the main canvas
            GameObject canvasGO = new GameObject("MainMenuCanvas");
            mainMenuCanvas = canvasGO.AddComponent<Canvas>();
            mainMenuCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            mainMenuCanvas.sortingOrder = 0;

            // Add CanvasScaler for responsive UI
            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // Add GraphicRaycaster for click detection
            canvasGO.AddComponent<GraphicRaycaster>();

            // Make this a child of this GameObject for organization
            canvasGO.transform.SetParent(transform, false);

            // Create the menu panel as a child of canvas
            GameObject panelGO = new GameObject("MenuPanel");
            panelGO.transform.SetParent(canvasGO.transform, false);
            menuPanel = panelGO;

            // Add and configure RectTransform to fill screen
            RectTransform panelRect = panelGO.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = new Vector2(50, 50);
            panelRect.offsetMax = new Vector2(-50, -50);

            // Add an Image component for background
            Image panelImage = panelGO.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.12f, 0.15f, 0.95f);

            // Create the title text
            titleText = CreateTextElement("TitleText", panelGO.transform, "Final Number", 64,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -80));

            // Create the subtitle text
            subtitleText = CreateTextElement("SubtitleText", panelGO.transform, "A Number Puzzle Game", 32,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -160));

            // Create Start Game button
            startGameButton = CreateButton("StartGameButton", panelGO.transform, "PLAY",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 40),
                new Vector2(300, 80));

            // Create Settings button
            settingsButton = CreateButton("SettingsButton", panelGO.transform, "SETTINGS",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -60),
                new Vector2(300, 80));

            // Create Quit button (hidden on mobile)
            quitButton = CreateButton("QuitButton", panelGO.transform, "QUIT",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -160),
                new Vector2(300, 80));

#if UNITY_ANDROID || UNITY_IOS || UNITY_WEBGL
            quitButton.gameObject.SetActive(false);
#endif

            // Ensure EventSystem exists
            EnsureEventSystemExists();

            Debug.Log("[MainMenuUI] Runtime UI creation complete.");
        }

        /// <summary>
        /// Helper to create a TextMeshPro text element.
        /// </summary>
        private TextMeshProUGUI CreateTextElement(string name, Transform parent, string text, int fontSize,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition)
        {
            GameObject textGO = new GameObject(name);
            textGO.transform.SetParent(parent, false);

            TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            RectTransform rect = textGO.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(600, 100);

            return tmp;
        }

        /// <summary>
        /// Helper to create a Button with text.
        /// </summary>
        private Button CreateButton(string name, Transform parent, string buttonText,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size)
        {
            // Create button GameObject
            GameObject buttonGO = new GameObject(name);
            buttonGO.transform.SetParent(parent, false);

            // Add RectTransform
            RectTransform rect = buttonGO.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            // Add Image component for button background
            Image image = buttonGO.AddComponent<Image>();
            image.color = new Color(0.2f, 0.5f, 0.9f, 1f); // Nice blue color

            // Add Button component
            Button button = buttonGO.AddComponent<Button>();
            button.targetGraphic = image;

            // Create text child for the button
            TextMeshProUGUI text = CreateTextElement(name + "Text", buttonGO.transform, buttonText, 28,
                Vector2.zero, Vector2.one, Vector2.zero);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;

            return button;
        }

        private void SubscribeToEvents()
        {
            // Listen for game events that might affect the menu
            GameEventBus.OnGameStarted += HandleGameStarted;
        }

        private void UnsubscribeFromEvents()
        {
            GameEventBus.OnGameStarted -= HandleGameStarted;
        }

        #region UI Event Handlers

        private void OnStartGameClicked()
        {
            if (isGameStarting)
                return;

            PlayButtonClickSound();
            StartGame();
        }

        private void OnSettingsClicked()
        {
            PlayButtonClickSound();
            ShowSettings();
        }

        private void OnQuitClicked()
        {
            PlayButtonClickSound();
            QuitGame();
        }

        #endregion

        #region Actions

        private void StartGame()
        {
            isGameStarting = true;

            // Show loading indicator
            if (loadingIndicator != null)
                loadingIndicator.SetActive(true);

            // Hide menu panel
            if (menuPanel != null)
                menuPanel.SetActive(false);

            // Trigger game start event
            GameEventBus.TriggerGameStarted();

            // Transition to game scene or initialize game
            // For now, we'll hide the main menu and let the game begin
            HideMainMenu();

            Debug.Log("[MainMenuUI] Game started!");
        }

        private void ShowSettings()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
            }
            else if (privacySettingsUI != null)
            {
                privacySettingsUI.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning("[MainMenuUI] No settings panel configured!");
            }
        }

        private void QuitGame()
        {
            Debug.Log("[MainMenuUI] Quitting game...");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion

        #region UI State Management

        private void ShowMainMenu()
        {
            if (mainMenuCanvas != null)
            {
                mainMenuCanvas.gameObject.SetActive(true);
            }

            if (menuPanel != null)
            {
                menuPanel.SetActive(true);
            }

            GameEventBus.TriggerScreenOpened("MainMenu");
        }

        private void HideMainMenu()
        {
            if (mainMenuCanvas != null)
            {
                mainMenuCanvas.gameObject.SetActive(false);
            }

            GameEventBus.TriggerScreenClosed("MainMenu");
        }

        private void HandleGameStarted()
        {
            // Main menu responds to game start by hiding itself
            HideMainMenu();
        }

        #endregion

        #region Audio

        private void PlayButtonClickSound()
        {
            if (buttonClickSound != null)
            {
                buttonClickSound.Play();
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Returns to the main menu from the game.
        /// </summary>
        public void ReturnToMainMenu()
        {
            isGameStarting = false;

            if (loadingIndicator != null)
                loadingIndicator.SetActive(false);

            ShowMainMenu();
        }

        /// <summary>
        /// Enables or disables all menu buttons.
        /// </summary>
        public void SetButtonsEnabled(bool enabled)
        {
            if (startGameButton != null)
                startGameButton.interactable = enabled;

            if (settingsButton != null)
                settingsButton.interactable = enabled;

            if (quitButton != null)
                quitButton.interactable = enabled;
        }

        #endregion
    }
}
