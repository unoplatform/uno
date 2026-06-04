using System;
using System.Runtime.CompilerServices;
using CoreAnimation;
using CoreGraphics;
using Foundation;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using UIKit;
using Uno.Foundation.Logging;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Extensions;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using PointerEventArgs = Windows.UI.Core.PointerEventArgs;

#if !HAS_UNO_WINUI
using Windows.UI.Input;
#endif

namespace Uno.UI.Runtime.Skia.AppleUIKit;

internal sealed class AppleUIKitCorePointerInputSource : IUnoCorePointerInputSource
{
#if __IOS__
	// Multiplier for converting trackpad translation (pts/frame) to WinUI MouseWheelDelta.
	// With DisableAnimation:true in PointerWheelScroll (see ScrollContentPresenter.cs),
	// each event directly moves the scroll by: round(delta * scrollPerNotch / 120),
	// where delta is the trackpad translation (pts/frame) multiplied by ScrollWheelDeltaMultiplier.
	// A value of -1 yields a near 1:1 ratio between physical trackpad pts and logical scroll pts
	// (for a typical viewport where scrollPerNotch ≈ 120). Using a large value like -60 over-amplifies
	// because GetScrollWheelDelta already normalizes by the 120-notch baseline.
	// Inertia uses the same multiplier (velocity / InertiaVelocityScale * this) for a seamless handoff.
	private const int ScrollWheelDeltaMultiplier = -1;
	// Approximate iOS logical pts reported by UIPanGestureRecognizer per one discrete mouse-wheel notch.
	// Used to normalize discrete scroll events to the WinUI standard of 120 units/notch, so that
	// GetScrollWheelDelta produces the same per-notch scroll distance as on Windows.
	private const double DiscreteScrollLineSize = 10.0;
	private const double DiscreteScrollScale = 120.0 / DiscreteScrollLineSize; // = 12
	private const double InertiaDecelerationRate = 0.95;
	// Stop the display link once velocity drops below ~30 pt/s (0.5 pt/frame).
	// Sub-pixel accumulation handles smooth deceleration down to this point.
	// Using a higher threshold avoids running the display link for 2+ seconds of
	// imperceptible movement, which could delay gesture recognition callbacks.
	private const double InertiaMagnitudeThreshold = 0.5;
	// Cap accumulated momentum so repeated rapid flicks cannot over-accelerate.
	private const double InertiaMaxVelocity = 50.0;
	// Maximum time between gesture end and next gesture start for momentum to be
	// preserved across the two gestures (rapid back-to-back flicking).
	private const double RapidFlickWindowSeconds = 0.35;
	// Divide velocity (pts/s) by display refresh rate to get per-frame delta,
	// matching the scale of active-scroll translation deltas for a smooth handoff.
	private const double InertiaVelocityScale = 60.0;

	private CADisplayLink? _momentumDisplayLink;
	private CADisplayLink? _activeScrollDisplayLink;
	private double _momentumVelocityX;
	private double _momentumVelocityY;
	// Sub-pixel accumulator: collects fractional pts across frames so inertia
	// decelerates smoothly all the way to InertiaMagnitudeThreshold instead of
	// stopping abruptly when velocity drops below 1 pt/frame.
	private double _inertiaPendingX;
	private double _inertiaPendingY;
	private double _lastGestureEndTime;
	private double _gestureBeganTime;
	private CGPoint _cachedScrollLocation;
	private bool _gestureIsNaturalScrolling;
	private double _activeScrollPendingX;
	private double _activeScrollPendingY;
#endif
	public static AppleUIKitCorePointerInputSource Instance { get; } = new();

	private AppleUIKitCorePointerInputSource()
	{
	}

#pragma warning disable CS0067
	public event TypedEventHandler<object, PointerEventArgs>? PointerCaptureLost;
	public event TypedEventHandler<object, PointerEventArgs>? PointerEntered;
	public event TypedEventHandler<object, PointerEventArgs>? PointerExited;
	public event TypedEventHandler<object, PointerEventArgs>? PointerMoved;
	public event TypedEventHandler<object, PointerEventArgs>? PointerPressed;
	public event TypedEventHandler<object, PointerEventArgs>? PointerReleased;
	public event TypedEventHandler<object, PointerEventArgs>? PointerWheelChanged;
	public event TypedEventHandler<object, PointerEventArgs>? PointerCancelled; // Uno Only
#pragma warning restore CS0067

	private PointerPointProperties? _previousProperties;
	private Point _pointerPosition;
	private CoreCursor? _pointerCursor = new(CoreCursorType.Arrow, 0);
	private Action? _invalidateCursor;

	public bool HasCapture => false;

	public Point PointerPosition => _pointerPosition;

	public CoreCursor PointerCursor
	{
		get => _pointerCursor!;
		set
		{
			// A null cursor hides the pointer. Only the cursor shape (Type) is observable, so skip
			// redundant assignments that would otherwise re-query the native pointer interaction.
			if (_pointerCursor?.Type == value?.Type)
			{
				_pointerCursor = value;
				return;
			}

			_pointerCursor = value;
			_invalidateCursor?.Invoke();
		}
	}

	public void SetPointerCapture()
	{

	}

	public void SetPointerCapture(PointerIdentifier pointer)
	{
	}

	public void ReleasePointerCapture()
	{
	}

	public void ReleasePointerCapture(PointerIdentifier pointer)
	{
	}

	internal void TouchesBegan(UIView source, NSSet touches, UIEvent? evt)
	{
		try
		{
			TraceStart(source, touches);

			foreach (UITouch touch in touches)
			{
				var args = CreatePointerEventArgs(source, touch, evt);

				TraceSingle(args);

				PointerPressed?.Invoke(this, args);
			}

			TraceEnd();
		}
		catch (Exception e)
		{
			TraceError(e);
			Application.Current.RaiseRecoverableUnhandledException(e);
		}
	}

	internal void TouchesMoved(UIView source, NSSet touches, UIEvent? evt)
	{
		try
		{
			TraceStart(source, touches);

			foreach (UITouch touch in touches)
			{
				var args = CreatePointerEventArgs(source, touch, evt);

				TraceSingle(args);

				PointerMoved?.Invoke(this, args);
			}

			TraceEnd();
		}
		catch (Exception e)
		{
			TraceError(e);
			Application.Current.RaiseRecoverableUnhandledException(e);
		}
	}

	internal void TouchesEnded(UIView source, NSSet touches, UIEvent? evt)
	{
		try
		{
			TraceStart(source, touches);

			foreach (UITouch touch in touches)
			{
				var args = CreatePointerEventArgs(source, touch, evt);

				TraceSingle(args);

				PointerReleased?.Invoke(this, args);
			}

			TraceEnd();
		}
		catch (Exception e)
		{
			TraceError(e);
			Application.Current.RaiseRecoverableUnhandledException(e);
		}
	}

	internal void TouchesCancelled(UIView source, NSSet touches, UIEvent? evt)
	{
		try
		{
			TraceStart(source, touches);

			foreach (UITouch touch in touches)
			{
				var args = CreatePointerEventArgs(source, touch, evt);

				TraceSingle(args);

				PointerCancelled?.Invoke(this, args);
			}

			TraceEnd();
		}
		catch (Exception e)
		{
			TraceError(e);
			Application.Current.RaiseRecoverableUnhandledException(e);
		}
	}

#if __IOS__
	internal void HandleScrollFromGesture(UIView source, UIPanGestureRecognizer gesture, bool isNaturalScrollingEnabled, bool isDiscreteScroll)
	{
		try
		{
			if (!UIDevice.CurrentDevice.CheckSystemVersion(13, 4))
			{
				return;
			}

			if (isDiscreteScroll)
			{
				HandleDiscreteScroll(source, gesture, isNaturalScrollingEnabled);
			}
			else
			{
				HandleContinuousScroll(source, gesture, isNaturalScrollingEnabled);
			}
		}
		catch (Exception e)
		{
			_trace?.Invoke($"</ScrollGesture error=true>\r\n" + e);
			Application.Current.RaiseRecoverableUnhandledException(e);
		}
	}

	/// <summary>
	/// Handles discrete (mouse wheel) scroll gestures. Each notch is dispatched
	/// immediately with no accumulation, display link, or inertia.
	/// On iPadOS a single notch fires Began → Ended with no Changed event,
	/// so translation is read in both Changed and Ended to cover all cases.
	/// </summary>
	private void HandleDiscreteScroll(UIView source, UIPanGestureRecognizer gesture, bool isNaturalScrollingEnabled)
	{
		switch (gesture.State)
		{
			case UIGestureRecognizerState.Began:
				StopInertiaScrolling();
				_cachedScrollLocation = gesture.LocationInView(source);
				_gestureIsNaturalScrolling = isNaturalScrollingEnabled;

				// Pre-position the scroll pointer so the first wheel event
				// does not trigger a PointerExited/PointerEntered flash.
				PointerMoved?.Invoke(this, CreateScrollGestureEventArgs(CGPoint.Empty, _cachedScrollLocation, isNaturalScrollingEnabled));
				return;

			case UIGestureRecognizerState.Changed:
				// Rapid notches: iOS batches multiple notches into one gesture,
				// firing Changed for each. Scale and dispatch immediately.
				DispatchDiscreteTranslation(gesture.TranslationInView(source));
				gesture.SetTranslation(CGPoint.Empty, source);
				return;

			case UIGestureRecognizerState.Ended:
				// Single notch: iPadOS fires Began → Ended with no Changed.
				// For rapid notches, Changed already called SetTranslation,
				// so translation here is only the remaining delta.
				DispatchDiscreteTranslation(gesture.TranslationInView(source));
				return;
		}
	}

	private void DispatchDiscreteTranslation(CGPoint translation)
	{
		var scrollX = (nfloat)((double)translation.X * DiscreteScrollScale);
		var scrollY = (nfloat)((double)translation.Y * DiscreteScrollScale);
		if (scrollX != 0 || scrollY != 0)
		{
			PointerWheelChanged?.Invoke(this,
				CreateScrollGestureEventArgs(new CGPoint(scrollX, scrollY), _cachedScrollLocation, _gestureIsNaturalScrolling));
		}
	}

	/// <summary>
	/// Handles continuous (trackpad) scroll gestures. Deltas are accumulated
	/// and dispatched once per vsync via a display link to avoid starving the
	/// render pass. Inertia provides momentum after the gesture ends.
	/// </summary>
	private void HandleContinuousScroll(UIView source, UIPanGestureRecognizer gesture, bool isNaturalScrollingEnabled)
	{
		switch (gesture.State)
		{
			case UIGestureRecognizerState.Began:
				// Preserve momentum for rapid back-to-back flicks (within the
				// time window). For slower starts the previous inertia is stale.
				var timeSinceLastEnd = CoreAnimation.CAAnimation.CurrentMediaTime() - _lastGestureEndTime;
				if (timeSinceLastEnd <= RapidFlickWindowSeconds)
				{
					PauseInertiaScrolling();
				}
				else
				{
					StopInertiaScrolling();
				}

				_activeScrollPendingX = 0;
				_activeScrollPendingY = 0;
				_gestureBeganTime = CoreAnimation.CAAnimation.CurrentMediaTime();
				_cachedScrollLocation = gesture.LocationInView(source);
				_gestureIsNaturalScrolling = isNaturalScrollingEnabled;

				// Pre-position the scroll pointer so the first wheel event
				// does not trigger a PointerExited/PointerEntered flash.
				PointerMoved?.Invoke(this, CreateScrollGestureEventArgs(CGPoint.Empty, _cachedScrollLocation, isNaturalScrollingEnabled));
				return;

			case UIGestureRecognizerState.Changed:
				var translation = gesture.TranslationInView(source);
				gesture.SetTranslation(CGPoint.Empty, source);

				// Accumulate and defer dispatch to a display link so that
				// scroll events are processed once per vsync frame.
				_activeScrollPendingX += (double)translation.X;
				_activeScrollPendingY += (double)translation.Y;
				if (_activeScrollDisplayLink is null)
				{
					_activeScrollDisplayLink = CADisplayLink.Create(FlushContinuousScroll);
					_activeScrollDisplayLink.AddToRunLoop(NSRunLoop.Main, NSRunLoopMode.Common);
				}
				return;

			case UIGestureRecognizerState.Ended:
				_gestureIsNaturalScrolling = isNaturalScrollingEnabled;
				FlushContinuousScroll();
				StopActiveScrollDisplayLink();
				// If the gesture lasted longer than the rapid-flick window, the preserved
				// momentum is unrelated to the current scroll — discard it.
				if (CoreAnimation.CAAnimation.CurrentMediaTime() - _gestureBeganTime > RapidFlickWindowSeconds)
				{
					_momentumVelocityX = 0;
					_momentumVelocityY = 0;
				}
				AccumulateInertiaScrolling(gesture.VelocityInView(source));
				return;

			case UIGestureRecognizerState.Cancelled:
			case UIGestureRecognizerState.Failed:
				StopActiveScrollDisplayLink();
				StopInertiaScrolling();
				return;
		}
	}

	// Dispatch accumulated continuous (trackpad) scroll deltas as a single
	// PointerWheelChanged event, once per vsync frame. Uses sub-pixel accumulation
	// so fractional remainders carry over to the next frame.
	private void FlushContinuousScroll()
	{
		if (_activeScrollPendingX == 0 && _activeScrollPendingY == 0)
		{
			return;
		}

		var scrollX = (nfloat)Math.Truncate(_activeScrollPendingX);
		var scrollY = (nfloat)Math.Truncate(_activeScrollPendingY);
		// Subtract only the integer part so the fractional remainder (e.g. 3.7 → 0.7)
		// carries over to the next frame. This preserves sub-pixel precision across
		// vsync frames, preventing choppy trackpad scroll at low velocities.
		_activeScrollPendingX -= (double)scrollX;
		_activeScrollPendingY -= (double)scrollY;

		if (scrollX == 0 && scrollY == 0)
		{
			return;
		}

		try
		{
			PointerWheelChanged?.Invoke(this,
				CreateScrollGestureEventArgs(new CGPoint(scrollX, scrollY), _cachedScrollLocation, _gestureIsNaturalScrolling));
		}
		catch (Exception e)
		{
			StopActiveScrollDisplayLink();
			Application.Current.RaiseRecoverableUnhandledException(e);
		}
	}

	private void StopActiveScrollDisplayLink()
	{
		_activeScrollDisplayLink?.Invalidate();
		_activeScrollDisplayLink = null;
	}

	// Pause the display link but preserve velocity so the next gesture's Ended
	// can accumulate momentum rather than starting from zero.
	private void PauseInertiaScrolling()
	{
		_momentumDisplayLink?.Invalidate();
		_momentumDisplayLink = null;
		_inertiaPendingX = 0;
		_inertiaPendingY = 0;
	}

	// Add the new gesture velocity to any preserved momentum, then (re)start the
	// display link. Called on gesture Ended so repeated quick flicks accumulate.
	private void AccumulateInertiaScrolling(CGPoint velocity)
	{
		var newVX = velocity.X / InertiaVelocityScale;
		var newVY = velocity.Y / InertiaVelocityScale;

		// If the new gesture ends in the opposite direction from preserved momentum,
		// discard the old momentum so it does not fight the user's intended direction.
		if (newVX != 0 && Math.Sign(newVX) != Math.Sign(_momentumVelocityX))
		{
			_momentumVelocityX = 0;
		}

		if (newVY != 0 && Math.Sign(newVY) != Math.Sign(_momentumVelocityY))
		{
			_momentumVelocityY = 0;
		}

		_momentumVelocityX += newVX;
		_momentumVelocityY += newVY;
		_momentumVelocityX = Math.Clamp(_momentumVelocityX, -InertiaMaxVelocity, InertiaMaxVelocity);
		_momentumVelocityY = Math.Clamp(_momentumVelocityY, -InertiaMaxVelocity, InertiaMaxVelocity);
		_lastGestureEndTime = CoreAnimation.CAAnimation.CurrentMediaTime();
		_momentumDisplayLink = CADisplayLink.Create(UpdateInertiaScrolling);
		_momentumDisplayLink.AddToRunLoop(NSRunLoop.Main, NSRunLoopMode.Common);
	}

	private void StopInertiaScrolling()
	{
		_momentumDisplayLink?.Invalidate();
		_momentumDisplayLink = null;
		_momentumVelocityX = 0;
		_momentumVelocityY = 0;
		_inertiaPendingX = 0;
		_inertiaPendingY = 0;
	}

	private void UpdateInertiaScrolling()
	{
		_momentumVelocityX *= InertiaDecelerationRate;
		_momentumVelocityY *= InertiaDecelerationRate;

		var magnitude = Math.Sqrt(_momentumVelocityX * _momentumVelocityX + _momentumVelocityY * _momentumVelocityY);
		if (magnitude < InertiaMagnitudeThreshold)
		{
			StopInertiaScrolling();
			return;
		}

		// Accumulate sub-pixel motion so inertia decelerates smoothly all the way
		// to InertiaMagnitudeThreshold instead of stopping abruptly when velocity
		// drops below 1 pt/frame (the old integer-truncation early-stop at ~60 pt/s).
		_inertiaPendingX += _momentumVelocityX;
		_inertiaPendingY += _momentumVelocityY;

		var scrollX = (nfloat)Math.Truncate(_inertiaPendingX);
		var scrollY = (nfloat)Math.Truncate(_inertiaPendingY);

		if (scrollX == 0 && scrollY == 0)
			return; // sub-pixel this frame — keep display link running

		_inertiaPendingX -= (double)scrollX;
		_inertiaPendingY -= (double)scrollY;

		try
		{
			PointerWheelChanged?.Invoke(this,
				CreateScrollGestureEventArgs(new CGPoint(scrollX, scrollY), _cachedScrollLocation, _gestureIsNaturalScrolling));
		}
		catch (Exception e)
		{
			StopInertiaScrolling();
			Application.Current.RaiseRecoverableUnhandledException(e);
		}
	}

	private PointerEventArgs CreateScrollGestureEventArgs(CGPoint translation, CGPoint location, bool isNaturalScrollingEnabled)
	{
		var multiplier = isNaturalScrollingEnabled ? -ScrollWheelDeltaMultiplier : ScrollWheelDeltaMultiplier;

		var scrollDeltaX = (int)(translation.X * multiplier);
		var scrollDeltaY = (int)(translation.Y * multiplier);

		var absX = Math.Abs(scrollDeltaX);
		var absY = Math.Abs(scrollDeltaY);
		var isHorizontal = absX > absY * 1.5;
		var wheelDelta = isHorizontal ? scrollDeltaX : scrollDeltaY;

		var properties = new PointerPointProperties()
		{
			IsLeftButtonPressed = false,
			IsRightButtonPressed = false,
			IsMiddleButtonPressed = false,
			IsXButton1Pressed = false,
			IsXButton2Pressed = false,
			PointerUpdateKind = PointerUpdateKind.Other,
			IsBarrelButtonPressed = false,
			IsEraser = false,
			IsHorizontalMouseWheel = isHorizontal,
			MouseWheelDelta = wheelDelta,
			IsPrimary = true,
			IsInRange = true,
			Orientation = 0,
			Pressure = 0,
			TouchConfidence = false,
		};

		var timestamp = PointerHelpers.ToTimestamp(CoreAnimation.CAAnimation.CurrentMediaTime());

		var point = new PointerPoint(
			frameId: PointerHelpers.ToFrameId(timestamp),
			timestamp: timestamp,
			device: PointerDevice.For(Windows.Devices.Input.PointerDeviceType.Mouse),
			pointerId: 1u,
			rawPosition: new Point(location.X, location.Y),
			position: new Point(location.X, location.Y),
			isInContact: false,
			properties: properties
		);

		return new PointerEventArgs(point, VirtualKeyModifiers.None);
	}
#endif

#if __IOS__
	private const uint MousePointerId = 1u;
	// Logical-point length of the I-beam / axis-resize pointer shapes.
	private const float CursorBeamLength = 20f;

	// Builds the pointer args for a mouse/trackpad ("indirect pointer") event. Shared by the touch
	// pipeline (clicks/drags, with the pressed buttons carried on the UIEvent) and by the hover
	// gesture recognizer (movement with no button pressed).
	private PointerEventArgs CreateMousePointerEventArgs(double nativeTimestamp, Point position, UIEventButtonMask buttonMask, VirtualKeyModifiers modifiers)
	{
		var isLeftPressed = (buttonMask & UIEventButtonMask.Primary) != 0;
		var isRightPressed = (buttonMask & UIEventButtonMask.Secondary) != 0;
		// iPadOS only names the primary/secondary buttons; the middle button is button index 2.
		var isMiddlePressed = ((long)buttonMask & (1L << 2)) != 0;

		var properties = new PointerPointProperties
		{
			IsLeftButtonPressed = isLeftPressed,
			IsRightButtonPressed = isRightPressed,
			IsMiddleButtonPressed = isMiddlePressed,
			IsXButton1Pressed = false,
			IsXButton2Pressed = false,
			IsBarrelButtonPressed = false,
			IsEraser = false,
			IsHorizontalMouseWheel = false,
			IsPrimary = true,
			IsInRange = true,
			Orientation = 0,
			Pressure = 0,
			TouchConfidence = false,
		}.SetUpdateKindFromPrevious(_previousProperties);

		_previousProperties = properties;
		_pointerPosition = position;

		var pointerPoint = new PointerPoint(
			PointerHelpers.ToFrameId(nativeTimestamp),
			PointerHelpers.ToTimestamp(nativeTimestamp),
			PointerDevice.For(Windows.Devices.Input.PointerDeviceType.Mouse),
			MousePointerId,
			position,
			position,
			properties.HasPressedButton, // isInContact == any mouse button pressed
			properties);

		return new PointerEventArgs(pointerPoint, modifiers);
	}

	// Forwards mouse/trackpad hover (movement with no button pressed) from the
	// UIHoverGestureRecognizer on the TopViewLayer. Began => entered, Changed => moved, Ended => exited.
	internal void HoverGesture(UIView source, UIHoverGestureRecognizer recognizer)
	{
		try
		{
			// While a button is held the touch pipeline owns the interaction (drag); ignore hover so
			// it cannot clobber the active contact state.
			if (_previousProperties?.HasPressedButton == true)
			{
				return;
			}

			var location = recognizer.LocationInView(source);
			var position = new Point(location.X, location.Y);
			var modifiers = VirtualKeyHelper.FromModifierFlags(recognizer.ModifierFlags);

			switch (recognizer.State)
			{
				case UIGestureRecognizerState.Began:
					PointerEntered?.Invoke(this, CreateMousePointerEventArgs(CAAnimation.CurrentMediaTime(), position, 0, modifiers));
					PointerMoved?.Invoke(this, CreateMousePointerEventArgs(CAAnimation.CurrentMediaTime(), position, 0, modifiers));
					break;
				case UIGestureRecognizerState.Changed:
					PointerMoved?.Invoke(this, CreateMousePointerEventArgs(CAAnimation.CurrentMediaTime(), position, 0, modifiers));
					break;
				case UIGestureRecognizerState.Ended:
				case UIGestureRecognizerState.Cancelled:
				case UIGestureRecognizerState.Failed:
					PointerExited?.Invoke(this, CreateMousePointerEventArgs(CAAnimation.CurrentMediaTime(), position, 0, modifiers));
					break;
			}
		}
		catch (Exception e)
		{
			TraceError(e);
			Application.Current.RaiseRecoverableUnhandledException(e);
		}
	}

	// Registers a callback the TopViewLayer uses to refresh its UIPointerInteraction whenever the
	// active cursor changes (the managed InputManager assigns PointerCursor as the pointer moves).
	internal void RegisterCursorInvalidator(Action invalidate) => _invalidateCursor = invalidate;

	// Maps the WinUI cursor to an iPadOS pointer style. iPadOS exposes an adaptive pointer rather than
	// a fixed cursor catalog, so only the text beam and axis beams have direct equivalents; a null
	// cursor hides the pointer and everything else falls back to the system pointer.
	internal UIPointerStyle GetPointerStyle()
	{
		var cursor = _pointerCursor;
		if (cursor is null)
		{
			return UIPointerStyle.CreateHiddenPointerStyle();
		}

		return cursor.Type switch
		{
			CoreCursorType.IBeam => UIPointerStyle.Create(UIPointerShape.CreateBeam((nfloat)CursorBeamLength, UIAxis.Vertical), UIAxis.Neither),
			CoreCursorType.SizeWestEast => UIPointerStyle.Create(UIPointerShape.CreateBeam((nfloat)CursorBeamLength, UIAxis.Horizontal), UIAxis.Neither),
			CoreCursorType.SizeNorthSouth => UIPointerStyle.Create(UIPointerShape.CreateBeam((nfloat)CursorBeamLength, UIAxis.Vertical), UIAxis.Neither),
			_ => UIPointerStyle.CreateSystemPointerStyle(),
		};
	}
#endif

	private PointerEventArgs CreatePointerEventArgs(UIView source, UITouch touch, UIEvent? evt)
	{
#if __TVOS__
		var position = touch.LocationInView(source);
#else
		var position = touch.GetPreciseLocation(source);
#endif
		var pointerDeviceType = touch.Type.ToPointerDeviceType();

#if __IOS__
		if (pointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
		{
			// Mouse/trackpad ("indirect pointer") clicks arrive through the touch pipeline on
			// iPadOS 13.4+, with the pressed buttons reported on the UIEvent (not the UITouch).
			var buttonMask = evt?.ButtonMask ?? (UIEventButtonMask)0;
			var mouseModifiers = VirtualKeyHelper.FromModifierFlags(evt?.ModifierFlags ?? default);
			return CreateMousePointerEventArgs(touch.Timestamp, new Point(position.X, position.Y), buttonMask, mouseModifiers);
		}
#endif
		var isInContact = touch.Phase == UITouchPhase.Began
			|| touch.Phase == UITouchPhase.Moved
			|| touch.Phase == UITouchPhase.Stationary;
		var pointerDevice = PointerDevice.For(pointerDeviceType);
		var id = (uint)(int)touch.Handle;
		var properties = new PointerPointProperties
		{
			IsLeftButtonPressed = isInContact,
			IsRightButtonPressed = false,
			IsMiddleButtonPressed = false,
			IsXButton1Pressed = false,
			IsXButton2Pressed = false,
			PointerUpdateKind = touch.Phase switch
			{
				UITouchPhase.Began => PointerUpdateKind.LeftButtonPressed,
				UITouchPhase.Ended => PointerUpdateKind.LeftButtonReleased,
				_ => PointerUpdateKind.Other
			},
			IsBarrelButtonPressed = false,
			IsEraser = false,
			IsHorizontalMouseWheel = false,
			IsPrimary = true,
			IsInRange = true,
			Orientation = 0,
			Pressure = (float)(touch.Force / touch.MaximumPossibleForce),
			TouchConfidence = isInContact,
		};

		var frameId = PointerHelpers.ToFrameId(touch.Timestamp);
		var timestamp = PointerHelpers.ToTimestamp(touch.Timestamp);
		var pointerPoint = new PointerPoint(
			frameId,
			timestamp,
			pointerDevice,
			id,
			position,
			position,
			isInContact,
			properties);
		return new PointerEventArgs(pointerPoint, Windows.System.VirtualKeyModifiers.None); // TODO:MZ: Key modifiers
	}

	#region Tracing
	private static readonly Action<string>? _trace = typeof(AppleUIKitCorePointerInputSource).Log().IsTraceEnabled()
		? typeof(AppleUIKitCorePointerInputSource).Log().Trace
		: null;

	private void TraceStart(UIView source, NSSet touches, [CallerMemberName] string action = "")
#if __TVOS__
		=> _trace?.Invoke($"<{action} touches={touches.Count} src={source.GetDebugName()}>");
#else
		=> _trace?.Invoke($"<{action} touches={touches.Count} src={source.GetDebugName()} multi={source.MultipleTouchEnabled} exclusive={source.ExclusiveTouch}>");
#endif

	private void TraceSingle(PointerEventArgs args, [CallerMemberName] string action = "")
		=> _trace?.Invoke($"{action}: {args}>");

	private void TraceEnd([CallerMemberName] string action = "")
		=> _trace?.Invoke($"</{action}>");

	private void TraceError(Exception error, [CallerMemberName] string action = "")
		=> _trace?.Invoke($"</{action} error=true>\r\n" + error);
	#endregion
}
