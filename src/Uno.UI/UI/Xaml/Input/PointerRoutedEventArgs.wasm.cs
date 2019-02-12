using System;
using Windows.UI.Input;

namespace Windows.UI.Xaml.Input
{
	public partial class PointerRoutedEventArgs
	{
		public PointerPoint GetCurrentPoint(UIElement relativeTo)
		{
			if (relativeTo == null)
			{
				throw new ArgumentNullException(nameof(relativeTo));
			}

			// Get the point, relative to root element
			var point = GetCurrentPoint();

			// Extract transform matrix from the element
			var transform = relativeTo.TransformToVisual(null).Inverse;

			// Apply the transform matrix to the point
			var transformPoint = transform.TransformPoint(point);

			// that's it
			return new PointerPoint(transformPoint);
		}
	}
}
