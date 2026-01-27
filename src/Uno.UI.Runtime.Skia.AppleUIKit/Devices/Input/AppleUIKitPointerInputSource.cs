using System;
using System.Runtime.CompilerServices;
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

#if __MACCATALYST__
	internal void ScrollWheelChanged(UIView source, UIEvent evt)
	{
		try
		{
			if (evt == null || evt.Type != UIEventType.Scroll)
			{
				return;
			}

			_trace?.Invoke($"<ScrollWheel src={source.GetDebugName()}>");

			var args = CreateScrollWheelEventArgs(source, evt);

			_trace?.Invoke($"ScrollWheel: {args}>");

			PointerWheelChanged?.Invoke(this, args);

			_trace?.Invoke("</ScrollWheel>");
		}
		catch (Exception e)
		{
			_trace?.Invoke($"</ScrollWheel error=true>\r\n" + e);
			Application.Current.RaiseRecoverableUnhandledException(e);
		}
	}
#endif

#if __IOS__ && !__MACCATALYST__
	internal void HandleScrollFromGesture(UIView source, CGPoint translation, CGPoint location)
	{
		try
		{
			if (!UIDevice.CurrentDevice.CheckSystemVersion(13, 4))
			{
				return;
			}

			_trace?.Invoke($"<ScrollGesture src={source.GetDebugName()}>");

			var args = CreateScrollGestureEventArgs(translation, location);

			_trace?.Invoke($"ScrollGesture: {args}>");

			_trace?.Invoke($"iOS HandleScrollFromGesture: Delta={args.CurrentPoint.Properties.MouseWheelDelta}, IsHorizontal={args.CurrentPoint.Properties.IsHorizontalMouseWheel}, Position=({args.CurrentPoint.Position.X}, {args.CurrentPoint.Position.Y}), Translation=({translation.X:F2}, {translation.Y:F2})");

			PointerWheelChanged?.Invoke(this, args);

			_trace?.Invoke("</ScrollGesture>");
		}
		catch (Exception e)
		{
			_trace?.Invoke($"</ScrollGesture error=true>\r\n" + e);
			Application.Current.RaiseRecoverableUnhandledException(e);
		}
	}

	private PointerEventArgs CreateScrollGestureEventArgs(CGPoint translation, CGPoint location)
	{
		var scrollDeltaX = (int)(translation.X * -120);
		var scrollDeltaY = (int)(translation.Y * -120);

		var position = location;

		var pointerDevice = PointerDevice.For(Windows.Devices.Input.PointerDeviceType.Mouse);
		var id = 1u;

		var isHorizontal = scrollDeltaY == 0 && scrollDeltaX != 0;
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

		var timestamp = PointerHelpers.ToTimestamp(DateTimeOffset.Now.Ticks);

		var point = new PointerPoint(
			frameId: PointerHelpers.ToFrameId(timestamp),
			timestamp: timestamp,
			device: pointerDevice,
			pointerId: id,
			rawPosition: new Point(position.X, position.Y),
			position: new Point(position.X, position.Y),
			isInContact: false,
			properties: properties
		);

		return new PointerEventArgs(point, VirtualKeyModifiers.None);
	}
#endif

#if __MACCATALYST__
	private PointerEventArgs CreateScrollWheelEventArgs(UIView source, UIEvent evt)
	{
		var scrollDeltaX = (int)(evt.ScrollingDeltaX * 120);
		var scrollDeltaY = (int)(evt.ScrollingDeltaY * 120);

		var position = evt.LocationInWindow(source.Window) ?? new CGPoint(source.Bounds.GetMidX(), source.Bounds.GetMidY());
		var positionInView = source.ConvertPointFromView(position, null);

		var pointerDevice = PointerDevice.For(Windows.Devices.Input.PointerDeviceType.Mouse);
		var id = 1u; // Use a fixed ID for mouse pointer

		var isHorizontal = scrollDeltaY == 0 && scrollDeltaX != 0;
		var wheelDelta = isHorizontal ? scrollDeltaX : scrollDeltaY;

		var properties = new PointerPointProperties
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

		var frameId = PointerHelpers.ToFrameId(evt.Timestamp);
		var timestamp = PointerHelpers.ToTimestamp(evt.Timestamp);
		var pointerPoint = new PointerPoint(
			frameId,
			timestamp,
			pointerDevice,
			id,
			new Point(positionInView.X, positionInView.Y),
			new Point(positionInView.X, positionInView.Y),
			false,
			properties);

		 // TODO: Key modifiers
		return new PointerEventArgs(pointerPoint, Windows.System.VirtualKeyModifiers.None);
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
