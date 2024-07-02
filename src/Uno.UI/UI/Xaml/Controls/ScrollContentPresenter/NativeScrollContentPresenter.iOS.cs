using Uno.Extensions;
using Uno.UI.DataBinding;
using Windows.UI.Xaml;
using Uno.UI.Extensions;
using Windows.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using System.Text;
using System.Linq;
using Foundation;
using UIKit;
using CoreGraphics;
using Uno.Foundation.Logging;
using Windows.Foundation;
using Windows.UI.Xaml.Controls.Primitives;
using Uno.UI;
using Uno.UI.UI.Xaml.Controls.Layouter;
using Uno.UI.Xaml.Input;
using DraggingEventArgs = UIKit.DraggingEventArgs;
using ObjCRuntime;

#if HAS_UNO_WINUI
using Microsoft.UI.Input;
#else
using Windows.UI.Input;
using Windows.Devices.Input;
#endif

namespace Windows.UI.Xaml.Controls
{
	partial class NativeScrollContentPresenter : UIScrollView, DependencyObject, ISetLayoutSlots
	{
		private readonly WeakReference<ScrollViewer> _scrollViewer;

		/// <summary>
		/// Is the UIScrollView currently undergoing animated scrolling, either user-initiated or programmatic.
		/// </summary>
		private bool _isInAnimatedScroll;

		CGPoint IUIScrollView.UpperScrollLimit => UpperScrollLimit;

		CGPoint IUIScrollView.ContentOffset => ContentOffset;

		nfloat IUIScrollView.ZoomScale => ZoomScale;

		internal CGPoint UpperScrollLimit
		{
			get
			{
				var extent = ContentSize;
				var viewport = Frame.Size;

				return new CGPoint(
					Math.Max(0, extent.Width - viewport.Width),
					Math.Max(0, extent.Height - viewport.Height));
			}
		}

		internal NativeScrollContentPresenter(ScrollViewer scroller) : this()
		{
			_scrollViewer = new WeakReference<ScrollViewer>(scroller);

			// Because the arrange pass is asynchronous on iOS, this is required for the ScrollViewer to get up-to-date viewport dimensions
			SizeChanged += (_, __) =>
			{
				GetParentScrollViewer()?.UpdateDimensionProperties();
			};
		}

		public NativeScrollContentPresenter()
		{
			Scrolled += OnScrolled;
			ViewForZoomingInScrollView = _ => Content as UIView;
			DidZoom += OnZoom;
			DraggingStarted += OnDraggingStarted;
			DraggingEnded += OnDraggingEnded;
			DecelerationEnded += OnDecelerationEnded;
			ScrollAnimationEnded += OnScrollAnimationEnded;

			if (ScrollViewer.UseContentInsetAdjustmentBehavior)
			{
				ContentInsetAdjustmentBehavior = UIScrollViewContentInsetAdjustmentBehavior.Never;
			}

			IFrameworkElementHelper.Initialize(this);
		}

		private void OnScrolled(object sender, EventArgs e)
		{
			InvokeOnScroll();
		}

		private void InvokeOnScroll()
		{
			var scroller = GetParentScrollViewer();
			if (scroller is null || scroller.Presenter is null)
			{
				return;
			}

			// iOS can return, eg, negative values for offset, whereas Windows never will, even for 'elastic' scrolling
			var clampedOffset = scroller.ShouldReportNegativeOffsets
				? ContentOffset
				: ContentOffset.Clamp(CGPoint.Empty, UpperScrollLimit);
			scroller.Presenter.OnNativeScroll(clampedOffset.X, clampedOffset.Y, isIntermediate: _isInAnimatedScroll);
		}

		private ScrollViewer GetParentScrollViewer() => _scrollViewer.TryGetTarget(out var s) ? s : TemplatedParent as ScrollViewer;

		// Called when user starts dragging
		private void OnDraggingStarted(object sender, EventArgs e)
		{
			_isInAnimatedScroll = true;
		}

		// Called when user stops dragging (lifts finger)
		private void OnDraggingEnded(object sender, DraggingEventArgs e)
		{
			if (!e.Decelerate)
			{
				//No fling, send final scroll event
				OnAnimatedScrollEnded();
			}
		}

		// Called when a user-initiated fling comes to a stop
		private void OnDecelerationEnded(object sender, EventArgs e)
		{
			OnAnimatedScrollEnded();
		}

		// Called at the end of a programmatic animated scroll
		private void OnScrollAnimationEnded(object sender, EventArgs e)
		{
			OnAnimatedScrollEnded();
		}

		private void OnAnimatedScrollEnded()
		{
			_isInAnimatedScroll = false;
			InvokeOnScroll();
		}

		private void OnZoom(object sender, EventArgs e)
		{
			if (GetParentScrollViewer()?.Presenter is { } presenter)
			{
				presenter.OnNativeZoom((float)ZoomScale);
			}
		}

		public override void SetContentOffset(CGPoint contentOffset, bool animated)
		{
			base.SetContentOffset(contentOffset, animated);
			if (animated)
			{
				_isInAnimatedScroll = true;
			}
		}

		void IUIScrollView.ApplyZoomScale(nfloat scale, bool animated)
		{
			if (!animated)
			{
				ZoomScale = scale;
			}
			else
			{
				base.SetZoomScale(scale, true);
			}
		}

		void IUIScrollView.ApplyContentOffset(CGPoint contentOffset, bool animated)
		{
			if (!animated)
			{
				ContentOffset = contentOffset;
			}
			else
			{
				base.SetContentOffset(contentOffset, true);
			}
		}

		partial void OnContentChanged(UIView previousView, UIView newView)
		{
			// If Content is a view it may have already been set as Content somewhere else in certain scenarios
			if (previousView?.Superview == this)
			{
				previousView.RemoveFromSuperview();
			}

			// Ensure we're working with an empty view, in case previously removed views were missed.
			while (Subviews.Length > 0)
			{
				Subviews[0].RemoveFromSuperview();
			}

			if (newView != null)
			{
				AddSubview(newView);
			}
		}

		public override void SetNeedsLayout()
		{
			base.SetNeedsLayout();

			_requiresMeasure = true;
			Superview?.SetNeedsLayout();
		}

		#region Layouting
		private CGSize _measuredSize;
		private bool _requiresMeasure;

		private ScrollBarVisibility _verticalScrollBarVisibility;
		private ScrollBarVisibility _horizotalScrollBarVisibility;

		public ScrollBarVisibility VerticalScrollBarVisibility
		{
			get { return _verticalScrollBarVisibility; }
			set
			{
				_verticalScrollBarVisibility = value;

				ShowsVerticalScrollIndicator = value == ScrollBarVisibility.Auto || value == ScrollBarVisibility.Visible;
			}
		}
		public ScrollBarVisibility HorizontalScrollBarVisibility
		{
			get { return _horizotalScrollBarVisibility; }
			set
			{
				_horizotalScrollBarVisibility = value;

				ShowsHorizontalScrollIndicator = value == ScrollBarVisibility.Auto || value == ScrollBarVisibility.Visible;
			}
		}

		public override CGSize SizeThatFits(CGSize size)
		{
			if (_content != null)
			{
				size = AdjustSize(size);

				var availableSizeForChild = size;

				//No need to add margin at this level. It's already taken care of during the Layouter measuring.
				_measuredSize = _content.SizeThatFits(availableSizeForChild);

				// The dimensions are constrained to the size of the ScrollViewer, if available
				// otherwise to the size of the child.
				return new CGSize(
					Math.Min(nfloat.IsNaN(size.Width) ? nfloat.MaxValue : size.Width, _measuredSize.Width),
					Math.Min(nfloat.IsNaN(size.Height) ? nfloat.MaxValue : size.Height, _measuredSize.Height)
				);
			}
			else
			{
				return _measuredSize = CGSize.Empty;
			}
		}

		public override void LayoutSubviews()
		{
			try
			{
				if (_content is null)
				{
					return;
				}

				var frame = Frame;
				if (_requiresMeasure)
				{
					_requiresMeasure = false;
					SizeThatFits(frame.Size);
				}

				var contentMargin = default(Thickness);
				if (_content is IFrameworkElement iFwElt)
				{
					contentMargin = iFwElt.Margin;

					var adjustedMeasure = new CGSize(
						GetAdjustedArrangeWidth(iFwElt, (nfloat)contentMargin.Horizontal()),
						GetAdjustedArrangeHeight(iFwElt, (nfloat)contentMargin.Vertical())
					);

					// Zoom works by applying a transform to the child view. If a view has a non-identity transform, its Frame shouldn't be set.
					if (ZoomScale == 1)
					{
						_content.Frame = new CGRect(
							GetAdjustedArrangeX(iFwElt, adjustedMeasure, (nfloat)contentMargin.Horizontal()),
							GetAdjustedArrangeY(iFwElt, adjustedMeasure, (nfloat)contentMargin.Vertical()),
							Math.Max(0, adjustedMeasure.Width),
							Math.Max(0, adjustedMeasure.Height)
						);
					}
				}

				// Sets the scroll extents using the effective Frame of the content
				// (which might be different than the frame set above if the '_content' has some layouting constraints).
				// Noticeably, it will be the case if the '_content' is bigger than the viewport.
				var finalRect = (Rect)_content.Frame;
				var extentSize = AdjustContentSize(finalRect.InflateBy(contentMargin).Size);

				ContentSize = extentSize;

				// ISetLayoutSlots contract implementation
				LayoutInformation.SetLayoutSlot(_content, new Rect(default, ((Size)extentSize).AtLeast(frame.Size)));
				if (_content is UIElement uiElt)
				{
					uiElt.LayoutSlotWithMarginsAndAlignments = finalRect;
				}

				// This prevents unnecessary touch delays (which affects the pressed visual states of buttons) when user can't scroll.
				UpdateDelayedTouches();
			}
			catch (Exception e)
			{
				this.Log().Error(e.ToString());
			}
		}

		private CGSize AdjustContentSize(CGSize measuredSize)
		{
			//ScrollMode does not affect the measure pass, so we only take it into account when setting the ContentSize of the UIScrollView and not in the SizeThatFits
			var isHorizontalScrollDisabled = HorizontalScrollBarVisibility == ScrollBarVisibility.Disabled;
			var isVerticalScrollDisabled = VerticalScrollBarVisibility == ScrollBarVisibility.Disabled;

			//UIScrollView will not scroll if its ContentSize is smaller than the available size
			//Therefore, force the ContentSize dimension to 0 when we do not want to scroll
			var adjustedWidth = isHorizontalScrollDisabled ? this.Frame.Width : measuredSize.Width;
			var adjustedHeight = isVerticalScrollDisabled ? this.Frame.Height : measuredSize.Height;

			return new CGSize(adjustedWidth, adjustedHeight);
		}

		private nfloat GetAdjustedArrangeX(IFrameworkElement child, CGSize adjustedMeasuredSize, double horizontalMargin)
		{
			var frameSize = Frame.Size;

			switch (child?.HorizontalAlignment ?? HorizontalAlignment.Stretch)
			{
				case HorizontalAlignment.Left:
					return (nfloat)child.Margin.Left;

				case HorizontalAlignment.Right:
					return (adjustedMeasuredSize.Width + horizontalMargin) <= frameSize.Width ?
						frameSize.Width - adjustedMeasuredSize.Width - (nfloat)child.Margin.Right :
						(nfloat)child.Margin.Left;

				case HorizontalAlignment.Stretch: //Treat Stretch the same as Center here even if the child is not the same Width as the container, adjustments have already been applied
				case HorizontalAlignment.Center:
					var layoutWidth = adjustedMeasuredSize.Width + horizontalMargin;
					if (layoutWidth <= frameSize.Width)
					{
						var marginToFrame = (frameSize.Width - layoutWidth) / 2;
						return (nfloat)(marginToFrame + child.Margin.Left);
					}
					else
					{
						return (nfloat)child.Margin.Left;
					}

				default:
					throw new NotSupportedException("Invalid HorizontalAlignment");
			}
		}

		private nfloat GetAdjustedArrangeY(IFrameworkElement child, CGSize adjustedMeasuredSize, double verticalMargin)
		{
			var frameSize = Frame.Size;

			switch (child?.VerticalAlignment ?? VerticalAlignment.Stretch)
			{
				case VerticalAlignment.Top:
					return (nfloat)child.Margin.Top;

				case VerticalAlignment.Bottom:
					return (adjustedMeasuredSize.Height + verticalMargin) <= frameSize.Height ?
						frameSize.Height - adjustedMeasuredSize.Height - (nfloat)child.Margin.Bottom :
						(nfloat)child.Margin.Top;

				case VerticalAlignment.Stretch: //Treat Stretch the same as Center even if the child is not the same Height as the container, adjustments have already been applied
				case VerticalAlignment.Center:
					var layoutHeight = adjustedMeasuredSize.Height + verticalMargin;
					if (layoutHeight <= frameSize.Height)
					{
						var marginToFrame = (frameSize.Height - layoutHeight) / 2;
						return (nfloat)(marginToFrame + child.Margin.Top);
					}
					else
					{
						return (nfloat)child.Margin.Top;
					}

				default:
					throw new NotSupportedException("Invalid VerticalAlignment");
			}
		}

		private nfloat GetAdjustedArrangeWidth(IFrameworkElement child, nfloat horizontalMargin)
		{
			var adjustedWidth = _measuredSize.Width - horizontalMargin;
			var viewPortWidth = Frame.Size.Width - horizontalMargin;

			// Apply Stretch
			if (child.HorizontalAlignment == HorizontalAlignment.Stretch)
			{
				// Make it at least as big as the view port
				adjustedWidth = (nfloat)Math.Max(adjustedWidth, viewPortWidth);
			}

			// Apply ScrollMode
			if (HorizontalScrollBarVisibility == ScrollBarVisibility.Disabled)
			{
				// Make it at most as big as the view port
				adjustedWidth = (nfloat)Math.Min(adjustedWidth, viewPortWidth);
			}

			var childDesiredWidth = GetDesiredValue(child.MinWidth, child.Width, child.MaxWidth);
			childDesiredWidth = double.IsNaN(childDesiredWidth) ? double.MaxValue : childDesiredWidth;  //Because Math.Min with a NaN will return NaN.
			adjustedWidth = (nfloat)Math.Min(adjustedWidth, childDesiredWidth); //Take the smallest between the child desired Width and the measured width

			return adjustedWidth;
		}

		/// <summary>
		/// Returns the biggest value it can based on if the different arguments are set or not (double.NaN)
		/// </summary>
		/// <param name="minimumValue"></param>
		/// <param name="desiredValue"></param>
		/// <param name="maximumValue"></param>
		/// <returns></returns>
		private double GetDesiredValue(double minimumValue, double desiredValue, double maximumValue)
		{
			//If nothing is defined return NaN as to indicate that no values was determine
			if (double.IsNaN(minimumValue) && double.IsNaN(desiredValue) && double.IsNaN(maximumValue))
			{
				return double.NaN;
			}

			//Make sure we have something to compare with, '>' or '<' does not work with NaN.
			var innerMinimumValue = double.IsNaN(minimumValue) ? double.MinValue : minimumValue;
			var innerDesiredValue = double.IsNaN(desiredValue) ? double.MinValue : desiredValue;
			var innerMaximumValue = double.IsNaN(maximumValue) ? double.MaxValue : maximumValue; //Set to Max of double

			// Case 10, NaN, 30 returns 30
			if (double.IsNaN(desiredValue) && //desiredValue was not set 
			   !double.IsNaN(maximumValue) && //maximumValue was set
			   innerMinimumValue < innerMaximumValue)
			{
				return innerMaximumValue;
			}

			// 20   10    30  = 20
			// 30   10    10  = 30
			// 20   NaN   NaN = 20
			// 50   NaN  30   = 50 
			if (innerMinimumValue > innerDesiredValue) // if minimumValue always wins over desiredValue and maximumValue
			{
				return innerMinimumValue;
			}

			// NaN  NaN   10  = 10 
			// NaN  50    20  = 30

			if (double.IsNaN(desiredValue) ||           // If desiredValue was not set
				innerDesiredValue > innerMaximumValue)  // or desireValue is bigger than maximumValue take maximumValue
			{
				return maximumValue;
			}

			// 10   20    30 = 20
			// NaN  20    NaN = 20
			// NaN  20    30 = 20
			return desiredValue;

		}

		private nfloat GetAdjustedArrangeHeight(IFrameworkElement child, nfloat verticalMargin)
		{
			var adjustedHeight = _measuredSize.Height - verticalMargin;
			var viewPortHeight = Frame.Size.Height - verticalMargin;

			// Apply Stretch
			if (child.VerticalAlignment == VerticalAlignment.Stretch)
			{
				// Make it at least as big as the view port
				adjustedHeight = (nfloat)Math.Max(adjustedHeight, viewPortHeight);
			}

			// Apply ScrollMode
			if (VerticalScrollBarVisibility == ScrollBarVisibility.Disabled)
			{
				// Make it at most as big as the view port
				adjustedHeight = (nfloat)Math.Min(adjustedHeight, viewPortHeight);
			}

			var childDesiredHeight = GetDesiredValue(child.MinHeight, child.Height, child.MaxHeight);
			childDesiredHeight = double.IsNaN(childDesiredHeight) ? double.MaxValue : childDesiredHeight;  //Because Math.Min with a NaN will return NaN.

			adjustedHeight = (nfloat)Math.Min(adjustedHeight, childDesiredHeight);

			return adjustedHeight;
		}

		private CGSize AdjustSize(CGSize size)
		{
			var width = GetMeasureValue(size.Width, HorizontalScrollBarVisibility);
			var height = GetMeasureValue(size.Height, VerticalScrollBarVisibility);

			return new CGSize(width, height);
		}

		private nfloat GetMeasureValue(nfloat value, ScrollBarVisibility scrollBarVisibility)
		{
			switch (scrollBarVisibility)
			{
				case ScrollBarVisibility.Auto:
				case ScrollBarVisibility.Hidden:
				case ScrollBarVisibility.Visible:
					return nfloat.PositiveInfinity;

				default:
				case ScrollBarVisibility.Disabled:
					return value;
			}
		}
		#endregion

		bool INativeScrollContentPresenter.Set(
			double? horizontalOffset,
			double? verticalOffset,
			float? zoomFactor,
			bool disableAnimation,
			bool isIntermediate)
			=> throw new NotImplementedException();

		#region Touches

		private UIElement _touchTarget;

		/// <inheritdoc />
		public override void TouchesBegan(NSSet touches, UIEvent evt)
		{
			base.TouchesBegan(touches, evt);

			// We wait for the first touches to get the parent so we don't have to track Loaded/UnLoaded
			// Like native dispatch on iOS, we do "implicit captures" the target.
			if (this.GetParent() is UIElement parent)
			{
				// canBubbleNatively: true => We let native bubbling occur properly as it's never swallowed by system
				//							  but blocking it would be breaking in lot of aspects
				//							  (e.g. it would prevent all sub-sequent events for the given pointer).

				_touchTarget = parent;
				_touchTarget.TouchesBegan(touches, evt, canBubbleNatively: true);
			}
		}

		/// <inheritdoc />
		public override void TouchesMoved(NSSet touches, UIEvent evt)
		{
			base.TouchesMoved(touches, evt);

			// canBubbleNatively: false => The system might silently swallow pointers after a few moves so we prefer to bubble in managed.
			_touchTarget?.TouchesMoved(touches, evt, canBubbleNatively: false);
		}

		/// <inheritdoc />
		public override void TouchesEnded(NSSet touches, UIEvent evt)
		{
			base.TouchesEnded(touches, evt);

			// canBubbleNatively: false => system might silently swallow pointer after few moves so we prefer to bubble in managed.
			_touchTarget?.TouchesEnded(touches, evt, canBubbleNatively: false);
			_touchTarget = null;
		}

		/// <inheritdoc />
		public override void TouchesCancelled(NSSet touches, UIEvent evt)
		{
			base.TouchesCancelled(touches, evt);

			// canBubbleNatively: false => system might silently swallow pointer after few moves so we prefer to bubble in managed.
			_touchTarget?.TouchesCancelled(touches, evt, canBubbleNatively: false);
			_touchTarget = null;
		}

		/*
		 * By default the UIScrollView will delay the touches to the content until it detects
		 * if the manipulation is a drag. And even there, if it detects that the manipulation
		 * is a Drag, it will cancel the touches on content and handle them internally
		 * (i.e. Touches<Began|Moved|Ended> will no longer be invoked on SubViews).
		 * cf. https://developer.apple.com/documentation/uikit/uiscrollview
		 *
		 * The "TouchesManager" give the ability to any child UIElement to alter this behavior
		 * if it needs to handle the gestures itself (e.g. the Thumb of a Slider / ToggleSwitch).
		 *
		 * On the UIElement this is defined by the ManipulationMode.
		 */

		private TouchesManager _touchesManager;
		internal TouchesManager TouchesManager => _touchesManager ??= new NativeScrollContentPresenterManipulationManager(this);

		private void UpdateDelayedTouches()
		{
			if ((_touchesManager?.Listeners ?? 0) == 0)
			{
				// This prevents unnecessary touch delays (which affects the pressed visual states of buttons) when user can't scroll.
				var canScrollVertically = VerticalScrollBarVisibility != ScrollBarVisibility.Disabled && ContentSize.Height > Frame.Height;
				var canScrollHorizontally = HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled && ContentSize.Width > Frame.Width;
				DelaysContentTouches = canScrollHorizontally || canScrollVertically;
			}
			else
			{
				DelaysContentTouches = false;
			}
		}

		private class NativeScrollContentPresenterManipulationManager : TouchesManager
		{
			private readonly NativeScrollContentPresenter _scrollPresenter;

			public NativeScrollContentPresenterManipulationManager(NativeScrollContentPresenter scrollPresenter)
			{
				_scrollPresenter = scrollPresenter;
			}

			/// <inheritdoc />
			protected override bool CanConflict(GestureRecognizer.Manipulation manipulation)
				=> _scrollPresenter.CanHorizontallyScroll && manipulation.IsTranslateXEnabled
					|| _scrollPresenter.CanVerticallyScroll && manipulation.IsTranslateYEnabled
					|| manipulation.IsDragManipulation; // This will actually always be false when CanConflict is being invoked in current setup.

			/// <inheritdoc />
			protected override void SetCanDelay(bool canDelay)
				=> _scrollPresenter.UpdateDelayedTouches();

			/// <inheritdoc />
			protected override void SetCanCancel(bool canCancel)
				=> _scrollPresenter.CanCancelContentTouches = canCancel;
		}
		#endregion
	}
}
