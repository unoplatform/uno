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

	public double RoundDouble(double value)
	{
		var rounded = value / increment;
		rounded = Rounder.Round(rounded, 0, RoundingAlgorithm);
		rounded *= increment;

		return rounded;
	}
}
