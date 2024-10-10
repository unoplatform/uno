using Uno.Extensions;
using Uno.UI.DataBinding;
using Microsoft.UI.Xaml;
using Uno.UI.Extensions;
using Microsoft.UI.Xaml.Data;
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
using Microsoft.UI.Xaml.Controls.Primitives;
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

namespace Microsoft.UI.Xaml.Controls
{
	partial class NativeScrollContentPresenter : UIScrollView, DependencyObject, ILayouterElement
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

		private ScrollViewer GetParentScrollViewer() => _scrollViewer.TryGetTarget(out var s) ? s : this.GetTemplatedParent() as ScrollViewer;

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

		#region Layouting

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
			Size availableSize = size;
			var child = this.GetChildren().FirstOrDefault();

			var desiredChildSize = default(Size);
			if (child != null)
			{
				var scrollSpace = availableSize;
				if (VerticalScrollBarVisibility != ScrollBarVisibility.Disabled)
				{
					scrollSpace.Height = double.PositiveInfinity;
				}
				if (HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled)
				{
					scrollSpace.Width = double.PositiveInfinity;
				}

				desiredChildSize = MobileLayoutingHelpers.MeasureElement(child, scrollSpace);

				// Give opportunity to the the content to define the viewport size itself
				(child as ICustomScrollInfo)?.ApplyViewport(ref desiredChildSize);
			}

			return new Size(Math.Min(availableSize.Width, desiredChildSize.Width), Math.Min(availableSize.Height, desiredChildSize.Height));
		}

		Size ILayouterElement.Measure(Size availableSize)
		{
			return this.SizeThatFits(availableSize);
		}

		void ILayouterElement.Arrange(Rect finalRect)
		{
			var child = this.GetChildren().FirstOrDefault();
			if (child != null)
			{
				var desiredChildSize = LayoutInformation.GetDesiredSize(child);

				var slotSize = finalRect.Size;

				var width = Math.Max(slotSize.Width, desiredChildSize.Width);
				var height = Math.Max(slotSize.Height, desiredChildSize.Height);

				MobileLayoutingHelpers.ArrangeElement(child, new Rect(0, 0, width, height));

				var marginSize = child is FrameworkElement { Margin: { } margin }
					? new Size(margin.Left + margin.Right, margin.Top + margin.Bottom)
					: default;

				ContentSize = child.Frame.Size + marginSize;

				// Give opportunity to the the content to define the viewport size itself
				(child as ICustomScrollInfo)?.ApplyViewport(ref slotSize);

				this.Frame = new Rect(0, 0, slotSize.Width, slotSize.Height);

				// This prevents unnecessary touch delays (which affects the pressed visual states of buttons) when user can't scroll.
				UpdateDelayedTouches();
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
