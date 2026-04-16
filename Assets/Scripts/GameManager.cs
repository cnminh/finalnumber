using UnityEngine;
using FinalNumber.UI;

namespace FinalNumber
{
    /// <summary>
    /// GameManager - Main entry point for game initialization.
    /// Ensures the MainMenuUI is properly set up at runtime.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("UI Setup")]
        [Tooltip("Whether to create the MainMenuUI component if not present")]
        public bool createMainMenuIfMissing = true;

        private void Awake()
        {
            Debug.Log("[GameManager] Initializing...");

            // Ensure we have a MainMenuUI component
            SetupMainMenuUI();

            Debug.Log("[GameManager] Initialization complete.");
        }

        private void SetupMainMenuUI()
        {
            // Check if MainMenuUI already exists on this GameObject
            MainMenuUI existingMenu = GetComponent<MainMenuUI>();
            if (existingMenu != null)
            {
                Debug.Log("[GameManager] MainMenuUI already attached.");
                return;
            }

            // Check if MainMenuUI exists anywhere in the scene
            #if UNITY_2022_1_OR_NEWER
            existingMenu = Object.FindFirstObjectByType<MainMenuUI>();
            #else
            existingMenu = Object.FindObjectOfType<MainMenuUI>();
            #endif
            if (existingMenu != null)
            {
                Debug.Log("[GameManager] MainMenuUI found on another GameObject.");
                return;
            }

            if (createMainMenuIfMissing)
            {
                Debug.Log("[GameManager] Creating MainMenuUI component...");
                gameObject.AddComponent<MainMenuUI>();
                Debug.Log("[GameManager] MainMenuUI component added successfully.");
            }
            else
            {
                Debug.LogWarning("[GameManager] MainMenuUI is missing and auto-creation is disabled!");
            }
        }
    }
}
