using Uno.UI.DataBinding;
using System;
using Windows.Foundation;
using Uno.UI;
using Windows.System;
#if __ANDROID__
using View = Android.Views.View;
using Font = Android.Graphics.Typeface;
#elif __APPLE_UIKIT__
using UIKit;
using View = UIKit.UIView;
using Color = UIKit.UIColor;
using Font = UIKit.UIFont;
#else
using View = Microsoft.UI.Xaml.UIElement;
#endif

namespace Microsoft.UI.Xaml.Controls
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
#if UNO_HAS_MANAGED_SCROLL_PRESENTER
					if (oldScroller.Target is ScrollViewer oldScrollerTarget)
					{
						UnhookScrollEvents(oldScrollerTarget);
					}
#endif
					WeakReferencePool.ReturnWeakReference(this, oldScroller);
				}

				_scroller = WeakReferencePool.RentWeakReference(this, value);
#if UNO_HAS_MANAGED_SCROLL_PRESENTER
				if (IsInLiveTree && value is ScrollViewer newTarget)
				{
					HookScrollEvents(newTarget);
				}
#endif
			}
		}
		#endregion

		private ScrollViewer Scroller => ScrollOwner as ScrollViewer;

		internal double TargetHorizontalOffset =>
#if __WASM__ // On wasm the scroll might be async (especially with disableAnimation: false), so we need to use the pending value to support high speed multiple scrolling events
			_pendingScrollTo?.horizontal ?? HorizontalOffset;
#else
			HorizontalOffset;
#endif

		internal double TargetVerticalOffset =>
#if __WASM__ // On wasm the scroll might be async (especially with disableAnimation: false), so we need to use the pending value to support high speed multiple scrolling events
			_pendingScrollTo?.vertical ?? VerticalOffset;
#else
			VerticalOffset;
#endif

#if UNO_HAS_MANAGED_SCROLL_PRESENTER || __WASM__
		public static DependencyProperty SizesContentToTemplatedParentProperty { get; } = DependencyProperty.Register(
			nameof(SizesContentToTemplatedParent),
			typeof(bool),
			typeof(ScrollContentPresenter),
			new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsMeasure));

		public bool SizesContentToTemplatedParent
		{
			get => (bool)GetValue(SizesContentToTemplatedParentProperty);
			set => SetValue(SizesContentToTemplatedParentProperty, value);
		}
#endif

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

				bool sizesContentToTemplatedParent = SizesContentToTemplatedParent;

				if (ScrollOwner is ScrollViewer scrollViewer)
				{
					if (sizesContentToTemplatedParent)
					{
						slotSize = scrollViewer.ViewportMeasureSize;
					}
				}

				// when set to true, this means that we wanted to set to infinity but were blocked in doing it.
				bool childPreventsInfiniteAvailableWidth = false;
				bool childPreventsInfiniteAvailableHeight = false;

				if (CanVerticallyScroll)
				{
					childPreventsInfiniteAvailableHeight = !child.WantsScrollViewerToObscureAvailableSizeBasedOnScrollBarVisibility(Orientation.Vertical);
					if (!sizesContentToTemplatedParent && !childPreventsInfiniteAvailableHeight)
					{
						slotSize.Height = double.PositiveInfinity;
					}
				}
				if (CanHorizontallyScroll)
				{
					childPreventsInfiniteAvailableWidth = !child.WantsScrollViewerToObscureAvailableSizeBasedOnScrollBarVisibility(Orientation.Horizontal);
					if (!sizesContentToTemplatedParent && !childPreventsInfiniteAvailableWidth)
					{
						slotSize.Width = double.PositiveInfinity;
					}
				}

				if (child is ItemsPresenter itemsPresenter)
				{
					itemsPresenter.EvaluateAndSetNonClippingBehavior(childPreventsInfiniteAvailableWidth || childPreventsInfiniteAvailableHeight);
				}

				child.Measure(slotSize);

				var desired = child.DesiredSize;

				// Give opportunity to the the content to define the viewport size itself
				(child as ICustomScrollInfo)?.ApplyViewport(ref desired);

				return new Size(
					Math.Min(availableSize.Width, desired.Width),
					Math.Min(availableSize.Height, desired.Height)
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

#if __CROSSRUNTIME__
		// This may need to be adjusted if/when CanContentRenderOutsideBounds is implemented.
		private protected override Rect? GetClipRect(bool needsClipToSlot, Point visualOffset, Rect finalRect, Size maxSize, Thickness margin)
			=> new Rect(default, RenderSize);
#endif

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

				var success = false;

				if (e.KeyModifiers == VirtualKeyModifiers.Control)
				{
#if UNO_HAS_MANAGED_SCROLL_PRESENTER
					if (Scroller?.ZoomMode == ZoomMode.Enabled)
					{
						// Calculate zoom change (positive delta = zoom in, negative = zoom out)
						// WinUI zooms toward viewport center for Ctrl+Wheel (not cursor position)
						var zoomDelta = delta > 0 ? 1.1f : 0.9f; // 10% zoom per wheel tick
						var newZoom = Math.Clamp(_zoomFactor * zoomDelta, _minZoomFactor, _maxZoomFactor);

						if (Math.Abs(newZoom - _zoomFactor) > 0.001f)
						{
							// Zoom toward viewport center - adjust offsets to keep center point fixed
							var zoomRatio = newZoom / _zoomFactor;
							var viewportCenterX = ViewportWidth / 2;
							var viewportCenterY = ViewportHeight / 2;

							// Formula: newOffset = center + (oldOffset - center) * zoomRatio
							// This keeps the content point at viewport center in the same position
							var newHOffset = viewportCenterX + (HorizontalOffset - viewportCenterX) * zoomRatio;
							var newVOffset = viewportCenterY + (VerticalOffset - viewportCenterY) * zoomRatio;

							success = Set(
								horizontalOffset: newHOffset,
								verticalOffset: newVOffset,
								zoomFactor: newZoom,
								disableAnimation: false);
						}
					}
#endif
				}
				else if (canScrollHorizontally && (properties.IsHorizontalMouseWheel || e.KeyModifiers == VirtualKeyModifiers.Shift))
				{
					success = Set(
						horizontalOffset: TargetHorizontalOffset + GetHorizontalScrollWheelDelta(DesiredSize, delta),
						disableAnimation: false);
				}
				else if (canScrollVertically && !properties.IsHorizontalMouseWheel)
				{
					success = Set(
						verticalOffset: TargetVerticalOffset + GetVerticalScrollWheelDelta(DesiredSize, -delta),
						disableAnimation: false);
				}

				// This is not similar to what WinUI is doing, since we already differ quite a bit from
				// the way WinUI does SCP scrolling. On WinUI, ScrollViewer is the PointerWheelChanged receiver
				// and is the one that decides when to mark as handled. However, this alternative is visually
				// close (even though not identical)
				e.Handled = success;
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

#elif __APPLE_UIKIT__ // Note: No __ANDROID__, the ICustomScrollInfo support is made directly in the NativeScrollContentPresenter
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

#if __WASM__ || __NETSTD_REFERENCE__
		protected override void OnContentChanged(object oldValue, object newValue) => base.OnContentChanged(oldValue, newValue);
#endif
	}
}
