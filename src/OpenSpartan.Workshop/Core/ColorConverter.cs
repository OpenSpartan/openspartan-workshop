using Windows.UI;

namespace OpenSpartan.Workshop.Core
{
    internal class ColorConverter
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
            byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);

            // If the hex string has 8 characters, parse the alpha value
            if (hex.Length == 8)
            {
                a = byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
            }

            return Color.FromArgb(a, r, g, b);
        }
    }
}
