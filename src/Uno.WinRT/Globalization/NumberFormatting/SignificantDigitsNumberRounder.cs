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

	public float RoundSingle(float value)
	{
		if (!float.IsFinite(value))
		{
			return float.NaN;
		}

		// WinRT uses binary32 scaling here, with more than eight significant digits left unchanged.
		if (value == 0 || SignificantDigits > 8)
		{
			return value;
		}

		var exponent = (int)MathF.Floor(MathF.Log10(MathF.Abs(value)));
		var decimalPlaces = (int)SignificantDigits - 1 - exponent;
		if (decimalPlaces > 38)
		{
			return Rounder.RoundSingle(value * 1E38f, decimalPlaces - 38, RoundingAlgorithm) / 1E38f;
		}

		return Rounder.RoundSingle(value, decimalPlaces, RoundingAlgorithm);
	}

	public double RoundDouble(double value)
	{
		if (!double.IsFinite(value))
		{
			return double.NaN;
		}

		// Exact zero preserves its sign without treating subnormal inputs as zero.
		if (value == 0 || SignificantDigits > 17)
		{
			return value;
		}

		// Match WinRT's decimal scaling order, including binary rounding at decade boundaries.
		var exponent = (int)Math.Floor(Math.Log10(Math.Abs(value)));
		var decimalPlaces = (int)SignificantDigits - 1 - exponent;
		if (decimalPlaces > 308)
		{
			// Split an otherwise overflowing factor; do not renormalize through a rounded tiny divisor.
			return Rounder.Round(value * 1E308, decimalPlaces - 308, RoundingAlgorithm) / 1E308;
		}

		return Rounder.Round(value, decimalPlaces, RoundingAlgorithm);
	}
}
