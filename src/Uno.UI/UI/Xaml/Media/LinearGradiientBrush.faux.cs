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

	/// <summary>
	/// Gets or sets a flag indicating whether this brush should use
	/// major gradient solid brush color in case the target platform
	/// cannot apply the linear gradient to border correctly.
	/// </summary>
	public bool SupportsFauxBorder
	{
		get => (bool)GetValue(SupportsFauxBorderProperty);
		set => SetValue(SupportsFauxBorderProperty, value);
	}

	/// <summary>
	/// Identifies the SupportsFauxBorder dependency property.
	/// </summary>
	public static DependencyProperty SupportsFauxBorderProperty { get; } =
		DependencyProperty.Register(
			nameof(SupportsFauxBorder),
			typeof(bool),
			typeof(LinearGradientBrush),
			new FrameworkPropertyMetadata(false));

	internal Color? MajorStopColor => _majorStopColor ??= GetMajorStop()?.Color.WithOpacity(Opacity);

	internal SolidColorBrush? FauxOverlayBrush => _fauxOverlayBrush ??= CreateFauxOverlayBrush();

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

	/// <summary>
	/// Returns major stop of given border gradient.
	/// </summary>
	/// <returns>Gradient stop.</returns>
	internal GradientStop? GetMajorStop()
	{
		if (!SupportsFauxBorder)
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
		var majorStopFirst = GetMajorStop()?.Offset < GradientStops.Where(s => s != GetMajorStop()).First().Offset;
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
		if (!SupportsFauxBorder)
		{
			return null;
		}

		var majorStop = GetMajorStop()!;
		var minorStop = GradientStops.First(s => s != majorStop)!;

		// As both major and minor colors may be semi-transparent, we need
		// to calculate an approximate color, where minor = major + faux if possible
		var faux = Color.FromArgb(
			(byte)(Math.Max(0, minorStop.Color.A - majorStop.Color.A)),
			(byte)(Math.Max(0, minorStop.Color.R - majorStop.Color.R)),
			(byte)(Math.Max(0, minorStop.Color.G - majorStop.Color.G)),
			(byte)(Math.Max(0, minorStop.Color.B - majorStop.Color.B)));

		return new SolidColorBrush(faux) { Opacity = Opacity };
	}
}
