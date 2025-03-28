using System;
using System.Numerics;
using Uno.Disposables;
using Windows.UI.Composition;
using Windows.UI;
using Microsoft/* UWP don't rename */.UI.Xaml.Media;
using System.Collections.Generic;
using Uno;

namespace Windows.UI.Xaml.Media
{
	public partial class Brush
	{
		private protected CompositionBrush _compositionBrush;

		internal delegate void BrushSetterHandler(CompositionBrush brush);

		internal virtual CompositionBrush GetOrCreateCompositionBrush(Compositor compositor)
		{
			if (_compositionBrush is null)
			{
				_compositionBrush = compositor.CreateColorBrush(Colors.Transparent);
				SynchronizeCompositionBrush();
			}

			return _compositionBrush;
		}

		internal virtual void SynchronizeCompositionBrush()
		{
		}

		private protected static void ConvertGradientColorStops(Compositor compositor, CompositionGradientBrush compositionBrush, IEnumerable<GradientStop> gradientStops, double opacity)
		{
			compositionBrush.ColorStops.Clear();

			foreach (var stop in gradientStops)
			{
				compositionBrush.ColorStops.Add(compositor.CreateColorGradientStop((float)stop.Offset, stop.Color.WithOpacity(opacity)));
			}
		}

		private protected static CompositionGradientExtendMode ConvertGradientExtendMode(GradientSpreadMethod spreadMethod)
		{
			switch (spreadMethod)
			{
				case GradientSpreadMethod.Repeat:
					return CompositionGradientExtendMode.Wrap;
				case GradientSpreadMethod.Reflect:
					return CompositionGradientExtendMode.Mirror;
				case GradientSpreadMethod.Pad:
				default:
					return CompositionGradientExtendMode.Clamp;
			}
		}

		private protected static CompositionMappingMode ConvertBrushMappingMode(BrushMappingMode mappingMode)
		{
			switch (mappingMode)
			{
				case BrushMappingMode.Absolute:
					return CompositionMappingMode.Absolute;
				case BrushMappingMode.RelativeToBoundingBox:
				default:
					return CompositionMappingMode.Relative;
			}
		}
	}

	internal static class BrushExtensions
	{
		internal static void TrySetColorFromBrush(this CompositionBrush brush, XamlCompositionBrushBase srcBrush)
		{
			if (brush is CompositionColorBrush colorBrush)
			{
				colorBrush.Color = srcBrush.FallbackColor;
			}
			else if (brush is CompositionBrushWrapper wrapper)
			{
				TrySetColorFromBrush(wrapper.WrappedBrush, srcBrush);
			}
		}
	}
}
