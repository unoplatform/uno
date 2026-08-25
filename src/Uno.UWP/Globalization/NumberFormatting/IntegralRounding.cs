#nullable enable

using System;
using Windows.Globalization.NumberFormatting;

namespace Uno.Globalization.NumberFormatting;

/// <summary>
/// Sign/magnitude conversions shared by the integral <see cref="INumberRounder"/> members, which must
/// round without going through <see cref="double"/> to stay exact past 2^53.
/// </summary>
/// <remarks>
/// A rounded value that no longer fits its type raises <see cref="ArithmeticException"/>, which is what
/// native WinRT reports and what the formatters turn into an infinity.
/// </remarks>
internal static class IntegralRounding
{
	private const ulong Int64MinValueMagnitude = (ulong)long.MaxValue + 1;

	public static ulong GetMagnitude(long value, out bool isNegative)
	{
		isNegative = value < 0;

		return isNegative ? (ulong)(-(value + 1)) + 1 : (ulong)value;
	}

	public static long ToInt64(ulong magnitude, bool isNegative)
	{
		if (isNegative)
		{
			if (magnitude > Int64MinValueMagnitude)
			{
				ExceptionHelper.ThrowArithmeticException();
			}

			return magnitude == Int64MinValueMagnitude ? long.MinValue : -(long)magnitude;
		}

		if (magnitude > long.MaxValue)
		{
			ExceptionHelper.ThrowArithmeticException();
		}

		return (long)magnitude;
	}

	public static int ToInt32(long rounded)
	{
		if (rounded is < int.MinValue or > int.MaxValue)
		{
			ExceptionHelper.ThrowArithmeticException();
		}

		return (int)rounded;
	}

	public static uint ToUInt32(ulong rounded)
	{
		if (rounded > uint.MaxValue)
		{
			ExceptionHelper.ThrowArithmeticException();
		}

		return (uint)rounded;
	}
}
