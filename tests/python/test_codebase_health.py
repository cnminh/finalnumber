#!/usr/bin/env python3
"""
Test suite for FinalNumber Unity game codebase.
This script analyzes C# code files for common issues and patterns.

Usage: python3 test_codebase_health.py
"""

import re
import sys
import os


def analyze_file(filepath, content):
    """Analyze a single C# file for common issues."""
    issues = []
    checks_passed = []
    filename = os.path.basename(filepath)

    # Check 1: Namespace declaration
    if "namespace " in content:
        checks_passed.append(f"✅ {filename}: Has namespace declaration")
    else:
        issues.append(f"⚠️ {filename}: Missing namespace declaration")

    # Check 2: Class documentation comments
    if "/// <summary>" in content or "/**" in content:
        checks_passed.append(f"✅ {filename}: Has documentation comments")
    else:
        issues.append(f"⚠️ {filename}: Missing documentation comments")

    # Check 3: Unity lifecycle methods that unsubscribe from events
    if "OnEnable" in content or "OnDisable" in content or "OnDestroy" in content:
        if "OnDisable" in content and ("-=" in content or "Unsubscribe" in content):
            checks_passed.append(f"✅ {filename}: Properly unsubscribes from events")
        elif "OnEnable" in content:
            issues.append(
                f"⚠️ {filename}: Has OnEnable but may not unsubscribe in OnDisable"
            )

    # Check 4: Singleton pattern with null check
    if "Instance" in content and "Instance != null" in content:
        checks_passed.append(f"✅ {filename}: Singleton has null check")
    elif "Instance" in content:
        issues.append(f"⚠️ {filename}: Singleton pattern without null check")

    # Check 5: Debug.LogError for critical failures
    if "Debug.LogError" in content:
        checks_passed.append(f"✅ {filename}: Has error logging")
    else:
        issues.append(f"ℹ️ {filename}: No error logging (may be OK)")

    # Check 6: Try-catch blocks for file I/O or risky operations
    risky_patterns = [
        "File.",
        "File.Write",
        "File.Read",
        "PlayerPrefs.Save",
        "PlayerPrefs.Delete",
    ]
    has_risky = any(p in content for p in risky_patterns)
    has_try_catch = "try" in content and "catch" in content
    if has_risky and has_try_catch:
        checks_passed.append(f"✅ {filename}: Risky operations wrapped in try-catch")
    elif has_risky:
        issues.append(f"❌ {filename}: Risky operations without try-catch protection")

    # Check 7: Null checks before accessing Unity components
    null_checks = content.count("!= null")
    if null_checks >= 3:
        checks_passed.append(f"✅ {filename}: Has {null_checks} null checks")
    elif null_checks < 2:
        issues.append(f"⚠️ {filename}: Only {null_checks} null checks (may need more)")

    return issues, checks_passed


def analyze_game_event_bus(content):
    """Specific analysis for GameEventBus."""
    issues = []
    checks_passed = []

    # Check: Events use ?.Invoke pattern
    if "?.Invoke" in content:
        checks_passed.append("✅ GameEventBus: Uses safe invoke pattern (?.)")
    else:
        issues.append("⚠️ GameEventBus: Not using safe invoke pattern")

    # Check: Has Trigger methods for all events
    event_count = len(re.findall(r"public static event", content))
    trigger_count = len(re.findall(r"public static void Trigger", content))
    if event_count == trigger_count:
        checks_passed.append(
            f"✅ GameEventBus: All {event_count} events have trigger methods"
        )
    else:
        issues.append(
            f"⚠️ GameEventBus: {event_count} events but only {trigger_count} trigger methods"
        )

    return issues, checks_passed


def analyze_analytics_manager(content):
    """Specific analysis for AnalyticsManager."""
    issues = []
    checks_passed = []

    # Check: Opt-out handling
    if "UserOptedOut" in content:
        checks_passed.append("✅ AnalyticsManager: Has opt-out handling")
    else:
        issues.append("❌ AnalyticsManager: Missing opt-out handling")

    # Check: Session tracking
    if "sessionStartTime" in content and "sessionLengthSeconds" in content:
        checks_passed.append("✅ AnalyticsManager: Has session tracking")
    else:
        issues.append("⚠️ AnalyticsManager: Missing session tracking")

    # Check: Batching or throttling
    if "batch" in content.lower() or "queue" in content.lower():
        checks_passed.append("✅ AnalyticsManager: Has event batching/queueing")
    else:
        issues.append(
            "ℹ️ AnalyticsManager: No event batching (may cause performance issues)"
        )

    # Check: Event cleanup on destroy
    if "OnDisable" in content and "-=" in content:
        checks_passed.append(
            "✅ AnalyticsManager: Unsubscribes from events in OnDisable"
        )
    else:
        issues.append("❌ AnalyticsManager: Not unsubscribing from GameEventBus")

    return issues, checks_passed


def analyze_privacy_compliance(content):
    """Specific analysis for PrivacyComplianceManager."""
    issues = []
    checks_passed = []

    # Check: GDPR/CCPA mentions
    if "GDPR" in content or "CCPA" in content:
        checks_passed.append("✅ PrivacyComplianceManager: Mentions GDPR/CCPA")
    else:
        issues.append("⚠️ PrivacyComplianceManager: No GDPR/CCPA references")

    # Check: Consent request method
    if "RequestConsent" in content:
        checks_passed.append("✅ PrivacyComplianceManager: Has consent request method")
    else:
        issues.append("❌ PrivacyComplianceManager: Missing consent request")

    # Check: Data deletion method
    if "RequestDataDeletion" in content:
        checks_passed.append("✅ PrivacyComplianceManager: Has data deletion method")
    else:
        issues.append("⚠️ PrivacyComplianceManager: Missing data deletion method")

    # Check: Privacy policy URL configuration
    if "privacyPolicyUrl" in content and "http" in content:
        checks_passed.append("✅ PrivacyComplianceManager: Has privacy policy URL")
    else:
        issues.append(
            "⚠️ PrivacyComplianceManager: Privacy policy URL may not be configured"
        )

    # Check: Region detection
    if "EU" in content or "California" in content or "region" in content.lower():
        checks_passed.append("✅ PrivacyComplianceManager: Has region detection logic")
    else:
        issues.append("⚠️ PrivacyComplianceManager: Missing region detection")

    return issues, checks_passed


def analyze_crash_reporter(content):
    """Specific analysis for CrashReporter."""
    issues = []
    checks_passed = []

    # Check: Unhandled exception handler
    if "UnhandledException" in content:
        checks_passed.append("✅ CrashReporter: Handles unhandled exceptions")
    else:
        issues.append("❌ CrashReporter: Missing unhandled exception handler")

    # Check: Breadcrumb system
    if "breadcrumb" in content.lower():
        checks_passed.append(
            "✅ CrashReporter: Has breadcrumb system for crash context"
        )
    else:
        issues.append("⚠️ CrashReporter: Missing breadcrumb system")

    # Check: Local crash report persistence
    if "persistentDataPath" in content or "SaveCrashReport" in content:
        checks_passed.append("✅ CrashReporter: Saves crash reports locally")
    else:
        issues.append("⚠️ CrashReporter: No local crash report persistence")

    # Check: Test crash method
    if "TestCrash" in content or "TestException" in content:
        checks_passed.append("✅ CrashReporter: Has test crash method")
    else:
        issues.append("ℹ️ CrashReporter: No test crash method (nice to have)")

    return issues, checks_passed


def analyze_performance_monitor(content):
    """Specific analysis for PerformanceMonitor."""
    issues = []
    checks_passed = []

    # Check: FPS tracking
    if "FPS" in content or "frame" in content.lower():
        checks_passed.append("✅ PerformanceMonitor: Tracks FPS")
    else:
        issues.append("❌ PerformanceMonitor: No FPS tracking")

    # Check: Memory tracking
    if (
        "memory" in content.lower()
        or "Memory" in content
        or "GC.GetTotalMemory" in content
    ):
        checks_passed.append("✅ PerformanceMonitor: Tracks memory usage")
    else:
        issues.append("⚠️ PerformanceMonitor: No memory tracking")

    # Check: Threshold warnings
    if "threshold" in content.lower():
        checks_passed.append("✅ PerformanceMonitor: Has performance thresholds")
    else:
        issues.append("ℹ️ PerformanceMonitor: No threshold warnings")

    # Check: Platform-specific handling
    if "#if UNITY" in content:
        checks_passed.append("✅ PerformanceMonitor: Has platform-specific code")
    else:
        issues.append("ℹ️ PerformanceMonitor: No platform-specific optimizations")

    return issues, checks_passed


def main():
    scripts_dir = "/Users/cuongnguyen/FinalNumber/Assets/Scripts"

    print("=" * 70)
    print("FinalNumber Codebase Health Analysis")
    print("=" * 70)
    print()

    all_issues = []
    all_checks = []

    # Find all C# files
    cs_files = []
    for root, dirs, files in os.walk(scripts_dir):
        for file in files:
            if file.endswith(".cs"):
                cs_files.append(os.path.join(root, file))

    print(f"Analyzing {len(cs_files)} C# files...")
    print()

    for filepath in cs_files:
        try:
            with open(filepath, "r", encoding="utf-8") as f:
                content = f.read()

            filename = os.path.basename(filepath)

            # Run general analysis
            issues, checks = analyze_file(filepath, content)

            # Run specific analysis for known files
            if "GameEventBus" in filename:
                i, c = analyze_game_event_bus(content)
                issues.extend(i)
                checks.extend(c)
            elif "AnalyticsManager" in filename:
                i, c = analyze_analytics_manager(content)
                issues.extend(i)
                checks.extend(c)
            elif "PrivacyComplianceManager" in filename:
                i, c = analyze_privacy_compliance(content)
                issues.extend(i)
                checks.extend(c)
            elif "CrashReporter" in filename:
                i, c = analyze_crash_reporter(content)
                issues.extend(i)
                checks.extend(c)
            elif "PerformanceMonitor" in filename:
                i, c = analyze_performance_monitor(content)
                issues.extend(i)
                checks.extend(c)

            all_issues.extend(issues)
            all_checks.extend(checks)

        except Exception as e:
            all_issues.append(f"❌ Error reading {filepath}: {e}")

    # Print results
    if all_checks:
        print("Checks Passed:")
        for check in sorted(set(all_checks)):
            print(f"  {check}")
        print()

    if all_issues:
        print("Issues Found:")
        for issue in sorted(set(all_issues)):
            print(f"  {issue}")
        print()

    # Summary
    critical = len([i for i in all_issues if i.startswith("❌")])
    warnings = len([i for i in all_issues if i.startswith("⚠️")])
    info = len([i for i in all_issues if i.startswith("ℹ️")])

    print("=" * 70)
    print(
        f"Summary: {len(all_checks)} checks passed, {critical} critical, {warnings} warnings, {info} info"
    )
    print("=" * 70)

    if critical > 0:
        print()
        print("RECOMMENDATION: Address critical issues before next release")
        return 1
    elif warnings > 5:
        print()
        print("RECOMMENDATION: Review and address warnings")
        return 0
    else:
        print()
        print("Codebase health: GOOD")
        return 0


if __name__ == "__main__":
    sys.exit(main())
