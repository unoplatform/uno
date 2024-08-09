using Windows.Foundation;
using Uno.UI.Extensions;

#if __IOS__ || __ANDROID__

namespace Windows.UI.Xaml.Controls
{
	// This file only contains support of NativeScrollContentPresenter

	partial class ScrollContentPresenter : IFrameworkTemplatePoolAware
	{
		internal INativeScrollContentPresenter Native { get; set; }

		private object RealContent => Native?.Content;

		public void OnTemplateRecycled()
		{
			// If the template is being recycled and it's TemplatedParent is null, this means that
			// this instance was part of the ControlTemplate of a ScrollViewer instance that was GC'ed.
			// We need to ensure that we're not keeping any content coming from ScrollView.Content.
			if (TemplatedParent is null && Native is { })
			{
				Native.Content = null;
				Native = null;
			}
		}

		protected internal override void OnTemplatedParentChanged(DependencyPropertyChangedEventArgs e)
		{
			// Clear the native content, in case this instance is being explicitly removed from its templated parent.
			// Note that in case the template is being recycled, the parent is not explicitly removed, but rather
			// relies on weak references, and this method is not called.
			if (e.NewValue is null)
			{
				Native.Content = null;
			}

			base.OnTemplatedParentChanged(e);
		}

		#region SCP to Native SCP
#pragma warning disable CA1044 // Properties should not be write only
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
#pragma warning restore CA1044 // Properties should not be write only

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

		public double ExtentHeight
		{
			get => Native?.ExtentHeight ?? 0;
			set
			{
				if (Native is { } native)
				{
					native.ExtentHeight = 0;
				}
			}
		}

		public double ExtentWidth
		{
			get => Native?.ExtentWidth ?? 0;
			set
			{
				if (Native is { } native)
				{
					native.ExtentWidth = 0;
				}
			}
		}
		#endregion

		#region Native SCP to SCP
		internal void OnNativeScroll(double horizontalOffset, double verticalOffset, bool isIntermediate)
		{
			ScrollOffsets = new Point(horizontalOffset, verticalOffset);

			Scroller?.OnPresenterScrolled(horizontalOffset, verticalOffset, isIntermediate);

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
