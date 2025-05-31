﻿#if UNO_HAS_MANAGED_SCROLL_PRESENTER
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;
using Microsoft.UI.Input;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Core;
using static Uno.UI.Xaml.Core.InputManager.PointerManager;
using PointerDeviceType = Windows.Devices.Input.PointerDeviceType;

#if HAS_UNO_WINUI
using _PointerDeviceType = global::Microsoft.UI.Input.PointerDeviceType;
#else
using _PointerDeviceType = global::Windows.Devices.Input.PointerDeviceType;
#endif

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ScrollContentPresenter : ContentPresenter, IDirectManipulationHandler
#if !__CROSSRUNTIME__ && !IS_UNIT_TESTS
		, ICustomClippingElement
#endif
	{
#nullable enable
		private static readonly Action<string>? _trace = typeof(ScrollContentPresenter).Log().IsEnabled(LogLevel.Trace)
			? typeof(ScrollContentPresenter).Log().Trace
			: null;
#nullable restore

		private /*readonly - partial*/ IScrollStrategy _strategy;
		private ScrollOptions? _touchInertiaOptions;
		private int _touchInertiaSkipTicks;
		private static readonly TimeSpan _defaultTouchIndependentAnimationDuration = TimeSpan.FromMilliseconds(80); // Inertia processors is configured to be at 25 fps, with 80 we skip half of the ticks.
		private static readonly TimeSpan _defaultTouchIndependentAnimationOverlap = TimeSpan.FromMilliseconds(5); // Duration of the animation to run after the expected next tick, to make sure we don't have a gap between animations.

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

		private ScrollableOffsets GetScrollableOffsets()
		{
			var hOffset = HorizontalOffset;
			var vOffset = VerticalOffset;

			double up, down, left, right;
			if (CanVerticallyScroll)
			{
				up = -vOffset;
				down = Math.Max(0, ExtentHeight - ViewportHeight) - vOffset;
			}
			else
			{
				up = down = 0;
			}

			if (CanHorizontallyScroll)
			{
				left = -hOffset;
				right = Math.Max(0, ExtentWidth - ViewportWidth) - hOffset;
			}
			else
			{
				left = right = 0;
			}

			return new(up, down, left, right);
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
		internal override bool HitTest(Point point)
			=> true; // Makes sure to get pointers events, even if no background.

		private void HookScrollEvents(ScrollViewer sv)
		{
			UnhookScrollEvents(sv);

			// Note: the way WinUI does scrolling is very different, and doesn't use PointerWheelChanged changes, etc.
			// Note 2: We subscribe on the ScrollViewer so no matter if the content of the SCP is hit-testable or not
			//		as the root Grid of the SV is hit-testable, we get the events.
			//		On WinUI, this is also the case: pointer presses are received on the SV, not on the SCP.
			// Note 2: All of those should probably be moved to the SV directly!
			// Note 3: We should also consider to use the new ScrollPresenter under the hood to re-use all the composition tracking logic.

			// Mouse wheel support
			sv.PointerWheelChanged += PointerWheelScroll;

			// Touch and pen scroll support
			// Note: We add handler on this (not SV) in order to make sure to get it first
			//		 (and especially before the RefreshContainers - which subscribe to the same event on the SV)
			var handler = new PointerEventHandler(TryEnableDirectManipulation);
			AddHandler(PointerPressedEvent, handler, handledEventsToo: true);

			_eventSubscriptions.Disposable = Disposable.Create(() =>
			{
				sv.PointerWheelChanged -= PointerWheelScroll;
				RemoveHandler(PointerPressedEvent, handler);
			});
		}

		private void UnhookScrollEvents(ScrollViewer sv)
		{
			_eventSubscriptions.Disposable = null;
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
			bool isIntermediate = false,
			[CallerMemberName] string callerName = "",
			[CallerLineNumber] int callerLine = -1)
			=> Set(horizontalOffset, verticalOffset, zoomFactor, options: new(disableAnimation), isIntermediate, callerName, callerLine);

		private bool Set(
			double? horizontalOffset = null,
			double? verticalOffset = null,
			float? zoomFactor = null,
			ScrollOptions options = default,
			bool isIntermediate = false,
			[CallerMemberName] string callerName = "",
			[CallerLineNumber] int callerLine = -1)
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

			_trace?.Invoke($"Scroll [{callerName}@{callerLine}] (success: {success} | req: h={horizontalOffset} v={verticalOffset} | actual: h={HorizontalOffset} v={VerticalOffset} | inter: {isIntermediate} | opts: {options})");

			Apply(options, isIntermediate);

			return success;
		}

		private void Apply(ScrollOptions options, bool isIntermediate)
		{
			if (options != _touchInertiaOptions)
			{
				_touchInertiaOptions = null; // abort inertia if ScrollTo is being requested
			}

			if (Content is UIElement contentElt)
			{
				_strategy.Update(contentElt, HorizontalOffset, VerticalOffset, 1, options);
			}

			Scroller?.OnPresenterScrolled(HorizontalOffset, VerticalOffset, isIntermediate);

			// Note: We do not capture the offset so if they are altered in the OnPresenterScrolled,
			//		 we will apply only the final ScrollOffsets and only once.
			ScrollOffsets = new Point(HorizontalOffset, VerticalOffset);
			InvalidateViewport();
		}

		private void TryEnableDirectManipulation(object sender, PointerRoutedEventArgs args)
		{
			if (args.Pointer.PointerDeviceType is not (_PointerDeviceType.Pen or _PointerDeviceType.Touch))
			{
				return;
			}

			XamlRoot?.VisualTree.ContentRoot.InputManager.Pointers.RegisterDirectManipulationTarget(args.Pointer.UniqueId, this);
		}

		/// <inheritdoc />
		ManipulationModes IDirectManipulationHandler.OnStarting(GestureRecognizer _, ManipulationStartingEventArgs args)
		{
			if (args.Pointer.Type is not (PointerDeviceType.Pen or PointerDeviceType.Touch)
				|| (PointerCapture.TryGet(args.Pointer, out var capture) && capture.Options.HasFlag(PointerCaptureOptions.PreventDirectManipulation)))
			{
				return ManipulationModes.None;
			}

			var mode = ManipulationModes.None;
			var scrollable = GetScrollableOffsets();
			if (scrollable.Horizontally)
			{
				mode |= ManipulationModes.TranslateX;
			}

			if (scrollable.Vertically)
			{
				mode |= ManipulationModes.TranslateY;
			}

			if (Scroller is { } sv)
			{
				if (sv.IsScrollInertiaEnabled)
				{
					mode |= ManipulationModes.TranslateInertia;
				}

				if (sv.IsHorizontalRailEnabled)
				{
					mode |= ManipulationModes.TranslateRailsX;
				}

				if (sv.IsVerticalRailEnabled)
				{
					mode |= ManipulationModes.TranslateRailsY;
				}
			}

			return mode;
		}

		/// <inheritdoc />
		void IDirectManipulationHandler.OnUpdated(GestureRecognizer recognizer, ManipulationUpdatedEventArgs args, ref ManipulationDelta unhandledDelta)
		{
			if (Scroller is not { } sv || unhandledDelta is { IsEmpty: true })
			{
				return;
			}

			if (args.IsInertial)
			{
				if (_touchInertiaOptions is null)
				{
					// A scroll to has been requested - OR - inertia was not allowed in the InertiaStarting (e.g. snap points / end of scroll)
					// Note: we do not stop the processor to let parent SV handle it (if any)
					return;
				}

				// We handle inertia locally, in that case we do not want to chain the scroll to the parent SV
				unhandledDelta = UI.Input.ManipulationDelta.Empty;

				if (--_touchInertiaSkipTicks > 0)
				{
					// Skipping this dependent update frame (composition animation is still running)
					return;
				}

				var independentAnimationDuration = _defaultTouchIndependentAnimationDuration;
				if (args.Manipulation.GetInertiaTickAligned(ref independentAnimationDuration, out _touchInertiaSkipTicks) is not { } next)
				{
					// Weird case, we do not have a next tick (we should always have at least the inertia processor interval), ignore it (expecting a complete soon)
					return;
				}

				// If inertial, we try to animate up to the next tick instead of applying current value synchronously
				_touchInertiaOptions = new(DisableAnimation: false, LinearAnimationDuration: independentAnimationDuration + _defaultTouchIndependentAnimationOverlap);
				Set(
					horizontalOffset: HorizontalOffset - next.Delta.Translation.X,
					verticalOffset: VerticalOffset - next.Delta.Translation.Y,
					options: _touchInertiaOptions.Value,
					isIntermediate: true);
			}
			else
			{
				var scrollable = GetScrollableOffsets();
				var deltaX = Math.Clamp(-unhandledDelta.Translation.X, scrollable.Left, scrollable.Right);
				var deltaY = Math.Clamp(-unhandledDelta.Translation.Y, scrollable.Up, scrollable.Down);

				unhandledDelta.Translation.X += deltaX;
				unhandledDelta.Translation.Y += deltaY;

				Set(
					horizontalOffset: HorizontalOffset + deltaX,
					verticalOffset: VerticalOffset + deltaY,
					disableAnimation: true,
					isIntermediate: true);

				if (!sv.IsHorizontalScrollChainingEnabled)
				{
					unhandledDelta.Translation.X = 0;
				}

				if (!sv.IsVerticalScrollChainingEnabled)
				{
					unhandledDelta.Translation.Y = 0;
				}
			}
		}

		/// <inheritdoc />
		void IDirectManipulationHandler.OnInertiaStarting(GestureRecognizer recognizer, ManipulationInertiaStartingEventArgs args, ref bool isHandled)
		{
			if (isHandled)
			{
				_touchInertiaOptions = null; // Make clear that inertia is not allowed for the OnUpdated, but this is only for safety!

				return;
			}

			var scrollable = GetScrollableOffsets();
			var direction = GetDirection(args.Velocities);
			if (Scroller is not { IsScrollInertiaEnabled: true } sv
				|| !scrollable.IsValid(direction) // Nothing to scroll
				|| recognizer.PendingManipulation is null) // Stopped by a child element (e.g. a child SV that is scrolling to a mandatory snap-point) - safety, should already be isHandled = true
			{
				// Inertia is starting but we cannot handle it.
				// At this point we don't know if we another (child) SV we be able to handle it, so we do NOT abort the gesture.

				_touchInertiaOptions = null; // Make clear that inertia is not allowed for the OnUpdated, but this is only for safety!

				return;
			}

			isHandled = true;

			var inertia = args.Manipulation.Inertia ?? throw new InvalidOperationException("Inertia processor is not available.");
			if (OperatingSystem.IsIOS())
			{
				var v0 = (scrollable.Horizontally, scrollable.Vertically) switch
				{
					(true, false) => args.Velocities.Linear.X,
					(false, true) => args.Velocities.Linear.Y,
					(true, true) => (Math.Abs(args.Velocities.Linear.X) + Math.Abs(args.Velocities.Linear.Y)) / 2,
					_ => 0
				};
				inertia.DesiredDisplacementDeceleration = GestureRecognizer.Manipulation.InertiaProcessor.GetDecelerationFromDesiredDuration(v0, 2750);
			}
			else if (OperatingSystem.IsAndroid())
			{
				inertia.DesiredDisplacementDeceleration = GestureRecognizer.Manipulation.InertiaProcessor.DefaultDesiredDisplacementDeceleration / 2;
			}
			else
			{
				inertia.DesiredDisplacementDeceleration = GestureRecognizer.Manipulation.InertiaProcessor.DefaultDesiredDisplacementDeceleration;
			}

			// If we have snap points, we disable the inertia support (for local SV).
			// However, we determine the final value of the inertia to snap on the right snap-point.
			var shouldSnapHorizontally = scrollable.Horizontally && sv is { HorizontalSnapPointsType: SnapPointsType.OptionalSingle or SnapPointsType.MandatorySingle };
			var shouldSnapVertically = scrollable.Vertically && sv is { VerticalSnapPointsType: SnapPointsType.OptionalSingle or SnapPointsType.MandatorySingle };
			if (shouldSnapHorizontally || shouldSnapVertically)
			{
				// Make clear that inertia is not allowed for the OnUpdated, but this is only for safety!
				_touchInertiaOptions = null;

				// We somehow handle the inertia ourselves, so we complete the gesture right now (prevent parents to also handle it).
				// Note: We must make sure to invoke CompleteGesture() before the `Set` below as the complete will invoke the OnCompleted handler.
				recognizer.CompleteGesture();

				double? h = null, v = null;

				if (shouldSnapHorizontally)
				{
					var v0 = args.Velocities.Linear.X;
					var duration = GestureRecognizer.Manipulation.InertiaProcessor.GetCompletionTime(v0, inertia.DesiredDisplacementDeceleration);
					var endValue = GestureRecognizer.Manipulation.InertiaProcessor.GetValue(v0, inertia.DesiredDisplacementDeceleration, duration);

					h = HorizontalOffset - endValue;
				}

				if (shouldSnapVertically)
				{
					var v0 = args.Velocities.Linear.Y;
					var duration = GestureRecognizer.Manipulation.InertiaProcessor.GetCompletionTime(v0, inertia.DesiredDisplacementDeceleration);
					var endValue = GestureRecognizer.Manipulation.InertiaProcessor.GetValue(v0, inertia.DesiredDisplacementDeceleration, duration);

					v = VerticalOffset - endValue;
				}

				sv.AdjustOffsetsForSnapPoints(ref h, ref v, null);
				Set(horizontalOffset: h, verticalOffset: v, disableAnimation: false, isIntermediate: false);

				return;
			}

			// We let the inertia start ...
			// ... but we run animation scroll animations by our own, so we request to the inertia processor to give us info for the next few ms, and we start a composition scroll animation.
			var independentAnimationDuration = _defaultTouchIndependentAnimationDuration;
			if (args.Manipulation.GetInertiaTickAligned(ref independentAnimationDuration, out _touchInertiaSkipTicks) is { } next)
			{
				// First we configure custom scrolling options to match what we just configured on the processor
				// Note: We configure animation to run a bit longer than tick to make sure to not have delay between animations
				_touchInertiaOptions = new(DisableAnimation: false, LinearAnimationDuration: independentAnimationDuration + _defaultTouchIndependentAnimationOverlap);

				// Then we start the animation
				Set(
					horizontalOffset: HorizontalOffset - next.Delta.Translation.X,
					verticalOffset: VerticalOffset - next.Delta.Translation.Y,
					options: _touchInertiaOptions.Value,
					isIntermediate: true);
			}
		}

		/// <inheritdoc />
		void IDirectManipulationHandler.OnCompleted(GestureRecognizer _, ManipulationCompletedEventArgs args)
		{
			if ((PointerDeviceType)args.PointerDeviceType != PointerDeviceType.Touch)
			{
				return;
			}

			if (args.IsInertial && _touchInertiaOptions is null)
			{
				// Inertia has been aborted (external ChangeView request?) or was not even allowed, do not try to apply the final value.
				return;
			}

			_touchInertiaOptions = null;

			Set(disableAnimation: true, isIntermediate: false);
		}

		private ScrollDirection GetDirection(ManipulationVelocities velocities)
		{
			var direction = default(ScrollDirection);

			direction |= velocities.Linear.X switch
			{
				< 0 => ScrollDirection.Right,
				> 0 => ScrollDirection.Left,
				_ => default
			};
			direction |= velocities.Linear.Y switch
			{
				< 0 => ScrollDirection.Down,
				> 0 => ScrollDirection.Up,
				_ => default
			};

			return direction;
		}


#if !__CROSSRUNTIME__ && !IS_UNIT_TESTS
		bool ICustomClippingElement.AllowClippingToLayoutSlot => true;
		bool ICustomClippingElement.ForceClippingToLayoutSlot => true; // force scrollviewer to always clip
#endif

		private record struct ScrollableOffsets(double Up, double Down, double Left, double Right)
		{
			public bool Vertically { get; } = Up < 0 || Down > 0;

			public bool Horizontally { get; } = Left < 0 || Right > 0;

			public bool None => !Vertically && !Horizontally;

			public bool IsValid(ScrollDirection direction)
			{
				if (direction.HasFlag(ScrollDirection.Up) && Up < 0)
				{
					return true;
				}
				if (direction.HasFlag(ScrollDirection.Down) && Down > 0)
				{
					return true;
				}
				if (direction.HasFlag(ScrollDirection.Left) && Left < 0)
				{
					return true;
				}
				if (direction.HasFlag(ScrollDirection.Right) && Right > 0)
				{
					return true;
				}

				return false;
			}
		}

		[Flags]
		private enum ScrollDirection
		{
			Up = 1 << 1,
			Down = 1 << 2,
			Left = 1 << 3,
			Right = 1 << 4
		}
	}
}
#endif
