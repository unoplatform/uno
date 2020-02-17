using System;
using System.Linq;

namespace Uno.Extensions
{
	internal static class MathEx
	{
		/// <summary>
		/// Clamp a value to lie within a supplied range.
		/// </summary>
		/// <param name="value">The value to clamp.</param>
		/// <param name="min">The minimum allowed value (inclusive).</param>
		/// <param name="max">The maximum allowed value (inclusive).</param>
		/// <returns>A clamped value.</returns>
		public static int Clamp(int value, int min, int max)
		{
			return Math.Min(Math.Max(value, min), max);
		}

		/// <summary>
		/// Clamp a value to lie within a supplied range.
		/// </summary>
		/// <param name="value">The value to clamp.</param>
		/// <param name="min">The minimum allowed value (inclusive).</param>
		/// <param name="max">The maximum allowed value (inclusive).</param>
		/// <returns>A clamped value.</returns>
		public static double Clamp(double value, double min, double max)
		{
			if (value < min)
			{
				return min;
			}

			if (value > max)
			{
				return max;
			}

			return value;
		}

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
		/// Normalize an angle in degrees in the [0, 360[ range.
		/// </summary>
		public static double NormalizeDegree(double angleDegree)
			=> angleDegree >= 0
				? angleDegree % 360
				: 360 + angleDegree % 360;

		/// <summary>
		/// Tests if two values are equal to within the specified error.
		/// </summary>
		/// <param name="value1">First value</param>
		/// <param name="value2">Second value</param>
		/// <param name="delta">Permitted error</param>
		/// <returns>True if the difference is less than the permitted error, false otherwise</returns>
		public static bool ApproxEqual(double value1, double value2, double delta = 1e-9)
			=> Math.Abs(value1 - value2) < delta;
	}
}
