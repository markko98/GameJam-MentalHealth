using System;

public static class ArrayExtensions
{
    public static void TryResizeArray<T>(ref T[] array, int newSize)
    {
        if (newSize < array.Length) return;
        Array.Resize(ref array, newSize);
    }
}
