using Windows.Foundation;
using Windows.UI.Xaml;

namespace Uno.UI.UI.Xaml.Input.Internal
{
	internal static class ManipulationEventArgsHelpers
	{
		internal static Point MapPointRelativeTo(UIElement element, Point point)
			=> element is null
				? point
				: element.TransformToVisual(null).Inverse.TransformPoint(point);
	}
}
