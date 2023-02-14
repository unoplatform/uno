using System;
using System.Linq;
using Uno.UI.Behaviors;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Uno.UI.Xaml.Media
{
	/// <summary>
	/// Helpers used specifically for WinUI 2.6 border gradients.
	/// </summary>
	internal static class BorderGradientBrushHelper
	{
		internal static bool CanApplySolidColorRendering(LinearGradientBrush brush) => brush.GradientStops.Count == 2;

		/// <summary>
		/// Returns major stop of given border gradient.
		/// </summary>
		/// <returns>Gradient stop.</returns>
		internal static GradientStop GetMajorStop(LinearGradientBrush brush)
		{
			if (brush.GradientStops.Count != 2)
			{
				return null;
			}

			var firstStop = brush.GradientStops[0];
			var secondStop = brush.GradientStops[1];

			if (brush.MappingMode == BrushMappingMode.Absolute)
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

		internal static VerticalAlignment GetMinorStopAlignment(LinearGradientBrush brush)
		{
			var scaleTransform = brush.RelativeTransform as ScaleTransform;
			return scaleTransform?.ScaleY != -1 ?
					VerticalAlignment.Top : VerticalAlignment.Bottom;
		}
	}
}
