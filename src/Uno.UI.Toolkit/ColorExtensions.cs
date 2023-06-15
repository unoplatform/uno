#nullable enable

using System;
using System.Linq;
using Windows.UI;

namespace Uno.Helpers;

internal static class ColorExtensions
{
	/// <summary>
	/// Returns the color that results from blending the color with the given background color.
	/// </summary>
	/// <param name="color">The color to blend.</param>
	/// <param name="background">The background color to use. This is assumed to be opaque (not checked for perf reason when used on pixel buffer).</param>
	/// <returns>The color that results from blending the color with the given background color.</returns>
	internal static Color ToOpaque(this Color color, Color background)
		=> new(
			255,
			(byte)(((byte.MaxValue - color.A) * background.R + color.A * color.R) / 255),
			(byte)(((byte.MaxValue - color.A) * background.G + color.A * color.G) / 255),
			(byte)(((byte.MaxValue - color.A) * background.B + color.A * color.B) / 255)
		);
}
