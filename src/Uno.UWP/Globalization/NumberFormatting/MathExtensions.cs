using System;

namespace Windows.Globalization.NumberFormatting
{
    internal static class MathExtensions
    {
        public static int GetLength(this int input)
        {
            if (input == 0)
            {
                return 1;
            }

            return (int)Math.Floor(Math.Log10(Math.Abs(input))) + 1;
        }
    }
}
