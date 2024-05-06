#nullable enable

using System;
using Windows.UI;

namespace Microsoft.UI.Xaml.Media;

public partial class LinearGradientBrush
{
	private SolidColorBrush? _fauxOverlayBrush;

	internal SolidColorBrush? FauxOverlayBrush => _fauxOverlayBrush ??= CreateFauxOverlayBrush();

	internal bool SupportsFauxGradientBorder => GradientStops.Count == 2;

	internal override bool CanApplyToBorder(CornerRadius cornerRadius)
	{
#if __WASM__
		return cornerRadius == CornerRadius.None;
#elif __IOS__ || __MACOS__
		return RelativeTransform == null;
#else
		return true;
#endif
	}

	private (GradientStop minor, GradientStop major) GetMinorMajorStops()
	{
		if (!SupportsFauxGradientBorder)
		{
			throw new InvalidOperationException("Method should not be called when faux gradient border is not supported.");
		}

		var fallbackColor = FallbackColor;
		var firstStop = GradientStops[0];
		var secondStop = GradientStops[1];

		return secondStop.Color == fallbackColor ? (firstStop, secondStop) : (secondStop, firstStop);
	}

	internal VerticalAlignment GetMinorStopAlignment()
	{
		if (!SupportsFauxGradientBorder)
		{
			return default;
		}

		var scaleTransform = RelativeTransform as ScaleTransform;
		var (minor, major) = GetMinorMajorStops();
		var majorStopFirst = major.Offset < minor.Offset;
		if (majorStopFirst)
		{
			return scaleTransform?.ScaleY != -1 ? VerticalAlignment.Bottom : VerticalAlignment.Top;
		}
		else
		{
			return scaleTransform?.ScaleY != -1 ? VerticalAlignment.Top : VerticalAlignment.Bottom;
		}
	}

	private SolidColorBrush? CreateFauxOverlayBrush()
	{
		if (!SupportsFauxGradientBorder)
		{
			return null;
		}

		(var minorStop, var majorStop) = GetMinorMajorStops();

		var fauxColor = minorStop.Color;

		// If minor color is semi-transparent, we calculate
		// an approximate color, where minor = major + faux
		if (minorStop.Color.A < 255)
		{
			fauxColor = Color.FromArgb(
				(byte)(Math.Max(0, minorStop.Color.A - majorStop.Color.A)),
				minorStop.Color.R,
				minorStop.Color.G,
				minorStop.Color.B);
		}

		return new SolidColorBrush(fauxColor) { Opacity = Opacity };
	}
}
