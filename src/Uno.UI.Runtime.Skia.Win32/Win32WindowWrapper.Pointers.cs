using System;
using System.Runtime.InteropServices;
using System.Threading;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.SystemServices;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia.Win32;

internal partial class Win32WindowWrapper : IUnoCorePointerInputSource
{
	private const int MousePointerId = 1;

	private static uint _currentPointerFrameId;
	private PointerEventArgs? _previousPointerArgs;

#pragma warning disable CS0067 // Some event are not raised on Win32 ... yet!
	public event TypedEventHandler<object, PointerEventArgs>? PointerCaptureLost;
	public event TypedEventHandler<object, PointerEventArgs>? PointerEntered;
	public event TypedEventHandler<object, PointerEventArgs>? PointerExited;
	public event TypedEventHandler<object, PointerEventArgs>? PointerMoved;
	public event TypedEventHandler<object, PointerEventArgs>? PointerPressed;
	public event TypedEventHandler<object, PointerEventArgs>? PointerReleased;
	public event TypedEventHandler<object, PointerEventArgs>? PointerWheelChanged;
	public event TypedEventHandler<object, PointerEventArgs>? PointerCancelled; // Uno Only
#pragma warning restore CS0067

	public bool HasCapture => PInvoke.GetCapture() == _hwnd;

	public CoreCursor? PointerCursor { get; set; }

	[NotImplemented] public Point PointerPosition => default;

	public void ReleasePointerCapture() => _ = PInvoke.ReleaseCapture() || this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.ReleaseCapture)} failed: {Win32Helper.GetErrorMessage()}");

	public void SetPointerCapture() => PInvoke.SetCapture(_hwnd);

	public void SetPointerCapture(PointerIdentifier pointer) => SetPointerCapture();

	public void ReleasePointerCapture(PointerIdentifier pointer) => ReleasePointerCapture();

	private void OnPointer(uint msg, WPARAM wParam, LPARAM lParam)
	{
		var (evt, msgName) = msg switch
		{
			PInvoke.WM_MOUSEWHEEL => (PointerWheelChanged, nameof(PInvoke.WM_MOUSEWHEEL)),
			PInvoke.WM_MOUSEHWHEEL => (PointerWheelChanged, nameof(PInvoke.WM_MOUSEHWHEEL)),
			PInvoke.WM_LBUTTONDOWN => (PointerPressed, nameof(PInvoke.WM_LBUTTONDOWN)),
			PInvoke.WM_RBUTTONDOWN => (PointerPressed, nameof(PInvoke.WM_RBUTTONDOWN)),
			PInvoke.WM_MBUTTONDOWN => (PointerPressed, nameof(PInvoke.WM_MBUTTONDOWN)),
			PInvoke.WM_XBUTTONDOWN => (PointerPressed, nameof(PInvoke.WM_XBUTTONDOWN)),
			PInvoke.WM_LBUTTONUP => (PointerReleased, nameof(PInvoke.WM_LBUTTONUP)),
			PInvoke.WM_RBUTTONUP => (PointerReleased, nameof(PInvoke.WM_RBUTTONUP)),
			PInvoke.WM_MBUTTONUP => (PointerReleased, nameof(PInvoke.WM_MBUTTONUP)),
			PInvoke.WM_XBUTTONUP => (PointerReleased, nameof(PInvoke.WM_XBUTTONUP)),
			PInvoke.WM_MOUSEMOVE => (PointerMoved, nameof(PInvoke.WM_MOUSEMOVE)),
			PInvoke.WM_MOUSELEAVE => (PointerExited, nameof(PInvoke.WM_MOUSELEAVE)),
			_ => throw new ArgumentOutOfRangeException(nameof(msg), msg, null)
		};

		this.Log().Log(LogLevel.Trace, msgName, static msgName => $"WndProc received a {msgName} message.");

		var x = unchecked((short)(lParam & 0xffff));
		var y = unchecked((short)((lParam >> 16) & 0xffff));
		var delta = unchecked((short)(wParam >> 16));

		if (delta is not 0)
		{
			// Only WM_MOUSEWHEEL gives the position in screen coordinates, not client-area coordinates
			x -= (short)Position.X;
			y -= (short)Position.Y;
		}

		PointerPointProperties properties = new()
		{
			IsLeftButtonPressed = (wParam & (ulong)MODIFIERKEYS_FLAGS.MK_LBUTTON) != 0,
			IsMiddleButtonPressed = (wParam & (ulong)MODIFIERKEYS_FLAGS.MK_MBUTTON) != 0,
			IsRightButtonPressed = (wParam & (ulong)MODIFIERKEYS_FLAGS.MK_RBUTTON) != 0,
			IsXButton1Pressed = (wParam & (ulong)MODIFIERKEYS_FLAGS.MK_XBUTTON1) != 0,
			IsXButton2Pressed = (wParam & (ulong)MODIFIERKEYS_FLAGS.MK_XBUTTON2) != 0,
			IsPrimary = true,
			IsInRange = true,
			MouseWheelDelta = delta,
			IsHorizontalMouseWheel = msg is PInvoke.WM_MOUSEHWHEEL
		};

		properties = properties.SetUpdateKindFromPrevious(_previousPointerArgs?.CurrentPoint.Properties);

		var point = new PointerPoint(
			frameId: Interlocked.Increment(ref _currentPointerFrameId),
			timestamp: (ulong)(DateTime.Now.Ticks / TimeSpan.TicksPerMicrosecond),
			device: PointerDevice.For(PointerDeviceType.Mouse),
			pointerId: MousePointerId,
			rawPosition: new Point(x, y),
			position: new Point(x, y),
			isInContact: properties.HasPressedButton,
			properties: properties
		);

		var ptArgs = new PointerEventArgs(point, Win32Helper.GetKeyModifiers());
		_previousPointerArgs = ptArgs;

		evt?.Invoke(this, ptArgs);
	}

	private void TrackLeave()
	{
		TRACKMOUSEEVENT tme = new()
		{
			cbSize = (uint)Marshal.SizeOf<TRACKMOUSEEVENT>(),
			dwFlags = TRACKMOUSEEVENT_FLAGS.TME_LEAVE,
			hwndTrack = _hwnd
		};
		_ = PInvoke.TrackMouseEvent(ref tme) || this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.TrackMouseEvent)} failed: {Win32Helper.GetErrorMessage()}");
	}
}
