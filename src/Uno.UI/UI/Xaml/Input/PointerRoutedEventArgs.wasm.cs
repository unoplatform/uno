using System;
using System.Collections.Generic;
using System.Text;
using Uno;
using Windows.Foundation;
using Windows.UI.Input;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Input
{
	public partial class PointerRoutedEventArgs
	{
		public PointerPoint GetCurrentPoint(UIElement relativeTo)
		{
			var point = GetCurrentPoint();
			var translation = relativeTo.TransformToVisual(null) as TranslateTransform;
			var offset = new Point(point.X - translation.X, point.Y - translation.Y);
			return new PointerPoint(offset);
		}
	}
}
