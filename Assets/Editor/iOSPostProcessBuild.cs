using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.iOS.Xcode;
using UnityEngine;

/// <summary>
/// Post-process build script for iOS App Store compliance.
/// Configures iOS entitlements, privacy manifest, and required settings.
/// </summary>
public class iOSPostProcessBuild : IPostprocessBuildWithReport
{
    public int callbackOrder => 100;

    public void OnPostprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.iOS)
            return;

        string path = report.summary.outputPath;
        string projPath = PBXProject.GetPBXProjectPath(path);
        PBXProject proj = new PBXProject();
        proj.ReadFromFile(projPath);

        string targetGuid = proj.GetUnityMainTargetGuid();
        string frameworkTargetGuid = proj.GetUnityFrameworkTargetGuid();

        // Configure minimum iOS version (12.0+)
        proj.SetBuildProperty(targetGuid, "IPHONEOS_DEPLOYMENT_TARGET", "12.0");
        proj.SetBuildProperty(frameworkTargetGuid, "IPHONEOS_DEPLOYMENT_TARGET", "12.0");

        // Disable Bitcode (deprecated, but ensure it's off for compatibility)
        proj.SetBuildProperty(targetGuid, "ENABLE_BITCODE", "NO");
        proj.SetBuildProperty(frameworkTargetGuid, "ENABLE_BITCODE", "NO");

        // Configure app thinning - required for App Store
        proj.SetBuildProperty(targetGuid, "THIN_ASSETS", "YES");

        // Enable app slicing
        proj.SetBuildProperty(targetGuid, "ASSETCATALOG_COMPILER_APPICON_NAME", "AppIcon");

        // Copy entitlements file
        string entitlementsSource = Path.Combine(Application.dataPath, "Plugins/iOS/Entitlements.entitlements");
        string entitlementsDest = Path.Combine(path, "Entitlements.entitlements");
        
        if (File.Exists(entitlementsSource))
        {
            File.Copy(entitlementsSource, entitlementsDest, true);
            proj.SetBuildProperty(targetGuid, "CODE_SIGN_ENTITLEMENTS", "Entitlements.entitlements");
        }

        // Set up privacy manifest (required for iOS 17+ / App Store)
        SetupPrivacyManifest(path, proj, targetGuid);

        proj.WriteToFile(projPath);

        Debug.Log("[iOSPostProcessBuild] App Store compliance settings applied.");
    }

    private void SetupPrivacyManifest(string buildPath, PBXProject proj, string targetGuid)
    {
        // Privacy manifest is required for App Store submissions starting 2024
        string privacyManifestPath = Path.Combine(buildPath, "PrivacyInfo.xcprivacy");
        
        var privacyManifest = new PlistDocument();
        var root = privacyManifest.root;

        // Privacy tracking fields
        root.SetString("NSPrivacyTrackingDomains", "");
        root.SetArray("NSPrivacyCollectedDataTypes");
        root.SetArray("NSPrivacyAccessedAPITypes");

        // Document API usage (if using NSUserDefaults, file timestamp APIs, etc.)
        var apiTypes = root.CreateArray("NSPrivacyAccessedAPITypes");
        
        // Unity uses these APIs internally - declare them
        var userDefaultsEntry = apiTypes.AddDict();
        userDefaultsEntry.SetString("NSPrivacyAccessedAPIType", "NSPrivacyAccessedAPICategoryUserDefaults");
        userDefaultsEntry.SetString("NSPrivacyAccessedAPITypeReasons", "CA92.1"); // Access info from same app

        privacyManifest.WriteToFile(privacyManifestPath);

        // Add to project
        string fileGuid = proj.AddFile("PrivacyInfo.xcprivacy", "PrivacyInfo.xcprivacy");
        proj.AddFileToBuild(targetGuid, fileGuid);
    }
}
