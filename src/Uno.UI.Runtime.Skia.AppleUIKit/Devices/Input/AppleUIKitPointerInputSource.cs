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
	private const double MinTranslationThreshold = 1.0;
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
	private double _momentumVelocityX;
	private double _momentumVelocityY;
	// Sub-pixel accumulator: collects fractional pts across frames so inertia
	// decelerates smoothly all the way to InertiaMagnitudeThreshold instead of
	// stopping abruptly when velocity drops below 1 pt/frame.
	private double _inertiaPendingX;
	private double _inertiaPendingY;
	private double _lastGestureEndTime;
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

	public bool HasCapture => false;

	public Point PointerPosition => default;

	public CoreCursor PointerCursor
	{
		get => new(CoreCursorType.Arrow, 0);
		set { }
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
				var args = CreatePointerEventArgs(source, touch);

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
				var args = CreatePointerEventArgs(source, touch);

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
				var args = CreatePointerEventArgs(source, touch);

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
				var args = CreatePointerEventArgs(source, touch);

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
	internal void HandleScrollFromGesture(UIView source, UIPanGestureRecognizer gesture, bool isNaturalScrollingEnabled)
	{
		try
		{
			if (!UIDevice.CurrentDevice.CheckSystemVersion(13, 4))
				return;

			switch (gesture.State)
			{
				case UIGestureRecognizerState.Began:
					// Only preserve momentum for rapid back-to-back flicks (within the
					// time window). For slower starts the previous inertia is stale and
					// fighting it causes the noticeable delay the user reported.
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
					_cachedScrollLocation = gesture.LocationInView(source);
					_gestureIsNaturalScrolling = isNaturalScrollingEnabled;
					// Pre-position the scroll pointer (ID=1) at the cursor location so that
					// the first PointerWheelChanged event does not trigger a PointerExited /
					// PointerEntered visual-state transition on the item under the cursor.
					// Without this, OnPointerWheelChanged's RaiseLeaveEnter sees the item as
					// stale (it was PointerOver via a UITouch hover pointer, a different ID)
					// and fires Leave then Enter — briefly flashing the item as selected and
					// causing a layout pass that delays the first scroll frame.
					PointerMoved?.Invoke(this, CreateScrollGestureEventArgs(CGPoint.Empty, _cachedScrollLocation, isNaturalScrollingEnabled));
					return;

				case UIGestureRecognizerState.Changed:
					var translation = gesture.TranslationInView(source);
					if (Math.Abs(translation.X) < MinTranslationThreshold && Math.Abs(translation.Y) < MinTranslationThreshold)
					{
						return;
					}

					_activeScrollPendingX += (double)translation.X;
					_activeScrollPendingY += (double)translation.Y;
					var activeScrollX = (nfloat)Math.Truncate(_activeScrollPendingX);
					var activeScrollY = (nfloat)Math.Truncate(_activeScrollPendingY);
					_activeScrollPendingX -= (double)activeScrollX;
					_activeScrollPendingY -= (double)activeScrollY;
					gesture.SetTranslation(CGPoint.Empty, source);

					if (activeScrollX == 0 && activeScrollY == 0)
					{
						return;
					}

					_cachedScrollLocation = gesture.LocationInView(source);
					_trace?.Invoke($"<ScrollGesture src={source.GetDebugName()} state={gesture.State}>");
					PointerWheelChanged?.Invoke(this, CreateScrollGestureEventArgs(new CGPoint(activeScrollX, activeScrollY), _cachedScrollLocation, isNaturalScrollingEnabled));
					_trace?.Invoke("</ScrollGesture>");
					return;

				case UIGestureRecognizerState.Ended:
					_gestureIsNaturalScrolling = isNaturalScrollingEnabled;
					AccumulateInertiaScrolling(gesture.VelocityInView(source));
					return;

				case UIGestureRecognizerState.Cancelled:
				case UIGestureRecognizerState.Failed:
					StopInertiaScrolling();
					return;
			}
		}
		catch (Exception e)
		{
			_trace?.Invoke($"</ScrollGesture error=true>\r\n" + e);
			Application.Current.RaiseRecoverableUnhandledException(e);
		}
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

	private PointerEventArgs CreatePointerEventArgs(UIView source, UITouch touch)
	{
#if __TVOS__
		var position = touch.LocationInView(source);
#else
		var position = touch.GetPreciseLocation(source);
#endif
		var pointerDeviceType = touch.Type.ToPointerDeviceType();
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
