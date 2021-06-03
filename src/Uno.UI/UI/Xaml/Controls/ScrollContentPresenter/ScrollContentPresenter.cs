using Uno.Extensions;
using Uno.Logging;
using Uno.UI.DataBinding;
using Windows.UI.Xaml.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using System.Text;
using Windows.Foundation;
#if XAMARIN_ANDROID
using View = Android.Views.View;
using Font = Android.Graphics.Typeface;
#elif XAMARIN_IOS_UNIFIED
using UIKit;
using View = UIKit.UIView;
using Color = UIKit.UIColor;
using Font = UIKit.UIFont;
#elif __MACOS__
using AppKit;
using View = AppKit.NSView;
using Color = AppKit.NSColor;
using Font = AppKit.NSFont;
#else
using View = Windows.UI.Xaml.UIElement;
#endif

namespace Windows.UI.Xaml.Controls
{
	public partial class ScrollContentPresenter : ContentPresenter, ILayoutConstraints
	{
		#region ScrollOwner
		private ManagedWeakReference _scroller;

		public object ScrollOwner
		{
			get => _scroller.Target;
			set
			{
				if (_scroller is { } oldScroller)
				{
					WeakReferencePool.ReturnWeakReference(this, oldScroller);
				}

				_scroller = WeakReferencePool.RentWeakReference(this, value);
			}
		}
		#endregion

		private void InitializeScrollContentPresenter()
		{
			this.RegisterParentChangedCallback(this, OnParentChanged);
		}

		private void OnParentChanged(object instance, object key, DependencyObjectParentChangedEventArgs args)
		{
			// Set Content to null when we are removed from TemplatedParent. Otherwise Content.RemoveFromSuperview() may
			// be called when it is attached to a different view.
			if (args.NewParent == null)
			{
				Content = null;
			}
		}

#if __IOS__ || __ANDROID__
		private NativeScrollContentPresenter Native => Content as NativeScrollContentPresenter;
		private object RealContent => Native?.Content;
		public ScrollBarVisibility HorizontalScrollBarVisibility => Native?.HorizontalScrollBarVisibility ?? default;
		public ScrollBarVisibility VerticalScrollBarVisibility => Native?.VerticalScrollBarVisibility ?? default;
		public bool CanHorizontallyScroll
		{
			get => HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled;
			set { }
		}

		public bool CanVerticallyScroll
		{
			get => VerticalScrollBarVisibility != ScrollBarVisibility.Disabled;
			set { }
		}
#endif

		bool ILayoutConstraints.IsWidthConstrained(View requester)
		{
			if (requester != null && CanHorizontallyScroll)
			{
				return false;
			}

			return this.IsWidthConstrainedSimple() ?? (Parent as ILayoutConstraints)?.IsWidthConstrained(this) ?? false;
		}

		bool ILayoutConstraints.IsHeightConstrained(View requester)
		{
			if (requester != null && CanVerticallyScroll)
			{
				return false;
			}

			return this.IsHeightConstrainedSimple() ?? (Parent as ILayoutConstraints)?.IsHeightConstrained(this) ?? false;
		}

		public double ExtentHeight
		{
			get
			{
				if (Content is FrameworkElement fe)
				{
					var explicitHeight = fe.Height;
					if (!explicitHeight.IsNaN())
					{
						return explicitHeight;
					}
					var canUseActualHeightAsExtent =
						ActualHeight > 0 &&
						fe.VerticalAlignment == VerticalAlignment.Stretch;

					return canUseActualHeightAsExtent ? fe.ActualHeight : fe.DesiredSize.Height;
				}

				return 0d;
			}
		}

		public double ExtentWidth
		{
			get
			{
				if (Content is FrameworkElement fe)
				{
					var explicitWidth = fe.Width;
					if (!explicitWidth.IsNaN())
					{
						return explicitWidth;
					}

					var canUseActualWidthAsExtent =
						ActualWidth > 0 &&
						fe.HorizontalAlignment == HorizontalAlignment.Stretch;

					return canUseActualWidthAsExtent ? fe.ActualWidth : fe.DesiredSize.Width;
				}

				return 0d;
			}
		}

		public double ViewportHeight => DesiredSize.Height;

		public double ViewportWidth => DesiredSize.Width;

#if UNO_HAS_MANAGED_SCROLL_PRESENTER || __WASM__
		protected override Size MeasureOverride(Size size)
		{
			if (Content is UIElement child)
			{
				var slotSize = size;

				if (CanVerticallyScroll)
				{
					slotSize.Height = double.PositiveInfinity;
				}
				if (CanHorizontallyScroll)
				{
					slotSize.Width = double.PositiveInfinity;
				}

				child.Measure(slotSize);

				var desired = child.DesiredSize;

				// Give opportunity to the the content to define the viewport size itself
				(child as ICustomScrollInfo)?.ApplyViewport(ref desired);

				return new Size(
					Math.Min(size.Width, desired.Width),
					Math.Min(size.Height, desired.Height)
				);
			}

			return new Size(0, 0);
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			if (Content is UIElement child)
			{
				Rect childRect = default;

				var desiredSize = child.DesiredSize;

				childRect.Width = Math.Max(finalSize.Width, desiredSize.Width);
				childRect.Height = Math.Max(finalSize.Height, desiredSize.Height);

				child.Arrange(childRect);

				// Give opportunity to the the content to define the viewport size itself
				(child as ICustomScrollInfo)?.ApplyViewport(ref finalSize);
			}

			return finalSize;
		}

		internal override bool IsViewHit()
			=> true;
#elif __IOS__ // Note: No __ANDROID__, the ICustomScrollInfo support is made directly in the NativeScrollContentPresenter
		protected override Size MeasureOverride(Size size)
		{
			var result = base.MeasureOverride(size);

			(RealContent as ICustomScrollInfo).ApplyViewport(ref result);

			return result;
		}

		/// <inheritdoc />
		protected override Size ArrangeOverride(Size finalSize)
		{
			var result = base.ArrangeOverride(finalSize);

			(RealContent as ICustomScrollInfo).ApplyViewport(ref result);

			return result;
		}
#endif
	}
}
