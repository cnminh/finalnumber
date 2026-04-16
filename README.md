# Final Number - Unity Mobile Game

A Unity-based mobile number puzzle game built for iOS App Store and Google Play Store distribution.

## Project Structure

```
FinalNumber/
├── Assets/
│   ├── Editor/           # Build scripts and editor tools
│   ├── Plugins/          # Platform-specific native plugins
│   │   ├── Android/      # Android manifest and config
│   │   └── iOS/          # iOS entitlements
│   ├── Scripts/
│   │   └── Compliance/   # Privacy, GDPR, build configuration
│   └── Resources/        # Game assets
├── Packages/             # Unity package manifest
├── ProjectSettings/      # Unity project configuration
└── StoreCompliance/      # Legal docs and compliance guide
```

## Store Compliance

This project is configured for App Store and Play Store submission:

- **iOS**: iOS 12.0+, ARM64, App Sandbox entitlements, Privacy Manifest
- **Android**: API 24+ min, API 34 target, 64-bit support, AAB (App Bundle)
- **Legal**: Privacy Policy, Terms of Service, GDPR/CCPA compliance framework
- **Versioning**: Semantic versioning with build flavoring (dev/beta/release)

See [Store Compliance Guide](StoreCompliance/STORE_COMPLIANCE_GUIDE.md) for full details.

## CI/CD Status

[![Build](https://github.com/finalnumber/finalnumber/actions/workflows/build.yml/badge.svg)](https://github.com/finalnumber/finalnumber/actions/workflows/build.yml)
[![PR Validation](https://github.com/finalnumber/finalnumber/actions/workflows/pr-validation.yml/badge.svg)](https://github.com/finalnumber/finalnumber/actions/workflows/pr-validation.yml)

## Quick Start

### Prerequisites
- Unity 2022.3 LTS or later
- iOS: Xcode 14+, Apple Developer account
- Android: Android Studio, API 34 SDK

### CI/CD Automated Builds

This project uses GitHub Actions for automated builds:

- **Android**: Automated AAB/APK builds on every PR and push
- **iOS**: Automated Xcode project generation (requires macOS)
- **Release**: Automatic GitHub releases with artifacts for version tags

See [CI/CD Documentation](.github/workflows/README.md) for setup instructions and troubleshooting.

### Configuration

1. **Build Config**: Create via `Assets > Create > Final Number > Build Configuration`
   - Set version numbers (major.minor.patch)
   - Choose build type (Development/Beta/Release)
   - Configure bundle ID

2. **Privacy URLs**: Update in `PrivacyComplianceManager` component
   - Set your actual privacy policy URL
   - Set your actual terms of service URL

3. **iOS**: Add your Apple Developer Team ID in Player Settings

4. **Android**: Configure signing keystore for release builds

### Building

**iOS:**
```
File > Build Settings > iOS > Build
# Open generated Xcode project, archive and upload
```

**Android:**
```
File > Build Settings > Android > Build App Bundle
# Upload AAB to Google Play Console
```

## Scripts Reference

| Script | Purpose |
|--------|---------|
| `BuildConfig.cs` | Semantic versioning, bundle ID management |
| `PrivacyComplianceManager.cs` | GDPR/CCPA consent, data management |
| `BuildPreprocess.cs` | Pre-build validation and version sync |
| `iOSPostProcessBuild.cs` | iOS entitlements, privacy manifest |
| `AndroidPostProcessBuild.cs` | Play Store validation |

## Legal Documentation

- [Privacy Policy](StoreCompliance/PRIVACY_POLICY.md)
- [Terms of Service](StoreCompliance/TERMS_OF_SERVICE.md)
- [Store Compliance Guide](StoreCompliance/STORE_COMPLIANCE_GUIDE.md)

## License

Copyright © 2026 Final Number Studios. All rights reserved.
