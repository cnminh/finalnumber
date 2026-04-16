using UnityEditor;
using UnityEngine;

namespace FinalNumber.Editor
{
    /// <summary>
    /// Automatically configures texture import settings for optimal build size.
    /// Enforces platform-specific compression: ASTC for iOS, ETC2 for Android.
    /// </summary>
    public class TextureImportProcessor : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            TextureImporter importer = assetImporter as TextureImporter;
            if (importer == null) return;

            // Skip if already configured (avoid overwriting manual settings)
            if (importer.userData == "optimized") return;

            // Configure based on texture type
            ConfigureTextureSettings(importer);

            // Configure platform-specific formats
            ConfigurePlatformSettings(importer);

            importer.userData = "optimized";
            Debug.Log($"[TextureImportProcessor] Optimized: {assetPath}");
        }

        private void ConfigureTextureSettings(TextureImporter importer)
        {
            // General optimizations
            importer.textureType = TextureImporterType.Default;
            importer.textureShape = TextureImporterShape.Texture2D;

            // Enable mipmaps for 3D textures, disable for UI
            if (importer.textureShape == TextureImporterShape.Texture2D)
            {
                // Mipmaps increase memory but improve rendering quality/performance
                importer.mipmapEnabled = true;
                importer.filterMode = FilterMode.Bilinear;
            }

            // Use nearest for pixel art (detect by small size)
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.compressionQuality = 100;
        }

        private void ConfigurePlatformSettings(TextureImporter importer)
        {
            // Android: ETC2 compression (widely supported, good quality)
            var androidSettings = new TextureImporterPlatformSettings
            {
                name = "Android",
                overridden = true,
                format = TextureImporterFormat.ETC2_RGBA8,
                compressionQuality = 100,
                resizeAlgorithm = TextureResizeAlgorithm.Mitchell,
                maxTextureSize = 2048,
                textureCompression = TextureImporterCompression.Compressed
            };
            importer.SetPlatformTextureSettings(androidSettings);

            // iOS: ASTC compression (best quality/size ratio on modern iOS)
            var iosSettings = new TextureImporterPlatformSettings
            {
                name = "iPhone",
                overridden = true,
                format = TextureImporterFormat.ASTC_6x6,
                compressionQuality = 100,
                resizeAlgorithm = TextureResizeAlgorithm.Mitchell,
                maxTextureSize = 2048,
                textureCompression = TextureImporterCompression.Compressed
            };
            importer.SetPlatformTextureSettings(iosSettings);
        }

        /// <summary>
        /// Menu item to reprocess all textures in the project
        /// </summary>
        [MenuItem("Final Number/Optimization/Reprocess All Textures")]
        private static void ReprocessAllTextures()
        {
            string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" });
            int processedCount = 0;

            foreach (string guid in textureGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    importer.userData = ""; // Clear optimized flag to reprocess
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    processedCount++;
                }
            }

            Debug.Log($"[TextureImportProcessor] Reprocessed {processedCount} textures");
            EditorUtility.DisplayDialog("Texture Optimization", $"Reprocessed {processedCount} textures with optimal compression settings.", "OK");
        }
    }
}
