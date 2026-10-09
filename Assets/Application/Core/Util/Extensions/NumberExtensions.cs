using System;

// Standard K/M/B/T big-number formatting, one decimal place past the first
// three digits (e.g. 12500 -> "12.5K").
public static class NumberExtensions
{
    private static readonly string[] Suffixes = { "", "K", "M", "B", "T" };

    public static string FormatBigNumber(this int number) => FormatBigNumber((long)number);

    public static string FormatBigNumber(this float number) => FormatBigNumber((long)number);

    public static string FormatBigNumber(this long number)
    {
        var isNegative = number < 0;
        var value = (double)Math.Abs(number);
        var suffixIndex = 0;

        while (value >= 1000 && suffixIndex < Suffixes.Length - 1)
        {
            value /= 1000;
            suffixIndex++;
        }

        var formatted = suffixIndex == 0 ? value.ToString("0") : value.ToString("0.#");
        return (isNegative ? "-" : "") + formatted + Suffixes[suffixIndex];
    }
}
