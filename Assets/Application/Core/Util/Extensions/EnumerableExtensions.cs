using System;
using System.Collections.Generic;

public static class EnumerableExtensions
{
    private static readonly Random randomizer = new Random();

    public static T GetRandomElement<T>(this T[] array)
    {
        if (array == null || array.Length == 0)
            throw new ArgumentException("Array must not be null or empty");

        return array[randomizer.Next(array.Length)];
    }

    public static T GetRandomElement<T>(this List<T> list)
    {
        if (list == null || list.Count == 0)
            throw new ArgumentException("List must not be null or empty");

        return list[randomizer.Next(list.Count)];
    }

    public static T GetRandomElementWithSeed<T>(this List<T> list, int seed)
    {
        if (list == null || list.Count == 0)
            throw new ArgumentException("List must not be null or empty");

        return list[new Random(seed).Next(list.Count)];
    }
}
