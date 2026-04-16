using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using FinalNumber;
using FinalNumber.UI;

namespace FinalNumber.Tests.PlayMode
{
    /// <summary>
    /// PlayMode tests for GameManager initialization and component setup.
    /// These tests run in the Unity runtime with full MonoBehaviour support.
    /// </summary>
    public class GameManagerPlayModeTests
    {
        private GameObject _gameManagerGO;
        private GameManager _gameManager;

        [SetUp]
        public void Setup()
        {
            // Create a clean GameManager instance for each test
            _gameManagerGO = new GameObject("TestGameManager");
        }

        [TearDown]
        public void TearDown()
        {
            if (_gameManagerGO != null)
            {
                Object.DestroyImmediate(_gameManagerGO);
            }

            // Clean up any MainMenuUI instances created during tests
            var existingMenus = Object.FindObjectsByType<MainMenuUI>(FindObjectsSortMode.None);
            foreach (var menu in existingMenus)
            {
                if (menu != null && menu.gameObject != null)
                {
                    Object.DestroyImmediate(menu.gameObject);
                }
            }
        }

        [UnityTest]
        public IEnumerator Awake_CreatesMainMenuUI_WhenMissing()
        {
            // Add GameManager without MainMenuUI
            _gameManager = _gameManagerGO.AddComponent<GameManager>();
            _gameManager.createMainMenuIfMissing = true;

            yield return null; // Wait one frame for Awake to execute

            // Check that MainMenuUI was created
            var mainMenuUI = _gameManagerGO.GetComponent<MainMenuUI>();
            Assert.IsNotNull(mainMenuUI, "MainMenuUI should be created when missing");
        }

        [UnityTest]
        public IEnumerator Awake_DoesNotCreateMenu_WhenAlreadyPresent()
        {
            // Add MainMenuUI first
            var existingMenu = _gameManagerGO.AddComponent<MainMenuUI>();
            
            // Then add GameManager
            _gameManager = _gameManagerGO.AddComponent<GameManager>();

            yield return null;

            // Should use existing, not create new
            var menus = Object.FindObjectsByType<MainMenuUI>(FindObjectsSortMode.None);
            Assert.AreEqual(1, menus.Length, "Should only have one MainMenuUI");
            Assert.AreEqual(existingMenu, menus[0], "Should use the existing MainMenuUI");
        }

        [UnityTest]
        public IEnumerator Awake_DoesNotCreateMenu_WhenFlagDisabled()
        {
            _gameManager = _gameManagerGO.AddComponent<GameManager>();
            _gameManager.createMainMenuIfMissing = false;

            yield return null;

            var mainMenuUI = _gameManagerGO.GetComponent<MainMenuUI>();
            Assert.IsNull(mainMenuUI, "MainMenuUI should not be created when flag is disabled");
        }

        [UnityTest]
        public IEnumerator Awake_LogsInitializationMessage()
        {
            _gameManager = _gameManagerGO.AddComponent<GameManager>();
            
            // Capture log
            string logMessage = "";
            Application.logMessageReceived += (message, stackTrace, type) =>
            {
                if (message.Contains("[GameManager]"))
                {
                    logMessage = message;
                }
            };

            yield return null;

            StringAssert.Contains("Initializing", logMessage, "Should log initialization message");
        }
    }
}
