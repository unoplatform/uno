#if UNO_HAS_MANAGED_SCROLL_PRESENTER
using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.UI.Composition;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Core;
using Windows.Foundation;

//using PointerDeviceType = Windows.Devices.Input.PointerDeviceType;

using static System.Net.Mime.MediaTypeNames;
using static Uno.UI.Xaml.Core.InputManager.PointerManager;

using _PointerDeviceType = global::Microsoft.UI.Input.PointerDeviceType;

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

		private GestureRecognizer.Manipulation? _touchInertia;
		private (double hOffset, double vOffset, bool isIntermediate) _lastScrolledEvent;

		// Batching: during active touch scrolling, we set AnchorPoint immediately (cheap) but
		// defer the expensive Updated() call (OnPresenterScrolled + InvalidateViewport + EVP propagation)
		// to once per frame via CompositionTarget.Rendering. This collapses N pointer moves
		// per frame into 1 Updated() call while keeping the visual perfectly in sync.
		private bool _hasPendingTouchUpdate;
		private double _pendingTouchHOffset;
		private double _pendingTouchVOffset;
		private bool _pendingTouchIsIntermediate;
		private EventHandler<object>? _touchUpdateHandler;

		// Inertia viewport throttle: PropagateEffectiveViewportChange() can trigger
		// heavy layout (ItemsRepeater measure, item recycling). If that layout doesn't
		// fully resolve in one UpdateLayout() pass, CanRecordPicture returns false and
		// the frame is SKIPPED, causing visible ghosting. By throttling EVP to every Nth
		// inertia frame, most frames are lightweight (just AnchorPoint + scroll offset DPs)
		// and always render. Items still materialize at ~20fps which is imperceptible.
		private int _inertiaFrameCount;
		private const int InertiaViewportUpdateInterval = 3;

#nullable restore

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
			Visual.Clip = Visual.Compositor.CreateInsetClip(0, 0, 0, 0);
#endif
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
			FlushPendingTouchUpdate();

			_touchInertia?.Complete();
			_touchInertia = null;
		}

		/// <inheritdoc />
		internal override bool HitTest(Point point)
			=> true; // Makes sure to get pointers events, even if no background.

#nullable enable
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
				Update(oldElt, 0, 0, 1, new(DisableAnimation: true));
			}

			base.OnContentChanged(oldValue, newValue);

			if (newValue is UIElement newElt)
			{
				Update(newElt, HorizontalOffset, VerticalOffset, 1, new(DisableAnimation: true));
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
			=> Set(horizontalOffset, verticalOffset, zoomFactor, options: new(disableAnimation, IsIntermediate: isIntermediate), callerName, callerLine);

		private bool Set(
			double? horizontalOffset = null,
			double? verticalOffset = null,
			float? zoomFactor = null,
			ScrollOptions options = default,
			[CallerMemberName] string callerName = "",
			[CallerLineNumber] int callerLine = -1)
		{
			bool success = true, updated = false;

			if (horizontalOffset is double hOffset)
			{
				var maxOffset = Scroller?.ScrollableWidth ?? ExtentWidth - ViewportWidth;
				var targetHorizontalOffset = ValidateInputOffset(hOffset, 0, maxOffset);

				success &= targetHorizontalOffset == hOffset;

				if (!NumericExtensions.AreClose(HorizontalOffset, targetHorizontalOffset))
				{
					HorizontalOffset = targetHorizontalOffset;
					updated = true;
				}
			}

			if (verticalOffset is double vOffset)
			{
				var maxOffset = Scroller?.ScrollableHeight ?? ExtentHeight - ViewportHeight;
				var targetVerticalOffset = ValidateInputOffset(vOffset, 0, maxOffset);

				success &= targetVerticalOffset == vOffset;

				if (!NumericExtensions.AreClose(VerticalOffset, targetVerticalOffset))
				{
					VerticalOffset = targetVerticalOffset;
					updated = true;
				}
			}

			_trace?.Invoke($"Scroll [{callerName}@{callerLine}] (success: {success} | updated: {updated} | req: h={horizontalOffset} v={verticalOffset} | actual: h={HorizontalOffset} v={VerticalOffset} | opts: {options})");

			if (!options.IsTouch)
			{
				// If we get a request to scroll to a specific offset **that is not flagged as IsInertial** (i.e. not coming from the inertia processing),
				// we stop the pending inertia processor.
				_touchInertia?.Complete();
			}

			var updatedHorizontalOffset = HorizontalOffset;
			var updatedVerticalOffset = VerticalOffset;
			if (updated || options.IsTouch)
			{
				if (Content is UIElement contentElt)
				{
					Update(contentElt, updatedHorizontalOffset, updatedVerticalOffset, 1, options);
				}
			}

			return success;
		}

		private long _stategyUpdateRequestId;
		private void Updated(double horizontalOffset, double verticalOffset, bool isIntermediate = false)
		{
			var request = Interlocked.Increment(ref _stategyUpdateRequestId);

			if (Uno.UI.Dispatching.NativeDispatcher.Main.HasThreadAccess)
			{
				UpdateOffsets(horizontalOffset, verticalOffset, isIntermediate);
			}
			else
			{
				DispatcherQueue.TryEnqueue(() =>
				{
					if (request == _stategyUpdateRequestId)
					{
						UpdateOffsets(horizontalOffset, verticalOffset, isIntermediate);
					}
				});
			}

			void UpdateOffsets(double updatedHorizontalOffset, double updatedVerticalOffset, bool isIntermediate = false)
			{
				// For the OnPresenterScrolled, we cannot rely only on the `updated` flag, we must also check for the isIntermediate flag!
				if (_lastScrolledEvent != (updatedHorizontalOffset, updatedVerticalOffset, isIntermediate))
				{
					_lastScrolledEvent = (updatedHorizontalOffset, updatedVerticalOffset, isIntermediate);

					Scroller?.OnPresenterScrolled(updatedHorizontalOffset, updatedVerticalOffset, isIntermediate);

				}

				// Note: We do not capture the offset so if they are altered in the OnPresenterScrolled,
				//		 we will apply only the final ScrollOffsets and only once.
				ScrollOffsets = new Point(updatedHorizontalOffset, updatedVerticalOffset);
				InvalidateViewport();
			}
		}

		private void Update(UIElement view, double horizontalOffset, double verticalOffset, double zoom, ScrollOptions options)
		{
			var target = new Vector2((float)-horizontalOffset, (float)-verticalOffset);
			var visual = view.Visual;

			// No matter the `options.DisableAnimation`, if we have an animation running
			if (visual.TryGetAnimationController(nameof(Visual.AnchorPoint)) is { } controller
				// ... that is animating to (almost) the same target value
				&& Vector2.DistanceSquared(visual.AnchorPoint, target) < 4
				// ... and which is about to complete
				&& controller.Remaining < TimeSpan.FromMilliseconds(50))
			{
				// We keep the animation running, making sure that we are not abruptly stopping scrolling animation
				// due to completion of the inertia processor a bit earlier than the animation itself.
				return;
			}


			if (options is { DisableAnimation: true } or { IsTouch: true })
			{
				// Only stop a running animation — during inertia no animation is active,
				// so calling StopAnimation every tick wastes time on dictionary lookups.
				if (visual.TryGetAnimationController(nameof(Visual.AnchorPoint)) is not null)
				{
					visual.StopAnimation(nameof(Visual.AnchorPoint));
				}

				// Always set AnchorPoint immediately — this is cheap (just a field write + RequestNewFrame).
				// Multiple writes per frame are fine; only the last value is used at render time.
				// Pixel-snap to integer values to avoid subpixel text rendering artifacts
				// (shimmer/ghosting from fractional offsets during inertia decay).
				visual.AnchorPoint = new Vector2(MathF.Round(target.X), MathF.Round(target.Y));

				if (options.IsTouch && options.IsIntermediate && _touchInertia is null)
				{
					// During active touch scrolling (finger on screen, NOT inertia), defer the expensive
					// Updated() call to once per frame. This batches N pointer moves into 1 Updated().
					ScheduleDeferredTouchUpdate(horizontalOffset, verticalOffset, options.IsIntermediate);
				}
				else if (options.IsTouch && options.IsIntermediate && _touchInertia is not null)
				{
					// Inertia: update immediately but throttle viewport propagation.
					// PropagateEffectiveViewportChange() can trigger heavy layout (ItemsRepeater
					// measure, item recycling). If layout doesn't fully resolve in one pass,
					// CanRecordPicture returns false → frame skipped → visible ghosting.
					// By only doing the full EVP every Nth frame, most frames are guaranteed
					// to render. Items materialize at ~20fps which is imperceptible.
					FlushPendingTouchUpdate();
					_inertiaFrameCount++;
					if (_inertiaFrameCount % InertiaViewportUpdateInterval == 0)
					{
						// Full update: scroll offsets + OnPresenterScrolled + InvalidateViewport
						Updated(horizontalOffset, verticalOffset, options.IsIntermediate);
					}
					else
					{
						// Lightweight update: scroll offsets + scrollbar DPs only (no EVP tree walk).
						// AnchorPoint was already set above, so the visual moves at 60fps.
						if (_lastScrolledEvent != (horizontalOffset, verticalOffset, true))
						{
							_lastScrolledEvent = (horizontalOffset, verticalOffset, true);
							Scroller?.OnPresenterScrolled(horizontalOffset, verticalOffset, isIntermediate: true);
						}
						ScrollOffsets = new Point(horizontalOffset, verticalOffset);
					}
				}
				else
				{
					// Non-intermediate (final) or non-touch: flush immediately with full update.
					FlushPendingTouchUpdate();
					Updated(horizontalOffset, verticalOffset, options.IsIntermediate);
				}
			}
			else
			{
				var compositor = visual.Compositor;
				var easing = CompositionEasingFunction.CreatePowerEasingFunction(compositor, CompositionEasingFunctionMode.Out, 10);
				var animation = compositor.CreateVector2KeyFrameAnimation();
				animation.InsertKeyFrame(1.0f, target, easing);
				animation.Duration = TimeSpan.FromSeconds(1);
				void OnFrame(CompositionAnimation? _) => Updated(Math.Round(-visual.AnchorPoint.X), Math.Round(-visual.AnchorPoint.Y), true);
				void OnStopped(object? _, EventArgs __)
				{
					animation.AnimationFrame -= OnFrame;
					animation.Stopped -= OnStopped;

					Updated(Math.Round(-visual.AnchorPoint.X), Math.Round(-visual.AnchorPoint.Y), false);
				}

				animation.AnimationFrame += OnFrame;
				animation.Stopped += OnStopped;

				visual.StartAnimation(nameof(Visual.AnchorPoint), animation);
			}
		}

		private void ScheduleDeferredTouchUpdate(double hOffset, double vOffset, bool isIntermediate)
		{
			_pendingTouchHOffset = hOffset;
			_pendingTouchVOffset = vOffset;
			_pendingTouchIsIntermediate = isIntermediate;

			if (!_hasPendingTouchUpdate)
			{
				_hasPendingTouchUpdate = true;
				_touchUpdateHandler ??= OnRenderingFlushTouchUpdate;
				CompositionTarget.Rendering += _touchUpdateHandler;
			}
		}

		private void OnRenderingFlushTouchUpdate(object? sender, object e)
		{
			// Unsubscribe immediately — we re-subscribe on next touch move if needed.
			CompositionTarget.Rendering -= _touchUpdateHandler;
			FlushPendingTouchUpdate();
		}

		private void FlushPendingTouchUpdate()
		{
			if (_hasPendingTouchUpdate)
			{
				_hasPendingTouchUpdate = false;
				Updated(_pendingTouchHOffset, _pendingTouchVOffset, _pendingTouchIsIntermediate);
			}
		}

		private void TryEnableDirectManipulation(object sender, PointerRoutedEventArgs args)
		{
			if (args.Pointer.PointerDeviceType is not (_PointerDeviceType.Pen or _PointerDeviceType.Touch))
			{
				return;
			}

			XamlRoot?.VisualTree.ContentRoot.InputManager.Pointers.RegisterDirectManipulationHandler(args.Pointer.UniqueId, this);
		}

		object? IDirectManipulationHandler.Owner => ScrollOwner;

		/// <inheritdoc />
		ManipulationModes IDirectManipulationHandler.OnStarting(GestureRecognizer _, ManipulationStartingEventArgs args)
		{
			// If a previous inertia is still tracked, clear it immediately so old inertial deltas
			// cannot fight with the new manipulation's direction.
			_touchInertia = null;

			// Stop any running composition-driven inertia animation (user touched during fling).


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

		bool IDirectManipulationHandler.CanAddPointerAt(in Point absoluteLocation)
			=> GetTransform(this, null).Transform(new Rect(new Point(), LayoutSlotWithMarginsAndAlignments.Size)).Contains(absoluteLocation);

		void IDirectManipulationHandler.OnStarted(GestureRecognizer recognizer, ManipulationStartedEventArgs args, bool isResuming)
		{
			Debug.Assert(_touchInertia is null || isResuming, "Inertia should already be null instead if we are resuming from a previous manipulation.");
			_touchInertia = null;
#if __SKIA__
			Scroller?.EnterIntermediateViewChangedMode();
#endif
		}

		/// <inheritdoc />
		void IDirectManipulationHandler.OnUpdated(GestureRecognizer recognizer, ManipulationUpdatedEventArgs args, ref ManipulationDelta unhandledDelta)
		{
			if (Scroller is not { } sv || unhandledDelta is { IsEmpty: true })
			{
				return;
			}

			var scrollable = GetScrollableOffsets();
			var deltaX = Math.Clamp(-unhandledDelta.Translation.X, scrollable.Left, scrollable.Right);
			var deltaY = Math.Clamp(-unhandledDelta.Translation.Y, scrollable.Up, scrollable.Down);

			if (args.IsInertial)
			{
				if (_touchInertia is null)
				{
					// A scroll to has been requested - OR - inertia was not allowed in the InertiaStarting (e.g. snap points / end of scroll)
					// Note: we do not stop the processor to let parent SV handle it (if any)
					return;
				}

				// We handle inertia locally, in that case we do not want to chain the scroll to the parent SV
				unhandledDelta = UI.Input.ManipulationDelta.Empty;

				Set(
					horizontalOffset: HorizontalOffset + deltaX,
					verticalOffset: VerticalOffset + deltaY,
					options: new(DisableAnimation: true, IsTouch: true, IsIntermediate: true));
			}
			else
			{
				unhandledDelta.Translation.X += deltaX;
				unhandledDelta.Translation.Y += deltaY;

				Set(
					horizontalOffset: HorizontalOffset + deltaX,
					verticalOffset: VerticalOffset + deltaY,
					options: new(DisableAnimation: true, IsTouch: true, IsIntermediate: true));

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
		bool IDirectManipulationHandler.OnInertiaStarting(GestureRecognizer recognizer, ManipulationInertiaStartingEventArgs args, bool isHandled)
		{
			if (isHandled)
			{
				_touchInertia = null; // Make clear that inertia is not allowed for the OnUpdated, but this is only for safety!

				return false;
			}

			if (Scroller is not { IsScrollInertiaEnabled: true } sv)
			{
				_touchInertia = null;
				return false;
			}

			var direction = GetDirection(args.Velocities);

			// Check if we have snap points configured - if so, we should handle inertia even with limited scrollable space
			bool hasValidSnapPoints(ScrollDirection dir)
			{
				if (dir.HasFlag(ScrollDirection.Left) || dir.HasFlag(ScrollDirection.Right))
				{
					return sv.HorizontalSnapPointsType is not SnapPointsType.None;
				}
				if (dir.HasFlag(ScrollDirection.Up) || dir.HasFlag(ScrollDirection.Down))
				{
					return sv.VerticalSnapPointsType is not SnapPointsType.None;
				}

				return false;
			}

			var scrollable = GetScrollableOffsets();
			var isScrollableValid = scrollable.IsValid(direction);
			var hasSnapPoints = hasValidSnapPoints(direction);

			if ((!isScrollableValid && !hasSnapPoints) // Nothing to scroll and no snap points
				|| recognizer.PendingManipulation is null) // Stopped by a child element (e.g. a child SV that is scrolling to a mandatory snap-point) - safety, should already be isHandled = true
			{
				// Inertia is starting but we cannot handle it.
				// At this point we don't know if we another (child) SV we be able to handle it, so we do NOT abort the gesture.

				_touchInertia = null; // Make clear that inertia is not allowed for the OnUpdated, but this is only for safety!

				return false;
			}

			var inertia = args.Manipulation.Inertia ?? throw new InvalidOperationException("Inertia processor is not available.");
			if (OperatingSystem.IsIOS())
			{
				var v0 = (scrollable.Horizontally, scrollable.Vertically) switch
				{
					(true, false) => Math.Abs(args.Velocities.Linear.X),
					(false, true) => Math.Abs(args.Velocities.Linear.Y),
					(true, true) => (Math.Abs(args.Velocities.Linear.X) + Math.Abs(args.Velocities.Linear.Y)) / 2,
					_ => 0
				};

				// PastryKit-inspired iOS scroll physics: 0.95 friction factor per frame at 60fps.
				// This maps directly to the exponential decay model: k = -ln(0.95)/16.67 ≈ 0.003
				const double PKScrollViewDecelerationFrictionFactor = 0.95;
				const double PKScrollViewDesiredAnimationFrameRate = 1000 / 60.0;
				const double PKScrollViewMinimumVelocity = 0.01;
				var frames = Math.Log(PKScrollViewMinimumVelocity / v0, PKScrollViewDecelerationFrictionFactor);
				var duration = frames * PKScrollViewDesiredAnimationFrameRate;

				inertia.DesiredDisplacementDeceleration = GestureRecognizer.Manipulation.InertiaProcessor.GetDecelerationFromDesiredDuration(v0, duration);
			}
			else if (OperatingSystem.IsAndroid())
			{
				// Gentler decay for Android, matching the longer fling feel of native Android OverScroller.
				// 0.0025 ≈ 0.96 decay per 16.67ms frame (vs WinUI's 0.95).
				inertia.DesiredDisplacementDeceleration = 0.0025;
			}
			else
			{
				// Desktop/other: match WinUI InteractionTracker default (0.95 decay per frame).
				inertia.DesiredDisplacementDeceleration = GestureRecognizer.Manipulation.InertiaProcessor.DefaultDesiredDisplacementDeceleration;
			}

			// If we have snap points, we disable the inertia support (for local SV).
			// However, we determine the final value of the inertia to snap on the right snap-point.
			var shouldSnapHorizontally = scrollable.Horizontally && sv is { HorizontalSnapPointsType: SnapPointsType.OptionalSingle or SnapPointsType.MandatorySingle };
			var shouldSnapVertically = scrollable.Vertically && sv is { VerticalSnapPointsType: SnapPointsType.OptionalSingle or SnapPointsType.MandatorySingle };
			var shouldSnapToTouchTextBox = sv.ShouldSnapToTouchTextBox();
			if (shouldSnapHorizontally || shouldSnapVertically || shouldSnapToTouchTextBox)
			{
				// Make clear that inertia is not allowed for the OnUpdated, but this is only for safety!
				_touchInertia = null;

				// We somehow handle the inertia ourselves, so we complete the gesture right now (prevent parents to also handle it).
				// Note: We must make sure to invoke CompleteGesture() before the `Set` below as the complete will invoke the OnCompleted handler.
				recognizer.CompleteGesture();

				double? h = null, v = null;

				if (shouldSnapHorizontally || shouldSnapToTouchTextBox)
				{
					var v0 = args.Velocities.Linear.X;
					var duration = GestureRecognizer.Manipulation.InertiaProcessor.GetCompletionTime(v0, inertia.DesiredDisplacementDeceleration);
					var endValue = GestureRecognizer.Manipulation.InertiaProcessor.GetValue(v0, inertia.DesiredDisplacementDeceleration, duration);

					h = HorizontalOffset - endValue;
				}

				if (shouldSnapVertically || shouldSnapToTouchTextBox)
				{
					var v0 = args.Velocities.Linear.Y;
					var duration = GestureRecognizer.Manipulation.InertiaProcessor.GetCompletionTime(v0, inertia.DesiredDisplacementDeceleration);
					var endValue = GestureRecognizer.Manipulation.InertiaProcessor.GetValue(v0, inertia.DesiredDisplacementDeceleration, duration);

					v = VerticalOffset - endValue;
				}

				sv.AdjustOffsetsForSnapPoints(ref h, ref v, null);

				// note: IsTouch = true as we are not in the touch scrolling anymore here, we are just snapping.
				Set(horizontalOffset: h, verticalOffset: v, disableAnimation: false, isIntermediate: false);
			}
			else
			{
				// We can handle the inertia scrolling, configure to accept allow it by assigning the _touchInertia field.
				_touchInertia = args.Manipulation;
				_inertiaFrameCount = 0;

				// Even if usually empty, make sure to apply the delta
				var deltaX = Math.Clamp(-args.Delta.Translation.X, scrollable.Left, scrollable.Right);
				var deltaY = Math.Clamp(-args.Delta.Translation.Y, scrollable.Up, scrollable.Down);

				Set(
					horizontalOffset: HorizontalOffset + deltaX,
					verticalOffset: VerticalOffset + deltaY,
					options: new(DisableAnimation: true, IsTouch: true, IsIntermediate: true));
			}

			return true;
		}

		/// <inheritdoc />
		void IDirectManipulationHandler.OnCompleted(GestureRecognizer _, ManipulationCompletedEventArgs? args)
		{
			if (args?.IsInertial is true && _touchInertia is null)
			{
				// Inertia has been aborted (external ChangeView request?) or was not even allowed, do not try to apply the final value.
#if __SKIA__
				Scroller?.LeaveIntermediateViewChangedMode(raiseFinalViewChanged: false);
#endif
				return;
			}

			_touchInertia = null;

			//Set(disableAnimation: true, isIntermediate: false);
			Set(options: new ScrollOptions(DisableAnimation: true, IsTouch: true, IsIntermediate: false));
#if __SKIA__
			Scroller?.LeaveIntermediateViewChangedMode(raiseFinalViewChanged: true);
#endif
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

		/// <param name="Up">Offset that can be scrolled up. THIS IS ALWAYS NEGATIVE.</param>
		/// <param name="Down">Offset that can be scrolled down. This is always positive.</param>
		/// <param name="Left">Offset that can be scrolled left. THIS IS ALWAYS NEGATIVE.</param>
		/// <param name="Right">Offset that can be scrolled up. This is always positive.</param>
		private record struct ScrollableOffsets(double Up, double Down, double Left, double Right)
		{
			public bool Vertically { get; } = Up < 0 || Down > 0;

			public bool Horizontally { get; } = Left < 0 || Right > 0;

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

	/// <summary>
	/// Options for the ScrollContentPrensenter.Update
	/// </summary>
	/// <param name="DisableAnimation">Request to disable the animation.</param>
	/// <param name="LinearAnimationDuration">
	/// Requests to use a linear animation with a specific duration instead of the default animation strategy.
	/// This is for the for inertia processor with touch scrolling where the total duration is calculated based on the velocity.
	/// </param>
	/// <param name="IsTouch">Indicates that the scroll is coming from an inertia processor.</param>
	/// <param name="IsIntermediate">
	/// Indicates that the scroll is an intermediate value, not the final one
	/// (i.e. active touch scrolling, touch scroll inertia or scroll animation).
	/// </param>
	internal record struct ScrollOptions(bool DisableAnimation = false, bool IsTouch = false, bool IsIntermediate = false);
}
#endif
