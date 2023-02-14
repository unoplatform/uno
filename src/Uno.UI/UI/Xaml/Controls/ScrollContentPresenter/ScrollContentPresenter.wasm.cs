using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.DataBinding;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using System.Text;
using Windows.Foundation;
using Uno.UI.Xaml;

using Uno.UI.Extensions;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ScrollContentPresenter : ContentPresenter, IScrollContentPresenter
	{
		private ScrollBarVisibility _verticalScrollBarVisibility;
		private ScrollBarVisibility _horizontalScrollBarVisibility;
		private bool _eventsRegistered;

		private (double? horizontal, double? vertical)? _pendingScrollTo;
		private FrameworkElement _rootEltUsedToProcessScrollTo;

		internal Size ScrollBarSize
		{
			get
			{
				var (clientSize, offsetSize) = WindowManagerInterop.GetClientViewSize(HtmlId);

				return new Size(offsetSize.Width - clientSize.Width, offsetSize.Height - clientSize.Height);
			}
		}

		private object RealContent => Content;

		private void TryRegisterEvents(ScrollBarVisibility visibility)
		{
			if (
				!_eventsRegistered
				&& (visibility == ScrollBarVisibility.Auto || visibility == ScrollBarVisibility.Visible))
			{
				// Those events are only needed when native scrollbars are used,
				// in order to handle pointer events on the native scrollbars themselves.
				// See HandlePointerEvent for more details.

				_eventsRegistered = true;

				PointerReleased += HandlePointerEvent;
				PointerPressed += HandlePointerEvent;
				PointerCanceled += HandlePointerEvent;
				PointerMoved += HandlePointerEvent;
				PointerEntered += HandlePointerEvent;
				PointerExited += HandlePointerEvent;
				PointerWheelChanged += HandlePointerEvent;
			}
		}

		private static void HandlePointerEvent(object sender, Input.PointerRoutedEventArgs e)
			=> ((ScrollContentPresenter)sender).HandlePointerEvent(e);

		private void HandlePointerEvent(Input.PointerRoutedEventArgs e)
		{
			var (clientSize, offsetSize) = WindowManagerInterop.GetClientViewSize(HtmlId);

			var hasHorizontalScroll = (offsetSize.Height - clientSize.Height) > 0;
			var hasVerticalScroll = (offsetSize.Width - clientSize.Width) > 0;

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"{HtmlId}: {offsetSize} / {clientSize} / {e.GetCurrentPoint(this)}");
			}

			if (!hasVerticalScroll && !hasHorizontalScroll)
			{
				return;
			}

			// The events coming from the native scrollbars are bubbled up
			// to the parents, as those are not XAML elements.
			// This can cause issues for popups with scrollable content and
			// light dismiss patterns.
			var position = e.GetCurrentPoint(this).Position;
			var isInVerticalScrollbar = hasVerticalScroll && position.X >= clientSize.Width;
			var isInHorizontalScrollbar = hasHorizontalScroll && position.Y >= clientSize.Height;

			if (isInVerticalScrollbar || isInHorizontalScrollbar)
			{
				e.Handled = true;
			}
		}

		private static readonly string[] VerticalVisibilityClasses = { "scroll-y-auto", "scroll-y-disabled", "scroll-y-hidden", "scroll-y-visible" };

		ScrollBarVisibility IScrollContentPresenter.NativeVerticalScrollBarVisibility { set => VerticalScrollBarVisibility = value; }
		internal ScrollBarVisibility VerticalScrollBarVisibility
		{
			get => _verticalScrollBarVisibility;
			set
			{
				if (_verticalScrollBarVisibility != value)
				{
					_verticalScrollBarVisibility = value;
					SetClasses(VerticalVisibilityClasses, (int)value);

					TryRegisterEvents(value);
				}
			}
		}

		private static readonly string[] HorizontalVisibilityClasses = { "scroll-x-auto", "scroll-x-disabled", "scroll-x-hidden", "scroll-x-visible" };

		ScrollBarVisibility IScrollContentPresenter.NativeHorizontalScrollBarVisibility { set => HorizontalScrollBarVisibility = value; }
		internal ScrollBarVisibility HorizontalScrollBarVisibility
		{
			get => _horizontalScrollBarVisibility;
			set
			{
				if (_horizontalScrollBarVisibility != value)
				{
					_horizontalScrollBarVisibility = value;
					SetClasses(HorizontalVisibilityClasses, (int)value);

					TryRegisterEvents(value);
				}
			}
		}

		private bool _canHorizontallyScroll;
		public bool CanHorizontallyScroll
		{
			get => _canHorizontallyScroll || _forceChangeToCurrentView;
			set => _canHorizontallyScroll = value;
		}

		private bool _canVerticallyScroll;
		public bool CanVerticallyScroll
		{
			get => _canVerticallyScroll || _forceChangeToCurrentView;
			set => _canVerticallyScroll = value;
		}

		public double HorizontalOffset { get; private set; }

		public double VerticalOffset { get; private set; }

		public double ExtentHeight { get; internal set; }

		public double ExtentWidth { get; internal set; }

		Size? IScrollContentPresenter.CustomContentExtent => null;

		private protected override void OnLoaded()
		{
			base.OnLoaded();
			RestoreScroll();
			RegisterEventHandler("scroll", (EventHandler)OnScroll, GenericEventHandlers.RaiseEventHandler);
		}

		private void RestoreScroll()
		{
			if (TemplatedParent is ScrollViewer sv)
			{
				if (sv.HorizontalOffset > 0 || sv.VerticalOffset > 0)
				{
					ScrollTo(sv.HorizontalOffset, sv.VerticalOffset, disableAnimation: true);
				}
			}
		}

		private protected override void OnUnloaded()
		{
			base.OnUnloaded();
			UnregisterEventHandler("scroll", (EventHandler)OnScroll, GenericEventHandlers.RaiseEventHandler);

			if (_rootEltUsedToProcessScrollTo is { } rootElt)
			{
				rootElt.LayoutUpdated -= TryProcessScrollTo;
				_rootEltUsedToProcessScrollTo = null;
			}
		}

		/// <inheritdoc />
		internal override void OnLayoutUpdated()
		{
			base.OnLayoutUpdated();

			TryProcessScrollTo();
		}

		public void ScrollTo(double? horizontalOffset, double? verticalOffset, bool disableAnimation)
		{
			_pendingScrollTo = (horizontalOffset, verticalOffset);

			WindowManagerInterop.ScrollTo(HtmlId, horizontalOffset, verticalOffset, disableAnimation);

			if (_pendingScrollTo.HasValue)
			{
				if (disableAnimation)
				{
					// Ensure offset values match native ones if animation is disabled
					// as the scroll event may occur only asynchronously
					// while the values are already set properly.
					HorizontalOffset = GetNativeHorizontalOffset();
					VerticalOffset = GetNativeVerticalOffset();
					ScrollOffsets = new Point(HorizontalOffset, VerticalOffset);
				}

				// The scroll to was not processed by the native SCP, we need to re-request ScrollTo a bit later.
				// This happen has soon as the native SCP element is not in a valid state (like un-arranged or hidden).

				if (_rootEltUsedToProcessScrollTo is null && Window.Current.RootElement is FrameworkElement rootFwElt)
				{
					_rootEltUsedToProcessScrollTo = rootFwElt;
					rootFwElt.LayoutUpdated += TryProcessScrollTo;
				}

				if (disableAnimation)
				{
					var nativeHorizontalOffset = GetNativeHorizontalOffset();
					var nativeVerticalOffset = GetNativeVerticalOffset();

					// There's an edge case here - requesting a negative offset while the current offset is exactly zero will not raise the
					// native event, which would cause the reported offset to be left at the incorrect value, so we suppress it
					var willNotScroll = horizontalOffset < 0 && nativeHorizontalOffset == 0
						|| verticalOffset < 0 && nativeVerticalOffset == 0;

					if (!willNotScroll)
					{
						// As the native ScrollTo is going to be async, we manually raise the event with the provided values.
						// If those values are invalid, the browser will raise the final event anyway.
						// Note: If the caller has allowed animation, we assume that it's not interested by a sync response,
						//		 we prefer to wait for the browser to effectively scroll.
						(TemplatedParent as ScrollViewer)?.OnPresenterScrolled(
							horizontalOffset ?? nativeHorizontalOffset,
							verticalOffset ?? nativeVerticalOffset,
							isIntermediate: false
						);
					}
				}
			}
		}

		private void TryProcessScrollTo(object sender, object e)
			=> TryProcessScrollTo();

		private void TryProcessScrollTo()
		{
			if (_pendingScrollTo is { } scrollTo)
			{
				WindowManagerInterop.ScrollTo(HtmlId, scrollTo.horizontal, scrollTo.vertical, disableAnimation: true);
			}
		}

		private void OnScroll(object sender, EventArgs args)
		{
			// We don't have any information from the DOM 'scroll' event about the intermediate vs. final state.
			// We could try to rely on the IsPointerPressed state to detect when the user is scrolling and use it.
			// This would however not include scrolling due to the inertia which should also be flagged as intermediate.
			// The main issue is that the IsPointerPressed be true ONLY when dragging the scrollbars with the mouse, 
			// as for finger and pen we will get a PointerCancelled which will reset the pressed state to false.
			// And it would also requires us to explicitly invoke OnScroll in PointerRelease in order to raise the
			// final SV.ViewChanged event with a IsIntermediate == false.
			// This is probably safer for now to always consider the scroll as final, even if it introduce a performance cost
			// (the SV updates mode is always sync when isIntermediate is false).
			var isIntermediate = false;

			var horizontalOffset = GetNativeHorizontalOffset();
			var verticalOffset = GetNativeVerticalOffset();

			if (IsArrangeDirty
				&& _pendingScrollTo is { } pending
				&& (
					pending.horizontal is { } hOffset && Math.Abs(horizontalOffset - hOffset) > 1
					|| pending.vertical is { } vOffset && Math.Abs(verticalOffset - vOffset) > 1)
				)
			{
				// When the native element of the SCP is becoming "valid" with a non 0 offset, it will raise a scroll event.
				// But if we have a manual scroll request pending, we need to mute it and wait for the next layout updated.

				return;
			}

			_pendingScrollTo = default;

			HorizontalOffset = horizontalOffset;
			VerticalOffset = verticalOffset;

			Scroller?.OnPresenterScrolled(horizontalOffset, verticalOffset, isIntermediate);

			ScrollOffsets = new Point(horizontalOffset, verticalOffset);
			InvalidateViewport();
		}

		private double GetNativeHorizontalOffset()
			=> double.TryParse(GetProperty("scrollLeft"), NumberStyles.Number, CultureInfo.InvariantCulture, out var horizontalOffset)
				? horizontalOffset
				: 0;

		private double GetNativeVerticalOffset()
			=> double.TryParse(GetProperty("scrollTop"), NumberStyles.Number, CultureInfo.InvariantCulture, out var verticalOffset)
				? verticalOffset
				: 0;

		void IScrollContentPresenter.OnMinZoomFactorChanged(float newValue) { }

		void IScrollContentPresenter.OnMaxZoomFactorChanged(float newValue) { }
	}
}
