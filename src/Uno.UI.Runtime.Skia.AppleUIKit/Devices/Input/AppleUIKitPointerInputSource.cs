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
	// each event directly moves the scroll by: round(delta * scrollPerNotch / 120).
	// For scrollPerNotch ≈ 90 (15% of a 600px viewport), multiplier=3 gives ~2:1 scroll ratio:
	//   slow  (2 pts/frame)  → delta=6  → ~5 px/frame  → 300 px/s
	//   normal(10 pts/frame) → delta=30 → ~23 px/frame → 1380 px/s
	//   fast  (30 pts/frame) → delta=90 → ~68 px/frame → 4080 px/s
	// Inertia uses the same multiplier (velocity/InertiaVelocityScale * this) for a seamless handoff.
	private const int ScrollWheelDeltaMultiplier = -60;
	private const double MinTranslationThreshold = 1.0;
	private const double InertiaDecelerationRate = 0.95;
	private const double InertiaMagnitudeThreshold = 0.0001;
	// Divide velocity (pts/s) by display refresh rate to get per-frame delta,
	// matching the scale of active-scroll translation deltas for a smooth handoff.
	private const double InertiaVelocityScale = 60.0;

	private CADisplayLink? _momentumDisplayLink;
	private double _momentumVelocityX;
	private double _momentumVelocityY;
	private CGPoint _cachedScrollLocation;
	private bool _gestureIsNaturalScrolling;
#endif
#if __MACCATALYST__
	private const double ScrollWheelDeltaMultiplier = 120.0;
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
					StopInertiaScrolling();
					_cachedScrollLocation = gesture.LocationInView(source);
					_gestureIsNaturalScrolling = isNaturalScrollingEnabled;
					return;

				case UIGestureRecognizerState.Changed:
					var translation = gesture.TranslationInView(source);
					if (Math.Abs(translation.X) < MinTranslationThreshold && Math.Abs(translation.Y) < MinTranslationThreshold)
						return;
					_cachedScrollLocation = gesture.LocationInView(source);
					_trace?.Invoke($"<ScrollGesture src={source.GetDebugName()} state={gesture.State}>");
					PointerWheelChanged?.Invoke(this, CreateScrollGestureEventArgs(translation, _cachedScrollLocation, isNaturalScrollingEnabled));
					_trace?.Invoke("</ScrollGesture>");
					gesture.SetTranslation(CGPoint.Empty, source);
					return;

				case UIGestureRecognizerState.Ended:
					_gestureIsNaturalScrolling = isNaturalScrollingEnabled;
					StartInertiaScrolling(gesture.VelocityInView(source));
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

	private void StartInertiaScrolling(CGPoint velocity)
	{
		StopInertiaScrolling();
		_momentumVelocityX = velocity.X / InertiaVelocityScale;
		_momentumVelocityY = velocity.Y / InertiaVelocityScale;
		_momentumDisplayLink = CADisplayLink.Create(UpdateInertiaScrolling);
		_momentumDisplayLink.AddToRunLoop(NSRunLoop.Main, NSRunLoopMode.Common);
	}

	private void StopInertiaScrolling()
	{
		_momentumDisplayLink?.Invalidate();
		_momentumDisplayLink = null;
		_momentumVelocityX = 0;
		_momentumVelocityY = 0;
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

		try
		{
			var translation = new CGPoint(_momentumVelocityX, _momentumVelocityY);
			PointerWheelChanged?.Invoke(this, CreateScrollGestureEventArgs(translation, _cachedScrollLocation, _gestureIsNaturalScrolling));
		}
		catch (Exception e)
		{
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
