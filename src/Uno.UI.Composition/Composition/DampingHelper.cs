using System;

namespace Windows.UI.Composition.Interactions;

// Equations from https://docs.google.com/presentation/d/152lQqvO6ImEGW2k98w-E5Dh8stBeRxBd/edit#slide=id.p51

internal static class DampingHelper
{
	// Settling time is 4 / (zeta * wd)
	public static double SolveUnderdamped(double zeta, double wn, double wd, double t)
	{
		if (zeta >= 1)
		{
			throw new ArgumentException($"Damping ratio '{zeta}' is invalid. It must be less than 1 for underdamped systems.");
		}

		return 1 - Math.Exp(-zeta * wn * t) * (Math.Cos(wd * t) + (zeta / Math.Sqrt(1 - zeta * zeta)) * Math.Sin(wd * t));
	}

	// Ts (settling time) = 5.8335 / wn
	// wn = 5.8335 / Ts
	public static double SolveCriticallyDamped(double wn, double t)
	{
		return 1 - Math.Exp(-wn * t) * (1 + wn * t);
	}
}
