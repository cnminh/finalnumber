using NUnit.Framework;
using FinalNumber.Gameplay;
using System;
using UnityEngine;

namespace FinalNumber.Tests.EditMode
{
    /// <summary>
    /// Unit tests for the Tile class
    /// </summary>
    public class TileTests
    {
        [Test]
        public void Constructor_SetsValuePositionAndDefaults()
        {
            // Arrange & Act
            var tile = new Tile(2, 1, 2);

            // Assert
            Assert.AreEqual(2, tile.Value);
            Assert.AreEqual(1, tile.Row);
            Assert.AreEqual(2, tile.Column);
            Assert.IsFalse(tile.HasMerged);
            Assert.IsTrue(tile.IsNew);
        }

        [Test]
        public void SetValue_UpdatesValue()
        {
            // Arrange
            var tile = new Tile(2, 0, 0);

            // Act
            tile.SetValue(4);

            // Assert
            Assert.AreEqual(4, tile.Value);
        }

        [Test]
        public void SetPosition_UpdatesRowAndColumn()
        {
            // Arrange
            var tile = new Tile(2, 0, 0);

            // Act
            tile.SetPosition(2, 3);

            // Assert
            Assert.AreEqual(2, tile.Row);
            Assert.AreEqual(3, tile.Column);
        }

        [Test]
        public void ResetMergeState_ClearsFlags()
        {
            // Arrange
            var tile = new Tile(2, 0, 0);
            tile.HasMerged = true;

            // Act
            tile.ResetMergeState();

            // Assert
            Assert.IsFalse(tile.HasMerged);
            Assert.IsFalse(tile.IsNew);
        }

        [Test]
        public void Merge_WithSameValue_DoublesValue()
        {
            // Arrange
            var tile1 = new Tile(2, 0, 0);
            var tile2 = new Tile(2, 0, 1);

            // Act
            tile1.Merge(tile2);

            // Assert
            Assert.AreEqual(4, tile1.Value);
            Assert.IsTrue(tile1.HasMerged);
        }

        [Test]
        public void Merge_WithDifferentValue_ThrowsException()
        {
            // Arrange
            var tile1 = new Tile(2, 0, 0);
            var tile2 = new Tile(4, 0, 1);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => tile1.Merge(tile2));
        }

        [Test]
        public void Merge_Chain256_Creates512()
        {
            // Arrange
            var tile1 = new Tile(256, 0, 0);
            var tile2 = new Tile(256, 0, 1);

            // Act
            tile1.Merge(tile2);

            // Assert
            Assert.AreEqual(512, tile1.Value);
            Assert.IsTrue(tile1.HasMerged);
        }

        [Test]
        public void GetTileColor_Value2_ReturnsLightBeige()
        {
            // Arrange
            var tile = new Tile(2, 0, 0);

            // Act
            Color color = tile.GetTileColor();

            // Assert
            Assert.AreEqual(new Color(0.93f, 0.89f, 0.85f), color);
        }

        [Test]
        public void GetTileColor_Value4_ReturnsLightTan()
        {
            // Arrange
            var tile = new Tile(4, 0, 0);

            // Act
            Color color = tile.GetTileColor();

            // Assert
            Assert.AreEqual(new Color(0.93f, 0.88f, 0.78f), color);
        }

        [Test]
        public void GetTileColor_Value2048_ReturnsGold()
        {
            // Arrange
            var tile = new Tile(2048, 0, 0);

            // Act
            Color color = tile.GetTileColor();

            // Assert
            Assert.AreEqual(new Color(0.93f, 0.77f, 0.17f), color);
        }

        [Test]
        public void GetTileColor_UnknownValue_ReturnsDarkBrown()
        {
            // Arrange
            var tile = new Tile(4096, 0, 0);

            // Act
            Color color = tile.GetTileColor();

            // Assert
            Assert.AreEqual(new Color(0.55f, 0.35f, 0.35f), color);
        }

        [Test]
        public void GetTextColor_Value2_ReturnsDarkText()
        {
            // Arrange
            var tile = new Tile(2, 0, 0);

            // Act
            Color color = tile.GetTextColor();

            // Assert
            Assert.AreEqual(new Color(0.47f, 0.43f, 0.40f), color);
        }

        [Test]
        public void GetTextColor_Value4_ReturnsDarkText()
        {
            // Arrange
            var tile = new Tile(4, 0, 0);

            // Act
            Color color = tile.GetTextColor();

            // Assert
            Assert.AreEqual(new Color(0.47f, 0.43f, 0.40f), color);
        }

        [Test]
        public void GetTextColor_Value8_ReturnsWhite()
        {
            // Arrange
            var tile = new Tile(8, 0, 0);

            // Act
            Color color = tile.GetTextColor();

            // Assert
            Assert.AreEqual(Color.white, color);
        }

        [Test]
        public void GetTextColor_HighValue_ReturnsWhite()
        {
            // Arrange
            var tile = new Tile(2048, 0, 0);

            // Act
            Color color = tile.GetTextColor();

            // Assert
            Assert.AreEqual(Color.white, color);
        }

        [Test]
        public void ToString_ContainsValueAndPosition()
        {
            // Arrange
            var tile = new Tile(16, 2, 3);

            // Act
            string result = tile.ToString();

            // Assert
            Assert.AreEqual("Tile(16, [2,3])", result);
        }

        [Test]
        public void NonSerializedFields_DefaultValues()
        {
            // Arrange & Act
            var tile = new Tile(2, 0, 0);

            // Assert
            Assert.AreEqual(Vector2.zero, tile.TargetPosition);
            Assert.AreEqual(0f, tile.AnimationProgress);
            Assert.IsFalse(tile.IsAnimating);
        }
    }
}
