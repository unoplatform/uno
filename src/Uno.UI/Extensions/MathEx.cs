using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.UI
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
		/// Converts an angle in degree into radians
		/// </summary>
		public static double ToRadians(double angleDegree) 
			=> (Math.PI / 180.0) * angleDegree;

		/// <summary>
		/// Converts an angle in radians into degrees
		/// </summary>
		public static double ToDegree(double angleRadian)
			=> angleRadian / (Math.PI / 180.0);
	}
}
