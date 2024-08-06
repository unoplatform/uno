#nullable enable

using System;
using Windows.UI;

namespace Microsoft.UI.Composition;

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
			var progress = (Visual.Compositor.TimestampInTicks - StartTimestamp) / (double)(EndTimestamp - StartTimestamp);

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
