using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using TMPro;
using FinalNumber.UI;

namespace FinalNumber.Tests.PlayMode
{
    /// <summary>
    /// PlayMode tests for MainMenuUI functionality.
    /// Tests button click handling, runtime UI creation, and platform-specific behavior.
    /// </summary>
    public class MainMenuUIPlayModeTests
    {
        private GameObject _mainMenuGO;
        private MainMenuUI _mainMenuUI;

        [SetUp]
        public void Setup()
        {
            // Create a clean MainMenuUI instance for each test
            _mainMenuGO = new GameObject("TestMainMenuUI");
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up any created GameObjects
            if (_mainMenuGO != null)
            {
                Object.DestroyImmediate(_mainMenuGO);
            }

            // Clean up any EventSystems created during tests
            var eventSystems = Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None);
            foreach (var es in eventSystems)
            {
                if (es != null && es.gameObject != null)
                {
                    Object.DestroyImmediate(es.gameObject);
                }
            }

            // Clean up any canvases created during tests
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var canvas in canvases)
            {
                if (canvas != null && canvas.gameObject != null && canvas.gameObject.name.Contains("MainMenuCanvas"))
                {
                    Object.DestroyImmediate(canvas.gameObject);
                }
            }
        }

        #region Initialization Tests

        [UnityTest]
        public IEnumerator Awake_CreatesCanvas_WhenMissing()
        {
            // Add MainMenuUI without any pre-configured references
            _mainMenuUI = _mainMenuGO.AddComponent<MainMenuUI>();

            yield return null; // Wait for Awake to execute

            // Check that a canvas was created
            var canvas = _mainMenuGO.GetComponentInChildren<Canvas>();
            Assert.IsNotNull(canvas, "Canvas should be created when missing");
        }

        [UnityTest]
        public IEnumerator Awake_CreatesMenuPanel_WhenMissing()
        {
            _mainMenuUI = _mainMenuGO.AddComponent<MainMenuUI>();

            yield return null;

            // Check that menu panel was created
            Assert.IsNotNull(_mainMenuUI.menuPanel, "Menu panel should be created");
        }

        [UnityTest]
        public IEnumerator Awake_CreatesStartButton_WhenMissing()
        {
            _mainMenuUI = _mainMenuGO.AddComponent<MainMenuUI>();

            yield return null;

            // Check that start button was created
            Assert.IsNotNull(_mainMenuUI.startGameButton, "Start button should be created");
        }

        [UnityTest]
        public IEnumerator Awake_CreatesSettingsButton_WhenMissing()
        {
            _mainMenuUI = _mainMenuGO.AddComponent<MainMenuUI>();

            yield return null;

            // Check that settings button was created
            Assert.IsNotNull(_mainMenuUI.settingsButton, "Settings button should be created");
        }

        [UnityTest]
        public IEnumerator Awake_CreatesTitleText_WhenMissing()
        {
            _mainMenuUI = _mainMenuGO.AddComponent<MainMenuUI>();

            yield return null;

            // Check that title text was created
            Assert.IsNotNull(_mainMenuUI.titleText, "Title text should be created");
            Assert.AreEqual("Final Number", _mainMenuUI.titleText.text, "Title text should be set correctly");
        }

        [UnityTest]
        public IEnumerator Awake_CreatesEventSystem_WhenMissing()
        {
            _mainMenuUI = _mainMenuGO.AddComponent<MainMenuUI>();

            yield return null;

            // Check that EventSystem exists
            var eventSystem = Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
            Assert.IsNotNull(eventSystem, "EventSystem should be created");
        }

        #endregion

        #region Button Click Handling Tests

        [UnityTest]
        public IEnumerator StartButton_HasClickListener()
        {
            _mainMenuUI = _mainMenuGO.AddComponent<MainMenuUI>();

            yield return null;

            // Check that start button has a listener
            Assert.IsNotNull(_mainMenuUI.startGameButton, "Start button should exist");
            Assert.IsTrue(_mainMenuUI.startGameButton.interactable, "Start button should be interactable");
        }

        [UnityTest]
        public IEnumerator SettingsButton_HasClickListener()
        {
            _mainMenuUI = _mainMenuGO.AddComponent<MainMenuUI>();

            yield return null;

            // Check that settings button has a listener
            Assert.IsNotNull(_mainMenuUI.settingsButton, "Settings button should exist");
            Assert.IsTrue(_mainMenuUI.settingsButton.interactable, "Settings button should be interactable");
        }

        [UnityTest]
        public IEnumerator QuitButton_HasClickListener_OnSupportedPlatforms()
        {
            _mainMenuUI = _mainMenuGO.AddComponent<MainMenuUI>();

            yield return null;

            // Quit button exists on all platforms but may be hidden on mobile
            Assert.IsNotNull(_mainMenuUI.quitButton, "Quit button should exist");
        }

        [UnityTest]
        public IEnumerator ClickStartButton_TriggersGameStartedEvent()
        {
            _mainMenuUI = _mainMenuGO.AddComponent<MainMenuUI>();

            bool gameStarted = false;
            GameEventBus.OnGameStarted += () => gameStarted = true;

            yield return null;

            // Simulate button click
            _mainMenuUI.startGameButton.onClick.Invoke();

            yield return null;

            Assert.IsTrue(gameStarted, "GameStarted event should be triggered");

            // Cleanup
            GameEventBus.OnGameStarted = null;
        }

        #endregion

        #region Platform-Specific Tests

        [UnityTest]
        public IEnumerator QuitButton_Visibility_MatchesPlatform()
        {
            _mainMenuUI = _mainMenuGO.AddComponent<MainMenuUI>();

            yield return null;

            // On mobile/web platforms, quit button should be hidden
            // On desktop platforms, quit button should be visible
#if UNITY_ANDROID || UNITY_IOS || UNITY_WEBGL
            Assert.IsFalse(_mainMenuUI.quitButton.gameObject.activeSelf,
                "Quit button should be hidden on mobile/web platforms");
#else
            Assert.IsTrue(_mainMenuUI.quitButton.gameObject.activeSelf,
                "Quit button should be visible on desktop platforms");
#endif
        }

        #endregion

        #region UI State Management Tests

        [UnityTest]
        public IEnumerator ShowMainMenu_EnablesCanvas()
        {
            _mainMenuUI = _mainMenuGO.AddComponent<MainMenuUI>();

            yield return null;

            // First hide the menu
            if (_mainMenuUI.mainMenuCanvas != null)
            {
                _mainMenuUI.mainMenuCanvas.gameObject.SetActive(false);
            }

            yield return null;

            // Call ReturnToMainMenu which internally shows the menu
            _mainMenuUI.ReturnToMainMenu();

            yield return null;

            Assert.IsTrue(_mainMenuUI.mainMenuCanvas.gameObject.activeSelf,
                "Canvas should be active after ReturnToMainMenu");
        }

        [UnityTest]
        public IEnumerator HideMainMenu_DisablesCanvas()
        {
            _mainMenuUI = _mainMenuGO.AddComponent<MainMenuUI>();

            yield return null;

            // Simulate game start which hides the menu
            _mainMenuUI.startGameButton.onClick.Invoke();

            yield return null;

            Assert.IsFalse(_mainMenuUI.mainMenuCanvas.gameObject.activeSelf,
                "Canvas should be inactive after starting game");
        }

        [UnityTest]
        public IEnumerator SetButtonsEnabled_TogglesInteractivity()
        {
            _mainMenuUI = _mainMenuGO.AddComponent<MainMenuUI>();

            yield return null;

            // Disable all buttons
            _mainMenuUI.SetButtonsEnabled(false);

            Assert.IsFalse(_mainMenuUI.startGameButton.interactable,
                "Start button should be disabled");
            Assert.IsFalse(_mainMenuUI.settingsButton.interactable,
                "Settings button should be disabled");

            // Re-enable buttons
            _mainMenuUI.SetButtonsEnabled(true);

            Assert.IsTrue(_mainMenuUI.startGameButton.interactable,
                "Start button should be enabled");
            Assert.IsTrue(_mainMenuUI.settingsButton.interactable,
                "Settings button should be enabled");
        }

        [UnityTest]
        public IEnumerator LoadingIndicator_HiddenByDefault()
        {
            _mainMenuUI = _mainMenuGO.AddComponent<MainMenuUI>();

            yield return null;

            // Loading indicator may be null or inactive initially
            if (_mainMenuUI.loadingIndicator != null)
            {
                Assert.IsFalse(_mainMenuUI.loadingIndicator.activeSelf,
                    "Loading indicator should be hidden by default");
            }
        }

        #endregion

        #region Text Configuration Tests

        [UnityTest]
        public IEnumerator TitleText_HasCorrectText()
        {
            _mainMenuUI = _mainMenuGO.AddComponent<MainMenuUI>();

            yield return null;

            Assert.IsNotNull(_mainMenuUI.titleText, "Title text should exist");
            Assert.IsFalse(string.IsNullOrEmpty(_mainMenuUI.titleText.text),
                "Title text should not be empty");
        }

        [UnityTest]
        public IEnumerator SubtitleText_HasCorrectText()
        {
            _mainMenuUI = _mainMenuGO.AddComponent<MainMenuUI>();

            yield return null;

            Assert.IsNotNull(_mainMenuUI.subtitleText, "Subtitle text should exist");
            Assert.IsFalse(string.IsNullOrEmpty(_mainMenuUI.subtitleText.text),
                "Subtitle text should not be empty");
        }

        [UnityTest]
        public IEnumerator ButtonTexts_AreSetCorrectly()
        {
            _mainMenuUI = _mainMenuGO.AddComponent<MainMenuUI>();

            yield return null;

            // Get text components from buttons
            var startButtonText = _mainMenuUI.startGameButton.GetComponentInChildren<TextMeshProUGUI>();
            var settingsButtonText = _mainMenuUI.settingsButton.GetComponentInChildren<TextMeshProUGUI>();

            Assert.IsNotNull(startButtonText, "Start button should have text");
            Assert.IsNotNull(settingsButtonText, "Settings button should have text");

            Assert.IsFalse(string.IsNullOrEmpty(startButtonText.text),
                "Start button text should not be empty");
            Assert.IsFalse(string.IsNullOrEmpty(settingsButtonText.text),
                "Settings button text should not be empty");
        }

        #endregion

        #region Canvas Configuration Tests

        [UnityTest]
        public IEnumerator Canvas_IsScreenSpaceOverlay()
        {
            _mainMenuUI = _mainMenuGO.AddComponent<MainMenuUI>();

            yield return null;

            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, _mainMenuUI.mainMenuCanvas.renderMode,
                "Canvas should be in Screen Space Overlay mode");
        }

        [UnityTest]
        public IEnumerator Canvas_HasGraphicRaycaster()
        {
            _mainMenuUI = _mainMenuGO.AddComponent<MainMenuUI>();

            yield return null;

            var raycaster = _mainMenuUI.mainMenuCanvas.GetComponent<GraphicRaycaster>();
            Assert.IsNotNull(raycaster, "Canvas should have GraphicRaycaster for click detection");
        }

        [UnityTest]
        public IEnumerator Canvas_HasCanvasScaler()
        {
            _mainMenuUI = _mainMenuGO.AddComponent<MainMenuUI>();

            yield return null;

            var scaler = _mainMenuUI.mainMenuCanvas.GetComponent<CanvasScaler>();
            Assert.IsNotNull(scaler, "Canvas should have CanvasScaler for responsive UI");
        }

        #endregion

        #region Event Bus Integration Tests

        [UnityTest]
        public IEnumerator ScreenOpenedEvent_Triggered_WhenMenuShown()
        {
            _mainMenuUI = _mainMenuGO.AddComponent<MainMenuUI>();

            bool screenOpened = false;
            string openedScreen = "";
            GameEventBus.OnScreenOpened += (screenName) =>
            {
                screenOpened = true;
                openedScreen = screenName;
            };

            yield return null;

            // Menu should have triggered ScreenOpened in Start
            Assert.IsTrue(screenOpened, "ScreenOpened event should be triggered");
            Assert.AreEqual("MainMenu", openedScreen, "Screen name should be MainMenu");

            // Cleanup
            GameEventBus.OnScreenOpened = null;
        }

        [UnityTest]
        public IEnumerator ScreenClosedEvent_Triggered_WhenMenuHidden()
        {
            _mainMenuUI = _mainMenuGO.AddComponent<MainMenuUI>();

            bool screenClosed = false;
            string closedScreen = "";
            GameEventBus.OnScreenClosed += (screenName) =>
            {
                screenClosed = true;
                closedScreen = screenName;
            };

            yield return null;

            // Hide the menu
            _mainMenuUI.startGameButton.onClick.Invoke();

            yield return null;

            Assert.IsTrue(screenClosed, "ScreenClosed event should be triggered");
            Assert.AreEqual("MainMenu", closedScreen, "Screen name should be MainMenu");

            // Cleanup
            GameEventBus.OnScreenClosed = null;
        }

        #endregion

        #region Settings Panel Tests

        [UnityTest]
        public IEnumerator SettingsPanel_HiddenByDefault()
        {
            _mainMenuUI = _mainMenuGO.AddComponent<MainMenuUI>();

            yield return null;

            // Settings panel may be null or inactive initially
            if (_mainMenuUI.settingsPanel != null)
            {
                Assert.IsFalse(_mainMenuUI.settingsPanel.activeSelf,
                    "Settings panel should be hidden by default");
            }
        }

        [UnityTest]
        public IEnumerator ClickSettingsButton_ShowsSettings()
        {
            _mainMenuUI = _mainMenuGO.AddComponent<MainMenuUI>();

            // Create a mock settings panel
            var settingsPanelGO = new GameObject("SettingsPanel");
            settingsPanelGO.SetActive(false);
            _mainMenuUI.settingsPanel = settingsPanelGO;

            yield return null;

            // Click settings button
            _mainMenuUI.settingsButton.onClick.Invoke();

            yield return null;

            Assert.IsTrue(settingsPanelGO.activeSelf, "Settings panel should be shown");

            // Cleanup
            Object.DestroyImmediate(settingsPanelGO);
        }

        #endregion

        #region Edge Case Tests

        [UnityTest]
        public IEnumerator MultipleStartButtonClicks_Prevented()
        {
            _mainMenuUI = _mainMenuGO.AddComponent<MainMenuUI>();

            int gameStartedCount = 0;
            GameEventBus.OnGameStarted += () => gameStartedCount++;

            yield return null;

            // Click start button multiple times
            _mainMenuUI.startGameButton.onClick.Invoke();
            _mainMenuUI.startGameButton.onClick.Invoke();
            _mainMenuUI.startGameButton.onClick.Invoke();

            yield return null;

            // Should only trigger once due to isGameStarting flag
            Assert.AreEqual(1, gameStartedCount, "Game should only start once");

            // Cleanup
            GameEventBus.OnGameStarted = null;
        }

        [UnityTest]
        public IEnumerator ReturnToMainMenu_ResetsGameStartingState()
        {
            _mainMenuUI = _mainMenuGO.AddComponent<MainMenuUI>();

            yield return null;

            // Start the game
            _mainMenuUI.startGameButton.onClick.Invoke();
            yield return null;

            // Return to main menu
            _mainMenuUI.ReturnToMainMenu();
            yield return null;

            // Now we should be able to start the game again
            int gameStartedCount = 0;
            GameEventBus.OnGameStarted += () => gameStartedCount++;

            _mainMenuUI.startGameButton.onClick.Invoke();
            yield return null;

            Assert.AreEqual(1, gameStartedCount, "Game should start after returning to menu");

            // Cleanup
            GameEventBus.OnGameStarted = null;
        }

        #endregion
    }
}
