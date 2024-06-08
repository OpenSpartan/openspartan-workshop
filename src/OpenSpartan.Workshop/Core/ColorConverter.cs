
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace OpenSpartan.Workshop.Core
{
    internal class ColorConverter
    {
        public static SolidColorBrush FromHex(string hex)
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

            var brush = new SolidColorBrush(Color.FromArgb(a, r, g, b));
            // Create a SolidColorBrush from the Color
            return brush;
        }
    }
}
