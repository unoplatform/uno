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
using Windows.Win32.UI.Input.Touch;
using Windows.Win32.UI.WindowsAndMessaging;
using Uno.Disposables;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia.Win32;

internal partial class Win32WindowWrapper : IUnoCorePointerInputSource
{
	private const int MousePointerId = 1;

	private static uint _currentPointerFrameId;
	private PointerEventArgs? _previousPointerArgs;
	private CoreCursor? _pointerCursor;

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

	public unsafe CoreCursor? PointerCursor
	{
		get => _pointerCursor;
		set
		{
			_pointerCursor = value;
			var cursor = value?.Type switch
			{
				CoreCursorType.Arrow => PInvoke.IDC_ARROW,
				CoreCursorType.Cross => PInvoke.IDC_CROSS,
				CoreCursorType.Hand => PInvoke.IDC_HAND,
				CoreCursorType.Help => PInvoke.IDC_HELP,
				CoreCursorType.IBeam => PInvoke.IDC_IBEAM,
				CoreCursorType.SizeAll => PInvoke.IDC_SIZEALL,
				CoreCursorType.SizeNortheastSouthwest => PInvoke.IDC_SIZENESW,
				CoreCursorType.SizeNorthSouth => PInvoke.IDC_SIZENS,
				CoreCursorType.SizeNorthwestSoutheast => PInvoke.IDC_SIZENWSE,
				CoreCursorType.SizeWestEast => PInvoke.IDC_SIZEWE,
				CoreCursorType.UniversalNo => PInvoke.IDC_NO,
				CoreCursorType.UpArrow => PInvoke.IDC_UPARROW,
				CoreCursorType.Wait => PInvoke.IDC_WAIT,
				CoreCursorType.Pin => PInvoke.IDC_PIN,
				CoreCursorType.Person => PInvoke.IDC_PERSON,
				CoreCursorType.Custom => PInvoke.IDC_ARROW,
				null => PInvoke.IDC_ARROW,
				_ => throw new ArgumentOutOfRangeException()
			};
			var hCursor = PInvoke.LoadCursor(HINSTANCE.Null, new PCWSTR((char*)cursor));
			PInvoke.SetCursor(hCursor);
			using var cursorDisposable = new DisposableStruct<HCURSOR, Win32WindowWrapper>(static (hCursor, @this) =>
			{
				_ = PInvoke.DestroyCursor(hCursor) || @this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.DestroyCursor)} failed: {Win32Helper.GetErrorMessage()}");
			}, hCursor, this);
		}
	}

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
			var systemDrawingPoint = new System.Drawing.Point(x, y);
			if (PInvoke.ScreenToClient(_hwnd, ref systemDrawingPoint)
				|| this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.ScreenToClient)} failed: {Win32Helper.GetErrorMessage()}"))
			{
				x = (short)systemDrawingPoint.X;
				y = (short)systemDrawingPoint.Y;
			}
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
			timestamp: (ulong)(PInvoke.GetMessageTime() * 1000), // GetMessageTime is in ms
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

	private unsafe void OnTouch(WPARAM wParam, LPARAM lParam)
	{
		var numInputs = (uint)wParam;
		var ti = stackalloc TOUCHINPUT[(int)numInputs];
		var hTouchInput = new HTOUCHINPUT(lParam);
		using var touchDisposable = new DisposableStruct<HTOUCHINPUT, Win32WindowWrapper>(static (hTouchInput, @this) =>
		{
			_ = PInvoke.CloseTouchInputHandle(hTouchInput) || @this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.CloseTouchInputHandle)} failed: {Win32Helper.GetErrorMessage()}");
		}, hTouchInput, this);

		if (PInvoke.GetTouchInputInfo(hTouchInput, numInputs, ti, Marshal.SizeOf<TOUCHINPUT>())
		   || this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.GetTouchInputInfo)} failed: {Win32Helper.GetErrorMessage()}"))
		{
			for (var i = 0; i < numInputs; i++)
			{
				var touchInfo = ti[i];

				var systemDrawingPoint = new System.Drawing.Point(touchInfo.x, touchInfo.y);
				_ = PInvoke.ScreenToClient(_hwnd, ref systemDrawingPoint)
					|| this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.ScreenToClient)} failed: {Win32Helper.GetErrorMessage()}");
				var x = systemDrawingPoint.X;
				var y = systemDrawingPoint.Y;

				PointerPointProperties properties = new()
				{
					IsLeftButtonPressed = (touchInfo.dwFlags & (TOUCHEVENTF_FLAGS.TOUCHEVENTF_DOWN | TOUCHEVENTF_FLAGS.TOUCHEVENTF_MOVE)) != 0,
					IsPrimary = true,
					IsInRange = true,
				};

				if ((touchInfo.dwMask & TOUCHINPUTMASKF_MASK.TOUCHINPUTMASKF_CONTACTAREA) != 0)
				{
					properties.ContactRect = new Rect(x, y, touchInfo.cxContact * 1.0 / 100, touchInfo.cyContact * 1.0 / 100);
				}

				var point = new PointerPoint(
					frameId: Interlocked.Increment(ref _currentPointerFrameId),
					timestamp: touchInfo.dwTime * 1000, // touchInfo.dwTime is in ms
					device: PointerDevice.For((touchInfo.dwFlags & TOUCHEVENTF_FLAGS.TOUCHEVENTF_PEN) != 0 ? PointerDeviceType.Pen : PointerDeviceType.Touch),
					pointerId: MousePointerId * 10 + touchInfo.dwID,
					rawPosition: new Point(x, y),
					position: new Point(x, y),
					isInContact: properties.HasPressedButton,
					properties: properties
				);

				var ptArgs = new PointerEventArgs(point, Win32Helper.GetKeyModifiers());
				_previousPointerArgs = ptArgs;

				if ((touchInfo.dwFlags & TOUCHEVENTF_FLAGS.TOUCHEVENTF_DOWN) != 0)
				{
					PointerEntered?.Invoke(this, ptArgs);
					PointerPressed?.Invoke(this, ptArgs);
				}
				else if ((touchInfo.dwFlags & TOUCHEVENTF_FLAGS.TOUCHEVENTF_MOVE) != 0)
				{
					PointerMoved?.Invoke(this, ptArgs);
				}
				else if ((touchInfo.dwFlags & TOUCHEVENTF_FLAGS.TOUCHEVENTF_UP) != 0)
				{
					PointerReleased?.Invoke(this, ptArgs);
					PointerExited?.Invoke(this, ptArgs);
				}
			}
		}
	}
}
