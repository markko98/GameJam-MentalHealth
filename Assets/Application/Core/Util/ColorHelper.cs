using System;
using System.Drawing;
using System.Globalization;
using Color = UnityEngine.Color;

public class ColorHelper
{
    public static Color PaleSpringGreen = FromHex("CDF5B1");
    public static Color SoftLilac = FromHex("948EDC");
    public static Color LavenderDusk = FromHex("7972D380");
    public static Color Disabled = FromHex("8F8F8F66");
    // 7972D3 regular color
    private static Color colorForRGBA(int r, int g, int b, int a = 255)
    {
        float rc = (float)r / 255;
        float gc = (float)g / 255;
        float bc = (float)b / 255;
        float ac = (float)a / 255;

        Color color = new Color(rc, gc, bc, ac);

        return color;
    }

    private static Color colorForRGBA(int r, int g, int b)
    {
        return colorForRGBA(r, g, b, 255);
    }
    
    public static Color FromHex(string hex)
    {
        if (hex.Length<6)
        {
            throw new System.FormatException("Needs a string with a length of at least 6");
        }

        var r = hex.Substring(0, 2);
        var g = hex.Substring(2, 2);
        var b = hex.Substring(4, 2);
        string alpha;
        if (hex.Length >= 8)
            alpha = hex.Substring(6, 2);
        else
            alpha = "FF";

        return new Color((int.Parse(r, NumberStyles.HexNumber) / 255f),
            (int.Parse(g, NumberStyles.HexNumber) / 255f),
            (int.Parse(b, NumberStyles.HexNumber) / 255f),
            (int.Parse(alpha, NumberStyles.HexNumber) / 255f));
    }
}