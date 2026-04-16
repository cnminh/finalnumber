using System;
using UnityEngine;

namespace FinalNumber.Gameplay
{
    /// <summary>
    /// Represents a single tile in the 2048 game.
    /// Contains value, position, and merge state.
    /// </summary>
    [Serializable]
    public class Tile
    {
        public int Value { get; private set; }
        public int Row { get; private set; }
        public int Column { get; private set; }
        public bool HasMerged { get; set; }
        public bool IsNew { get; set; }

        // Animation/Visual state (not serialized)
        [NonSerialized] public Vector2 TargetPosition;
        [NonSerialized] public float AnimationProgress;
        [NonSerialized] public bool IsAnimating;

        public Tile(int value, int row, int column)
        {
            Value = value;
            Row = row;
            Column = column;
            HasMerged = false;
            IsNew = true;
        }

        public void SetValue(int newValue)
        {
            Value = newValue;
        }

        public void SetPosition(int row, int column)
        {
            Row = row;
            Column = column;
        }

        public void ResetMergeState()
        {
            HasMerged = false;
            IsNew = false;
        }

        /// <summary>
        /// Returns the color for this tile value (for UI rendering)
        /// </summary>
        public Color GetTileColor()
        {
            return Value switch
            {
                2 => new Color(0.93f, 0.89f, 0.85f),      // Light beige
                4 => new Color(0.93f, 0.88f, 0.78f),      // Light tan
                8 => new Color(0.96f, 0.69f, 0.53f),      // Orange
                16 => new Color(0.96f, 0.58f, 0.47f),     // Dark orange
                32 => new Color(0.96f, 0.48f, 0.47f),     // Red-orange
                64 => new Color(0.96f, 0.37f, 0.33f),     // Red
                128 => new Color(0.93f, 0.81f, 0.45f),     // Yellow
                256 => new Color(0.93f, 0.80f, 0.38f),    // Bright yellow
                512 => new Color(0.93f, 0.79f, 0.31f),    // Golden
                1024 => new Color(0.93f, 0.78f, 0.24f),   // Dark gold
                2048 => new Color(0.93f, 0.77f, 0.17f),   // Gold
                _ => new Color(0.55f, 0.35f, 0.35f)        // Dark brown for higher values
            };
        }

        /// <summary>
        /// Returns the text color for this tile value
        /// </summary>
        public Color GetTextColor()
        {
            return Value <= 4 ? new Color(0.47f, 0.43f, 0.40f) : Color.white;
        }

        /// <summary>
        /// Merges this tile with another tile of the same value
        /// </summary>
        public void Merge(Tile other)
        {
            if (other.Value != Value)
                throw new InvalidOperationException("Cannot merge tiles with different values");

            Value *= 2;
            HasMerged = true;
        }

        public override string ToString()
        {
            return $"Tile({Value}, [{Row},{Column}])";
        }
    }
}
