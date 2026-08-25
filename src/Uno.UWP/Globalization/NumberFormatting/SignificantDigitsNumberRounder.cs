#nullable enable

using System;
using Uno.Globalization.NumberFormatting;

namespace Windows.Globalization.NumberFormatting;

public partial class SignificantDigitsNumberRounder : INumberRounder
{
	private uint significantDigits = 1;
	private RoundingAlgorithm roundingAlgorithm = RoundingAlgorithm.RoundHalfUp;

	public uint SignificantDigits
	{
		get => significantDigits;
		set
		{
			if (value == 0)
			{
				ExceptionHelper.ThrowArgumentException(nameof(value));
			}

			significantDigits = value;
		}
	}

	public RoundingAlgorithm RoundingAlgorithm
	{
		get => roundingAlgorithm;
		set
		{
			if (value == RoundingAlgorithm.None)
			{
				ExceptionHelper.ThrowArgumentException(nameof(value));
			}

			roundingAlgorithm = value;
		}
	}

	public SignificantDigitsNumberRounder()
	{
	}

	public int RoundInt32(int value) => IntegralRounding.ToInt32(RoundInt64(value), value);

	public uint RoundUInt32(uint value) => IntegralRounding.ToUInt32(RoundUInt64(value), value);

	public long RoundInt64(long value)
	{
		var magnitude = IntegralRounding.GetMagnitude(value, out var isNegative);
		var rounded = RoundMagnitude(magnitude, isNegative);

		return IntegralRounding.ToInt64(rounded, isNegative, value);
	}

	public ulong RoundUInt64(ulong value) => RoundMagnitude(value, false);

	private ulong RoundMagnitude(ulong magnitude, bool isNegative)
	{
		var digitCount = Rounder.GetDigitCount(magnitude);

		if (digitCount <= SignificantDigits)
		{
			return magnitude;
		}

		var increment = Rounder.GetPowerOfTen(digitCount - (int)SignificantDigits);

		return Rounder.RoundMagnitude(magnitude, increment, isNegative, RoundingAlgorithm);
	}

	public float RoundSingle(float value)
	{
		return (float)Math.Round(value, (int)SignificantDigits, MidpointRounding.AwayFromZero);
	}

	public double RoundDouble(double value)
	{
		if (double.IsNaN(value) ||
			double.IsInfinity(value))
		{
			return double.NaN;
		}

		var integerPart = (int)Math.Truncate(value);
		var integerPartLength = (uint)integerPart.GetLength();
		var diffLength = SignificantDigits - integerPartLength;

		if (SignificantDigits < integerPartLength)
		{
			diffLength = integerPartLength - SignificantDigits;
			var pow10 = Math.Pow(10, diffLength);
			value /= pow10;
			value = Rounder.Round(value, 0, RoundingAlgorithm);
			value *= pow10;
			return value;
		}

		return Rounder.Round(value, (int)diffLength, RoundingAlgorithm);
	}
}
