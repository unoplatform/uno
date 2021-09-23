using Windows.Foundation;
using Uno.UI.Extensions;

#if !UNO_HAS_MANAGED_SCROLL_PRESENTER && !__WASM__

namespace Windows.UI.Xaml.Controls
{
	// This file only contains support of NativeScrollContentPresenter

	partial class ScrollContentPresenter
	{
		internal INativeScrollContentPresenter Native { get; set; }

		private object RealContent => Native?.Content;

		#region SCP to Native SCP
		public ScrollBarVisibility NativeHorizontalScrollBarVisibility
		{
			set
			{
				if (Native is { } native)
				{
					native.HorizontalScrollBarVisibility = value;
				}
			}
		}

		public ScrollBarVisibility NativeVerticalScrollBarVisibility
		{
			set
			{
				if (Native is { } native)
				{
					native.VerticalScrollBarVisibility = value;
				}
			}
		}

		public bool CanHorizontallyScroll
		{
			get => Native?.CanHorizontallyScroll ?? false;
			set
			{
				if (Native is { } native)
				{
					native.CanHorizontallyScroll = value;
				}
			}
		}

		public bool CanVerticallyScroll
		{
			get => Native?.CanVerticallyScroll ?? false;
			set
			{
				if (Native is { } native)
				{
					native.CanVerticallyScroll = value;
				}
			}
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
#endif
