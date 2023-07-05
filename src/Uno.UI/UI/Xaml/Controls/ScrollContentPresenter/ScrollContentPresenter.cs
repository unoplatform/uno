#nullable disable

using Uno.UI.DataBinding;
using System;
using Windows.Foundation;
using Uno.UI;
using Windows.System;
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
		public ScrollContentPresenter()
		{
			InitializePartial();
			RegisterAsScrollPort(this);

			InitializeScrollContentPresenter();
		}

		partial void InitializePartial();

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

		private ScrollViewer Scroller => ScrollOwner as ScrollViewer;

		public Rect MakeVisible(UIElement visual, Rect rectangle)
		{
			// Simulate a BringIntoView request
			var args = new BringIntoViewRequestedEventArgs()
			{
				AnimationDesired = true,
				TargetRect = rectangle,
				TargetElement = visual,
				OriginalSource = visual
			};
			OnBringIntoViewRequested(args);

			return args.TargetRect;
		}

#if __WASM__
		bool _forceChangeToCurrentView;
		bool IScrollContentPresenter.ForceChangeToCurrentView
		{
			get => _forceChangeToCurrentView;
			set => _forceChangeToCurrentView = value;
		}

#elif __SKIA__
		bool _forceChangeToCurrentView;
		internal bool ForceChangeToCurrentView
		{
			get => _forceChangeToCurrentView;
			set => _forceChangeToCurrentView = value;
		}
#endif

		private void InitializeScrollContentPresenter()
		{
			this.RegisterParentChangedCallbackStrong(this, OnParentChanged);
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

		public double ViewportHeight => DesiredSize.Height - Margin.Top - Margin.Bottom;

		public double ViewportWidth => DesiredSize.Width - Margin.Left - Margin.Right;

#if UNO_HAS_MANAGED_SCROLL_PRESENTER || __WASM__
		protected override Size MeasureOverride(Size availableSize)
		{
			if (Content is UIElement child)
			{
				var (minSize, maxSize) = Scroller.GetMinMax();

				var slotSize = availableSize
					.AtMost(maxSize)
					.AtLeast(minSize);

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
					Math.Min(slotSize.Width, desired.Width),
					Math.Min(slotSize.Height, desired.Height)
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

		private void PointerWheelScroll(object sender, Input.PointerRoutedEventArgs e)
		{
			var properties = e.GetCurrentPoint(null).Properties;

			if (Content is UIElement)
			{
				var canScrollHorizontally = CanHorizontallyScroll;
				var canScrollVertically = CanVerticallyScroll;
				var delta = IsPointerWheelReversed
					? -properties.MouseWheelDelta
					: properties.MouseWheelDelta;

				if (e.KeyModifiers == VirtualKeyModifiers.Control)
				{
					// TODO: Handle zoom https://github.com/unoplatform/uno/issues/4309
				}
				else if (!canScrollVertically || properties.IsHorizontalMouseWheel || e.KeyModifiers == VirtualKeyModifiers.Shift)
				{
					if (canScrollHorizontally)
					{
#if __WASM__ // On wasm the scroll might be async (especially with disableAnimation: false), so we need to use the pending value to support high speed multiple wheel events
						var horizontalOffset = _pendingScrollTo?.horizontal ?? HorizontalOffset;
#else
						var horizontalOffset = HorizontalOffset;
#endif

						Set(
							horizontalOffset: horizontalOffset + GetHorizontalScrollWheelDelta(DesiredSize, delta),
							disableAnimation: false);
					}
				}
				else
				{
#if __WASM__ // On wasm the scroll might be async (especially with disableAnimation: false), so we need to use the pending value to support high speed multiple wheel events
					var verticalOffset = _pendingScrollTo?.vertical ?? VerticalOffset;
#else
					var verticalOffset = VerticalOffset;
#endif

					Set(
						verticalOffset: verticalOffset + GetVerticalScrollWheelDelta(DesiredSize, -delta),
						disableAnimation: false);
				}
			}
		}

		public void SetVerticalOffset(double offset)
			=> Set(verticalOffset: offset, disableAnimation: true);

		public void SetHorizontalOffset(double offset)
			=> Set(horizontalOffset: offset, disableAnimation: true);

		// Ensure the offset we're scrolling to is valid.
		private double ValidateInputOffset(double offset, int minOffset, double maxOffset)
		{
			if (offset.IsNaN())
			{
				throw new InvalidOperationException($"Invalid scroll offset value");
			}

			return Math.Max(minOffset, Math.Min(offset, maxOffset));
		}

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
