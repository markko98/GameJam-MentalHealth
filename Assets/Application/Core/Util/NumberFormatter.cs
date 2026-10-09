using System;
using UnityEngine;

public static class NumberFormatter
{
    /// <summary>
    /// Formats a number into short form like 123.4K, 3.4M, 2.1B.
    /// Values <= 100,000 are shown in full.
    /// </summary>
    public static string ShortFormat(int value, IFormatProvider culture = null)
    {
        if (value > 1_000_000_000) return (value / 1_000_000_000f).ToString("0.#") + "B";
        if (value > 1_000_000) return (value / 1_000_000f).ToString("0.#") + "M";
        if (value > 100_000) return (value / 1_000f).ToString("0.#") + "K";

        return culture == null
            ? value.ToString()
            : value.ToString("#,0", culture);
    }

    public static string LongFormat(int value, IFormatProvider culture)
        => value.ToString("#,0", culture);
}