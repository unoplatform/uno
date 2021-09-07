using System;

namespace Windows.Globalization.NumberFormatting
{
    public static class Rounder
    {
        internal const ulong ExponentMask = 0x7FF0_0000_0000_0000;
        internal const int ExponentShift = 52;
        internal const uint ShiftedExponentMask = (uint)(ExponentMask >> ExponentShift);

        internal const long SignificandMask = 0x000F_FFFF_FFFF_FFFF;

        public static double Round(double value, int digits, RoundingAlgorithm roundingAlgorithm)
        {
            var pow10 = Math.Pow(10, digits);
            value *= pow10;

            switch (roundingAlgorithm)
            {
                case RoundingAlgorithm.RoundDown:
                    value = Math.Floor(value);
                    break;
                case RoundingAlgorithm.RoundUp:
                    value = Math.Ceiling(value);
                    break;
                case RoundingAlgorithm.RoundTowardsZero:
                    {
                        if (value > 0)
                        {
                            value = Round(value, 0, RoundingAlgorithm.RoundDown);
                        }
                        else
                        {
                            value = Round(value, 0, RoundingAlgorithm.RoundUp);
                        }
                    }
                    break;
                case RoundingAlgorithm.RoundAwayFromZero:
                    {
                        if (value > 0)
                        {
                            value = Round(value, 0, RoundingAlgorithm.RoundUp);
                        }
                        else
                        {
                            value = Round(value, 0, RoundingAlgorithm.RoundDown);
                        }
                    }
                    break;
                case RoundingAlgorithm.RoundHalfDown:
                    {
                        var isHalf = IsFractionExactlyHalf(value);
                        if (isHalf)
                        {
                            value = Round(value, 0, RoundingAlgorithm.RoundDown);
                        }
                        else
                        {
                            value = Math.Round(value, 0, MidpointRounding.AwayFromZero);
                        }
                    }
                    break;
                case RoundingAlgorithm.RoundHalfUp:
                    {
                        var isHalf = IsFractionExactlyHalf(value);
                        if (isHalf)
                        {
                            value = Round(value, 0, RoundingAlgorithm.RoundUp);
                        }
                        else
                        {
                            value = Math.Round(value, 0, MidpointRounding.AwayFromZero);
                        }
                    }
                    break;
                case RoundingAlgorithm.RoundHalfTowardsZero:
                    {
                        var isHalf = IsFractionExactlyHalf(value);
                        if (isHalf)
                        {
                            value = Round(value, 0, RoundingAlgorithm.RoundTowardsZero);
                        }
                        else
                        {
                            value = Math.Round(value, 0, MidpointRounding.AwayFromZero);
                        }
                    }
                    break;
                case RoundingAlgorithm.RoundHalfAwayFromZero:
                    {
                        var isHalf = IsFractionExactlyHalf(value);
                        if (isHalf)
                        {
                            value = Round(value, 0, RoundingAlgorithm.RoundAwayFromZero);
                        }
                        else
                        {
                            value = Math.Round(value, 0, MidpointRounding.AwayFromZero);
                        }
                    }
                    break;
                case RoundingAlgorithm.RoundHalfToEven:
                    value = Math.Round(value, 0, MidpointRounding.ToEven);
                    break;
                case RoundingAlgorithm.RoundHalfToOdd:
                    {
                        var intPart = Math.Truncate(value);
                        var isHalf = IsFractionExactlyHalf(value);

                        if (isHalf)
                        {
                            if (intPart % 2 == 1 && value > 0 ||
                                intPart % 2 == 0 && value < 0)
                            {
                                value = Math.Floor(value);
                            }
                            else
                            {
                                value = Math.Ceiling(value);
                            }
                        }
                        else
                        {
                            value = Math.Round(value, 0, MidpointRounding.AwayFromZero);
                        }
                    }
                    break;
                default:
                    value = Math.Round(value, 0, MidpointRounding.AwayFromZero);
                    break;
            }

            value /= pow10;
            return value;
        }

        internal static bool IsFractionExactlyHalf(double value)
        {
            long bits = BitConverter.DoubleToInt64Bits(value);
            int exponent = ExtractExponentFromBits(bits);
            int nonFractionLength = exponent - 0x03ff;

            if (nonFractionLength < -1)
            {
                return false;
            }
            else if (nonFractionLength == -1)
            {
                long significand = ExtractSignificandFromBits(bits);
                return significand == 0;
            }
            else
            {
                long significand = ExtractSignificandFromBits(bits);
                var shifted = (significand << nonFractionLength) & SignificandMask;
                return shifted == 1L << 51;
            }

        }

        // Adjusted from Microsoft dotnet/runtime System.Double class
        internal static int ExtractExponentFromBits(long bits)
        {
            return (int)(bits >> ExponentShift) & (int)ShiftedExponentMask;
        }

        // Adjusted from Microsoft dotnet/runtime System.Double class
        internal static long ExtractSignificandFromBits(long bits)
        {
            return bits & SignificandMask;
        }
    }
}
