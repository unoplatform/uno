#nullable enable

using Windows.UI;

namespace Windows.UI.Composition;

internal sealed class ColorBrushTransitionState
{
	internal ColorBrushTransitionState(BorderVisual visual, Color fromColor, Color toColor, long startTimestamp, long endTimestamp)
	{
		Visual = visual;
		FromColor = fromColor;
		ToColor = toColor;
		StartTimestamp = startTimestamp;
		EndTimestamp = endTimestamp;
	}

	internal BorderVisual Visual { get; }
	internal Color FromColor { get; }
	internal Color ToColor { get; }
	internal long StartTimestamp { get; }
	internal long EndTimestamp { get; }

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
