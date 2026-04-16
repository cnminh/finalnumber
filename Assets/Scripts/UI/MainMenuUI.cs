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
            // Ensure we have an EventSystem for UI input handling
            EnsureEventSystemExists();

            // Setup button listeners
            if (startGameButton != null)
            {
                startGameButton.onClick.RemoveAllListeners();
                startGameButton.onClick.AddListener(OnStartGameClicked);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveAllListeners();
                settingsButton.onClick.AddListener(OnSettingsClicked);
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveAllListeners();
                quitButton.onClick.AddListener(OnQuitClicked);
#if UNITY_ANDROID || UNITY_IOS || UNITY_WEBGL
                // Hide quit button on mobile/web platforms
                quitButton.gameObject.SetActive(false);
#endif
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
