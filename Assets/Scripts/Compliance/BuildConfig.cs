using System;
using UnityEngine;

namespace FinalNumber.Build
{
    /// <summary>
    /// Semantic versioning and build configuration for store releases.
    /// Supports iOS (CFBundleVersion/CFBundleShortVersionString) and 
    /// Android (versionCode/versionName) requirements.
    /// </summary>
    [CreateAssetMenu(fileName = "BuildConfig", menuName = "Final Number/Build Configuration")]
    public class BuildConfig : ScriptableObject
    {
        [Header("Semantic Version")]
        [Tooltip("Major version - increments for breaking changes/major releases")]
        [Range(0, 99)]
        public int major = 1;
        
        [Tooltip("Minor version - increments for new features")]
        [Range(0, 99)]
        public int minor = 0;
        
        [Tooltip("Patch version - increments for bug fixes")]
        [Range(0, 99)]
        public int patch = 0;

        [Header("Build Metadata")]
        [Tooltip("Build number - auto-increments with each build")]
        public int buildNumber = 1;
        
        [Tooltip("Build type affects versioning and bundle ID")]
        public BuildType buildType = BuildType.Development;

        [Header("Bundle Identification")]
        [Tooltip("Base bundle identifier (e.g., com.finalnumber.game)")]
        public string baseBundleId = "com.finalnumber.game";

        [Header("Store Targets")]
        [Tooltip("Target iOS version (12.0 minimum for App Store)")]
        public string iOSTargetVersion = "12.0";
        
        [Tooltip("Minimum Android SDK (API 24 = Android 7.0)")]
        public int androidMinSdk = 24;
        
        [Tooltip("Target Android SDK (API 34 = Android 14)")]
        public int androidTargetSdk = 34;

        public enum BuildType
        {
            Development,
            Beta,
            Release
        }

        /// <summary>
        /// Semantic version string (e.g., "1.0.0")
        /// </summary>
        public string SemanticVersion => $"{major}.{minor}.{patch}";

        /// <summary>
        /// Full version string with build metadata (e.g., "1.0.0+123.dev")
        /// </summary>
        public string FullVersion => $"{SemanticVersion}+{buildNumber}.{buildType.ToString().ToLower()}";

        /// <summary>
        /// iOS/macOS bundle version (CFBundleShortVersionString) - uses semantic version
        /// </summary>
        public string iOSBundleVersion => SemanticVersion;

        /// <summary>
        /// iOS/macOS build number (CFBundleVersion) - must increment for each upload
        /// </summary>
        public string iOSBuildNumber => buildNumber.ToString();

        /// <summary>
        /// Android version name (human readable)
        /// </summary>
        public string AndroidVersionName => FullVersion;

        /// <summary>
        /// Android version code (must increment for each release)
        /// Calculated as: major * 1000000 + minor * 10000 + patch * 100 + build
        /// Supports up to version 99.99.99 with 99 builds per patch
        /// </summary>
        public int AndroidVersionCode => major * 1000000 + minor * 10000 + patch * 100 + (buildNumber % 100);

        /// <summary>
        /// Bundle ID with build type suffix (e.g., com.finalnumber.game.dev)
        /// </summary>
        public string GetBundleId()
        {
            string suffix = buildType switch
            {
                BuildType.Development => ".dev",
                BuildType.Beta => ".beta",
                BuildType.Release => "",
                _ => ""
            };
            return baseBundleId + suffix;
        }

        /// <summary>
        /// Product name with build type indicator
        /// </summary>
        public string GetProductName(string baseName = "Final Number")
        {
            return buildType switch
            {
                BuildType.Development => $"{baseName} [Dev]",
                BuildType.Beta => $"{baseName} [Beta]",
                BuildType.Release => baseName,
                _ => baseName
            };
        }

        /// <summary>
        /// Increment build number after a successful build
        /// </summary>
        public void IncrementBuildNumber()
        {
            buildNumber++;
        }

        /// <summary>
        /// Bump version components
        /// </summary>
        public void BumpPatch() { patch++; buildNumber = 1; }
        public void BumpMinor() { minor++; patch = 0; buildNumber = 1; }
        public void BumpMajor() { major++; minor = 0; patch = 0; buildNumber = 1; }

        /// <summary>
        /// Validate version against store requirements
        /// </summary>
        public bool IsValidForStore()
        {
            if (buildType == BuildType.Development)
            {
                Debug.LogWarning("[BuildConfig] Development builds should not be submitted to stores!");
                return false;
            }

            if (major == 0 && minor == 0 && patch == 0)
            {
                Debug.LogError("[BuildConfig] Version cannot be 0.0.0!");
                return false;
            }

            if (string.IsNullOrEmpty(baseBundleId) || !baseBundleId.Contains("."))
            {
                Debug.LogError("[BuildConfig] Invalid bundle ID format!");
                return false;
            }

            return true;
        }
    }
}
