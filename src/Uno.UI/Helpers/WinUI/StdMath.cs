#nullable enable

using System;

namespace Uno.UI.Helpers.WinUI;

// C++ std::max / std::min / std::clamp / std::round semantics: comparisons return the first argument
// when a NaN is involved (Math.Max/Min propagate NaN), Clamp never throws when lo > hi (Math.Clamp does),
// and Round rounds half away from zero (Math.Round defaults to banker's rounding).
internal static class StdMath
{
	public static double Max(double a, double b) => (a < b) ? b : a;

	public static double Min(double a, double b) => (b < a) ? b : a;

	public static double Clamp(double v, double lo, double hi) => (v < lo) ? lo : (hi < v) ? hi : v;

	public static double Round(double v) => Math.Round(v, MidpointRounding.AwayFromZero);
}
