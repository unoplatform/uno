#nullable enable

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.Foundation.Logging;
using Uno.UI.Dispatching;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Input;
using static Windows.UI.Input.PointerUpdateKind;
using _NativeMethods = __Windows.UI.Core.CoreWindow.NativeMethods;
using _PointerIdentifier = Windows.Devices.Input.PointerIdentifier; // internal type (should be in Uno namespace)
using _PointerIdentifierPool = Windows.Devices.Input.PointerIdentifierPool; // internal type (should be in Uno namespace)

namespace Uno.UI.Runtime;

internal partial class BrowserPointerInputSource : IUnoCorePointerInputSource
{
	// Ref:
	// https://www.w3.org/TR/pointerevents/
	// https://developer.mozilla.org/en-US/docs/Web/API/PointerEvent
	// https://developer.mozilla.org/en-US/docs/Web/API/WheelEvent

	private static readonly Logger _log = typeof(BrowserPointerInputSource).Log();
	private static readonly Logger? _logTrace = _log.IsTraceEnabled() ? _log : null;

	// TODO: Verify the boot time unit (ms or ticks)
	private ulong _bootTime;
	private bool _isIOs;
	private bool _isOver;
	private PointerPoint? _lastPoint;
	private CoreCursor _pointerCursor = new(CoreCursorType.Arrow, 0);

#pragma warning disable CS0067 // Some event are not raised on skia browser ... yet!
	public event TypedEventHandler<object, Windows.UI.Core.PointerEventArgs>? PointerCaptureLost;
#pragma warning restore CS0067 // Some event are not raised on skia browser ... yet!
	public event TypedEventHandler<object, Windows.UI.Core.PointerEventArgs>? PointerEntered;
	public event TypedEventHandler<object, Windows.UI.Core.PointerEventArgs>? PointerExited;
	public event TypedEventHandler<object, Windows.UI.Core.PointerEventArgs>? PointerMoved;
	public event TypedEventHandler<object, Windows.UI.Core.PointerEventArgs>? PointerPressed;
	public event TypedEventHandler<object, Windows.UI.Core.PointerEventArgs>? PointerReleased;
	public event TypedEventHandler<object, Windows.UI.Core.PointerEventArgs>? PointerWheelChanged;
	public event TypedEventHandler<object, Windows.UI.Core.PointerEventArgs>? PointerCancelled; // Uno Only

	public BrowserPointerInputSource()
	{
		_logTrace?.Trace("Initializing BrowserPointerInputSource");

		Initialize(this);
	}

	[JSImport("globalThis.Uno.UI.Runtime.Skia.BrowserPointerInputSource.initialize")]
	private static partial void Initialize([JSMarshalAs<JSType.Any>] object inputSource);

	[JSExport]
	private static void OnInitialized([JSMarshalAs<JSType.Any>] object inputSource, double bootTime, string userAgent)
	{
		if (inputSource is BrowserPointerInputSource that)
		{
			that._bootTime = (ulong)bootTime;

			// Note: OperatingSystem.IsIOS() is false
			that._isIOs = userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase)
				|| userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase);

			_logTrace?.Trace("Complete initialization of BrowserPointerInputSource, we are now ready to receive pointer events!");
		}
		else if (_log.IsEnabled(LogLevel.Error))
		{
			_log.Error("Requested init using an invalid source.");
		}
	}

	[JSExport]
	[return: JSMarshalAs<JSType.Number>]
	private static int OnNativeEvent(
		[JSMarshalAs<JSType.Any>] object inputSource,
		byte @event, // ONE of NativePointerEvent
		double timestamp,
		int deviceType, // ONE of _PointerDeviceType
		double pointerId, // Warning: This is a Number in JS, and it might be negative on safari for iOS
		double x,
		double y,
		bool ctrl,
		bool shift,
		int buttons,
		int buttonUpdate,
		double pressure,
		double wheelDeltaX,
		double wheelDeltaY,
		bool hasRelatedTarget)
	{

		try
		{
			_logTrace?.Trace($"Pointer evt={(HtmlPointerEvent)@event}|id={pointerId}|x={x}|y={x}|ctrl={ctrl}|shift={shift}|bts={buttons}|btUpdate={buttonUpdate}|type={(PointerDeviceType)deviceType}|ts={timestamp}|pres={pressure}|wheelX={wheelDeltaX}|wheelY={wheelDeltaY}|relTarget={hasRelatedTarget}");

			// Ensure that the async context is set properly, since we're raising
			// events from outside the dispatcher.
			using var syncContextScope = NativeDispatcher.Main.SynchronizationContext.Apply();

			var that = (BrowserPointerInputSource)inputSource;
			var evt = (HtmlPointerEvent)@event;
			var pointerType = (PointerDeviceType)deviceType;
			var pointerDevice = PointerDevice.For(pointerType);
			var pointerIdentifier = _PointerIdentifierPool.RentManaged(new _PointerIdentifier((PointerDeviceType)deviceType, (uint)pointerId));

			var frameId = ToFrameId(timestamp);
			var ts = that.ToTimestamp(timestamp);
			var isInContact = buttons != 0;
			var isInRange = GetIsInRange(@event, hasRelatedTarget, pointerType, isInContact);
			var keyModifiers = GetKeyModifiers(ctrl, shift);
			var position = new Point(x, y);

			var properties = GetProperties(pointerType, isInRange, (HtmlPointerButtonsState)buttons, (HtmlPointerButtonUpdate)buttonUpdate, wheel: (false, -wheelDeltaY), pressure);

			var point = new PointerPoint(frameId, ts, pointerDevice, pointerIdentifier.Id, position, position, isInContact, properties);
			var args = new PointerEventArgs(point, keyModifiers);

			that._lastPoint = point;

			switch (evt)
			{
				case HtmlPointerEvent.pointerover when that._isOver:
					// If there isn't any html node that hit-tested, we might get a pointer-over event, but we are interested only in pointer entering over the window
					_logTrace?.Trace("Ignoring pointer-over event since pointer is already over the window.");
					break;

				case HtmlPointerEvent.pointerover:
					that._isOver = true;
					that.PointerEntered?.Invoke(that, args);
					break;

				case HtmlPointerEvent.pointerleave:
					that._isOver = false;
					that.PointerExited?.Invoke(that, args);
					_PointerIdentifierPool.ReleaseManaged(pointerIdentifier);
					break;

				case HtmlPointerEvent.pointerdown:
					that.PointerPressed?.Invoke(that, args);
					break;

				case HtmlPointerEvent.pointerup:
					//case HtmlPointerEvent.lostpointercapture: // if pointer is captured, we don't get a up, just a capture lost (with skia for wasm)
					that.PointerReleased?.Invoke(that, args);
					if (that._isIOs && args is { CurrentPoint.PointerDeviceType: PointerDeviceType.Touch, DispatchResult: UIElement.PointerEventDispatchResult { VisualTreeAltered: true } })
					{
						// On iOS, when the element under the pointer is removed, the browser won't send any pointer leave event.

						args.DispatchResult = null; // To be clean only, the value is not used in the leave case.
						goto case HtmlPointerEvent.pointerleave;
					}
					break;

				case HtmlPointerEvent.pointermove:
					that.PointerMoved?.Invoke(that, args);
					break;

				case HtmlPointerEvent.wheel:
					if (wheelDeltaY is not 0)
					{
						that.PointerWheelChanged?.Invoke(that, args);
					}

					if (wheelDeltaX is not 0)
					{
						properties = GetProperties(pointerType, isInRange, (HtmlPointerButtonsState)buttons, (HtmlPointerButtonUpdate)buttonUpdate, wheel: (true, wheelDeltaX), pressure);
						point = new PointerPoint(frameId, ts, pointerDevice, pointerIdentifier.Id, position, position, isInContact, properties);
						args = new PointerEventArgs(point, keyModifiers);

						that.PointerWheelChanged?.Invoke(that, args);
					}

					break;

				case HtmlPointerEvent.pointercancel:
					that.PointerCancelled?.Invoke(that, args);
					_PointerIdentifierPool.ReleaseManaged(pointerIdentifier);
					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(@event), $"Unknown event ({@event}-{evt}).");
			}

			return (int)Microsoft.UI.Xaml.HtmlEventDispatchResult.Ok;
		}
		catch (Exception error)
		{
			if (_log.IsEnabled(LogLevel.Error))
			{
				_log.Error($"Failed to dispatch native pointer event: {error}");
			}

			return (int)Microsoft.UI.Xaml.HtmlEventDispatchResult.Ok;
		}
	}

	[NotImplemented] public bool HasCapture => false;

	public CoreCursor PointerCursor
	{
		get => _pointerCursor;
		set
		{
			_pointerCursor = value;
			_NativeMethods.SetCursor(_pointerCursor.Type.ToCssCursor());
		}
	}

	public Point PointerPosition => _lastPoint?.Position ?? default;

	#region Captures
	public void SetPointerCapture()
	{
		if (_lastPoint is not null)
		{
			SetPointerCapture(_lastPoint.Pointer);
		}
	}
	public void SetPointerCapture(PointerIdentifier pointer)
	{
		if (_PointerIdentifierPool.TryGetNative(pointer, out var native))
		{
			SetPointerCaptureNative(native.Id);
		}
		else if (this.Log().IsEnabled(LogLevel.Warning))
		{
			this.Log().Warn($"Cannot capture pointer, could not find native pointer id for managed pointer id {pointer}");
		}
	}

	[JSImport("globalThis.Uno.UI.Runtime.Skia.BrowserPointerInputSource.setPointerCapture")]
	private static partial void SetPointerCaptureNative(double pointerId); // double as it might be negative on safari for iOS

	public void ReleasePointerCapture()
	{
		if (_lastPoint is not null)
		{
			ReleasePointerCapture(_lastPoint.Pointer);
		}
	}

	public void ReleasePointerCapture(PointerIdentifier pointer)
	{
		if (_PointerIdentifierPool.TryGetNative(pointer, out var native))
		{
			ReleasePointerCaptureNative(native.Id);
		}
		else if (this.Log().IsEnabled(LogLevel.Warning))
		{
			this.Log().Warn($"Cannot release pointer, could not find native pointer id for managed pointer id {pointer}");
		}
	}

	[JSImport("globalThis.Uno.UI.Runtime.Skia.BrowserPointerInputSource.releasePointerCapture")]
	private static partial void ReleasePointerCaptureNative(double pointerId); // double as it might be negative on safari for iOS
	#endregion

	#region Native to manage convertion helpers
	private static VirtualKeyModifiers GetKeyModifiers(bool ctrl, bool shift)
	{
		var keyModifiers = VirtualKeyModifiers.None;
		if (ctrl) keyModifiers |= VirtualKeyModifiers.Control;
		if (shift) keyModifiers |= VirtualKeyModifiers.Shift;
		return keyModifiers;
	}

	private static bool GetIsInRange(byte @event, bool hasRelatedTarget, PointerDeviceType pointerType, bool isInContact)
	{
		const int cancel = (int)HtmlPointerEvent.pointercancel;
		const int exitOrUp = (int)(HtmlPointerEvent.pointerleave | HtmlPointerEvent.pointerup);

		var isInRange = true;
		if (@event is cancel)
		{
			isInRange = false;
		}
		else if ((@event & exitOrUp) != 0)
		{
			isInRange = pointerType switch
			{
				PointerDeviceType.Mouse => true, // Mouse is always in range (unless for 'cancel' eg. if it was unplugged from computer)
				PointerDeviceType.Touch => false, // If we get a pointer out for touch, it means that pointer left the screen (pt up - a.k.a. Implicit capture)
				PointerDeviceType.Pen => hasRelatedTarget, // If the relatedTarget is null it means pointer left the detection range ... but only for out event!
				_ => !isInContact // Safety!
			};
		}

		return isInRange;
	}

	private static PointerPointProperties GetProperties(
		PointerDeviceType deviceType,
		bool isInRange,
		HtmlPointerButtonsState buttons,
		HtmlPointerButtonUpdate buttonUpdate,
		(bool isHorizontalWheel, double delta) wheel,
		double pressure)
	{
		var props = new PointerPointProperties
		{
			IsPrimary = true,
			IsInRange = isInRange,
			IsLeftButtonPressed = buttons.HasFlag(HtmlPointerButtonsState.Left),
			IsMiddleButtonPressed = buttons.HasFlag(HtmlPointerButtonsState.Middle),
			IsRightButtonPressed = buttons.HasFlag(HtmlPointerButtonsState.Right),
			IsXButton1Pressed = buttons.HasFlag(HtmlPointerButtonsState.X1),
			IsXButton2Pressed = buttons.HasFlag(HtmlPointerButtonsState.X2),
			IsEraser = buttons.HasFlag(HtmlPointerButtonsState.Eraser),
			IsHorizontalMouseWheel = wheel.isHorizontalWheel,
			MouseWheelDelta = (int)wheel.delta
		};

		switch (deviceType)
		{
			// For touch and mouse, we keep the default pressure of .5, as WinUI

			case PointerDeviceType.Pen:
				// !!! WARNING !!! Here we have a slight different behavior compared to WinUI:
				// On WinUI we will get IsRightButtonPressed (with IsBarrelButtonPressed) only if the user is pressing
				// the barrel button when pen goes "in contact" (i.e. touches the screen), otherwise we will get
				// IsLeftButtonPressed and IsBarrelButtonPressed.
				// Here we set IsRightButtonPressed as soon as the barrel button was pressed, no matter
				// if the pen was already in contact or not.
				// This is acceptable since the UIElement pressed state is **per pointer** (not buttons of pointer)
				// and GestureRecognizer always checks that pressed buttons didn't changed for a single gesture.
				props.IsBarrelButtonPressed = props.IsRightButtonPressed;
				props.Pressure = (float)pressure;
				break;
		}

		// Needs to be computed only after the props is almost completed
		props.PointerUpdateKind = ToUpdateKind(buttonUpdate, props);

		return props;
	}

	internal static uint ToFrameId(double timestamp)
		// Known limitation: After 49 days, we will overflow the uint and frame IDs will restart at 0.
		=> (uint)(timestamp % uint.MaxValue);

	private ulong ToTimestamp(double timestamp)
		=> _bootTime + (ulong)(timestamp * 1000);

	private static PointerUpdateKind ToUpdateKind(HtmlPointerButtonUpdate update, PointerPointProperties props)
		=> update switch
		{
			HtmlPointerButtonUpdate.Left when props.IsLeftButtonPressed => PointerUpdateKind.LeftButtonPressed,
			HtmlPointerButtonUpdate.Left => PointerUpdateKind.LeftButtonReleased,
			HtmlPointerButtonUpdate.Middle when props.IsMiddleButtonPressed => PointerUpdateKind.MiddleButtonPressed,
			HtmlPointerButtonUpdate.Middle => PointerUpdateKind.MiddleButtonReleased,
			HtmlPointerButtonUpdate.Right when props.IsRightButtonPressed => PointerUpdateKind.RightButtonPressed,
			HtmlPointerButtonUpdate.Right => PointerUpdateKind.RightButtonReleased,
			HtmlPointerButtonUpdate.X1 when props.IsXButton1Pressed => PointerUpdateKind.XButton1Pressed,
			HtmlPointerButtonUpdate.X1 => PointerUpdateKind.XButton1Released,
			HtmlPointerButtonUpdate.X2 when props.IsXButton2Pressed => PointerUpdateKind.XButton1Pressed,
			HtmlPointerButtonUpdate.X2 => PointerUpdateKind.XButton1Released,
			_ => PointerUpdateKind.Other
		};

	private enum HtmlPointerEvent : byte
	{
		// WARNING: This enum has a corresponding version in TypeScript!

		// Minimal default pointer required to maintain state
		pointerover = 1,
		pointerleave = 1 << 1,
		pointerdown = 1 << 2,
		pointerup = 1 << 3,
		pointercancel = 1 << 4,
		pointermove = 1 << 5, // Required since when pt is captured, isOver will be maintained by the moveWithOverCheck
		lostpointercapture = 1 << 6, // Required to get pointer up when pointer is captured

		// Optional pointer events
		wheel = 1 << 7,
	}

	[Flags]
	internal enum HtmlPointerButtonsState
	{
		// https://developer.mozilla.org/en-US/docs/Web/API/Pointer_events#Determining_button_states

		None = 0,
		Left = 1,
		Middle = 4,
		Right = 2,
		X1 = 8,
		X2 = 16,
		Eraser = 32,
	}

	private enum HtmlPointerButtonUpdate
	{
		None = -1,
		Left = 0,
		Middle = 1,
		Right = 2,
		X1 = 3,
		X2 = 4,
		Eraser = 5
	}
	#endregion
}
