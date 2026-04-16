using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FinalNumber.UI
{
    /// <summary>
    /// Privacy and analytics settings UI for the settings menu.
    /// Allows users to opt-out of analytics and view privacy info.
    /// </summary>
    public class PrivacySettingsUI : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Toggle for analytics opt-out")]
        public Toggle analyticsToggle;

        [Tooltip("Toggle for crash reporting opt-out")]
        public Toggle crashReportingToggle;

        [Tooltip("Button to view privacy policy")]
        public Button privacyPolicyButton;

        [Tooltip("Button to view terms of service")]
        public Button termsOfServiceButton;

        [Tooltip("Button to request data export")]
        public Button exportDataButton;

        [Tooltip("Button to request data deletion")]
        public Button deleteDataButton;

        [Header("Text Elements")]
        [Tooltip("Text showing current privacy status")]
        public TextMeshProUGUI privacyStatusText;

        [Tooltip("Text showing data collection summary")]
        public TextMeshProUGUI dataCollectionText;

        [Header("Confirmation Dialog")]
        [Tooltip("Confirmation dialog panel")]
        public GameObject confirmationDialog;

        [Tooltip("Confirmation dialog text")]
        public TextMeshProUGUI confirmationText;

        [Tooltip("Confirm button")]
        public Button confirmButton;

        [Tooltip("Cancel button")]
        public Button cancelButton;

        private System.Action<bool> pendingConfirmationCallback;

        private void Start()
        {
            InitializeUI();
            SubscribeToEvents();
            UpdatePrivacyDisplay();
        }

        private void InitializeUI()
        {
            // Set toggle initial states (inverted because toggle represents "enabled")
            bool analyticsEnabled = !IsAnalyticsOptedOut();
            bool crashEnabled = !IsCrashReportingOptedOut();

            if (analyticsToggle != null)
            {
                analyticsToggle.isOn = analyticsEnabled;
                analyticsToggle.onValueChanged.AddListener(OnAnalyticsToggleChanged);
            }

            if (crashReportingToggle != null)
            {
                crashReportingToggle.isOn = crashEnabled;
                crashReportingToggle.onValueChanged.AddListener(OnCrashToggleChanged);
            }

            if (privacyPolicyButton != null)
                privacyPolicyButton.onClick.AddListener(OpenPrivacyPolicy);

            if (termsOfServiceButton != null)
                termsOfServiceButton.onClick.AddListener(OpenTermsOfService);

            if (exportDataButton != null)
                exportDataButton.onClick.AddListener(RequestDataExport);

            if (deleteDataButton != null)
                deleteDataButton.onClick.AddListener(RequestDataDeletion);

            // Confirmation dialog
            if (confirmationDialog != null)
                confirmationDialog.SetActive(false);

            if (confirmButton != null)
                confirmButton.onClick.AddListener(OnConfirmAction);

            if (cancelButton != null)
                cancelButton.onClick.AddListener(OnCancelAction);
        }

        private void SubscribeToEvents()
        {
            // Listen for privacy manager events
            if (Compliance.PrivacyComplianceManager.Instance != null)
            {
                // Subscribe to any future privacy events
            }
        }

        private void UpdatePrivacyDisplay()
        {
            if (privacyStatusText != null)
            {
                bool analyticsOptedOut = IsAnalyticsOptedOut();
                bool crashOptedOut = IsCrashReportingOptedOut();

                if (analyticsOptedOut && crashOptedOut)
                {
                    privacyStatusText.text = "Privacy Mode: <color=#4CAF50>Maximum</color>\nAll tracking disabled";
                }
                else if (analyticsOptedOut || crashOptedOut)
                {
                    privacyStatusText.text = "Privacy Mode: <color=#FFC107>Partial</color>\nSome tracking disabled";
                }
                else
                {
                    privacyStatusText.text = "Privacy Mode: <color=#2196F3>Standard</color>\nAnalytics and crash reporting enabled for app improvement";
                }
            }

            if (dataCollectionText != null)
            {
                var dataTypes = Compliance.PrivacyComplianceManager.Instance?.collectedDataTypes;
                if (dataTypes != null && dataTypes.Count > 0)
                {
                    dataCollectionText.text = $"Data collected:\n• {string.Join("\n• ", dataTypes)}\n\nThis data helps improve the game and fix bugs.";
                }
                else
                {
                    dataCollectionText.text = "No personal data is collected. Basic analytics help us improve the game.";
                }
            }
        }

        #region UI Event Handlers

        private void OnAnalyticsToggleChanged(bool enabled)
        {
            if (enabled)
            {
                // User opted IN
                ShowConfirmation(
                    "Enable Analytics?\n\nThis helps us understand how players enjoy the game and where we can improve. Data is anonymized.",
                    (confirmed) =>
                    {
                        if (confirmed)
                        {
                            Analytics.AnalyticsManager.Instance?.OptIn();
                            UpdatePrivacyDisplay();
                        }
                        else
                        {
                            analyticsToggle.isOn = false;
                        }
                    }
                );
            }
            else
            {
                // User opted OUT
                ShowConfirmation(
                    "Disable Analytics?\n\nWe won't be able to track game performance or understand how to improve the experience.",
                    (confirmed) =>
                    {
                        if (confirmed)
                        {
                            Analytics.AnalyticsManager.Instance?.OptOut();
                            UpdatePrivacyDisplay();
                        }
                        else
                        {
                            analyticsToggle.isOn = true;
                        }
                    }
                );
            }
        }

        private void OnCrashToggleChanged(bool enabled)
        {
            if (enabled)
            {
                // User enabled crash reporting
                CrashReporting.CrashReporter.Instance?.SetEnabled(true);
                PlayerPrefs.SetInt("CrashReportingOptOut", 0);
            }
            else
            {
                // User disabled crash reporting
                ShowConfirmation(
                    "Disable Crash Reporting?\n\nWe won't receive automatic crash reports, making it harder to fix stability issues.",
                    (confirmed) =>
                    {
                        if (confirmed)
                        {
                            CrashReporting.CrashReporter.Instance?.SetEnabled(false);
                            PlayerPrefs.SetInt("CrashReportingOptOut", 1);
                            UpdatePrivacyDisplay();
                        }
                        else
                        {
                            crashReportingToggle.isOn = true;
                        }
                    }
                );
            }
        }

        private void OpenPrivacyPolicy()
        {
            Compliance.PrivacyComplianceManager.Instance?.OpenPrivacyPolicy();
            Analytics.AnalyticsManager.Instance?.TrackEvent("privacy_policy_viewed");
        }

        private void OpenTermsOfService()
        {
            Compliance.PrivacyComplianceManager.Instance?.OpenTermsOfService();
            Analytics.AnalyticsManager.Instance?.TrackEvent("terms_of_service_viewed");
        }

        private void RequestDataExport()
        {
            ShowConfirmation(
                "Request Data Export?\n\nWe'll compile all data associated with your device and prepare it for download. This may take a few minutes.",
                (confirmed) =>
                {
                    if (confirmed)
                    {
                        PerformDataExport();
                    }
                }
            );
        }

        private void RequestDataDeletion()
        {
            ShowConfirmation(
                "Request Data Deletion?\n\n<color=#F44336>WARNING: This cannot be undone.</color>\n\nAll your game progress, achievements, and account data will be permanently deleted.",
                (confirmed) =>
                {
                    if (confirmed)
                    {
                        PerformDataDeletion();
                    }
                }
            );
        }

        #endregion

        #region Actions

        private void PerformDataExport()
        {
            Compliance.PrivacyComplianceManager.Instance?.RequestDataExport((jsonData) =>
            {
                // Show success message or initiate download
                ShowMessage("Data Export Ready", "Your data export has been prepared. Check your email or downloads folder.");
                
                Analytics.AnalyticsManager.Instance?.TrackEvent("data_export_requested");
            });
        }

        private void PerformDataDeletion()
        {
            Compliance.PrivacyComplianceManager.Instance?.RequestDataDeletion((success) =>
            {
                if (success)
                {
                    ShowMessage("Data Deleted", "Your data has been deleted. The app will now restart.");
                    Analytics.AnalyticsManager.Instance?.TrackEvent("data_deletion_requested");
                    
                    // Clear all local data
                    PlayerPrefs.DeleteAll();
                    
                    // Restart app
                    Invoke(nameof(RestartApp), 2f);
                }
                else
                {
                    ShowMessage("Error", "Failed to delete data. Please try again or contact support.");
                }
            });
        }

        private void RestartApp()
        {
            #if UNITY_2023_1_OR_NEWER
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
            #else
            // Use older method
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            UnityEngine.SceneManagement.SceneManager.LoadScene(scene.buildIndex);
            #endif
        }

        #endregion

        #region Helpers

        private void ShowConfirmation(string message, System.Action<bool> callback)
        {
            pendingConfirmationCallback = callback;

            if (confirmationDialog != null)
            {
                confirmationDialog.SetActive(true);

                if (confirmationText != null)
                    confirmationText.text = message;
            }
            else
            {
                // Fallback if no dialog configured
                callback?.Invoke(true);
            }
        }

        private void OnConfirmAction()
        {
            confirmationDialog?.SetActive(false);
            pendingConfirmationCallback?.Invoke(true);
            pendingConfirmationCallback = null;
        }

        private void OnCancelAction()
        {
            confirmationDialog?.SetActive(false);
            pendingConfirmationCallback?.Invoke(false);
            pendingConfirmationCallback = null;
        }

        private void ShowMessage(string title, string message)
        {
            ShowConfirmation($"{title}\n\n{message}\n\n[Click Confirm to close]", null);
        }

        private bool IsAnalyticsOptedOut()
        {
            return PlayerPrefs.GetInt("AnalyticsOptOut", 0) == 1;
        }

        private bool IsCrashReportingOptedOut()
        {
            return PlayerPrefs.GetInt("CrashReportingOptOut", 0) == 1;
        }

        #endregion
    }
}
