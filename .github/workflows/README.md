# CI/CD Pipeline Documentation

This document describes the GitHub Actions-based CI/CD pipeline for the Final Number Unity mobile game.

## Overview

The pipeline automates building, testing, and releasing the game for iOS and Android platforms.

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| `build.yml` | Push, PR, tags, manual | Main build pipeline for Android & iOS |
| `pr-validation.yml` | Pull requests | Validate code quality and build health |
| `version-bump.yml` | Manual | Automate semantic versioning |

---

## Quick Start

### 1. Required Secrets

Configure these in **Settings > Secrets and variables > Actions**:

| Secret | Description | How to Obtain |
|--------|-------------|---------------|
| `UNITY_LICENSE` | Unity license file content | [game-ci documentation](https://game.ci/docs/github/activation) |
| `UNITY_EMAIL` | Unity account email | Your Unity ID email |
| `UNITY_PASSWORD` | Unity account password | Your Unity ID password |
| `DISCORD_WEBHOOK_URL` | Discord notification channel | Discord Server Settings > Integrations |

### 2. Unity License Activation

Follow the [game-ci activation guide](https://game.ci/docs/github/activation) to obtain your license file.

For **Plus/Pro licenses**: Use the `.ulf` file content  
For **Personal licenses**: Use the activation file from the manual activation process

### 3. Verify Setup

1. Push any change to `main` or `develop`
2. Check **Actions** tab for running workflows
3. Builds should appear as artifacts

---

## Workflow Details

### Build Pipeline (`build.yml`)

**Triggers:**
- Push to `main` or `develop`
- Pull requests targeting `main`
- Tags starting with `v` (e.g., `v1.0.0`)
- Manual dispatch via Actions tab

**Jobs:**

| Job | Runner | Output | Notes |
|-----|--------|--------|-------|
| `build-android` | Ubuntu | `.aab` (Play Store), `.apk` (testing) | AAB required for Play Store |
| `build-ios` | macOS | Xcode project folder | Manual archiving required |
| `release` | Ubuntu | GitHub Release with artifacts | Only runs on version tags |
| `notify` | Ubuntu | Discord notification | Summarizes build status |

**Manual Build Parameters:**
- **Build Target**: Android, iOS, or Both
- **Build Type**: Development, Beta, or Release

### PR Validation (`pr-validation.yml`)

**Checks performed:**
- ✅ Unity project builds successfully (Linux target for speed)
- ✅ C# files don't contain tabs (spaces only)
- ✅ No files exceed 100MB (GitHub limit)
- ✅ Required directories exist
- ⚠️ Warning for missing `.meta` files

### Version Bumping (`version-bump.yml`)

**Manual version management:**
- Choose bump type: `patch` (bug fixes), `minor` (features), `major` (breaking)
- Creates annotated git tag automatically
- Triggers build workflow on new tag

---

## Build Outputs

### Android

| File | Purpose | Store Submission |
|------|---------|------------------|
| `.aab` | Android App Bundle | ✅ Upload to Google Play Console |
| `.apk` | Installable APK | ❌ For internal testing only |

### iOS

| Output | Purpose | Store Submission |
|--------|---------|------------------|
| Xcode Project | iOS app source | ⚠️ Requires manual archiving |

**iOS Build Steps (Manual):**
1. Download Xcode project artifact
2. Open `Unity-iPhone.xcodeproj` in Xcode
3. Set signing team and bundle ID
4. Product > Archive
5. Distribute App > App Store Connect

---

## Versioning Strategy

The project uses **Semantic Versioning** (`MAJOR.MINOR.PATCH`):

```
v1.0.0    # Initial release
v1.0.1    # Patch: Bug fixes
v1.1.0    # Minor: New features
v2.0.0    # Major: Breaking changes
```

### Version Components

| Component | Description | Store Mapping |
|-----------|-------------|---------------|
| `major` | Breaking changes | Major version |
| `minor` | New features | Minor version |
| `patch` | Bug fixes | Patch version |
| `buildNumber` | CI run number | Android: versionCode, iOS: CFBundleVersion |

### Build Types

| Type | Suffix | Bundle ID | Use Case |
|------|--------|-----------|----------|
| `Development` | `[Dev]` | `.dev` | Internal testing |
| `Beta` | `[Beta]` | `.beta` | Beta testing (TestFlight/Internal Testing) |
| `Release` | none | base | Production release |

---

## Store Submission Workflow

### Google Play Store (Android)

1. **Create release tag:**
   ```bash
   git tag -a v1.0.0 -m "Initial release"
   git push origin v1.0.0
   ```

2. **Wait for CI:** Build workflow completes, creates GitHub Release

3. **Download AAB** from release artifacts

4. **Upload to Play Console:**
   - Open [Google Play Console](https://play.google.com/console)
   - Select your app
   - Production > Create new release
   - Upload AAB file
   - Fill release notes
   - Submit for review

### App Store (iOS)

1. **Create release tag** (same as Android)

2. **Wait for CI:** iOS build completes

3. **Download Xcode project** artifact

4. **Archive and upload:**
   ```bash
   # Extract and open
   unzip FinalNumber-iOS-*.zip
   open build/iOS/Unity-iPhone.xcodeproj
   ```

5. **In Xcode:**
   - Set signing team
   - Product > Archive
   - Distribute App > App Store Connect

---

## Troubleshooting

### Build Failures

| Issue | Solution |
|-------|----------|
| "License activation failed" | Re-activate Unity license (see secrets section) |
| "Build target not supported" | Ensure `CommandLineBuild.cs` is in `Assets/Editor` |
| iOS build fails on Ubuntu | iOS requires macOS runner (configured correctly) |
| Library cache issues | Delete cache in Actions > Caches |

### Common Errors

**"No enabled scenes found"**
- Add at least one scene to File > Build Settings > Scenes in Build

**"Bundle ID invalid"**
- Check `BuildConfig.asset` has valid `baseBundleId` format (`com.company.app`)

**"Android build fails with IL2CPP"**
- IL2CPP requires Android NDK. Ensure Unity is installed with Android Build Support + IL2CPP

---

## Customization

### Adding More Platforms

Edit `.github/workflows/build.yml`:

```yaml
build-webgl:
  runs-on: ubuntu-latest
  steps:
    - uses: game-ci/unity-builder@v4
      with:
        targetPlatform: WebGL
```

### Adding Tests

Add test job to `pr-validation.yml`:

```yaml
test:
  runs-on: ubuntu-latest
  steps:
    - uses: game-ci/unity-test-runner@v4
      with:
        testMode: all
```

### Custom Build Script

Modify `Assets/Editor/CommandLineBuild.cs`:

```csharp
public static void BuildCustom()
{
    // Your custom build logic
}
```

---

## Security Notes

- ⚠️ **Never commit** keystore files or signing credentials
- ✅ Use GitHub Secrets for all sensitive data
- ✅ Unity license is encrypted by GitHub
- ⚠️ Personal Unity licenses may need periodic reactivation

---

## Related Documentation

- [game-ci Unity Builder](https://game.ci/docs/github/builder)
- [Unity Command Line Arguments](https://docs.unity3d.com/Manual/CommandLineArguments.html)
- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [STORE_COMPLIANCE_GUIDE.md](./StoreCompliance/STORE_COMPLIANCE_GUIDE.md)

---

## Maintenance

| Task | Frequency | Owner |
|------|-----------|-------|
| Update Unity version in workflows | Quarterly | Tech Lead |
| Refresh Unity license | Annually | DevOps |
| Review build artifacts retention | Monthly | Tech Lead |
| Update runner versions | As needed | DevOps |

---

*Last updated: April 2026*
