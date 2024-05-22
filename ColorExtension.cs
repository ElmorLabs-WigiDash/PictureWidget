using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PictureWidget;

public static class ColorExtension {
    public static string ToHex(this System.Drawing.Color color)
    {
        return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    public static System.Drawing.Color ToColor(this string hex)
    {
        if (hex.StartsWith("#")) hex = hex.Substring(1);
        if (hex.Length != 8 && hex.Length != 6)
        {
            return Color.Black;
            //throw new ArgumentException("Hex string must be 8 or 6 characters long");
        }

        if (hex.Length == 6) hex = "FF" + hex;

        System.Drawing.Color color = System.Drawing.Color.FromArgb(
            int.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber),
            int.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber),
            int.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber),
            int.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber)
        );

        return color;
    }
}