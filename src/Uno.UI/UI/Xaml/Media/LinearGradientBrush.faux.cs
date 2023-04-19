#if __WASM__ || __IOS__ || __MACOS__
#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Uno.UI.Xaml.Media;
using Windows.Foundation.Collections;

namespace Windows.UI.Xaml.Media;

public partial class LinearGradientBrush
{
	private SolidColorBrush? _fauxOverlayBrush;

	internal SolidColorBrush? FauxOverlayBrush => _fauxOverlayBrush ??= CreateFauxOverlayBrush();

	internal static bool CanApplySolidColorRendering() => brush.GradientStops.Count == 2;

	/// <summary>
	/// Returns major stop of given border gradient.
	/// </summary>
	/// <returns>Gradient stop.</returns>
	internal GradientStop? GetMajorStop()
	{
		if (!CanApplySolidColorRendering())
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
		var scaleTransform = brush.RelativeTransform as ScaleTransform;
		return scaleTransform?.ScaleY != -1 ?
				VerticalAlignment.Top : VerticalAlignment.Bottom;
	}

	private SolidColorBrush? CreateFauxOverlayBrush()
	{
		if (!CanApplySolidColorRendering())
		{
			return null;
		}

		var majorStop = GetMajorStop();
		var minorStop = GradientStops.First(s => s != majorStop);

		return new SolidColorBrush(minorStop.Color);
	}
}
#endif
