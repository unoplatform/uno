using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;
using MUXControlsTestApp.Utilities;

namespace Uno.UI.RuntimeTests.MUX.Helpers
{
	internal static class InputHelper
	{
		internal static async Task Tap(FrameworkElement element)
		{
			await TapAtPercent(element, 0.5f, 0.5f);
		}

		internal static async Task TapAtPercent(FrameworkElement element, float fractionOfWidth, float fractionOfHeight)
		{
			Point elementCenter = await GetElementPosition(element, fractionOfWidth, fractionOfHeight);

			TapOnPoint(elementCenter);
		}

		internal static void TapOnPoint(Point point)
		{
			// TODO
			throw new NotImplementedException("TapOnPoint not implemented yet");
		}

		internal static async Task<Point> GetElementPosition(FrameworkElement element, float fractionOfWidth, float fractionOfHeight)
		{
			Point point = default;

			await RunOnUIThread.ExecuteAsync(() =>
				{

					double height = 0;
					double width = 0;
					height = element.ActualHeight;
					width = element.ActualWidth;

					if (height == 0.0) throw new InvalidOperationException("Element has no height.");
					if (width == 0.0) throw new InvalidOperationException("Element has no width.");

					// Start with the point at the specified fraction into the element, and then
					// transform that to global coordinates.
					point.X = width * fractionOfWidth;
					point.Y = height * fractionOfHeight;

					var spTransform = element.TransformToVisual(null);
					point = spTransform.TransformPoint(point);
				});

			return point;
		}
	}
}
