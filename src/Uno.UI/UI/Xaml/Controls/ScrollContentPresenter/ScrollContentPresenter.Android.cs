using System;
using System.Collections.Generic;
using System.Text;
using Windows.Devices.Input;
using Windows.Foundation;
using Uno.Disposables;
using Uno.UI;

namespace Windows.UI.Xaml.Controls
{
	partial class ScrollContentPresenter
	{
		partial void InitializePartial()
		{
			PointerMoved += OnPointerMoved;
		}

		private void OnPointerMoved(object sender, Input.PointerRoutedEventArgs e)
		{
			if ((PointerDeviceType)e.Pointer.PointerDeviceType == PointerDeviceType.Touch)
			{
				// Prevent PointerMoved being called on parent, as on UWP.
				// Note: We only want to do this if the ScrollViewer is actually scrollable (in any direction), but at present on Android
				// PointerMoved is only raised if content is scrollable - presumably because it's in this case that the inner
				// NativeScrollContentPresenter 'blocks' the touch according to Uno's pointer logic.
				e.Handled = true;
			}
		}

		#region SCP to Native SCP
		private Thickness _oldNativePadding;
		private Thickness _occludedRectPadding;

		internal IDisposable Pad(Rect occludedRect)
		{
			var viewPortPoint = UIElement.TransformToVisual(this, null).TransformPoint(new Point());
			var viewPortSize = new Size(ActualWidth, ActualHeight);
			var viewPortRect = new Rect(viewPortPoint, viewPortSize);
			var intersection = viewPortRect;
			intersection.Intersect(occludedRect);

			if (intersection.IsEmpty)
			{
				SetOccludedRectPadding(new Thickness());
			}
			else
			{
				_oldNativePadding = Native.Padding;
				SetOccludedRectPadding(new Thickness(_oldNativePadding.Left, _oldNativePadding.Top, _oldNativePadding.Right, intersection.Height));
			}

			return Disposable.Create(() => SetOccludedRectPadding(new Thickness()));
		}

		private void SetOccludedRectPadding(Thickness occludedRectPadding)
		{
			_occludedRectPadding = occludedRectPadding;
			Native.Padding = occludedRectPadding;
		}
		#endregion
	}
}
