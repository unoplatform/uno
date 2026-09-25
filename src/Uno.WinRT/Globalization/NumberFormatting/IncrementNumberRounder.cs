#nullable enable

using System;
using System.Linq;
using Uno.Globalization.NumberFormatting;

namespace Windows.Globalization.NumberFormatting;

public partial class IncrementNumberRounder : INumberRounder
{
	private const double TwoPow64 = 18446744073709551616d;

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
		if (increment < 1)
		{
			return value;
		}

		var magnitude = IntegralRounding.GetMagnitude(value, out var isNegative);

		if (!TryGetIntegralIncrement(out var incrementMagnitude))
		{
			return IntegralRounding.ToInt64(RoundWithOversizedIncrement(magnitude, isNegative), isNegative);
		}

		var rounded = Rounder.RoundMagnitude(magnitude, incrementMagnitude, isNegative, RoundingAlgorithm);

		return IntegralRounding.ToInt64(rounded, isNegative);
	}

	public ulong RoundUInt64(ulong value)
	{
		if (increment < 1)
		{
			return value;
		}

		return TryGetIntegralIncrement(out var incrementMagnitude)
			? Rounder.RoundMagnitude(value, incrementMagnitude, false, RoundingAlgorithm)
			: RoundWithOversizedIncrement(value, false);
	}

	public float RoundSingle(float value)
	{
		if (increment > float.MaxValue)
		{
			return (float)RoundDouble(value);
		}

		var singleIncrement = (float)increment;
		var rounded = (float)Rounder.Round(value / singleIncrement, 0, RoundingAlgorithm);

		return rounded * singleIncrement;
	}

	/// <summary>
	/// Gets the increment as an exact integer magnitude.
	/// </summary>
	/// <remarks>
	/// Increments below 1 are always 1/n (validated by <see cref="Increment"/>), so every integral value
	/// is already a multiple of them and rounding is a no-op.
	/// </remarks>
	private bool TryGetIntegralIncrement(out ulong incrementMagnitude)
	{
		if (increment >= TwoPow64)
		{
			incrementMagnitude = 0;
			return false;
		}

		incrementMagnitude = (ulong)increment;
		return true;
	}

	private ulong RoundWithOversizedIncrement(ulong magnitude, bool isNegative)
	{
		if (magnitude == 0)
		{
			return 0;
		}

		var roundsAwayFromZero = RoundingAlgorithm switch
		{
			RoundingAlgorithm.RoundDown => isNegative,
			RoundingAlgorithm.RoundUp => !isNegative,
			RoundingAlgorithm.RoundTowardsZero => false,
			RoundingAlgorithm.RoundAwayFromZero => true,
			_ => RoundsNearestAwayFromZero(magnitude, isNegative),
		};

		if (roundsAwayFromZero)
		{
			ExceptionHelper.ThrowArithmeticException();
		}

		return 0;
	}

	private bool RoundsNearestAwayFromZero(ulong magnitude, bool isNegative)
	{
		var midpoint = increment / 2;
		if (midpoint >= TwoPow64)
		{
			return false;
		}

		var integralMidpoint = (ulong)midpoint;
		if (magnitude != integralMidpoint)
		{
			return magnitude > integralMidpoint;
		}

		return RoundingAlgorithm switch
		{
			RoundingAlgorithm.RoundHalfDown => isNegative,
			RoundingAlgorithm.RoundHalfUp => !isNegative,
			RoundingAlgorithm.RoundHalfTowardsZero => false,
			RoundingAlgorithm.RoundHalfToEven => false,
			RoundingAlgorithm.RoundHalfToOdd => true,
			_ => true,
		};
	}

	public double RoundDouble(double value)
	{
		var rounded = value / increment;
		rounded = Rounder.Round(rounded, 0, RoundingAlgorithm);
		rounded *= increment;

		return rounded;
	}
}
