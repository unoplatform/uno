using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.DataBinding;
using Windows.UI.Xaml.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using System.Text;
using Windows.Foundation;
using Windows.UI.Xaml.Input;
using Uno.UI.Xaml;

using Uno.UI.Extensions;
using Uno.UI.Xaml.Input;

namespace Windows.UI.Xaml.Controls
{
	public partial class ScrollContentPresenter : ContentPresenter, IScrollContentPresenter
	{
		// On wasm, the default of "overflow" property is "visible".
		// This doesn't match any of our scroll-[x|y]-[auto|disabled|hidden|visible] defined in Uno.UI.css
		// (see https://github.com/unoplatform/uno/blob/dca49dadd6feaf0b3addd2ec2195c3af1b6ac9f4/src/Uno.UI/WasmCSS/Uno.UI.css#L183-L223)
		// So, we want the first setter call to be executed, regardless of the value being set.
		// The logic in the setter is done under "if (_[vertical|horizontal]ScrollBarVisibility != value)"
		// So, to make sure this condition is always true for the first call of the setter, we set the initial value to -1, which isn't a valid value for ScrollBarVisibility.
		private ScrollBarVisibility _verticalScrollBarVisibility = (ScrollBarVisibility)(-1);
		private ScrollBarVisibility _horizontalScrollBarVisibility = (ScrollBarVisibility)(-1);
		private bool _eventsRegistered;

		private (double? horizontal, double? vertical)? _pendingScrollTo;
		private (double? horizontal, double? vertical) _lastScrollToRequest;
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

		partial void OnIsPointerWheelReversedChanged(bool isReversed)
		{
			PointerWheelChanged -= ManagedScroll;
			if (isReversed)
			{
				PointerWheelChanged += ManagedScroll;
			}

			static void ManagedScroll(object sender, Input.PointerRoutedEventArgs e)
			{
				// When pointer wheel is reversed, we scroll in managed code and prevent the browser to scroll (PreventDefault)
				e.Handled = true;
				((IHtmlHandleableRoutedEventArgs)e).HandledResult |= HtmlEventDispatchResult.PreventDefault;

				((ScrollContentPresenter)sender).PointerWheelScroll(sender, e);
			}
		}

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

				PointerReleased += HandlePointerEventIfOverNativeScrollbars;
				PointerPressed += HandlePointerEventIfOverNativeScrollbars;
				PointerCanceled += HandlePointerEventIfOverNativeScrollbars;
				PointerMoved += HandlePointerEventIfOverNativeScrollbars;
				PointerEntered += HandlePointerEventIfOverNativeScrollbars;
				PointerExited += HandlePointerEventIfOverNativeScrollbars;
				PointerWheelChanged += HandlePointerEventIfOverNativeScrollbars;
			}
		}

		private static void HandlePointerEventIfOverNativeScrollbars(object sender, Input.PointerRoutedEventArgs e)
			=> ((ScrollContentPresenter)sender).HandlePointerEventIfOverNativeScrollbars(e);

		private void HandlePointerEventIfOverNativeScrollbars(Input.PointerRoutedEventArgs e)
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

		private static readonly string[] VerticalVisibilityClasses = { "scroll-y-disabled", "scroll-y-auto", "scroll-y-hidden", "scroll-y-visible" };

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

		private static readonly string[] HorizontalVisibilityClasses = { "scroll-x-disabled", "scroll-x-auto", "scroll-x-hidden", "scroll-x-visible" };

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
			RegisterEventHandler("scroll", (RoutedEventHandlerWithHandled)OnScroll, GenericEventHandlers.RaiseRoutedEventHandlerWithHandled);

			// a workaround to make scrolling cancelable on WASM
			AddHandler(PointerWheelChangedEvent, new PointerEventHandler(OnPointerWheelChanged), true);
		}

		private void OnPointerWheelChanged(object _, PointerRoutedEventArgs args)
		{
			// On other Uno targets, handling PointerWheelChanged cancels the scrolling (which makes sense, but doesn't necessarily
			// match WinUI). So, to make the behaviour uniform, we also prevent native scrolling here.
			// Unlike KeyDown, we can't wait until OnScroll to prevent the scrolling. We have to cancel it right here.
			if (args.Handled)
			{
				((IHtmlHandleableRoutedEventArgs)args).HandledResult |= HtmlEventDispatchResult.PreventDefault;
			}
		}

		private void RestoreScroll()
		{
			if (TemplatedParent is ScrollViewer sv)
			{
				if (sv.HorizontalOffset > 0 || sv.VerticalOffset > 0)
				{
					Set(
						horizontalOffset: sv.HorizontalOffset,
						verticalOffset: sv.VerticalOffset,
						disableAnimation: true);
				}
			}
		}

		private protected override void OnUnloaded()
		{
			base.OnUnloaded();
			UnregisterEventHandler("scroll", new RoutedEventHandlerWithHandled(OnScroll), GenericEventHandlers.RaiseEventHandler);

			RemoveHandler(PointerWheelChangedEvent, new PointerEventHandler(OnPointerWheelChanged));

			if (_rootEltUsedToProcessScrollTo is { } rootElt)
			{
				rootElt.LayoutUpdated -= TryProcessScrollTo;
				_rootEltUsedToProcessScrollTo = null;
			}
		}

		internal override void AfterArrange()
		{
			base.AfterArrange();
			TryProcessScrollTo();
		}

		internal bool Set(
			double? horizontalOffset = null,
			double? verticalOffset = null,
			float? zoomFactor = null, // Not supported yet
			bool disableAnimation = false,
			bool isIntermediate = false) // Not supported yet
		{
			var success = true;
			if (horizontalOffset is double hOffset)
			{
				var extentWidth = ExtentWidth;
				var viewportWidth = ViewportWidth;

				horizontalOffset = ValidateInputOffset(hOffset, 0, extentWidth - viewportWidth);
				success &= horizontalOffset == hOffset;
			}

			if (verticalOffset is double vOffset)
			{
				var extentHeight = ExtentHeight;
				var viewportHeight = ViewportHeight;

				verticalOffset = ValidateInputOffset(vOffset, 0, extentHeight - viewportHeight);
				success &= verticalOffset == vOffset;
			}

			_pendingScrollTo = (horizontalOffset, verticalOffset);
			_lastScrollToRequest = (horizontalOffset, verticalOffset);

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
				var rootElement = XamlRoot?.VisualTree.RootElement ?? Window.CurrentSafe?.RootElement;
				if (_rootEltUsedToProcessScrollTo is null && rootElement is FrameworkElement rootFwElt)
				{
					_rootEltUsedToProcessScrollTo = rootFwElt;
					// TODO: LayoutUpdated doesn't look like the right thing to do.
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

					if (willNotScroll)
					{
						return false;
					}
					else
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

			return success; // If if not yet processed, we assume that it will be.
		}

		void IScrollContentPresenter.ScrollTo(double? horizontalOffset, double? verticalOffset, bool disableAnimation)
			=> Set(
				horizontalOffset: horizontalOffset,
				verticalOffset: verticalOffset,
				disableAnimation: disableAnimation);

		private void TryProcessScrollTo(object sender, object e)
			=> TryProcessScrollTo();

		private void TryProcessScrollTo()
		{
			if (_pendingScrollTo is { } scrollTo)
			{
				WindowManagerInterop.ScrollTo(HtmlId, scrollTo.horizontal, scrollTo.vertical, disableAnimation: true);
			}
		}

		private bool OnScroll(object sender, RoutedEventArgs routedEventArgs)
		{
			if (Scroller?.CancelNextNativeScroll ?? false)
			{
				return true;
			}
			// We don't have any information from the DOM 'scroll' event about the intermediate vs. final state.
			// We could try to rely on the IsPointerPressed state to detect when the user is scrolling and use it.
			// This would however not include scrolling due to the inertia which should also be flagged as intermediate.
			// The main issue is that the IsPointerPressed be true ONLY when dragging the scrollbars with the mouse, 
			// as for finger and pen we will get a PointerCancelled which will reset the pressed state to false.
			// And it would also require us to explicitly invoke OnScroll in PointerRelease in order to raise the
			// final SV.ViewChanged event with an IsIntermediate == false.
			// As a best-effort guess, we will consider the scroll as intermediate if the native offset is different from the last ScrollTo request.
			var horizontalOffset = GetNativeHorizontalOffset();
			var verticalOffset = GetNativeVerticalOffset();
			var isIntermediate =
				(_lastScrollToRequest.horizontal.HasValue && _lastScrollToRequest.horizontal.Value != horizontalOffset) ||
				(_lastScrollToRequest.vertical.HasValue && _lastScrollToRequest.vertical.Value != verticalOffset);
			if (!isIntermediate)
			{
				_lastScrollToRequest = (null, null);
			}

			if (IsArrangeDirty
				&& _pendingScrollTo is { } pending
				&& (
					pending.horizontal is { } hOffset && Math.Abs(horizontalOffset - hOffset) > 1
					|| pending.vertical is { } vOffset && Math.Abs(verticalOffset - vOffset) > 1)
				)
			{
				// When the native element of the SCP is becoming "valid" with a non 0 offset, it will raise a scroll event.
				// But if we have a manual scroll request pending, we need to mute it and wait for the next layout updated.

				return false;
			}

			_pendingScrollTo = default;

			HorizontalOffset = horizontalOffset;
			VerticalOffset = verticalOffset;

			Scroller?.OnPresenterScrolled(horizontalOffset, verticalOffset, isIntermediate);

			ScrollOffsets = new Point(horizontalOffset, verticalOffset);
			InvalidateViewport();

			return false;
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
