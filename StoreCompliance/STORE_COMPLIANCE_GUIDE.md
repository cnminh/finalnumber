# App Store & Play Store Compliance Guide

## Overview

This document outlines the store compliance configuration for **Final Number** mobile game.

## Quick Reference

| Requirement | iOS App Store | Google Play Store | Status |
|-------------|---------------|-------------------|--------|
| Min OS Version | iOS 12.0+ | Android 7.0+ (API 24) | ✅ |
| Target SDK | N/A | API 34 (Android 14) | ✅ |
| 64-bit Support | Required (arm64) | Required (arm64-v8a) | ✅ |
| App Bundle | IPA | AAB (Android App Bundle) | ✅ |
| Privacy Manifest | Required (iOS 17+) | Privacy Policy URL | ✅ |
| Bitcode | Disabled | N/A | ✅ |

## Project Structure

```
FinalNumber/
├── Assets/
│   ├── Editor/
│   │   ├── iOSPostProcessBuild.cs      # iOS entitlements & privacy manifest
│   │   ├── AndroidPostProcessBuild.cs  # Play Store validation
│   │   └── BuildPreprocess.cs          # Version sync pre-build
│   ├── Plugins/
│   │   ├── iOS/
│   │   │   └── Entitlements.entitlements # App Sandbox & permissions
│   │   └── Android/
│   │       └── AndroidManifest.xml       # SDK versions & permissions
│   └── Scripts/Compliance/
│       ├── PrivacyComplianceManager.cs   # GDPR/CCPA runtime compliance
│       └── BuildConfig.cs               # Semantic versioning
├── StoreCompliance/
│   ├── PRIVACY_POLICY.md               # Full privacy policy
│   └── TERMS_OF_SERVICE.md             # Full terms of service
└── ProjectSettings/
    └── ProjectSettings.asset           # Unity player settings
```

## iOS App Store Configuration

### Version Requirements
- **Minimum iOS Version**: 12.0 (configured in `PlayerSettings.iOS.targetOSVersionString`)
- **Architecture**: ARM64 (32-bit iOS deprecated by Apple)
- **Bitcode**: Disabled (Apple deprecated Bitcode in Xcode 14)

### Entitlements (`Assets/Plugins/iOS/Entitlements.entitlements`)
- App Sandbox enabled (required)
- Network client access
- User-selected file access (for save data)
- Device battery access

### Privacy Manifest (`PrivacyInfo.xcprivacy`)
Generated automatically by `iOSPostProcessBuild.cs` during build:
- Declares NSUserDefaults usage (game save data)
- Lists tracking domains (empty if no tracking)
- Documents data collection types

### Build Settings
```csharp
PlayerSettings.iOS.targetOSVersionString = "12.0";
PlayerSettings.iOS.buildNumber = config.iOSBuildNumber;
PlayerSettings.bundleVersion = config.iOSBundleVersion;
```

## Google Play Store Configuration

### SDK Requirements
- **Minimum SDK**: API 24 (Android 7.0, Nougat)
- **Target SDK**: API 34 (Android 14) - required for new apps
- **Compile SDK**: API 34

### Android Manifest (`Assets/Plugins/Android/AndroidManifest.xml`)
```xml
<uses-sdk android:minSdkVersion="24" android:targetSdkVersion="34" />
```

### Architecture Support
Required: ARMv7 and ARM64 (configured in Player Settings)
```csharp
PlayerSettings.Android.targetArchitectures = 
    AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
```

### Android App Bundle (AAB)
Required for Play Store. Enabled in:
- `BuildPreprocess.cs` sets `EditorUserBuildSettings.buildAppBundle = true`
- Or manually: Build Settings > Build App Bundle (Google Play)

### Version Code Strategy
Calculated automatically by `BuildConfig.cs`:
```
versionCode = major * 1000000 + minor * 10000 + patch * 100 + build
Example: v1.2.3 build 45 = 1020345
```
This ensures each release has a unique, increasing version code.

## Semantic Versioning

See `Assets/Scripts/Compliance/BuildConfig.cs`

Format: `MAJOR.MINOR.PATCH+build.TYPE`

| Component | Meaning | When to Bump |
|-----------|---------|--------------|
| MAJOR | Breaking changes / major releases | New game modes, save format changes |
| MINOR | New features | New levels, mechanics |
| PATCH | Bug fixes | Hotfixes, patches |
| build | CI/build number | Every build |
| TYPE | dev/beta/release | Build environment |

### Bundle ID Flavors
- **Development**: `com.finalnumber.game.dev`
- **Beta**: `com.finalnumber.game.beta`
- **Release**: `com.finalnumber.game`

## GDPR/CCPA Compliance

### Runtime Implementation
`PrivacyComplianceManager.cs` provides:
- Consent tracking (EU/California detection)
- Privacy policy / ToS display
- Data deletion requests ("Right to be forgotten")
- Data export ("Right to data portability")
- Advertising personalization control

### Required Setup
1. Set `privacyPolicyUrl` in `PrivacyComplianceManager` component
2. Set `termsOfServiceUrl` in `PrivacyComplianceManager` component
3. Implement consent UI flow for first launch
4. Add privacy settings button in game settings

### Data Collection Declaration
Update `collectedDataTypes` list in `PrivacyComplianceManager`:
```csharp
public List<DataCollectionType> collectedDataTypes = new List<DataCollectionType>
{
    DataCollectionType.GameplayContent,  // Save games
    DataCollectionType.UsageData,          // Analytics
    DataCollectionType.Diagnostics         // Crash reports
};
```

## App Store Privacy Labels

When submitting to App Store Connect:

### Data Used to Track You
- [ ] Advertising Data (if using personalized ads)

### Data Linked to You
- [x] Gameplay Content
- [x] Usage Data
- [x] Diagnostics
- [ ] Location (if using)

### Data Not Linked to You
- [x] Diagnostics (crash logs)

## Store Submission Checklist

### Before Building
- [ ] Update `BuildConfig` version numbers
- [ ] Set build type to `Release`
- [ ] Configure bundle ID for release
- [ ] Update privacy policy URLs
- [ ] Validate all required entitlements

### iOS Build
- [ ] Target iOS 12.0+
- [ ] Build for ARM64 only
- [ ] Verify entitlements in Xcode
- [ ] Test on physical device
- [ ] Archive and upload to App Store Connect
- [ ] Complete App Privacy questionnaire

### Android Build
- [ ] Verify target SDK is 34
- [ ] Enable AAB in build settings
- [ ] Verify arm64-v8a in architectures
- [ ] Test on physical devices
- [ ] Upload AAB to Play Console
- [ ] Complete Data Safety form

## Important URLs to Configure

Update these before store submission:

| URL | Location | Purpose |
|-----|----------|---------|
| Privacy Policy | `PrivacyComplianceManager.privacyPolicyUrl` | Legal compliance |
| Terms of Service | `PrivacyComplianceManager.termsOfServiceUrl` | Legal compliance |
| Support | Game settings UI | User assistance |

## Notes

1. **Apple Developer Team ID**: Set in Player Settings before iOS builds
2. **Keystore**: Configure for Android release builds (do not lose!)
3. **AdMob App ID**: Update in `AndroidManifest.xml` if using ads
4. **iCloud**: Enable in Capabilities if using cloud saves

## References

- [App Store Review Guidelines](https://developer.apple.com/app-store/review/guidelines/)
- [Google Play Policy Center](https://play.google.com/about/developer-content-policy/)
- [Android Target API Level Requirements](https://developer.android.com/google/play/requirements/target-sdk)
- [GDPR Compliance for Mobile Apps](https://gdpr.eu/)
