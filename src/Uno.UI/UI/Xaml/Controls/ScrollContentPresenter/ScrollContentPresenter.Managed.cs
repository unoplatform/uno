#if UNO_HAS_MANAGED_SCROLL_PRESENTER
using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.UI.Xaml.Input;
using Windows.Devices.Input;
using Windows.Foundation;
using Uno.Disposables;
using Uno.UI.Xaml.Core;

#if HAS_UNO_WINUI
using _PointerDeviceType = global::Microsoft.UI.Input.PointerDeviceType;
#else
using _PointerDeviceType = global::Windows.Devices.Input.PointerDeviceType;
#endif

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ScrollContentPresenter : ContentPresenter
#if !__CROSSRUNTIME__ && !IS_UNIT_TESTS
		, ICustomClippingElement
#endif
	{
		private /*readonly - partial*/ IScrollStrategy _strategy;
		private ScrollOptions? _touchInertiaOptions;

		private bool _canHorizontallyScroll;
		public bool CanHorizontallyScroll
		{
			get => _canHorizontallyScroll
#if __SKIA__
			|| _forceChangeToCurrentView
#endif
			;
			set => _canHorizontallyScroll = value;
		}

		private bool _canVerticallyScroll;
		public bool CanVerticallyScroll
		{
			get => _canVerticallyScroll
#if __SKIA__
			|| _forceChangeToCurrentView
#endif
			;
			set => _canVerticallyScroll = value;
		}

		public double HorizontalOffset { get; private set; }

		public double VerticalOffset { get; private set; }

		public double ExtentHeight { get; internal set; }

		public double ExtentWidth { get; internal set; }

		internal Size ScrollBarSize => new Size(0, 0);

		internal Size? CustomContentExtent => null;

		private object RealContent => Content;

		private readonly SerialDisposable _eventSubscriptions = new();

		partial void InitializePartial()
		{
#if __SKIA__
			_strategy = CompositorScrollStrategy.Instance;
#endif

			_strategy.Initialize(this);

			ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY; // Updated in PrepareTouchScroll!
		}

		private void HookScrollEvents(ScrollViewer sv)
		{
			UnhookScrollEvents(sv);

			// Note: the way WinUI does scrolling is very different, and doesn't use
			// PointerWheelChanged changes, etc.
			// We can either subscribe on the ScrollViewer or the SCP directly, but due to
			// the way hit-testing works (see #16201), the SCP will not receive any pointer
			// events. On WinUI, this is also the case: pointer presses are received on the SV,
			// not on the SCP.

			// Mouse wheel support
			sv.PointerWheelChanged += PointerWheelScroll;

			// Touch scroll support
			// Note: Events are hooked on the SCP itself, not the ScrollViewer
			ManipulationStarting += PrepareTouchScroll;
			ManipulationStarted += StartTouchScroll;
			ManipulationDelta += UpdateTouchScroll;
			ManipulationInertiaStarting += StartInertialTouchScroll;
			ManipulationCompleted += CompleteTouchScroll;

			_eventSubscriptions.Disposable = Disposable.Create(() =>
			{
				sv.PointerWheelChanged -= PointerWheelScroll;

				ManipulationStarting -= PrepareTouchScroll;
				ManipulationStarted -= StartTouchScroll;
				ManipulationDelta -= UpdateTouchScroll;
				ManipulationInertiaStarting -= StartInertialTouchScroll;
				ManipulationCompleted -= CompleteTouchScroll;
			});
		}

		private void UnhookScrollEvents(ScrollViewer sv)
		{
			_eventSubscriptions.Disposable = null;
		}

		private protected override void OnLoaded()
		{
			base.OnLoaded();
			if (Scroller is { } sv)
			{
				HookScrollEvents(sv);
			}
		}

		private protected override void OnUnloaded()
		{
			base.OnUnloaded();
			if (Scroller is { } sv)
			{
				UnhookScrollEvents(sv);
			}
		}

		/// <inheritdoc />
		protected override void OnContentChanged(object oldValue, object newValue)
		{
			if (oldValue is UIElement oldElt)
			{
				_strategy.Update(oldElt, 0, 0, 1, new(DisableAnimation: true));
			}

			base.OnContentChanged(oldValue, newValue);

			if (newValue is UIElement newElt)
			{
				_strategy.Update(newElt, HorizontalOffset, VerticalOffset, 1, new(DisableAnimation: true));
			}
		}

		internal void OnMinZoomFactorChanged(float newValue) { }

		internal void OnMaxZoomFactorChanged(float newValue) { }

		internal bool Set(
			double? horizontalOffset = null,
			double? verticalOffset = null,
			float? zoomFactor = null,
			bool disableAnimation = false,
			bool isIntermediate = false)
		{
			var success = true;

			if (horizontalOffset is double hOffset)
			{
				var maxOffset = Scroller?.ScrollableWidth ?? ExtentWidth - ViewportWidth;

				var scrollX = ValidateInputOffset(hOffset, 0, maxOffset);

				success &= scrollX == hOffset;

				if (!NumericExtensions.AreClose(HorizontalOffset, scrollX))
				{
					HorizontalOffset = scrollX;
				}
			}

			if (verticalOffset is double vOffset)
			{
				var maxOffset = Scroller?.ScrollableHeight ?? ExtentHeight - ViewportHeight;
				var scrollY = ValidateInputOffset(vOffset, 0, maxOffset);

				success &= scrollY == vOffset;

				if (!NumericExtensions.AreClose(VerticalOffset, scrollY))
				{
					VerticalOffset = scrollY;
				}
			}

			Apply(disableAnimation, isIntermediate);

			return success;
		}

		private void Apply(bool disableAnimation, bool isIntermediate)
		{
			if (Content is UIElement contentElt)
			{
				_strategy.Update(contentElt, HorizontalOffset, VerticalOffset, 1, _touchInertiaOptions ?? new(disableAnimation));
			}

			Scroller?.OnPresenterScrolled(HorizontalOffset, VerticalOffset, isIntermediate);

			// Note: We do not capture the offset so if they are altered in the OnPresenterScrolled,
			//		 we will apply only the final ScrollOffsets and only once.
			ScrollOffsets = new Point(HorizontalOffset, VerticalOffset);
			InvalidateViewport();
		}

		private void PrepareTouchScroll(object sender, ManipulationStartingRoutedEventArgs e)
		{
			if (e.Container != this)
			{
				// This gesture is coming from a nested element, we just ignore it!
				return;
			}

			if (e.Pointer.Type != PointerDeviceType.Touch
				|| (PointerCapture.TryGet(e.Pointer, out var capture) && capture.Options.HasFlag(PointerCaptureOptions.PreventOSSteal)))
			{
				e.Mode = ManipulationModes.None;
				return;
			}

			if (Scroller is
				{
					IsScrollInertiaEnabled: true,
					HorizontalSnapPointsType: not SnapPointsType.OptionalSingle and not SnapPointsType.MandatorySingle,
					VerticalSnapPointsType: not SnapPointsType.OptionalSingle and not SnapPointsType.MandatorySingle
				})
			{
				e.Mode |= ManipulationModes.TranslateInertia;
			}

			if (!CanVerticallyScroll || ExtentHeight <= 0)
			{
				e.Mode &= ~ManipulationModes.TranslateY;
			}

			if (!CanHorizontallyScroll || ExtentWidth <= 0)
			{
				e.Mode &= ~ManipulationModes.TranslateX;
			}
		}

		private void StartTouchScroll(object sender, ManipulationStartedRoutedEventArgs e)
		{
			if (e.Container != this)
			{
				// This gesture is coming from a nested element, we just ignore it!
				return;
			}

			if (e.PointerDeviceType == _PointerDeviceType.Touch)
			{
				Debug.Assert(PointerRoutedEventArgs.LastPointerEvent.Pointer.UniqueId == e.Pointers[0]);
				this.CapturePointer(PointerRoutedEventArgs.LastPointerEvent.Pointer);
			}
		}

		private void UpdateTouchScroll(object sender, ManipulationDeltaRoutedEventArgs e)
		{
			if (e.Container != this) // No needs to check the pointer type, if the manip is local it's touch, otherwise it was cancelled in starting.
			{
				// This gesture is coming from a nested element, we just ignore it!
				return;
			}


			if (e is { IsInertial: true, Manipulation: { } manipulation }
				&& manipulation.GetInertiaNextTick() is { } next)
			{
				// If inertial, we try to animate up to the next tick instead of applying current value synchronously
				Set(
					horizontalOffset: HorizontalOffset - next.Delta.Translation.X,
					verticalOffset: VerticalOffset - next.Delta.Translation.Y,
					disableAnimation: false,
					isIntermediate: true);
			}
			else
			{
				Set(
					horizontalOffset: HorizontalOffset - e.Delta.Translation.X,
					verticalOffset: VerticalOffset - e.Delta.Translation.Y,
					disableAnimation: true,
					isIntermediate: true);
			}
		}
		private void StartInertialTouchScroll(object sender, ManipulationInertiaStartingRoutedEventArgs e)
		{
			if (e.Container != this)
			{
				return;
			}

			// As we run animation by our own, we request to have pretty long delay between ticks to avoid too many updates.
			e.Interval = TimeSpan.FromMilliseconds(100);

			// Once the Interval is set, we try to begin animation up to the next (i.e. first) tick
			if (e.Manipulation.GetInertiaNextTick() is { } next)
			{
				// First we configure custom scrolling options to match what we just configured on the processor
				// Note: We configure animation to run a bit longer that a single tick to make sure to not have delay between animations
				_touchInertiaOptions = new(DisableAnimation: false, LinearAnimationDuration: TimeSpan.FromMilliseconds(105));

				// Then we start the animation
				Set(
					horizontalOffset: HorizontalOffset - next.Delta.Translation.X,
					verticalOffset: VerticalOffset - next.Delta.Translation.Y,
					disableAnimation: false,
					isIntermediate: true);
			}
		}

		private void CompleteTouchScroll(object sender, ManipulationCompletedRoutedEventArgs e)
		{
			if (e.Container != this || (PointerDeviceType)e.PointerDeviceType != PointerDeviceType.Touch)
			{
				return;
			}

			_touchInertiaOptions = null;

			Set(disableAnimation: true, isIntermediate: false);

			if (!e.IsInertial)
			{
				// If inertial the pointer as already been captured, and the LastPointerEvent can now be a new one!
				Debug.Assert(PointerRoutedEventArgs.LastPointerEvent.Pointer.UniqueId == e.Pointers[0]);
				this.ReleasePointerCapture(PointerRoutedEventArgs.LastPointerEvent.Pointer);
			}
		}

#if !__CROSSRUNTIME__ && !IS_UNIT_TESTS
		bool ICustomClippingElement.AllowClippingToLayoutSlot => true;
		bool ICustomClippingElement.ForceClippingToLayoutSlot => true; // force scrollviewer to always clip
#endif
	}
}
#endif
