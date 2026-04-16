# Analytics, Crash Reporting, and Telemetry Documentation

## Overview

This document describes the analytics, crash reporting, and telemetry systems implemented for **Final Number**.

## Systems Implemented

### 1. Analytics Manager (`Analytics/AnalyticsManager.cs`)

Unity Analytics integration for tracking player behavior and game events.

#### Key Features
- **Session Tracking**: Automatic session start/end with engagement detection
- **Retention Tracking**: D1, D7, D30 retention milestones
- **Level Progression**: Track level starts, completions, failures
- **Power-up Usage**: Track which power-ups are used and when
- **Achievement Tracking**: Achievement unlock events
- **Purchase Tracking**: IAP transaction logging
- **Ad Impression Tracking**: Banner/interstitial/rewarded ad metrics
- **Tutorial Funnel**: Tutorial step completion tracking

#### Key Events Tracked
```
session_start, session_end
level_start, level_complete, level_fail
world_unlock, power_up_used, achievement_unlocked
retention_d1, retention_d7, retention_d30
purchase, ad_impression
```

#### Usage
```csharp
// Track custom event
AnalyticsManager.Instance.TrackEvent("custom_event", new Dictionary<string, object>
{
    { "param1", value1 },
    { "param2", value2 }
});

// Track purchase
AnalyticsManager.Instance.TrackPurchase("power_up_pack", 1.99m, "USD");
```

---

### 2. Crash Reporter (`CrashReporting/CrashReporter.cs`)

Automated crash detection and reporting with breadcrumb context.

#### Key Features
- **Exception Handling**: Captures unhandled exceptions
- **Breadcrumbs**: Context trail leading to crash
- **Device Info**: Automatic device and OS information capture
- **Log Capture**: Recent log lines included with crash reports
- **Local Persistence**: Crash reports saved locally for retry
- **Multiple Backends**: Supports Unity Cloud Diagnostics and Firebase Crashlytics

#### Breadcrumb Types
```csharp
LogBreadcrumb("category", "message");           // General context
LogUserAction("purchase", "product_id");        // User actions
LogNavigation("MainMenu", "LevelSelect");       // Screen navigation
LogApiCall("/api/leaderboard", true, 200);       // API calls
```

#### Testing Crash Reporting
```csharp
// From debug menu or test scene
CrashReporter.Instance.TestCrashReporting();
```

---

### 3. Performance Monitor (`Performance/PerformanceMonitor.cs`)

Real-time performance tracking for FPS, memory, and load times.

#### Key Features
- **FPS Tracking**: Current, average, min, max FPS
- **FPS Drop Detection**: Automatic spike detection and analytics
- **Memory Tracking**: Current and peak memory usage
- **Scene Load Times**: Track loading performance
- **Thermal State**: iOS device temperature monitoring
- **Battery Level**: Battery state during gameplay

#### Performance Metrics
```csharp
// Get current stats
float fps = PerformanceMonitor.Instance.GetCurrentFPS();
long memoryMB = PerformanceMonitor.Instance.GetCurrentMemoryMB();
var report = PerformanceMonitor.Instance.GetPerformanceReport();
```

#### Configuration
```csharp
// In inspector or via script
monitoringEnabled = true;
targetFrameRate = 60;
fpsWarningThreshold = 30f;
memoryWarningThresholdMB = 512;
captureInterval = 5f;
```

---

### 4. Privacy Settings UI (`UI/PrivacySettingsUI.cs`)

User-facing privacy controls for GDPR/CCPA compliance.

#### Features
- **Analytics Opt-out Toggle**: Enable/disable analytics collection
- **Crash Reporting Toggle**: Enable/disable crash reporting
- **Privacy Policy Link**: Opens privacy policy in browser
- **Data Export**: User-requested data export (GDPR right to portability)
- **Data Deletion**: User-requested data deletion (GDPR right to be forgotten)

#### Integration
Attach to a settings panel GameObject with UI Toggle and Button references.

---

## Privacy Compliance

### Data Collection Disclosure

The following data types are collected:

| Data Type | Purpose | Linked to User | Used for Tracking |
|-----------|---------|----------------|-------------------|
| Gameplay Content | Track progress, levels completed | No | No |
| Usage Data | Session length, feature usage | No | No |
| Diagnostics | Crash logs, performance data | No | No |
| Device Info | Device model, OS version | No | No |

### User Rights (GDPR/CCPA)

1. **Right to Access**: Users can request their data via the settings UI
2. **Right to Deletion**: Users can request data deletion via settings UI
3. **Right to Opt-out**: Users can disable analytics at any time
4. **Transparency**: Privacy policy and data collection info accessible in-game

### COPPA Compliance

- No advertising identifiers collected for children
- No location data collected
- No behavioral advertising
- Parental consent flow for users under 13

---

## Setup Instructions

### 1. Unity Analytics Setup

1. Open **Window > Services**
2. Sign in to Unity Cloud
3. Enable **Analytics**
4. Copy your Project ID to Project Settings

### 2. Firebase Crashlytics Setup (Optional but Recommended)

1. Download [Firebase Unity SDK](https://firebase.google.com/download/unity)
2. Import `FirebaseAnalytics.unitypackage`
3. Import `FirebaseCrashlytics.unitypackage`
4. Download `google-services.json` (Android) and `GoogleService-Info.plist` (iOS)
5. Place in `Assets/` folder
6. Uncomment Firebase code in `CrashReporter.cs`

### 3. iOS Privacy Manifest

For iOS 17+, create `PrivacyInfo.xcprivacy`:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" ...>
<plist version="1.0">
<dict>
    <key>NSPrivacyTrackingDomains</key>
    <array/>
    <key>NSPrivacyCollectedDataTypes</key>
    <array>
        <dict>
            <key>NSPrivacyCollectedDataType</key>
            <string>NSPrivacyCollectedDataTypeGameplayContent</string>
            <key>NSPrivacyCollectedDataTypeLinked</key>
            <false/>
            <key>NSPrivacyCollectedDataTypeTracking</key>
            <false/>
            <key>NSPrivacyCollectedDataTypePurposes</key>
            <array>
                <string>NSPrivacyCollectedDataTypePurposeAnalytics</string>
            </array>
        </dict>
    </array>
</dict>
</plist>
```

---

## Dashboard Access

### Unity Analytics Dashboard
- URL: https://dashboard.unity3d.com/analytics
- View: Retention, Session Length, Level Progression

### Firebase Console
- URL: https://console.firebase.google.com
- View: Crash-free users, Performance traces, Custom events

### Crash Reports (Local)
- Location: `Application.persistentDataPath/CrashReports/`
- Format: JSON files with device info and stack traces

---

## Testing Checklist

- [x] Analytics events fire correctly
- [x] Session tracking works (start/end)
- [x] Level progression events tracked
- [x] Crash reporting captures exceptions
- [x] Performance monitor tracks FPS
- [x] Privacy settings UI functional
- [x] Opt-out disables analytics
- [x] Data export returns valid JSON
- [x] Local crash reports saved correctly

---

## Implementation Notes

### Event Batching
Analytics events are batched for better performance. Default batch interval is 30 seconds or 10 events.

### Memory Efficiency
- Breadcrumbs limited to 50 entries
- Recent logs limited to 100 lines
- FPS samples limited to 60 windows

### Battery Considerations
- Performance monitoring has minimal overhead
- Analytics batching reduces network requests
- Crash reporting only activates on exceptions

---

## Support

For questions about analytics implementation, contact the engineering team.
For Unity Analytics issues: https://support.unity.com
For Firebase issues: https://firebase.google.com/support
