using System;
using System.Globalization;
using Windows.UI;

namespace OpenSpartan.Workshop.Core
{
    internal static class ColorConverter
    {
        /// <summary>
        /// Parses a hex color string and returns a Color.
        /// Note: Returns Color instead of SolidColorBrush because brushes must be created on the UI thread.
        /// </summary>
        public static Color FromHex(string hex)
        {
            // Remove any leading '#' characters
            hex = hex.TrimStart('#');

            // Parse the hexadecimal color string
            byte a = 255; // Default alpha value
            byte r = byte.Parse(hex.AsSpan(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            byte g = byte.Parse(hex.AsSpan(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            byte b = byte.Parse(hex.AsSpan(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

            // If the hex string has 8 characters, parse the alpha value
            if (hex.Length == 8)
            {
                a = byte.Parse(hex.AsSpan(6, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }

            return Color.FromArgb(a, r, g, b);
        }
    }
}
