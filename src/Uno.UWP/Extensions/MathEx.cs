using System;

namespace Uno.Extensions
{
	internal static class MathEx
	{
		/// <summary>
		/// Converts an angle in degree into radians
		/// </summary>
		public static double ToRadians(double angleDegree)
			=> angleDegree * Math.PI / 180.0;

		/// <summary>
		/// Converts an angle in radians into degrees
		/// </summary>
		public static double ToDegree(double angleRadian)
			=> angleRadian * 180.0 / Math.PI;

		/// <summary>
		/// Converts an angle in radians into degrees normalized in the [0, 360[ range.
		/// </summary>
		public static double ToDegreeNormalized(double angleRadian)
			=> angleRadian >= 0
				? ToDegree(angleRadian) % 360
				: 360 + ToDegree(angleRadian) % 360;

		/// <summary>
		/// Tests if two values are equal to within the specified error.
		/// </summary>
		/// <param name="value1">First value</param>
		/// <param name="value2">Second value</param>
		/// <param name="delta">Permitted error</param>
		/// <returns>True if the difference is less than the permitted error, false otherwise</returns>
		public static bool ApproxEqual(double value1, double value2, double delta = 1e-9)
			=> Math.Abs(value1 - value2) < delta;

		/// <summary>
		/// Returns the maximum of two nullable doubles if both have values. Returns null if either or both are null.
		/// </summary>
		public static double? Max(double? val1, double? val2)
		{
			if (val1 is { } v1 && val2 is { } v2)
			{
				return Math.Max(v1, v2);
			}

			return null;
		}
	}
}
