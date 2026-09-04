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

	public int RoundInt32(int value) => IntegralRounding.ToInt32(RoundInt64(value));

	public uint RoundUInt32(uint value) => IntegralRounding.ToUInt32(RoundUInt64(value));

	public long RoundInt64(long value)
	{
		var magnitude = IntegralRounding.GetMagnitude(value, out var isNegative);
		var rounded = RoundMagnitude(magnitude, isNegative);

		return IntegralRounding.ToInt64(rounded, isNegative);
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

	public float RoundSingle(float value) => (float)RoundFloatingPoint(value, 9);

	public double RoundDouble(double value) => RoundFloatingPoint(value, 17);

	private double RoundFloatingPoint(double value, uint maximumSignificantDigits)
	{
		if (double.IsNaN(value) ||
			double.IsInfinity(value))
		{
			return double.NaN;
		}

		if (value == 0)
		{
			return value;
		}

		if (SignificantDigits > maximumSignificantDigits)
		{
			return value;
		}

		var magnitude = Math.Abs(value);
		var exponent = (int)Math.Floor(Math.Log10(magnitude));
		var scale = Math.Pow(10, exponent);

		if (scale == 0)
		{
			return value;
		}

		if (magnitude < scale)
		{
			exponent--;
			scale /= 10;
		}
		else
		{
			var nextScale = Math.Pow(10, exponent + 1);
			if (nextScale > 0 && magnitude >= nextScale)
			{
				exponent++;
				scale = nextScale;
			}
		}

		var decimalPlaces = (int)SignificantDigits - 1 - exponent;
		if (decimalPlaces is >= -308 and <= 308)
		{
			var factor = Math.Pow(10, decimalPlaces);
			if (double.IsFinite(value * factor))
			{
				return Rounder.Round(value, decimalPlaces, RoundingAlgorithm);
			}
		}

		var normalized = exponent < -308
			? value * 1E308 * Math.Pow(10, -exponent - 308)
			: value / scale;
		var rounded = Rounder.Round(normalized, (int)SignificantDigits - 1, RoundingAlgorithm);
		var result = exponent < -308
			? rounded * Math.Pow(10, exponent + 308) * 1E-308
			: rounded * scale;

		return RoundingAlgorithm switch
		{
			RoundingAlgorithm.RoundDown when result > value => Math.BitDecrement(result),
			RoundingAlgorithm.RoundUp when result < value => Math.BitIncrement(result),
			RoundingAlgorithm.RoundTowardsZero when value > 0 && result > value => Math.BitDecrement(result),
			RoundingAlgorithm.RoundTowardsZero when value < 0 && result < value => Math.BitIncrement(result),
			RoundingAlgorithm.RoundAwayFromZero when value > 0 && result < value => Math.BitIncrement(result),
			RoundingAlgorithm.RoundAwayFromZero when value < 0 && result > value => Math.BitDecrement(result),
			_ => result,
		};
	}
}
