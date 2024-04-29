#if __WASM__ || __IOS__ || __MACOS__
#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Uno.UI.Xaml.Media;
using Windows.Foundation.Collections;
using Windows.UI;

namespace Microsoft.UI.Xaml.Media;

public partial class LinearGradientBrush
{
	private Color? _majorStopColor;
	private SolidColorBrush? _fauxOverlayBrush;

	internal Color? MajorStopColor => _majorStopColor ??= GetMajorStop()?.Color.WithOpacity(Opacity);

	internal SolidColorBrush? FauxOverlayBrush => _fauxOverlayBrush ??= CreateFauxOverlayBrush();

	internal bool SupportsFauxGradientBorder() => GradientStops.Count == 2;

	internal override bool CanApplyToBorder(CornerRadius cornerRadius)
	{
#if __WASM__
		return cornerRadius == CornerRadius.None;
#endif

#if __IOS__ || __MACOS__
		return RelativeTransform == null;
#endif
	}

	/// <summary>
	/// Returns major stop of given border gradient.
	/// </summary>
	/// <returns>Gradient stop.</returns>
	internal GradientStop? GetMajorStop()
	{
		if (!SupportsFauxGradientBorder())
		{
			return null;
		}

		var firstStop = GradientStops[0];
		var secondStop = GradientStops[1];

		if (MappingMode == BrushMappingMode.Absolute)
		{
			// When absolute, the major stop is either the one with
			// larger offset or the second stop in order if same.
			return firstStop.Offset <= secondStop.Offset ?
				secondStop : firstStop;
		}
		else
		{
			if (secondStop.Offset < firstStop.Offset)
			{
				(firstStop, secondStop) = (secondStop, firstStop);
			}
			return firstStop.Offset > (1 - secondStop.Offset) ?
				firstStop : secondStop;
		}
	}

	internal VerticalAlignment GetMinorStopAlignment()
	{
		var scaleTransform = RelativeTransform as ScaleTransform;
		return scaleTransform?.ScaleY != -1 ? VerticalAlignment.Top : VerticalAlignment.Bottom;
	}

	private SolidColorBrush? CreateFauxOverlayBrush()
	{
		if (!SupportsFauxGradientBorder())
		{
			return null;
		}

		var majorStop = GetMajorStop();
		var minorStop = GradientStops.First(s => s != majorStop);

		return new SolidColorBrush(minorStop.Color) { Opacity = Opacity };
	}
}
#endif
