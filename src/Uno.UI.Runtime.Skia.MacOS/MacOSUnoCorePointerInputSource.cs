#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Windows.Devices.Input;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;

using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSUnoCorePointerInputSource : IUnoCorePointerInputSource
{
	public static MacOSUnoCorePointerInputSource Instance = new();

	private CoreCursor? _pointerCursor = new(CoreCursorType.Arrow, 0);

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

	public Point PointerPosition { get; private set; }

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
	internal static int MouseEvent(int type, double x, double y, VirtualKeyModifiers mods, PointerDeviceType pdt, uint frameId, ulong timestamp, uint pid)
	{
		var position = new Point(x, y);
		try
		{
			TypedEventHandler<object, PointerEventArgs>? evnt = null;
			switch (type)
			{
				case 1:
					evnt = Instance.PointerEntered;
					break;
				case 2:
					evnt = Instance.PointerExited;
					break;
				case 3:
					evnt = Instance.PointerPressed;
					break;
				case 4:
					evnt = Instance.PointerReleased;
					break;
				case 5:
					evnt = Instance.PointerMoved;
					break;
				case 6:
					evnt = Instance.PointerWheelChanged;
					break;
			}
			if (evnt is null)
			{
				return 0; // unhandled
			}
			// TODO: collect/marshal more data from native
			var ppp = new Windows.UI.Input.PointerPointProperties();
			var pp = new Windows.UI.Input.PointerPoint(frameId, timestamp, PointerDevice.For(pdt), pid, position, position, false, ppp);
			evnt(Instance, new PointerEventArgs(pp, mods));
			return 1; // handled
		}
		catch (Exception e)
		{
			Microsoft.UI.Xaml.Application.Current.RaiseRecoverableUnhandledException(e);
			return 0;
		}
		finally
		{
			Instance.PointerPosition = position;
		}
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
