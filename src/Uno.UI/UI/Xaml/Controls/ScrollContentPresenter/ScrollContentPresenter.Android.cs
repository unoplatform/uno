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
		public ScrollContentPresenter()
		{
			PointerMoved += OnPointerMoved;

			RegisterAsScrollPort(this);
		}

		private void OnPointerMoved(object sender, Input.PointerRoutedEventArgs e)
		{
			if (e.Pointer.PointerDeviceType == PointerDeviceType.Touch)
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

		public Rect MakeVisible(UIElement visual, Rect rectangle)
		{
			if (visual is FrameworkElement fe && !(Native is null))
			{
				var scrollRect = new Rect(
					_occludedRectPadding.Left,
					_occludedRectPadding.Top,
					ActualWidth - _occludedRectPadding.Right,
					ActualHeight - _occludedRectPadding.Bottom
				);

				var visualPoint = UIElement.TransformToVisual(visual, this).TransformPoint(new Point());
				var visualRect = new Rect(visualPoint, new Size(fe.ActualWidth, fe.ActualHeight));

				var deltaX = Math.Min(visualRect.Left - scrollRect.Left, Math.Max(0, visualRect.Right - scrollRect.Right));
				var deltaY = Math.Min(visualRect.Top - scrollRect.Top, Math.Max(0, visualRect.Bottom - scrollRect.Bottom));

				Native.SmoothScrollBy(
					ViewHelper.LogicalToPhysicalPixels(deltaX),
					ViewHelper.LogicalToPhysicalPixels(deltaY)
				);
			}

			return rectangle;
		}

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

		#region Native SCP to SCP
		internal void OnNativeScroll(double horizontalOffset, double verticalOffset, bool isIntermediate)
		{
			Scroller?.OnPresenterScrolled(horizontalOffset, verticalOffset, isIntermediate);

			ScrollOffsets = new Point(horizontalOffset, verticalOffset);
			InvalidateViewport();
		}

		internal void OnNativeZoom(float zoomFactor)
		{
			Scroller?.OnPresenterZoomed(zoomFactor);
		} 
		#endregion
	}
}
