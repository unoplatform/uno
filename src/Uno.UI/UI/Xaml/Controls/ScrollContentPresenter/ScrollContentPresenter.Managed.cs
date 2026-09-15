using System;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.UI.Composition;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using Uno.Disposables;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Core;
using Windows.Foundation;

//using PointerDeviceType = Windows.Devices.Input.PointerDeviceType;

using static Uno.UI.Xaml.Core.InputManager.PointerManager;

using _PointerDeviceType = global::Microsoft.UI.Input.PointerDeviceType;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ScrollContentPresenter : ContentPresenter, IDirectManipulationHandler
#if !__CROSSRUNTIME__
		, ICustomClippingElement
#endif
	{
#nullable enable
		private static readonly Action<string>? _trace = typeof(ScrollContentPresenter).Log().IsEnabled(LogLevel.Trace)
			? typeof(ScrollContentPresenter).Log().Trace
			: null;

		private (double hOffset, double vOffset, bool isIntermediate) _lastScrolledEvent;

		private ScrollDecaySimulation _wheelDecayH;
		private ScrollDecaySimulation _wheelDecayV;
		private bool _isWheelDecayRunning;

		private readonly Microsoft.UI.Input.ScrollVelocityTracker _velocityTracker = new();
		private uint _velocityTrackerContacts;
		private ScrollFlingSimulation _flingH;
		private ScrollFlingSimulation _flingV;
		private long _flingStartTimestamp;
		private bool _isFlingRunning;

		// The manipulation kept inertial for the duration of a fling. It moves nothing — the fling does —
		// but holding it is what lets a press over the coasting content stop it and be swallowed.
		private GestureRecognizer.Manipulation? _touchInertia;

		// The frame driver is resolved through the visual, which drops its CompositionTarget on unload.
		// Unsubscribing has to go through the target we subscribed to, or the handler leaks and the
		// compositor keeps a frame driver alive forever.
		private Media.CompositionTarget? _flingTarget;
		private Media.CompositionTarget? _wheelDecayTarget;
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

			// When zoomed, the effective extent is scaled by the zoom factor
			var scaledExtentWidth = ExtentWidth * _zoomFactor;
			var scaledExtentHeight = ExtentHeight * _zoomFactor;

			double up, down, left, right;
			if (CanVerticallyScroll)
			{
				up = -vOffset;
				down = Math.Max(0, scaledExtentHeight - ViewportHeight) - vOffset;
			}
			else
			{
				up = down = 0;
			}

			if (CanHorizontallyScroll)
			{
				left = -hOffset;
				right = Math.Max(0, scaledExtentWidth - ViewportWidth) - hOffset;
			}
			else
			{
				left = right = 0;
			}

			return new(up, down, left, right);
		}

		public double HorizontalOffset { get; private set; }

		public double VerticalOffset { get; private set; }

		// Zoom state
		private float _zoomFactor = 1.0f;
		private float _minZoomFactor = 0.1f;
		private float _maxZoomFactor = 10.0f;
		internal float ZoomFactor => _zoomFactor;

		public double ExtentHeight { get; internal set; }

		public double ExtentWidth { get; internal set; }

		internal Size ScrollBarSize => new Size(0, 0);

		internal Size? CustomContentExtent => null;

		// True while a scroll motion owns the offsets. Used by ScrollViewer.RecomputeOffsetsFromIntent
		// to avoid interrupting one with an instant Set; the recompute runs again on the next layout
		// pass once the motion is over.
		internal bool IsScrollAnimationInProgress
		{
			get
			{
				// A fling and a wheel decay own the offsets exactly like an animation does, but they are frame
				// drivers rather than composition animations, so the controller check below cannot see them.
				// Left unreported, an intent armed mid-motion snaps the offset back on the next layout pass.
				if (_isFlingRunning || _isWheelDecayRunning)
				{
					return true;
				}

				if (Content is UIElement contentElt && contentElt.Visual is { } visual
					&& visual.TryGetAnimationController(nameof(Visual.AnchorPoint)) is { } controller)
				{
					// A KeyFrameAnimation that completed naturally stays in the owning
					// CompositionObject's animation dictionary (only StopAnimation removes it),
					// so the controller's mere presence is not a reliable "in progress" signal.
					// Check the remaining time instead.
					return controller.Remaining > TimeSpan.Zero;
				}
				return false;
			}
		}

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
			StopWheelDecayAndPublishFinalOffsets();
			StopFlingAndPublishFinalOffsets();

			// The processor outlives the fling on purpose, so unloading has to end it explicitly: left running
			// it holds a frame driver, keeps this presenter rooted and swallows presses on a tree it has left.
			CompleteTouchInertia();
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
				// Reset old content's transform
				Update(oldElt, 0, 0, 1, new(DisableAnimation: true));
			}

			base.OnContentChanged(oldValue, newValue);

			if (newValue is UIElement newElt)
			{
				// Apply current scroll and zoom state to new content
				Update(newElt, HorizontalOffset, VerticalOffset, _zoomFactor, new(DisableAnimation: true));
			}
		}

		internal void OnMinZoomFactorChanged(float newValue)
		{
			// While zoom is disabled the range must stay pinned to 1, otherwise a later MinZoomFactor
			// change would silently re-open the zoom range.
			if (Scroller?.ZoomMode == ZoomMode.Disabled)
			{
				newValue = 1f;
			}

			_minZoomFactor = Math.Max(0.1f, newValue);
			// Clamp current zoom if it's now below the new minimum
			if (_zoomFactor < _minZoomFactor)
			{
				Set(zoomFactor: _minZoomFactor, disableAnimation: true);
			}
		}

		internal void OnMaxZoomFactorChanged(float newValue)
		{
			if (Scroller?.ZoomMode == ZoomMode.Disabled)
			{
				newValue = 1f;
			}

			_maxZoomFactor = Math.Max(_minZoomFactor, newValue);
			// Clamp current zoom if it's now above the new maximum
			if (_zoomFactor > _maxZoomFactor)
			{
				Set(zoomFactor: _maxZoomFactor, disableAnimation: true);
			}
		}

		internal bool Set(
			double? horizontalOffset = null,
			double? verticalOffset = null,
			float? zoomFactor = null,
			bool disableAnimation = false,
			bool isIntermediate = false,
			bool isTouch = false,
			[CallerMemberName] string callerName = "",
			[CallerLineNumber] int callerLine = -1)
			=> Set(horizontalOffset, verticalOffset, zoomFactor, options: new(disableAnimation, IsTouch: isTouch, IsIntermediate: isIntermediate), callerName, callerLine);

		private bool Set(
			double? horizontalOffset = null,
			double? verticalOffset = null,
			float? zoomFactor = null,
			ScrollOptions options = default,
			[CallerMemberName] string callerName = "",
			[CallerLineNumber] int callerLine = -1)
		{
			bool success = true, updated = false;

			// The zoom factor is applied first: the scrollable range - and therefore the offset clamping
			// below - depends on it. Applying it afterwards would clamp offsets against the previous zoom.
			var zoomUpdated = false;
			if (zoomFactor is float zoom)
			{
				var targetZoom = Math.Clamp(zoom, _minZoomFactor, _maxZoomFactor);
				success &= Math.Abs(targetZoom - zoom) < 0.0001f;

				if (Math.Abs(_zoomFactor - targetZoom) > 0.0001f)
				{
					_zoomFactor = targetZoom;
					updated = true;
					zoomUpdated = true;
				}
			}

			if (horizontalOffset is double hOffset)
			{
				// Scroller.ScrollableWidth is only refreshed once the zoom change has been reported back to it,
				// so while zooming the range has to be recomputed from the target zoom factor.
				var maxOffset = zoomUpdated
					? Math.Max(0, ExtentWidth * _zoomFactor - ViewportWidth)
					: Scroller?.ScrollableWidth ?? ExtentWidth - ViewportWidth;
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
				var maxOffset = zoomUpdated
					? Math.Max(0, ExtentHeight * _zoomFactor - ViewportHeight)
					: Scroller?.ScrollableHeight ?? ExtentHeight - ViewportHeight;
				var targetVerticalOffset = ValidateInputOffset(vOffset, 0, maxOffset);

				success &= targetVerticalOffset == vOffset;

				if (!NumericExtensions.AreClose(VerticalOffset, targetVerticalOffset))
				{
					VerticalOffset = targetVerticalOffset;
					updated = true;
				}
			}

			// A zoom-only change (offsets left null) can shrink the scrollable range below the current
			// offsets, so the existing offsets have to be re-clamped against the target zoom factor.
			if (zoomUpdated && horizontalOffset is null)
			{
				var maxOffset = Math.Max(0, ExtentWidth * _zoomFactor - ViewportWidth);
				var clampedOffset = ValidateInputOffset(HorizontalOffset, 0, maxOffset);
				if (!NumericExtensions.AreClose(HorizontalOffset, clampedOffset))
				{
					HorizontalOffset = clampedOffset;
					updated = true;
				}
			}

			if (zoomUpdated && verticalOffset is null)
			{
				var maxOffset = Math.Max(0, ExtentHeight * _zoomFactor - ViewportHeight);
				var clampedOffset = ValidateInputOffset(VerticalOffset, 0, maxOffset);
				if (!NumericExtensions.AreClose(VerticalOffset, clampedOffset))
				{
					VerticalOffset = clampedOffset;
					updated = true;
				}
			}

			_trace?.Invoke($"Scroll [{callerName}@{callerLine}] (success: {success} | updated: {updated} | req: h={horizontalOffset} v={verticalOffset} z={zoomFactor} | actual: h={HorizontalOffset} v={VerticalOffset} z={_zoomFactor} | opts: {options})");

			if (!options.IsWheelDecay)
			{
				StopWheelDecay();
			}

			if (!options.IsTouch)
			{
				StopFling();

				// The processor is configured to outlast the fling, so stopping the driver is not enough: a
				// programmatic scroll has to end the manipulation too, or it keeps a frame driver alive and
				// swallows every press until it expires on its own.
				CompleteTouchInertia();
			}

			var updatedHorizontalOffset = HorizontalOffset;
			var updatedVerticalOffset = VerticalOffset;
			if (updated || options.IsTouch)
			{
				if (Content is UIElement contentElt)
				{
					Update(contentElt, updatedHorizontalOffset, updatedVerticalOffset, _zoomFactor, options);
				}
			}

			// Notify ScrollViewer of zoom change
			if (zoomUpdated)
			{
				Scroller?.OnPresenterZoomed(_zoomFactor);
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

		private void Update(UIElement view, double horizontalOffset, double verticalOffset, float zoom, ScrollOptions options)
		{
			// Calculate centering offset when zoomed content is smaller than viewport
			// This matches WinUI behavior where content stays centered when zoomed out
			var scaledExtentWidth = ExtentWidth * zoom;
			var scaledExtentHeight = ExtentHeight * zoom;

			var centeringOffsetX = scaledExtentWidth < ViewportWidth
				? (ViewportWidth - scaledExtentWidth) / 2
				: 0;
			var centeringOffsetY = scaledExtentHeight < ViewportHeight
				? (ViewportHeight - scaledExtentHeight) / 2
				: 0;

			var target = new Vector2(
				(float)(-horizontalOffset + centeringOffsetX),
				(float)(-verticalOffset + centeringOffsetY));
			var targetScale = new Vector3(zoom, zoom, 1);
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
				// But still apply zoom if needed (with animation if enabled)
				if (Math.Abs(visual.Scale.X - zoom) > 0.0001f)
				{
					if (options.DisableAnimation)
					{
						visual.Scale = targetScale;
					}
					else
					{
						var compositor = visual.Compositor;
						var easing = CompositionEasingFunction.CreatePowerEasingFunction(compositor, CompositionEasingFunctionMode.Out, 10);
						var zoomAnimation = compositor.CreateVector3KeyFrameAnimation();
						zoomAnimation.InsertKeyFrame(1.0f, targetScale, easing);
						zoomAnimation.Duration = TimeSpan.FromMilliseconds(300);
						visual.StartAnimation(nameof(Visual.Scale), zoomAnimation);
					}
				}
				return;
			}


			if (options is { DisableAnimation: true } or { IsTouch: true })
			{
				visual.StopAnimation(nameof(Visual.AnchorPoint));
				visual.StopAnimation(nameof(Visual.Scale));
				visual.AnchorPoint = target;
				visual.Scale = targetScale;
				Updated(horizontalOffset, verticalOffset, options.IsIntermediate);
			}
			else
			{
				var compositor = visual.Compositor;
				var easing = CompositionEasingFunction.CreatePowerEasingFunction(compositor, CompositionEasingFunctionMode.Out, 10);

				// Scroll offset animation
				var scrollAnimation = compositor.CreateVector2KeyFrameAnimation();
				scrollAnimation.InsertKeyFrame(1.0f, target, easing);
				scrollAnimation.Duration = TimeSpan.FromSeconds(1);
				// AnchorPoint also carries the centering offset, which has to be removed to get back the logical scroll offsets.
				var stopped = false;
				void OnFrame(CompositionAnimation? _)
				{
					// The compositor stops a completed animation from inside its own AnimationFrame handler, so
					// OnStopped can already have published the final offset by the time this runs — removing the
					// handler there does not affect the invocation list of the raise that is already in flight.
					if (stopped)
					{
						return;
					}

					Updated(GetAnimatedHorizontalOffset(), GetAnimatedVerticalOffset(), true);
				}
				void OnStopped(object? _, EventArgs __)
				{
					stopped = true;
					scrollAnimation.AnimationFrame -= OnFrame;
					scrollAnimation.Stopped -= OnStopped;

					Updated(GetAnimatedHorizontalOffset(), GetAnimatedVerticalOffset(), false);
				}

				double GetAnimatedHorizontalOffset() => Math.Round(-visual.AnchorPoint.X + centeringOffsetX);
				double GetAnimatedVerticalOffset() => Math.Round(-visual.AnchorPoint.Y + centeringOffsetY);

				scrollAnimation.Stopped += OnStopped;

				visual.StartAnimation(nameof(Visual.AnchorPoint), scrollAnimation);

				// Subscribed after StartAnimation so it runs after the compositor's own ReEvaluateAnimation
				// handler: AnimationFrame invokes in subscription order, so subscribing first would publish
				// the previous frame's AnchorPoint and leave virtualization one frame behind the viewport.
				scrollAnimation.AnimationFrame += OnFrame;

				// Zoom animation (if zoom is changing)
				if (Math.Abs(visual.Scale.X - zoom) > 0.0001f)
				{
					var zoomAnimation = compositor.CreateVector3KeyFrameAnimation();
					zoomAnimation.InsertKeyFrame(1.0f, targetScale, easing);
					zoomAnimation.Duration = TimeSpan.FromMilliseconds(300); // Shorter duration for zoom per WinUI style
					visual.StartAnimation(nameof(Visual.Scale), zoomAnimation);
				}
			}
		}


		/// <summary>Runs a touch fling from the frame clock, using the platform's own deceleration curve.</summary>
		private void StartFling(double velocityXPerSecond, double velocityYPerSecond)
		{
			StopFling();

			// Both drivers write the same offsets from the same frame clock, so they must never overlap:
			// the loser would publish its own stale position over the winner's on every frame.
			StopWheelDecay();

			// Anchored on the first frame rather than here: the finger lifts at an arbitrary point within
			// a frame, so timing from now would make the first inertial step a random fraction of a full
			// one — at the exact moment the motion is fastest and a stutter most visible.
			_flingStartTimestamp = 0;
			_flingH = ScrollFlingSimulation.Create(HorizontalOffset, -velocityXPerSecond);
			_flingV = ScrollFlingSimulation.Create(VerticalOffset, -velocityYPerSecond);
			if (FrameDriverTarget is not { } flingTarget)
			{
				return;
			}

			_isFlingRunning = true;
			_flingTarget = flingTarget;

			flingTarget.FrameStarting += OnFlingFrame;
		}

		private void StopFling()
		{
			if (!_isFlingRunning)
			{
				return;
			}

			if (_flingTarget is { } flingTarget)
			{
				flingTarget.FrameStarting -= OnFlingFrame;
				_flingTarget = null;
			}

			_isFlingRunning = false;
		}

		/// <summary>
		/// Stops a fling that is cut short (press, unload), making sure the offsets it reached are still
		/// published as final: <see cref="OnFlingFrame"/> is the only place that would otherwise do it.
		/// </summary>
		private void StopFlingAndPublishFinalOffsets()
		{
			if (!_isFlingRunning)
			{
				return;
			}

			StopFling();
			Set(options: new ScrollOptions(DisableAnimation: true, IsTouch: true, IsIntermediate: false));
		}

		/// <summary>
		/// Ends the manipulation a coast was holding. Cleared before completing so the completion that follows
		/// does not publish a final offset over the one the caller is about to set.
		/// </summary>
		private void CompleteTouchInertia()
		{
			var inertia = _touchInertia;
			_touchInertia = null;
			inertia?.Complete();
		}

		/// <summary>The target whose tick drives this presenter's fling and wheel decay.</summary>
		private Media.CompositionTarget? FrameDriverTarget => Visual.CompositionTarget as Media.CompositionTarget;

		private void OnFlingFrame(object? sender, long timestampInTicks)
		{
			// The frame raise captures its invocation list, so a handler stopped by an earlier handler
			// of the same frame still gets invoked and must not write back its own position.
			if (!_isFlingRunning)
			{
				return;
			}

			if (_flingStartTimestamp == 0)
			{
				// One interval back, so this first frame advances by the same step the drag was producing
				// when the finger left the glass.
				_flingStartTimestamp = timestampInTicks - (_flingTarget?.FrameIntervalInTicks ?? 0);
			}

			var elapsed = (timestampInTicks - _flingStartTimestamp) / (double)TimeSpan.TicksPerSecond;

			var maxH = Scroller?.ScrollableWidth ?? Math.Max(0, ExtentWidth - ViewportWidth);
			var maxV = Scroller?.ScrollableHeight ?? Math.Max(0, ExtentHeight - ViewportHeight);

			var h = Math.Clamp(_flingH.GetPosition(elapsed), 0, maxH);
			var v = Math.Clamp(_flingV.GetPosition(elapsed), 0, maxV);

			// Each axis against its own curve: sharing one duration keeps the fling ticking after the axis
			// that was actually moving has settled at an edge, publishing no-op frames until the other
			// axis's curve — which nothing is travelling — runs out.
			var running = (elapsed < _flingH.Duration && h > 0 && h < maxH)
				|| (elapsed < _flingV.Duration && v > 0 && v < maxV);

			if (!running)
			{
				StopFling();
			}

			Set(horizontalOffset: h, verticalOffset: v, options: new(DisableAnimation: true, IsTouch: true, IsIntermediate: running));

			if (!running)
			{
				// The processor is deliberately configured to outlast the curve so a press can still resume it.
				// Once there is nothing left to resume, holding it only keeps a frame driver alive and swallows
				// presses on content that stopped moving.
				CompleteTouchInertia();
			}
		}

		/// <summary>Feeds a wheel detent into the running decay, starting it if idle.</summary>
		/// <returns>
		/// False when this presenter has no room left in the requested direction, so the wheel event stays
		/// unhandled and chains to a parent ScrollViewer.
		/// </returns>
		internal bool AddWheelImpulse(double horizontalDistance, double verticalDistance)
		{
			var maxH = Scroller?.ScrollableWidth ?? Math.Max(0, ExtentWidth - ViewportWidth);
			var maxV = Scroller?.ScrollableHeight ?? Math.Max(0, ExtentHeight - ViewportHeight);

			// Against where the motion in flight will come to rest, not where it is now: a decay that is
			// already destined for the end of the extent has no room left, and the event must chain to a parent.
			var fromH = _isWheelDecayRunning ? _wheelDecayH.ProjectedEnd : HorizontalOffset;
			var fromV = _isWheelDecayRunning ? _wheelDecayV.ProjectedEnd : VerticalOffset;

			if (!HasRoom(fromH, horizontalDistance, maxH) && !HasRoom(fromV, verticalDistance, maxV))
			{
				return false;
			}

			if (!_isWheelDecayRunning)
			{
				if (FrameDriverTarget is not { } wheelTarget)
				{
					return false;
				}

				// Seeded from the current offset, which a coasting fling may still be advancing, and anchored
				// on the first frame rather than on a clock read here — cf. StartFling.
				_wheelDecayH.Start(HorizontalOffset, wheelTarget.FrameIntervalInTicks);
				_wheelDecayV.Start(VerticalOffset, wheelTarget.FrameIntervalInTicks);
				_isWheelDecayRunning = true;
				_wheelDecayTarget = wheelTarget;
				wheelTarget.FrameStarting += OnWheelDecayFrame;
			}

			// The decay has taken the notch over, so it also takes over from any coasting touch fling:
			// both drive the same offsets from the same frame clock, and the fling would otherwise keep
			// publishing its own position over the decay on every frame.
			StopFling();

			_wheelDecayH.AddImpulse(horizontalDistance);
			_wheelDecayV.AddImpulse(verticalDistance);
			return true;

			static bool HasRoom(double from, double distance, double max)
				=> distance < 0 ? from > 0 : distance > 0 && from < max;
		}

		internal void StopWheelDecay()
		{
			if (!_isWheelDecayRunning)
			{
				return;
			}

			if (_wheelDecayTarget is { } wheelTarget)
			{
				wheelTarget.FrameStarting -= OnWheelDecayFrame;
				_wheelDecayTarget = null;
			}

			_isWheelDecayRunning = false;
			_wheelDecayH.Stop();
			_wheelDecayV.Stop();
		}

		/// <summary>
		/// Stops a decay that is cut short, making sure the offsets it reached are still published as final:
		/// <see cref="OnWheelDecayFrame"/> is the only place that would otherwise do it.
		/// </summary>
		private void StopWheelDecayAndPublishFinalOffsets()
		{
			if (!_isWheelDecayRunning)
			{
				return;
			}

			StopWheelDecay();
			Set(options: new ScrollOptions(DisableAnimation: true, IsIntermediate: false, IsWheelDecay: true));
		}

		private void OnWheelDecayFrame(object? sender, long timestampInTicks)
		{
			// cf. OnFlingFrame: the invocation list of the frame in flight still contains this handler
			// even when it was unsubscribed by another handler of that same frame.
			if (!_isWheelDecayRunning)
			{
				return;
			}

			var maxH = Scroller?.ScrollableWidth ?? Math.Max(0, ExtentWidth - ViewportWidth);
			var maxV = Scroller?.ScrollableHeight ?? Math.Max(0, ExtentHeight - ViewportHeight);

			var runningH = _wheelDecayH.Tick(timestampInTicks, 0, maxH);
			var runningV = _wheelDecayV.Tick(timestampInTicks, 0, maxV);
			var running = runningH || runningV;

			if (!running)
			{
				StopWheelDecay();
			}

			Set(
				horizontalOffset: _wheelDecayH.Position,
				verticalOffset: _wheelDecayV.Position,
				options: new(DisableAnimation: true, IsIntermediate: running, IsWheelDecay: true));
		}

		private void TryEnableDirectManipulation(object sender, PointerRoutedEventArgs args)
		{
			if (args.Pointer.PointerDeviceType is not (_PointerDeviceType.Pen or _PointerDeviceType.Touch))
			{
				return;
			}

			// Touch/pen press invalidates any armed offset intent so subsequent layout cascades from
			// realization don't push the offset back toward the intent during the user's manipulation.
			Scroller?.ClearOffsetIntents();

			// A press over a coast is stopped and swallowed by the manipulation's own resume path, which
			// runs ahead of routing — see the inertia hand-off in OnInertiaStarting.
			XamlRoot?.VisualTree.ContentRoot.InputManager.Pointers.RegisterDirectManipulationHandler(args.Pointer.UniqueId, this);
		}

		object? IDirectManipulationHandler.Owner => ScrollOwner;

		/// <inheritdoc />
		ManipulationModes IDirectManipulationHandler.OnStarting(GestureRecognizer _, ManipulationStartingEventArgs args)
		{
			// A press on content coasting from the wheel stops it. The touch paths go through
			// OnInertiaInterrupted instead, which a wheel decay never reaches: it holds no manipulation.
			StopWheelDecay();

			return ComputeAcceptedManipulationModes();
		}

		/// <inheritdoc />
		ManipulationModes IDirectManipulationHandler.GetCurrentlyAcceptedModes()
			=> ComputeAcceptedManipulationModes();

		private ManipulationModes ComputeAcceptedManipulationModes()
		{
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

				// Enable pinch-to-zoom when ZoomMode is Enabled
				if (sv.ZoomMode == ZoomMode.Enabled)
				{
					mode |= ManipulationModes.Scale;
				}
			}

			return mode;
		}

		bool IDirectManipulationHandler.CanAddPointerAt(in Point absoluteLocation)
			=> GetTransform(this, null).Transform(new Rect(new Point(), LayoutSlotWithMarginsAndAlignments.Size)).Contains(absoluteLocation);

		/// <inheritdoc />
		void IDirectManipulationHandler.OnInertiaInterrupted(GestureRecognizer recognizer)
		{
			// The press that takes the coast over stops it where it is. The completion that follows is
			// suppressed because it is a resume, and the restarted manipulation only reports Started once the
			// finger has travelled the start threshold — so nothing else would stop the content under it.
			_touchInertia = null;
			StopFling();
			StopWheelDecay();
		}

		/// <inheritdoc />
		void IDirectManipulationHandler.OnStarted(GestureRecognizer recognizer, ManipulationStartedEventArgs args, bool isResuming)
		{
			_velocityTracker.Reset();
			_velocityTrackerContacts = 0;
			_touchInertia = null;
			StopFling();
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

			// Handle zoom (pinch gesture)
			float? newZoomFactor = null;
			if (sv.ZoomMode == ZoomMode.Enabled && Math.Abs(unhandledDelta.Scale - 1.0f) > 0.0001f)
			{
				var proposedZoom = _zoomFactor * unhandledDelta.Scale;
				newZoomFactor = Math.Clamp(proposedZoom, _minZoomFactor, _maxZoomFactor);

				// Adjust scroll offsets to keep the pinch center point fixed
				// When zooming around a center point, we need to adjust offsets so that
				// the content point under the center stays in the same screen position
				var center = args.Position;
				var zoomRatio = newZoomFactor.Value / _zoomFactor;

				// Formula: new_offset = (old_offset + center) * zoomRatio - center
				// Expressed as a delta: delta = (old_offset + center) * (zoomRatio - 1)
				var zoomOffsetDeltaX = (HorizontalOffset + center.X) * (zoomRatio - 1);
				var zoomOffsetDeltaY = (VerticalOffset + center.Y) * (zoomRatio - 1);

				deltaX += (float)zoomOffsetDeltaX;
				deltaY += (float)zoomOffsetDeltaY;

				// Mark scale as handled
				unhandledDelta.Scale = 1.0f;
			}


			if (args.IsInertial)
			{
				// Inertia is settled by the fling (or by a snap animation), never by the recognizer's
				// processor, so an inertial update can only belong to another ScrollViewer of the chain.
				return;
			}

			// The fit runs on the centroid of the contacts: adding or removing a finger moves it by half
			// the finger separation, which is a jump in the samples but not motion of the content.
			if (_velocityTrackerContacts != args.CurrentContactCount)
			{
				_velocityTrackerContacts = args.CurrentContactCount;
				_velocityTracker.Reset();
			}

			// Input time, not the time this update is being dispatched on the UI thread: a dispatch stall
			// crushes a burst of samples into a fraction of a millisecond and inflates the fitted velocity.
			_velocityTracker.AddPosition(
				args.Manipulation.CurrentTimestampInMicroseconds / 1000d,
				args.Position);

			unhandledDelta.Translation.X += deltaX;
			unhandledDelta.Translation.Y += deltaY;

			Set(
				horizontalOffset: HorizontalOffset + deltaX,
				verticalOffset: VerticalOffset + deltaY,
				zoomFactor: newZoomFactor,
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

		/// <inheritdoc />
		bool IDirectManipulationHandler.OnInertiaStarting(GestureRecognizer recognizer, ManipulationInertiaStartingEventArgs args, bool isHandled)
		{
			if (isHandled)
			{
				return false;
			}

			if (Scroller is not { IsScrollInertiaEnabled: true } sv)
			{
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

				// calculate the duration based on PKScrollView.prototype.stepThroughDecelerationAnimation from https://github.com/jimeh/PastryKit
				// momentum should decay by 5% each frame, at 60fps, until the minimum threshold is reached.
				const double PKScrollViewDecelerationFrictionFactor = 0.95;
				const double PKScrollViewDesiredAnimationFrameRate = 1000 / 60.0;
				const double PKScrollViewMinimumVelocity = 0.01;
				var frames = Math.Log(PKScrollViewMinimumVelocity / v0, PKScrollViewDecelerationFrictionFactor);
				var duration = frames * PKScrollViewDesiredAnimationFrameRate;

				inertia.DesiredDisplacementDeceleration = GestureRecognizer.Manipulation.InertiaProcessor.GetDecelerationFromDesiredDuration(v0, duration);
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

				// note: IsTouch = true as we are not in the touch scrolling anymore here, we are just snapping.
				Set(horizontalOffset: h, verticalOffset: v, disableAnimation: false, isIntermediate: false);
			}
			else
			{
				// Fitted over the recent gesture rather than taken from the last two samples: inertia
				// distance grows with the square of the launch velocity, so a two-point estimate that
				// catches one short interval sends the content thousands of pixels.
				var fitted = _velocityTracker.GetVelocity();
				var vx = (fitted?.X ?? args.Velocities.Linear.X) * 1000;
				var vy = (fitted?.Y ?? args.Velocities.Linear.Y) * 1000;

				// Deliberately not completed: the manipulation has to stay inertial so a press over the
				// coasting content resumes it (DirectManipulation.TryProcessDown), which stops the content
				// and swallows the press. A routed handler cannot do that — a press reaches the element
				// under the finger before it reaches this presenter.
				var manipulation = args.Manipulation;
				_touchInertia = manipulation;

				StartFling(vx, vy);

				if (!_isFlingRunning)
				{
					// No frame driver (e.g. the presenter has no composition target): nothing would ever
					// publish the final offsets, so end the manipulation and let OnCompleted do it.
					_touchInertia = null;
					manipulation.Complete();
					return true;
				}

				// The recognizer's processor no longer moves anything, but it is what holds the inertial
				// state: letting it complete before the fling does would drop the press-to-stop mid-coast.
				var launchVelocity = Math.Max(Math.Abs(args.Velocities.Linear.X), Math.Abs(args.Velocities.Linear.Y));
				var flingDurationMs = Math.Max(_flingH.Duration, _flingV.Duration) * 1000;
				inertia.DesiredDisplacementDeceleration = GestureRecognizer.Manipulation.InertiaProcessor
					.GetDecelerationFromDesiredDuration(launchVelocity, flingDurationMs + 500);

				return true;
			}

			return true;
		}

		/// <inheritdoc />
		void IDirectManipulationHandler.OnCompleted(GestureRecognizer _, ManipulationCompletedEventArgs? args)
		{
			var wasCoasting = _touchInertia is not null;
			_touchInertia = null;
			StopFling();

			if (args?.IsInertial is true && !wasCoasting)
			{
				// This presenter never owned the coast: either a snap animation took it over and publishes the
				// final offsets itself (the Set below would stop it), or the inertia belongs to another scroller
				// of the chain, whose offsets this one never moved.
				return;
			}

			Set(options: new ScrollOptions(DisableAnimation: true, IsTouch: true, IsIntermediate: false));
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

#if !__CROSSRUNTIME__
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
	/// <param name="IsTouch">Indicates that the scroll is coming from an inertia processor.</param>
	/// <param name="IsIntermediate">
	/// Indicates that the scroll is an intermediate value, not the final one
	/// (i.e. active touch scrolling, touch scroll inertia or scroll animation).
	/// </param>
	/// <param name="IsWheelDecay">Indicates that the scroll is a frame of the wheel's decay, which must not stop itself.</param>
	internal record struct ScrollOptions(bool DisableAnimation = false, bool IsTouch = false, bool IsIntermediate = false, bool IsWheelDecay = false);
}
