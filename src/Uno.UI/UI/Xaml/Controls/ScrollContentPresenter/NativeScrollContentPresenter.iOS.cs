using Uno.Extensions;
using Uno.UI.DataBinding;
using Windows.UI.Xaml;
using Uno.UI.Extensions;
using Windows.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using System.Text;
using System.Linq;
using Foundation;
using UIKit;
using CoreGraphics;
using Uno.Logging;
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls
{
	partial class NativeScrollContentPresenter : UIScrollView, DependencyObject
	{
		/// <summary>
		/// Is the UIScrollView currently undergoing animated scrolling, either user-initiated or programmatic.
		/// </summary>
		private bool _isInAnimatedScroll;

		internal CGPoint UpperScrollLimit { get { return (CGPoint)(ContentSize - Frame.Size); } }
		CGPoint IUIScrollView.UpperScrollLimit { get { return UpperScrollLimit; } }

		public NativeScrollContentPresenter()
		{
			TouchesManager = new ScrollContentPresenterManipulationManager(this);
			Scrolled += OnScrolled;
			ViewForZoomingInScrollView = _ => Content;
			DidZoom += OnZoom;
			DraggingStarted += OnDraggingStarted;
			DraggingEnded += OnDraggingEnded;
			DecelerationEnded += OnDecelerationEnded;
			ScrollAnimationEnded += OnScrollAnimationEnded;

			if (ScrollViewer.UseContentInsetAdjustmentBehavior)
			{
				ContentInsetAdjustmentBehavior = UIScrollViewContentInsetAdjustmentBehavior.Never;
			}
		}

		private void OnScrolled(object sender, EventArgs e)
		{
			InvokeOnScroll();
		}

		private void InvokeOnScroll()
		{
			var shouldReportNegativeOffsets = (TemplatedParent as ScrollViewer)?.ShouldReportNegativeOffsets ?? false;
			// iOS can return, eg, negative values for offset, whereas Windows never will, even for 'elastic' scrolling
			var clampedOffset = shouldReportNegativeOffsets ?
				ContentOffset :
				ContentOffset.Clamp(CGPoint.Empty, UpperScrollLimit);
			(TemplatedParent as ScrollViewer)?.OnScrollInternal(clampedOffset.X, clampedOffset.Y, isIntermediate: _isInAnimatedScroll);
		}

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
			(TemplatedParent as ScrollViewer)?.OnZoomInternal((float)ZoomScale);
		}

		public override void SetContentOffset(CGPoint contentOffset, bool animated)
		{
			base.SetContentOffset(contentOffset, animated);
			if (animated)
			{
				_isInAnimatedScroll = true;
			}
		}

		partial void OnContentChanged(UIView previousView, UIView newView)
		{
			// If Content is a view it may have already been set as Content somewhere else in certain scenarios
			if (previousView?.Superview == this)
			{
				previousView.RemoveFromSuperview();
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

			if (Superview != null)
			{
				Superview.SetNeedsLayout();
			}
		}

		

		

		

		public Rect MakeVisible(UIElement visual, Rect rectangle)
		{
			ScrollViewExtensions.BringIntoView(this, visual, BringIntoViewMode.ClosestEdge);
			return rectangle;
		}

		#region Touches

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

		internal UIElement.TouchesManager TouchesManager { get; }

		private void UpdateDelayedTouches()
		{
			if (TouchesManager.Listeners == 0)
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

		private class ScrollContentPresenterManipulationManager : UIElement.TouchesManager
		{
			private readonly NativeScrollContentPresenter _scrollPresenter;

			public ScrollContentPresenterManipulationManager(NativeScrollContentPresenter scrollPresenter)
			{
				_scrollPresenter = scrollPresenter;
			}

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
