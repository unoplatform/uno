#nullable enable

using System;
using Windows.Globalization.NumberFormatting;

namespace Uno.Globalization.NumberFormatting;

/// <summary>
/// Sign/magnitude conversions shared by the integral <see cref="INumberRounder"/> members, which must
/// round without going through <see cref="double"/> to stay exact past 2^53.
/// </summary>
internal static class IntegralRounding
{
	private const ulong Int64MinValueMagnitude = (ulong)long.MaxValue + 1;

	public static ulong GetMagnitude(long value, out bool isNegative)
	{
		isNegative = value < 0;

		return isNegative ? (ulong)(-(value + 1)) + 1 : (ulong)value;
	}

	/// <summary>
	/// Rebuilds a <see cref="long"/> from a rounded magnitude, keeping <paramref name="value"/> when
	/// rounding pushed it out of the Int64 range.
	/// </summary>
	public static long ToInt64(ulong magnitude, bool isNegative, long value)
	{
		if (isNegative)
		{
			if (magnitude > Int64MinValueMagnitude)
			{
				return value;
			}

			return magnitude == Int64MinValueMagnitude ? long.MinValue : -(long)magnitude;
		}

		return magnitude > long.MaxValue ? value : (long)magnitude;
	}

	public static int ToInt32(long rounded, int value) =>
		rounded is < int.MinValue or > int.MaxValue ? value : (int)rounded;

	public static uint ToUInt32(ulong rounded, uint value) =>
		rounded > uint.MaxValue ? value : (uint)rounded;
}
