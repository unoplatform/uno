#nullable enable

using System;
using Windows.Globalization.NumberFormatting;

namespace Uno.Globalization.NumberFormatting;

internal static class Rounder
{
	internal const ulong ExponentMask = 0x7FF0_0000_0000_0000;
	internal const int ExponentShift = 52;
	internal const uint ShiftedExponentMask = (uint)(ExponentMask >> ExponentShift);

	internal const long SignificandMask = 0x000F_FFFF_FFFF_FFFF;

	/// <summary>
	/// Rounds <paramref name="magnitude"/> to a multiple of <paramref name="increment"/> using integer
	/// arithmetic, so that values beyond 2^53 keep every digit.
	/// </summary>
	/// <exception cref="ArithmeticException">The rounded value leaves the <see cref="ulong"/> range.</exception>
	public static ulong RoundMagnitude(ulong magnitude, ulong increment, bool isNegative, RoundingAlgorithm roundingAlgorithm)
	{
		if (increment <= 1)
		{
			return magnitude;
		}

		var quotient = magnitude / increment;
		var remainder = magnitude - quotient * increment;

		if (remainder == 0)
		{
			return magnitude;
		}

		if (RoundsAwayFromZero(quotient, remainder, increment, isNegative, roundingAlgorithm))
		{
			if (quotient + 1 > ulong.MaxValue / increment)
			{
				ExceptionHelper.ThrowArithmeticException();
			}

			quotient++;
		}

		return quotient * increment;
	}

	private static bool RoundsAwayFromZero(ulong quotient, ulong remainder, ulong increment, bool isNegative, RoundingAlgorithm roundingAlgorithm)
	{
		switch (roundingAlgorithm)
		{
			case RoundingAlgorithm.RoundDown:
				return isNegative;
			case RoundingAlgorithm.RoundUp:
				return !isNegative;
			case RoundingAlgorithm.RoundTowardsZero:
				return false;
			case RoundingAlgorithm.RoundAwayFromZero:
				return true;
		}

		// Comparing the two distances avoids overflowing when doubling the remainder.
		var complement = increment - remainder;

		if (remainder != complement)
		{
			return remainder > complement;
		}

		return roundingAlgorithm switch
		{
			RoundingAlgorithm.RoundHalfDown => isNegative,
			RoundingAlgorithm.RoundHalfUp => !isNegative,
			RoundingAlgorithm.RoundHalfTowardsZero => false,
			RoundingAlgorithm.RoundHalfToEven => quotient % 2 != 0,
			RoundingAlgorithm.RoundHalfToOdd => quotient % 2 == 0,
			_ => true,
		};
	}

	/// <summary>
	/// Gets the number of decimal digits of <paramref name="magnitude"/>, counting zero as one digit.
	/// </summary>
	public static int GetDigitCount(ulong magnitude)
	{
		var count = 1;

		while (magnitude >= 10)
		{
			magnitude /= 10;
			count++;
		}

		return count;
	}

	/// <summary>
	/// Gets 10^<paramref name="exponent"/> for the exponents that fit in a <see cref="ulong"/> (0-19).
	/// </summary>
	public static ulong GetPowerOfTen(int exponent)
	{
		var result = 1UL;

		for (var i = 0; i < exponent; i++)
		{
			result *= 10;
		}

		return result;
	}

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
						if ((intPart % 2 == 1 && value > 0) ||
							(intPart % 2 == 0 && value < 0))
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
