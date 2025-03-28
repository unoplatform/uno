#nullable enable

using System;
using Windows.UI;

namespace Windows.UI.Composition;

/// <param name="IsActive">If false, the transition is "disabled" and the <see cref="CurrentColor"/> of the transition won't be used.</param>
internal readonly record struct ColorBrushTransitionState(
	BorderVisual Visual,
	Color FromColor,
	Color ToColor,
	long StartTimestamp,
	long EndTimestamp,
	bool IsActive)
{
	internal Color CurrentColor
	{
		get
		{
			var progress = Math.Min(1, (Visual.Compositor.TimestampInTicks - StartTimestamp) / (double)(EndTimestamp - StartTimestamp));

			var a = Lerp(FromColor.A, ToColor.A, progress);
			var r = Lerp(FromColor.R, ToColor.R, progress);
			var g = Lerp(FromColor.G, ToColor.G, progress);
			var b = Lerp(FromColor.B, ToColor.B, progress);

			return new Color(a, r, g, b);

			byte Lerp(byte start, byte end, double progress)
				=> (byte)(((end - start) * progress) + start);
		}
	}
}
