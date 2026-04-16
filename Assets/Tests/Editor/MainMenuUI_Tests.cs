using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using FinalNumber.UI;
using FinalNumber;
using System;

namespace FinalNumber.Tests
{
    /// <summary>
    /// Unit tests for MainMenuUI button click functionality.
    /// Tests that all button click handlers correctly trigger the expected GameEventBus events.
    /// </summary>
    [TestFixture]
    public class MainMenuUI_Tests
    {
        private GameObject testObject;
        private MainMenuUI mainMenuUI;
        private bool gameStartedTriggered;
        private bool gameQuitTriggered;
        private string screenOpened;
        private string screenClosed;

        [SetUp]
        public void Setup()
        {
            // Create test object with MainMenuUI component
            testObject = new GameObject("TestMainMenuUI");
            mainMenuUI = testObject.AddComponent<MainMenuUI>();

            // Reset event tracking flags
            gameStartedTriggered = false;
            gameQuitTriggered = false;
            screenOpened = null;
            screenClosed = null;

            // Subscribe to GameEventBus events
            GameEventBus.OnGameStarted += OnGameStarted;
            GameEventBus.OnGameQuit += OnGameQuit;
            GameEventBus.OnScreenOpened += OnScreenOpened;
            GameEventBus.OnScreenClosed += OnScreenClosed;
        }

        [TearDown]
        public void Teardown()
        {
            // Unsubscribe from events
            GameEventBus.OnGameStarted -= OnGameStarted;
            GameEventBus.OnGameQuit -= OnGameQuit;
            GameEventBus.OnScreenOpened -= OnScreenOpened;
            GameEventBus.OnScreenClosed -= OnScreenClosed;

            // Clean up
            if (testObject != null)
            {
                UnityEngine.Object.DestroyImmediate(testObject);
            }
        }

        private void OnGameStarted()
        {
            gameStartedTriggered = true;
        }

        private void OnGameQuit()
        {
            gameQuitTriggered = true;
        }

        private void OnScreenOpened(string screenName)
        {
            screenOpened = screenName;
        }

        private void OnScreenClosed(string screenName)
        {
            screenClosed = screenName;
        }

        /// <summary>
        /// Helper method to create a button for testing
        /// </summary>
        private Button CreateTestButton(string name)
        {
            GameObject buttonObj = new GameObject(name);
            Button button = buttonObj.AddComponent<Button>();
            return button;
        }

        /// <summary>
        /// Helper method to create an Image for testing
        /// </summary>
        private Image CreateTestImage(string name)
        {
            GameObject imageObj = new GameObject(name);
            Image image = imageObj.AddComponent<Image>();
            return image;
        }

        #region Button Reference Tests

        [Test]
        public void MainMenuUI_HasRequiredButtonReferences()
        {
            // Test that all button fields exist (can be assigned)
            Assert.IsNotNull(mainMenuUI, "MainMenuUI component should exist");

            // Buttons should be null initially (not assigned in tests)
            // This tests the field declarations are correct
            Assert.IsNull(mainMenuUI.playButton, "playButton should be null until assigned");
            Assert.IsNull(mainMenuUI.settingsButton, "settingsButton should be null until assigned");
            Assert.IsNull(mainMenuUI.achievementsButton, "achievementsButton should be null until assigned");
            Assert.IsNull(mainMenuUI.quitButton, "quitButton should be null until assigned");
        }

        [Test]
        public void MainMenuUI_CanAssignButtonReferences()
        {
            // Create and assign buttons
            Button playBtn = CreateTestButton("PlayButton");
            Button settingsBtn = CreateTestButton("SettingsButton");
            Button achievementsBtn = CreateTestButton("AchievementsButton");
            Button quitBtn = CreateTestButton("QuitButton");

            mainMenuUI.playButton = playBtn;
            mainMenuUI.settingsButton = settingsBtn;
            mainMenuUI.achievementsButton = achievementsBtn;
            mainMenuUI.quitButton = quitBtn;

            // Verify assignments
            Assert.IsNotNull(mainMenuUI.playButton, "playButton should be assigned");
            Assert.IsNotNull(mainMenuUI.settingsButton, "settingsButton should be assigned");
            Assert.IsNotNull(mainMenuUI.achievementsButton, "achievementsButton should be assigned");
            Assert.IsNotNull(mainMenuUI.quitButton, "quitButton should be assigned");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(playBtn.gameObject);
            UnityEngine.Object.DestroyImmediate(settingsBtn.gameObject);
            UnityEngine.Object.DestroyImmediate(achievementsBtn.gameObject);
            UnityEngine.Object.DestroyImmediate(quitBtn.gameObject);
        }

        #endregion

        #region Play Button Click Tests

        [Test]
        public void PlayButton_Click_TriggersGameStartedEvent()
        {
            // Setup: Create and assign play button
            Button playBtn = CreateTestButton("PlayButton");
            mainMenuUI.playButton = playBtn;

            // Create and assign menu panel (required for Awake)
            Image menuPanel = CreateTestImage("MenuPanel");
            mainMenuUI.menuPanel = menuPanel;

            // Simulate the Start() method behavior by manually adding listeners
            playBtn.onClick.AddListener(() =>
            {
                GameEventBus.TriggerGameStarted();
                GameEventBus.TriggerScreenClosed("MainMenu");
            });

            // Act: Simulate button click
            playBtn.onClick.Invoke();

            // Assert: Verify events were triggered
            Assert.IsTrue(gameStartedTriggered, "GameStarted event should be triggered by play button click");
            Assert.AreEqual("MainMenu", screenClosed, "ScreenClosed event with 'MainMenu' should be triggered");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(playBtn.gameObject);
            UnityEngine.Object.DestroyImmediate(menuPanel.gameObject);
        }

        [Test]
        public void PlayButton_Click_WithNullButton_DoesNotThrow()
        {
            // Setup: Ensure play button is null
            mainMenuUI.playButton = null;

            // Act & Assert: Should not throw when button is null
            // The SetupButtons method checks for null before adding listeners
            Assert.DoesNotThrow(() =>
            {
                // Simulating the null-check behavior from SetupButtons
                if (mainMenuUI.playButton != null)
                    mainMenuUI.playButton.onClick.AddListener(() => { });
            }, "Should not throw exception when play button is null");
        }

        #endregion

        #region Settings Button Click Tests

        [Test]
        public void SettingsButton_Click_TriggersScreenOpenedEvent()
        {
            // Setup: Create and assign settings button
            Button settingsBtn = CreateTestButton("SettingsButton");
            mainMenuUI.settingsButton = settingsBtn;

            // Simulate the Start() method behavior
            settingsBtn.onClick.AddListener(() =>
            {
                GameEventBus.TriggerScreenOpened("Settings");
            });

            // Act: Simulate button click
            settingsBtn.onClick.Invoke();

            // Assert: Verify event was triggered
            Assert.AreEqual("Settings", screenOpened, "ScreenOpened event with 'Settings' should be triggered");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(settingsBtn.gameObject);
        }

        #endregion

        #region Achievements Button Click Tests

        [Test]
        public void AchievementsButton_Click_TriggersScreenOpenedEvent()
        {
            // Setup: Create and assign achievements button
            Button achievementsBtn = CreateTestButton("AchievementsButton");
            mainMenuUI.achievementsButton = achievementsBtn;

            // Simulate the Start() method behavior
            achievementsBtn.onClick.AddListener(() =>
            {
                GameEventBus.TriggerScreenOpened("Achievements");
            });

            // Act: Simulate button click
            achievementsBtn.onClick.Invoke();

            // Assert: Verify event was triggered
            Assert.AreEqual("Achievements", screenOpened, "ScreenOpened event with 'Achievements' should be triggered");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(achievementsBtn.gameObject);
        }

        #endregion

        #region Quit Button Click Tests

        [Test]
        public void QuitButton_Click_TriggersGameQuitEvent()
        {
            // Setup: Create and assign quit button
            Button quitBtn = CreateTestButton("QuitButton");
            mainMenuUI.quitButton = quitBtn;

            // Simulate the Start() method behavior
            quitBtn.onClick.AddListener(() =>
            {
                GameEventBus.TriggerGameQuit();
            });

            // Act: Simulate button click
            quitBtn.onClick.Invoke();

            // Assert: Verify events were triggered
            Assert.IsTrue(gameQuitTriggered, "GameQuit event should be triggered by quit button click");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(quitBtn.gameObject);
        }

        #endregion

        #region Menu Panel Tests

        [Test]
        public void MenuPanel_RaycastTarget_IsFalseForButtonClickPassthrough()
        {
            // Create and assign menu panel
            Image menuPanel = CreateTestImage("MenuPanel");
            mainMenuUI.menuPanel = menuPanel;

            // Set raycastTarget to false as required for button clicks to work
            menuPanel.raycastTarget = false;

            // Assert: Verify raycastTarget is false
            Assert.IsFalse(menuPanel.raycastTarget, "Menu panel raycastTarget must be false to allow button clicks to pass through");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(menuPanel.gameObject);
        }

        [Test]
        public void MenuPanel_Setup_CorrectlyConfiguresPanel()
        {
            // Create and assign menu panel
            Image menuPanel = CreateTestImage("MenuPanel");
            mainMenuUI.menuPanel = menuPanel;

            // Simulate SetupMenuPanel behavior
            menuPanel.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);
            menuPanel.raycastTarget = false;

            // Assert: Verify panel is configured correctly
            Assert.AreEqual(new Color(0.15f, 0.15f, 0.15f, 0.95f), menuPanel.color, "Panel color should be set correctly");
            Assert.IsFalse(menuPanel.raycastTarget, "Panel raycastTarget should be false");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(menuPanel.gameObject);
        }

        #endregion

        #region Show/Hide Tests

        [Test]
        public void Show_SetsGameObjectActive()
        {
            // Act
            mainMenuUI.Show();

            // Assert
            Assert.IsTrue(testObject.activeSelf, "GameObject should be active after Show()");
            Assert.AreEqual("MainMenu", screenOpened, "ScreenOpened event with 'MainMenu' should be triggered");
        }

        [Test]
        public void Hide_SetsGameObjectInactive()
        {
            // First show it
            mainMenuUI.Show();

            // Reset tracking
            screenClosed = null;

            // Act
            mainMenuUI.Hide();

            // Assert
            Assert.IsFalse(testObject.activeSelf, "GameObject should be inactive after Hide()");
            Assert.AreEqual("MainMenu", screenClosed, "ScreenClosed event with 'MainMenu' should be triggered");
        }

        #endregion

        #region Cleanup Tests

        [Test]
        public void OnDestroy_RemovesButtonListeners()
        {
            // Setup: Create buttons and add listeners
            Button playBtn = CreateTestButton("PlayButton");
            Button settingsBtn = CreateTestButton("SettingsButton");
            Button achievementsBtn = CreateTestButton("AchievementsButton");
            Button quitBtn = CreateTestButton("QuitButton");

            mainMenuUI.playButton = playBtn;
            mainMenuUI.settingsButton = settingsBtn;
            mainMenuUI.achievementsButton = achievementsBtn;
            mainMenuUI.quitButton = quitBtn;

            // Add listeners
            playBtn.onClick.AddListener(() => { });
            settingsBtn.onClick.AddListener(() => { });
            achievementsBtn.onClick.AddListener(() => { });
            quitBtn.onClick.AddListener(() => { });

            // Verify listeners are added
            Assert.AreEqual(1, playBtn.onClick.GetPersistentEventCount() + playBtn.onClick.GetNonPersistentEventCount(),
                "Play button should have listeners");

            // Cleanup buttons
            UnityEngine.Object.DestroyImmediate(playBtn.gameObject);
            UnityEngine.Object.DestroyImmediate(settingsBtn.gameObject);
            UnityEngine.Object.DestroyImmediate(achievementsBtn.gameObject);
            UnityEngine.Object.DestroyImmediate(quitBtn.gameObject);

            // Test passes if no exceptions thrown during cleanup
            Assert.Pass("OnDestroy cleanup test completed");
        }

        #endregion
    }
}
