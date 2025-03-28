#if __WASM__ || __IOS__ || __MACOS__
#nullable enable

using System;
using Windows.UI;

namespace Windows.UI.Xaml.Media;

public partial class LinearGradientBrush
{
	private SolidColorBrush? _fauxOverlayBrush;

	internal SolidColorBrush? FauxOverlayBrush => _fauxOverlayBrush ??= CreateFauxOverlayBrush();

	internal bool SupportsFauxGradientBorder =>
		GradientStops.Count == 2 &&
		(GradientStops[0].Color == FallbackColor || GradientStops[1].Color == FallbackColor);

	internal override bool CanApplyToBorder(CornerRadius cornerRadius)
	{
#if __WASM__ // On WASM, linear gradient borders work only if there is no CornerRadius applied to the control
		return cornerRadius == CornerRadius.None;
#elif __IOS__ || __MACOS__ // On iOS and macOS, we can apply linear gradient borders reliably only when there is no RelativeTransform applied
		return RelativeTransform == null;
#else
		throw new NotSupportedException("This target does not have a LinearGradientBrush.CanApplyToBorder check yet.");
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
#endif
