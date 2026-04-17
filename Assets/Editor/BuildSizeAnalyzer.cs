using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FinalNumber.Editor
{
    /// <summary>
    /// Analyzes build size and reports optimization opportunities.
    /// Tracks against target of <100MB download size.
    /// </summary>
    public class BuildSizeAnalyzer : IPostprocessBuildWithReport
    {
        public int callbackOrder => 100; // Run after other post-processors

        // Target sizes in bytes
        private const long TARGET_DOWNLOAD_SIZE = 100 * 1024 * 1024; // 100MB
        private const long TARGET_APP_SIZE = 200 * 1024 * 1024; // 200MB installed

        public void OnPostprocessBuild(BuildReport report)
        {
            Debug.Log("[BuildSizeAnalyzer] Analyzing build size...");

            // Get build summary
            BuildSummary summary = report.summary;
            long totalSize = (long)summary.totalSize;
            BuildTarget platform = summary.platform;
            string outputPath = summary.outputPath;

            // Analyze build
            var analysis = new BuildSizeAnalysis
            {
                Platform = platform.ToString(),
                BuildType = EditorUserBuildSettings.development ? "Development" : "Release",
                TotalSize = totalSize,
                DownloadEstimate = EstimateDownloadSize(totalSize, platform),
                TargetMet = totalSize < TARGET_APP_SIZE
            };

            // Log results
            LogBuildSize(analysis);

            // Warn if over target
            if (analysis.DownloadEstimate > TARGET_DOWNLOAD_SIZE)
            {
                Debug.LogWarning($"[BuildSizeAnalyzer] Build size exceeds 100MB target!" +
                    $"\nActual: {FormatBytes(analysis.DownloadEstimate)}" +
                    $"\nTarget: {FormatBytes(TARGET_DOWNLOAD_SIZE)}");
            }

            // Write detailed report
            WriteReport(report, analysis, outputPath);
        }

        private long EstimateDownloadSize(long buildSize, BuildTarget platform)
        {
            // AAB has additional compression, APK less so
            double compressionRatio = platform == BuildTarget.Android ? 0.55 : 0.65;
            return (long)(buildSize * compressionRatio);
        }

        private void LogBuildSize(BuildSizeAnalysis analysis)
        {
            Debug.Log($"[BuildSizeAnalyzer] Build Size Report:\n" +
                $"  Platform: {analysis.Platform}\n" +
                $"  Type: {analysis.BuildType}\n" +
                $"  Total: {FormatBytes(analysis.TotalSize)}\n" +
                $"  Download Est: {FormatBytes(analysis.DownloadEstimate)}\n" +
                $"  Target Met: {analysis.TargetMet}");
        }

        private void WriteReport(BuildReport report, BuildSizeAnalysis analysis, string outputPath)
        {
            string reportPath = Path.Combine(
                Path.GetDirectoryName(outputPath),
                $"BuildSizeReport_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
            );

            using (var writer = new StreamWriter(reportPath))
            {
                writer.WriteLine("=== Final Number Build Size Report ===");
                writer.WriteLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                writer.WriteLine($"Platform: {analysis.Platform}");
                writer.WriteLine($"Build Type: {analysis.BuildType}");
                writer.WriteLine();
                writer.WriteLine("SIZE BREAKDOWN:");
                writer.WriteLine($"  Total Size: {FormatBytes(analysis.TotalSize)}");
                writer.WriteLine($"  Download Estimate: {FormatBytes(analysis.DownloadEstimate)}");
                writer.WriteLine($"  Target (<100MB): {(analysis.DownloadEstimate < TARGET_DOWNLOAD_SIZE ? "PASS" : "FAIL")}");
                writer.WriteLine();
                writer.WriteLine("ASSET BREAKDOWN:");

                foreach (var group in report.GetFiles().GroupBy(f => f.role).OrderByDescending(g => g.Sum(f => (long)f.size)))
                {
                    long groupSize = group.Sum(f => (long)f.size);
                    writer.WriteLine($"  {group.Key}: {FormatBytes(groupSize)} ({group.Count()} files)");
                }

                writer.WriteLine();
                writer.WriteLine("OPTIMIZATION RECOMMENDATIONS:");
                writer.WriteLine(GetOptimizationTips(analysis));
            }

            Debug.Log($"[BuildSizeAnalyzer] Report saved to: {reportPath}");
        }

        private string GetOptimizationTips(BuildSizeAnalysis analysis)
        {
            var tips = new System.Collections.Generic.List<string>();

            if (analysis.DownloadEstimate > TARGET_DOWNLOAD_SIZE)
            {
                tips.Add("- Enable LZ4HC compression in Player Settings");
                tips.Add("- Use ARM64-only architecture (remove ARMv7)");
                tips.Add("- Verify texture compression: ASTC for iOS, ETC2 for Android");
                tips.Add("- Remove unused assets from Resources folder");
                tips.Add("- Use Asset Bundles for optional content");
            }

            return tips.Count > 0 ? string.Join("\n", tips) : "- Build size is optimal";
        }

        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double size = bytes;

            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }

            return $"{size:0.00} {sizes[order]}";
        }

        [MenuItem("Final Number/Optimization/Check Build Size")]
        private static void CheckBuildSize()
        {
            // Check installed package size estimate
            long assetsSize = GetAssetsFolderSize();
            long librariesEstimate = 50 * 1024 * 1024; // ~50MB for Unity runtime

            long estimatedBuild = assetsSize + librariesEstimate;

            Debug.Log($"[BuildSizeAnalyzer] Estimated Build Size:\n" +
                $"  Assets: {FormatBytesStatic(assetsSize)}\n" +
                $"  Engine: {FormatBytesStatic(librariesEstimate)}\n" +
                $"  Total Est: {FormatBytesStatic(estimatedBuild)}");

            EditorUtility.DisplayDialog("Build Size Estimate",
                $"Assets: {FormatBytesStatic(assetsSize)}\n" +
                $"Engine: {FormatBytesStatic(librariesEstimate)}\n" +
                $"Total: {FormatBytesStatic(estimatedBuild)}\n\n" +
                $"Target: <100MB download",
                estimatedBuild < TARGET_APP_SIZE ? "On Track" : "Needs Optimization");
        }

        private static long GetAssetsFolderSize()
        {
            long size = 0;
            string assetsPath = Application.dataPath;

            if (Directory.Exists(assetsPath))
            {
                foreach (string file in Directory.GetFiles(assetsPath, "*", SearchOption.AllDirectories))
                {
                    if (!file.EndsWith(".meta"))
                    {
                        size += new FileInfo(file).Length;
                    }
                }
            }

            return size;
        }

        private static string FormatBytesStatic(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double size = bytes;

            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }

            return $"{size:0.00} {sizes[order]}";
        }

        private class BuildSizeAnalysis
        {
            public string Platform;
            public string BuildType;
            public long TotalSize;
            public long DownloadEstimate;
            public bool TargetMet;
        }
    }
}
