#nullable enable

using System;
using System.Linq;
using Uno.Globalization.NumberFormatting;

namespace Windows.Globalization.NumberFormatting;

public partial class IncrementNumberRounder : INumberRounder
{
	private static readonly double[] Exceptions = new double[]
	{
			1E-11,
			1E-12,
			1E-13,
			1E-14,
			1E-15,
			1E-16,
			1E-17,
			1E-18,
			1E-19,
			1E-20,
	};

	private RoundingAlgorithm roundingAlgorithm = RoundingAlgorithm.RoundHalfUp;
	private double increment = 1d;

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

	public double Increment
	{
		get => increment;
		set
		{
			if (value <= 0)
			{
				ExceptionHelper.ThrowArgumentException(nameof(value));
			}
			else if (value <= 0.5)
			{
				if (!Exceptions.Any(e => e == value))
				{
					var inv = (1 / value);
					var n = Math.Truncate(inv);
					if (n < 2 || n > 10000000000)
					{
						ExceptionHelper.ThrowArgumentException(nameof(value));
					}

					var modf = Math.Round(inv % 1, 14, MidpointRounding.AwayFromZero);
					if (modf > 0)
					{
						ExceptionHelper.ThrowArgumentException(nameof(value));
					}
				}
			}
			else if (value < 1)
			{
				ExceptionHelper.ThrowArgumentException(nameof(value));
			}
			else if (Math.Truncate(value) != value)
			{
				ExceptionHelper.ThrowArgumentException(nameof(value));
			}


			increment = value;
		}
	}

	public IncrementNumberRounder()
	{
	}

	public int RoundInt32(int value) => IntegralRounding.ToInt32(RoundInt64(value));

	public uint RoundUInt32(uint value) => IntegralRounding.ToUInt32(RoundUInt64(value));

	public long RoundInt64(long value)
	{
		if (!TryGetIntegralIncrement(out var incrementMagnitude))
		{
			return value;
		}

		var magnitude = IntegralRounding.GetMagnitude(value, out var isNegative);
		var rounded = Rounder.RoundMagnitude(magnitude, incrementMagnitude, isNegative, RoundingAlgorithm);

		return IntegralRounding.ToInt64(rounded, isNegative);
	}

	public ulong RoundUInt64(ulong value) =>
		TryGetIntegralIncrement(out var incrementMagnitude)
			? Rounder.RoundMagnitude(value, incrementMagnitude, false, RoundingAlgorithm)
			: value;

	public float RoundSingle(float value) => (float)RoundDouble(value);

	/// <summary>
	/// Gets the increment as an exact integer magnitude.
	/// </summary>
	/// <remarks>
	/// Increments below 1 are always 1/n (validated by <see cref="Increment"/>), so every integral value
	/// is already a multiple of them and rounding is a no-op. Increments at or beyond the UInt64 range
	/// cannot be represented, so the value is left untouched.
	/// </remarks>
	private bool TryGetIntegralIncrement(out ulong incrementMagnitude)
	{
		if (increment < 1 ||
			increment >= 18446744073709551616d)
		{
			incrementMagnitude = 0;
			return false;
		}

		incrementMagnitude = (ulong)increment;
		return true;
	}

	public double RoundDouble(double value)
	{
		var rounded = value / increment;
		rounded = Rounder.Round(rounded, 0, RoundingAlgorithm);
		rounded *= increment;

		return rounded;
	}
}
