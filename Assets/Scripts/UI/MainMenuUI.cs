using UnityEngine;
using UnityEngine.UI;

namespace FinalNumber.UI
{
    /// <summary>
    /// Main menu UI controller for the game's homescreen.
    /// Handles the play button, settings button, and other main menu interactions.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("The main menu panel background image")]
        public Image menuPanel;

        [Tooltip("Button to start the game")]
        public Button playButton;

        [Tooltip("Button to open settings")]
        public Button settingsButton;

        [Tooltip("Button to view achievements")]
        public Button achievementsButton;

        [Tooltip("Button to quit the game")]
        public Button quitButton;

        [Header("Panel Settings")]
        [Tooltip("Background color of the menu panel")]
        public Color panelColor = new Color(0.15f, 0.15f, 0.15f, 0.95f);

        [Tooltip("Enable raycasting on the panel (should be false to allow button clicks)")]
        public bool panelRaycastTarget = false;

        private void Awake()
        {
            SetupMenuPanel();
        }

        private void Start()
        {
            SetupButtons();
        }

        /// <summary>
        /// Configures the menu panel appearance and raycast settings.
        /// The panel is a parent of the buttons, so raycastTarget must be false
        /// to allow click events to pass through to the buttons underneath.
        /// </summary>
        private void SetupMenuPanel()
        {
            if (menuPanel == null)
            {
                Debug.LogError("[MainMenuUI] Menu panel is not assigned!");
                return;
            }

            // Configure panel visual appearance
            menuPanel.color = panelColor;

            // CRITICAL FIX: raycastTarget must be false so clicks pass through to buttons
            // When raycastTarget is true, the panel blocks all click events from reaching
            // child buttons because Unity's EventSystem gives priority to the parent Image
            menuPanel.raycastTarget = false;
        }

        /// <summary>
        /// Sets up button click listeners
        /// </summary>
        private void SetupButtons()
        {
            if (playButton != null)
                playButton.onClick.AddListener(OnPlayClicked);

            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettingsClicked);

            if (achievementsButton != null)
                achievementsButton.onClick.AddListener(OnAchievementsClicked);

            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);
        }

        /// <summary>
        /// Called when the play button is clicked
        /// </summary>
        private void OnPlayClicked()
        {
            Debug.Log("[MainMenuUI] Play button clicked - starting game");
            GameEventBus.TriggerGameStarted();
            GameEventBus.TriggerScreenClosed("MainMenu");
            // TODO: Load game scene or transition to gameplay
        }

        /// <summary>
        /// Called when the settings button is clicked
        /// </summary>
        private void OnSettingsClicked()
        {
            Debug.Log("[MainMenuUI] Settings button clicked");
            GameEventBus.TriggerScreenOpened("Settings");
            // TODO: Open settings panel
        }

        /// <summary>
        /// Called when the achievements button is clicked
        /// </summary>
        private void OnAchievementsClicked()
        {
            Debug.Log("[MainMenuUI] Achievements button clicked");
            GameEventBus.TriggerScreenOpened("Achievements");
            // TODO: Open achievements panel
        }

        /// <summary>
        /// Called when the quit button is clicked
        /// </summary>
        private void OnQuitClicked()
        {
            Debug.Log("[MainMenuUI] Quit button clicked");
            GameEventBus.TriggerGameQuit();
            Application.Quit();
        }

        private void OnDestroy()
        {
            // Clean up button listeners
            if (playButton != null)
                playButton.onClick.RemoveListener(OnPlayClicked);

            if (settingsButton != null)
                settingsButton.onClick.RemoveListener(OnSettingsClicked);

            if (achievementsButton != null)
                achievementsButton.onClick.RemoveListener(OnAchievementsClicked);

            if (quitButton != null)
                quitButton.onClick.RemoveListener(OnQuitClicked);
        }

        /// <summary>
        /// Shows the main menu
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
            GameEventBus.TriggerScreenOpened("MainMenu");
        }

        /// <summary>
        /// Hides the main menu
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
            GameEventBus.TriggerScreenClosed("MainMenu");
        }
    }
}
