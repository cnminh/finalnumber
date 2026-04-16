using NUnit.Framework;
using FinalNumber.Build;
using UnityEngine;

namespace FinalNumber.Tests.EditMode
{
    /// <summary>
    /// Unit tests for BuildConfig version management and validation.
    /// </summary>
    public class BuildConfigTests
    {
        private BuildConfig _config;

        [SetUp]
        public void Setup()
        {
            _config = ScriptableObject.CreateInstance<BuildConfig>();
            _config.major = 1;
            _config.minor = 2;
            _config.patch = 3;
            _config.buildNumber = 45;
            _config.baseBundleId = "com.finalnumber.game";
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        #region Semantic Version Tests

        [Test]
        public void SemanticVersion_ReturnsCorrectFormat()
        {
            Assert.AreEqual("1.2.3", _config.SemanticVersion);
        }

        [Test]
        public void SemanticVersion_ZeroVersion_Returns000()
        {
            _config.major = 0;
            _config.minor = 0;
            _config.patch = 0;
            Assert.AreEqual("0.0.0", _config.SemanticVersion);
        }

        [Test]
        public void FullVersion_IncludesBuildNumberAndType()
        {
            _config.buildType = BuildConfig.BuildType.Development;
            StringAssert.Contains("+45.dev", _config.FullVersion);
            StringAssert.StartsWith("1.2.3", _config.FullVersion);
        }

        [Test]
        public void FullVersion_BetaType_IncludesBetaSuffix()
        {
            _config.buildType = BuildConfig.BuildType.Beta;
            StringAssert.Contains(".beta", _config.FullVersion);
        }

        [Test]
        public void FullVersion_ReleaseType_NoSuffix()
        {
            _config.buildType = BuildConfig.BuildType.Release;
            StringAssert.DoesNotContain("dev", _config.FullVersion);
            StringAssert.DoesNotContain("beta", _config.FullVersion);
        }

        #endregion

        #region iOS Version Tests

        [Test]
        public void iOSBundleVersion_UsesSemanticVersion()
        {
            Assert.AreEqual("1.2.3", _config.iOSBundleVersion);
        }

        [Test]
        public void iOSBuildNumber_IsStringRepresentation()
        {
            Assert.AreEqual("45", _config.iOSBuildNumber);
        }

        #endregion

        #region Android Version Tests

        [Test]
        public void AndroidVersionCode_CalculatesCorrectly()
        {
            // Formula: major * 1000000 + minor * 10000 + patch * 100 + build
            // 1 * 1000000 + 2 * 10000 + 3 * 100 + 45 = 1000000 + 20000 + 300 + 45 = 1020345
            Assert.AreEqual(1020345, _config.AndroidVersionCode);
        }

        [Test]
        public void AndroidVersionCode_MaxVersion_DoesNotOverflow()
        {
            _config.major = 99;
            _config.minor = 99;
            _config.patch = 99;
            _config.buildNumber = 99;
            // 99 * 1000000 + 99 * 10000 + 99 * 100 + 99 = 99999999
            Assert.AreEqual(99999999, _config.AndroidVersionCode);
        }

        [Test]
        public void AndroidVersionCode_BuildNumberOverflow_CapsAt99()
        {
            _config.buildNumber = 150;
            int expected = 1 * 1000000 + 2 * 10000 + 3 * 100 + 50; // 50, not 150
            Assert.AreEqual(expected, _config.AndroidVersionCode);
        }

        [Test]
        public void AndroidVersionName_ContainsFullVersion()
        {
            _config.buildType = BuildConfig.BuildType.Release;
            StringAssert.Contains("1.2.3", _config.AndroidVersionName);
            StringAssert.Contains("45", _config.AndroidVersionName);
        }

        #endregion

        #region Bundle ID Tests

        [Test]
        public void GetBundleId_Development_AddsDevSuffix()
        {
            _config.buildType = BuildConfig.BuildType.Development;
            Assert.AreEqual("com.finalnumber.game.dev", _config.GetBundleId());
        }

        [Test]
        public void GetBundleId_Beta_AddsBetaSuffix()
        {
            _config.buildType = BuildConfig.BuildType.Beta;
            Assert.AreEqual("com.finalnumber.game.beta", _config.GetBundleId());
        }

        [Test]
        public void GetBundleId_Release_NoSuffix()
        {
            _config.buildType = BuildConfig.BuildType.Release;
            Assert.AreEqual("com.finalnumber.game", _config.GetBundleId());
        }

        #endregion

        #region Product Name Tests

        [Test]
        public void GetProductName_Development_AddsDevTag()
        {
            _config.buildType = BuildConfig.BuildType.Development;
            Assert.AreEqual("Final Number [Dev]", _config.GetProductName());
        }

        [Test]
        public void GetProductName_Beta_AddsBetaTag()
        {
            _config.buildType = BuildConfig.BuildType.Beta;
            Assert.AreEqual("Final Number [Beta]", _config.GetProductName());
        }

        [Test]
        public void GetProductName_Release_NoTag()
        {
            _config.buildType = BuildConfig.BuildType.Release;
            Assert.AreEqual("Final Number", _config.GetProductName());
        }

        #endregion

        #region Version Bumping Tests

        [Test]
        public void BumpPatch_IncrementsPatchAndResetsBuild()
        {
            _config.BumpPatch();
            Assert.AreEqual(1, _config.major);
            Assert.AreEqual(2, _config.minor);
            Assert.AreEqual(4, _config.patch);
            Assert.AreEqual(1, _config.buildNumber);
        }

        [Test]
        public void BumpMinor_IncrementsMinorResetsPatchAndBuild()
        {
            _config.BumpMinor();
            Assert.AreEqual(1, _config.major);
            Assert.AreEqual(3, _config.minor);
            Assert.AreEqual(0, _config.patch);
            Assert.AreEqual(1, _config.buildNumber);
        }

        [Test]
        public void BumpMajor_IncrementsMajorResetsAll()
        {
            _config.BumpMajor();
            Assert.AreEqual(2, _config.major);
            Assert.AreEqual(0, _config.minor);
            Assert.AreEqual(0, _config.patch);
            Assert.AreEqual(1, _config.buildNumber);
        }

        [Test]
        public void IncrementBuildNumber_IncrementsByOne()
        {
            _config.IncrementBuildNumber();
            Assert.AreEqual(46, _config.buildNumber);
        }

        #endregion

        #region Validation Tests

        [Test]
        public void IsValidForStore_Development_ReturnsFalse()
        {
            _config.buildType = BuildConfig.BuildType.Development;
            Assert.IsFalse(_config.IsValidForStore());
        }

        [Test]
        public void IsValidForStore_Beta_ReturnsTrue()
        {
            _config.buildType = BuildConfig.BuildType.Beta;
            Assert.IsTrue(_config.IsValidForStore());
        }

        [Test]
        public void IsValidForStore_Release_ReturnsTrue()
        {
            _config.buildType = BuildConfig.BuildType.Release;
            Assert.IsTrue(_config.IsValidForStore());
        }

        [Test]
        public void IsValidForStore_ZeroVersion_ReturnsFalse()
        {
            _config.major = 0;
            _config.minor = 0;
            _config.patch = 0;
            _config.buildType = BuildConfig.BuildType.Release;
            Assert.IsFalse(_config.IsValidForStore());
        }

        [Test]
        public void IsValidForStore_EmptyBundleId_ReturnsFalse()
        {
            _config.baseBundleId = "";
            _config.buildType = BuildConfig.BuildType.Release;
            Assert.IsFalse(_config.IsValidForStore());
        }

        [Test]
        public void IsValidForStore_InvalidBundleIdFormat_ReturnsFalse()
        {
            _config.baseBundleId = "invalidbundleid"; // No dots
            _config.buildType = BuildConfig.BuildType.Release;
            Assert.IsFalse(_config.IsValidForStore());
        }

        #endregion
    }
}
