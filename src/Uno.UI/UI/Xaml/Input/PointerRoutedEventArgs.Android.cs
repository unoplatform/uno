using System;
using System.Collections.Generic;
using System.Text;
using Android.Views;
using Uno.UI;
using Windows.Foundation;
using Windows.UI.Input;

namespace Windows.UI.Xaml.Input
{
	public partial class PointerRoutedEventArgs
	{
		private readonly MotionEvent _nativeEvent;

		internal PointerRoutedEventArgs(MotionEvent nativeEvent)
		{
			_nativeEvent = nativeEvent;
			CanBubbleNatively = true;
		}

		public PointerPoint GetCurrentPoint(UIElement relativeTo)
		{
			int xOrigin = 0, yOrigin = 0; 
			if (relativeTo != null)
			{
				var viewCoords = new int[2];
				relativeTo.GetLocationInWindow(viewCoords);
				xOrigin = viewCoords[0];
				yOrigin = viewCoords[1];
			}

			var physicalPoint = new Point(_nativeEvent.RawX - xOrigin, _nativeEvent.RawY - yOrigin);

			return new PointerPoint(physicalPoint.PhysicalToLogicalPixels());
		}
	}
}
