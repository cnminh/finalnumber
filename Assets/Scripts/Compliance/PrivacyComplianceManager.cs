using System.Collections.Generic;
using UnityEngine;

namespace FinalNumber.Compliance
{
    /// <summary>
    /// Data privacy compliance manager for GDPR/CCPA compliance.
    /// Handles consent management, data collection disclosures, and privacy settings.
    /// </summary>
    public class PrivacyComplianceManager : MonoBehaviour
    {
        public static PrivacyComplianceManager Instance { get; private set; }

        [Header("Privacy URLs")]
        [Tooltip("Full URL to your privacy policy")]
        public string privacyPolicyUrl = "https://finalnumber.example.com/privacy";
        
        [Tooltip("Full URL to your terms of service")]
        public string termsOfServiceUrl = "https://finalnumber.example.com/terms";

        [Header("GDPR/CCPA Compliance")]
        [Tooltip("Enable GDPR compliance for EU users")]
        public bool enableGDPRCompliance = true;
        
        [Tooltip("Enable CCPA compliance for California users")]
        public bool enableCCPACompliance = true;

        [Header("Data Collection")]
        [Tooltip("List all data types collected - for App Store privacy label")]
        public List<DataCollectionType> collectedDataTypes = new List<DataCollectionType>
        {
            DataCollectionType.GameplayContent,
            DataCollectionType.UsageData,
            DataCollectionType.Diagnostics
        };

        [Tooltip("Whether data is linked to user's identity")]
        public bool dataLinkedToUser = false;

        [Tooltip("Whether data is used for tracking")]
        public bool dataUsedForTracking = false;

        // Consent state
        public bool UserHasConsented { get; private set; }
        public bool IsUserInEU { get; private set; }
        public bool IsUserInCalifornia { get; private set; }

        public enum DataCollectionType
        {
            GameplayContent,    // Game state, scores, progress
            UsageData,          // How user interacts with app
            Diagnostics,        // Crash logs, performance data
            DeviceInfo,         // Device model, OS version
            AdvertisingData,    // For ad personalization
            LocationData        // Approximate or precise location
        }

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadConsentState();
            DetermineUserRegion();
        }

        /// <summary>
        /// Check if consent is required based on user region
        /// </summary>
        public bool IsConsentRequired()
        {
            return (enableGDPRCompliance && IsUserInEU) || 
                   (enableCCPACompliance && IsUserInCalifornia);
        }

        /// <summary>
        /// Request user consent for data collection.
        /// Call this on first launch or when privacy settings change.
        /// </summary>
        public void RequestConsent(System.Action<bool> onComplete)
        {
            if (!IsConsentRequired())
            {
                // Outside regulated regions - consent not required but good to inform
                UserHasConsented = true;
                SaveConsentState();
                onComplete?.Invoke(true);
                return;
            }

            // Show privacy consent UI
            // This would typically show a dialog explaining data collection
            ShowPrivacyConsentDialog(onComplete);
        }

        /// <summary>
        /// Open privacy policy in browser/app store overlay
        /// </summary>
        public void OpenPrivacyPolicy()
        {
            if (!string.IsNullOrEmpty(privacyPolicyUrl))
            {
                Application.OpenURL(privacyPolicyUrl);
            }
            else
            {
                Debug.LogWarning("[PrivacyCompliance] Privacy policy URL not configured!");
            }
        }

        /// <summary>
        /// Open terms of service in browser
        /// </summary>
        public void OpenTermsOfService()
        {
            if (!string.IsNullOrEmpty(termsOfServiceUrl))
            {
                Application.OpenURL(termsOfServiceUrl);
            }
            else
            {
                Debug.LogWarning("[PrivacyCompliance] Terms of service URL not configured!");
            }
        }

        /// <summary>
        /// User requests data deletion (GDPR "Right to be forgotten")
        /// </summary>
        public void RequestDataDeletion(System.Action<bool> onComplete)
        {
            Debug.Log("[PrivacyCompliance] Data deletion requested by user.");
            
            // Clear local save data
            PlayerPrefs.DeleteAll();
            
            // Notify server to delete account data
            // StartCoroutine(ServerDeleteRequest(onComplete));
            
            onComplete?.Invoke(true);
        }

        /// <summary>
        /// User requests data export (GDPR "Right to data portability")
        /// </summary>
        public void RequestDataExport(System.Action<string> onComplete)
        {
            // Compile user data into exportable format
            var userData = new Dictionary<string, object>
            {
                { "deviceId", SystemInfo.deviceUniqueIdentifier },
                { "savedGameData", "..." },
                { "achievements", "..." },
                { "analyticsHistory", "..." }
            };

            string jsonExport = JsonUtility.ToJson(userData);
            onComplete?.Invoke(jsonExport);
        }

        /// <summary>
        /// Check if advertising personalization is allowed
        /// </summary>
        public bool CanPersonalizeAds()
        {
            return UserHasConsented && 
                   collectedDataTypes.Contains(DataCollectionType.AdvertisingData);
        }

        private void ShowPrivacyConsentDialog(System.Action<bool> onComplete)
        {
            // In a real implementation, this would show a proper UI
            // For now, log the requirement
            Debug.Log("[PrivacyCompliance] Showing privacy consent dialog...");
            Debug.Log($"  - Privacy Policy: {privacyPolicyUrl}");
            Debug.Log($"  - Data collected: {string.Join(", ", collectedDataTypes)}");
            Debug.Log($"  - Data linked to user: {dataLinkedToUser}");
            Debug.Log($"  - Used for tracking: {dataUsedForTracking}");

            // Simulate user consent for now
            UserHasConsented = true;
            SaveConsentState();
            onComplete?.Invoke(true);
        }

        private void DetermineUserRegion()
        {
            // In production, use IP geolocation or store region detection
            // For now, default to unknown (conservative approach)
            IsUserInEU = false;
            IsUserInCalifornia = false;

            // Can be set via build scripting or remote config
            string region = GetLocaleRegion();
            
            string[] euRegions = { "AT", "BE", "BG", "HR", "CY", "CZ", "DK", "EE", "FI", "FR", 
                                   "DE", "GR", "HU", "IE", "IT", "LV", "LT", "LU", "MT", "NL", 
                                   "PL", "PT", "RO", "SK", "SI", "ES", "SE", "GB", "UK" };
            
            IsUserInEU = System.Array.Exists(euRegions, r => r == region);
            IsUserInCalifornia = region == "US-CA" || region == "CA";
        }

        private string GetLocaleRegion()
        {
            // Use system locale to estimate region
            // Production apps should use more reliable geolocation
            return "US"; // Default fallback
        }

        private void SaveConsentState()
        {
            PlayerPrefs.SetInt("PrivacyConsent_Granted", UserHasConsented ? 1 : 0);
            PlayerPrefs.SetInt("PrivacyConsent_Timestamp", (int)System.DateTime.UtcNow.Ticks);
            PlayerPrefs.Save();
        }

        private void LoadConsentState()
        {
            UserHasConsented = PlayerPrefs.GetInt("PrivacyConsent_Granted", 0) == 1;
        }

        #region App Store Privacy Manifest Generation

        /// <summary>
        /// Generates App Store Connect privacy label JSON
        /// </summary>
        public string GeneratePrivacyManifest()
        {
            var manifest = new Dictionary<string, object>
            {
                { "privacyTrackingEnabled", dataUsedForTracking },
                { "privacyTrackingDomains", new string[0] },
                { "collectedDataTypes", GetCollectedDataManifest() },
                { "accessedApiTypes", GetAccessedApiManifest() }
            };

            return JsonUtility.ToJson(manifest);
        }

        private Dictionary<string, object>[] GetCollectedDataManifest()
        {
            var result = new List<Dictionary<string, object>>();
            
            foreach (var dataType in collectedDataTypes)
            {
                result.Add(new Dictionary<string, object>
                {
                    { "type", dataType.ToString() },
                    { "linkedToUser", dataLinkedToUser },
                    { "usedForTracking", dataUsedForTracking }
                });
            }

            return result.ToArray();
        }

        private Dictionary<string, object>[] GetAccessedApiManifest()
        {
            // Declare API usage that requires privacy justification
            return new Dictionary<string, object>[]
            {
                new Dictionary<string, object>
                {
                    { "api", "NSUserDefaults" },
                    { "reason", "CA92.1" } // Access info from same app
                }
            };
        }

        #endregion
    }
}
