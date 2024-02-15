#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Windows.Devices.Input;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Input;

using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSUnoCorePointerInputSource : IUnoCorePointerInputSource
{
	// https://developer.apple.com/documentation/appkit/nseventtype
	const int NSEventTypeLeftMouseDown = 1;
	const int NSEventTypeRightMouseDown = 2;
	const int NSEventTypeOtherMouseDown = 25;

	public static MacOSUnoCorePointerInputSource Instance = new();

	private CoreCursor? _pointerCursor = new(CoreCursorType.Arrow, 0);

	private static Point _previousPosition;
	private static PointerPointProperties? _previousProperties;

	private MacOSUnoCorePointerInputSource()
	{
	}

	public static unsafe void Register()
	{
		ApiExtensibility.Register(typeof(IUnoCorePointerInputSource), o => Instance);
		NativeUno.uno_set_window_mouse_event_callback(&MouseEvent);
	}

	[NotImplemented] public bool HasCapture => false;

	public CoreCursor? PointerCursor
	{
		get => _pointerCursor;
		set
		{
			if (value is null)
			{
				if (_pointerCursor is not null)
				{
					NativeUno.uno_cursor_hide();
					_pointerCursor = null;
				}
			}
			else
			{
				if (_pointerCursor is null)
				{
					NativeUno.uno_cursor_unhide();
				}
				_pointerCursor = value;
				if (!NativeUno.uno_cursor_set(_pointerCursor.Type))
				{
					if (this.Log().IsEnabled(LogLevel.Warning))
					{
						this.Log().LogWarning($"Cursor type '{_pointerCursor.Type}' is not supported on macOS. Default cursor is used instead.");
					}
				}
			}
		}
	}

	public Point PointerPosition => _previousPosition;

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

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	internal static unsafe int MouseEvent(NativeMouseEventData* data)
	{
		try
		{
			TypedEventHandler<object, PointerEventArgs>? mouseEvent = null;
			switch (data->EventType)
			{
				case NativeMouseEvents.Entered:
					mouseEvent = Instance.PointerEntered;
					break;
				case NativeMouseEvents.Exited:
					mouseEvent = Instance.PointerExited;
					break;
				case NativeMouseEvents.Down:
					mouseEvent = Instance.PointerPressed;
					break;
				case NativeMouseEvents.Up:
					mouseEvent = Instance.PointerReleased;
					break;
				case NativeMouseEvents.Moved:
					mouseEvent = Instance.PointerMoved;
					break;
				case NativeMouseEvents.ScrollWheel:
					mouseEvent = Instance.PointerWheelChanged;
					break;
			}
			if (mouseEvent is null)
			{
				return 0; // unhandled
			}

			mouseEvent(Instance, BuildPointerArgs(*data));
			return 1; // handled
		}
		catch (Exception e)
		{
			Microsoft.UI.Xaml.Application.Current.RaiseRecoverableUnhandledException(e);
			return 0;
		}
	}

	private static PointerEventArgs BuildPointerArgs(NativeMouseEventData data)
	{
		var position = new Point(data.X, data.Y);
		var pointerDevice = PointerDevice.For(data.PointerDeviceType);
		var properties = GetPointerProperties(data).SetUpdateKindFromPrevious(_previousProperties);

		var point = new PointerPoint(data.FrameId, data.Timestamp, pointerDevice, data.Pid, position, position, data.InContact, properties);
		var args = new PointerEventArgs(point, data.KeyModifiers);

		_previousPosition = position;
		_previousProperties = properties;

		return args;
	}

	private static PointerPointProperties GetPointerProperties(NativeMouseEventData data)
	{
		var properties = new PointerPointProperties()
		{
			IsInRange = true,
			IsPrimary = true,
			IsLeftButtonPressed = (data.MouseButtons & NSEventTypeLeftMouseDown) == NSEventTypeLeftMouseDown,
			IsRightButtonPressed = (data.MouseButtons & NSEventTypeRightMouseDown) == NSEventTypeRightMouseDown,
			IsMiddleButtonPressed = (data.MouseButtons & NSEventTypeOtherMouseDown) == NSEventTypeOtherMouseDown,
		};

		if (data.PointerDeviceType == PointerDeviceType.Pen)
		{
			properties.XTilt = data.TiltX;
			properties.YTilt = data.TiltY;
			properties.Pressure = data.Pressure;
		}

		if (data.EventType == NativeMouseEvents.ScrollWheel)
		{
			var y = data.ScrollingDeltaY;
			if (y == 0)
			{
				// Note: if X and Y are != 0, we should raise 2 events!
				properties.IsHorizontalMouseWheel = true;
				properties.MouseWheelDelta = data.ScrollingDeltaX;
			}
			else
			{
				properties.MouseWheelDelta = -y;
			}
		}

		return properties;
	}


	public void ReleasePointerCapture() => LogNotSupported();
	public void ReleasePointerCapture(PointerIdentifier p) => LogNotSupported();
	public void SetPointerCapture() => LogNotSupported();
	public void SetPointerCapture(PointerIdentifier p) => LogNotSupported();

	private void LogNotSupported([CallerMemberName] string member = "")
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"{member} not supported on macOS.");
		}
	}
}
