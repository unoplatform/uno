using System;
using System.Threading;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.Pointer;
using Windows.Win32.UI.WindowsAndMessaging;
using Uno.Disposables;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia.Win32;

internal partial class Win32WindowWrapper : IUnoCorePointerInputSource
{
	private static uint _currentPointerFrameId;
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

	public CoreCursor? PointerCursor
	{
		get => _pointerCursor;
		set
		{
			_pointerCursor = value;
			SetCursor(value);
		}
	}

	private unsafe void SetCursor(CoreCursor? coreCursor)
	{
		var cursor = coreCursor?.Type switch
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
		using var cursorDisposable = new DisposableStruct<HCURSOR, Win32WindowWrapper>(static (hCursor, @this) =>
		{
			var success = PInvoke.DestroyCursor(hCursor);
			if (!success) { @this.LogError()?.Error($"{nameof(PInvoke.DestroyCursor)} failed: {Win32Helper.GetErrorMessage()}"); }
		}, hCursor, this);
		PInvoke.SetCursor(hCursor);
	}

	[NotImplemented] public Point PointerPosition => default;

	public void ReleasePointerCapture()
	{
		var success = PInvoke.ReleaseCapture();
		if (!success) { this.LogError()?.Error($"{nameof(PInvoke.ReleaseCapture)} failed: {Win32Helper.GetErrorMessage()}"); }
	}

	public void SetPointerCapture() => PInvoke.SetCapture(_hwnd);

	public void SetPointerCapture(PointerIdentifier pointer) => SetPointerCapture();

	public void ReleasePointerCapture(PointerIdentifier pointer) => ReleasePointerCapture();

	private ushort ReadCommonWParamInfo(WPARAM wParam, out POINTER_INFO pointerInfo, out POINTER_INPUT_TYPE pointerType, out System.Drawing.Point position, out System.Drawing.Point rawPosition)
	{
		var pointerId = Win32Helper.GET_POINTERID_WPARAM(wParam);

		if (!PInvoke.GetPointerType(pointerId, out pointerType))
		{
			throw new InvalidOperationException($"{nameof(PInvoke.GetPointerType)} failed: {Win32Helper.GetErrorMessage()}");
		}

		if (!PInvoke.GetPointerInfo(pointerId, out pointerInfo))
		{
			throw new InvalidOperationException($"{nameof(PInvoke.GetPointerInfo)} failed: {Win32Helper.GetErrorMessage()}");
		}

		position = pointerInfo.ptPixelLocation;
		rawPosition = pointerInfo.ptPixelLocationRaw;
		var success = PInvoke.ScreenToClient(_hwnd, ref position);
		if (!success) { this.LogError()?.Error($"{nameof(PInvoke.ScreenToClient)} failed: {Win32Helper.GetErrorMessage()}"); }
		var success2 = PInvoke.ScreenToClient(_hwnd, ref rawPosition);
		if (!success2) { this.LogError()?.Error($"{nameof(PInvoke.ScreenToClient)} failed: {Win32Helper.GetErrorMessage()}"); }

		var scale = XamlRoot!.RasterizationScale;
		position = new System.Drawing.Point((int)(position.X / scale), (int)(position.Y / scale));
		rawPosition = new System.Drawing.Point((int)(rawPosition.X / scale), (int)(rawPosition.Y / scale));
		return pointerId;
	}

	private void OnPointerCaptureChanged(WPARAM wParam)
	{
		var pointerId = ReadCommonWParamInfo(wParam, out _, out var pointerType, out var position, out var rawPosition);

		var point = new PointerPoint(
			frameId: Interlocked.Increment(ref _currentPointerFrameId),
			timestamp: (ulong)(PInvoke.GetMessageTime() * 1000), // GetMessageTime is in ms
			device: PointerDevice.For(pointerType switch
			{
				POINTER_INPUT_TYPE.PT_PEN => PointerDeviceType.Pen,
				POINTER_INPUT_TYPE.PT_TOUCH => PointerDeviceType.Touch,
				_ => PointerDeviceType.Mouse
			}),
			pointerId: pointerId,
			rawPosition: new Point(rawPosition.X, rawPosition.Y),
			position: new Point(position.X, position.Y),
			isInContact: false,
			properties: null);
		PointerCaptureLost?.Invoke(this, new PointerEventArgs(point, Win32Helper.GetKeyModifiers()));
	}

	internal unsafe void OnPointer(uint msg, WPARAM wParam, HWND hwnd)
	{
		var pointerId = ReadCommonWParamInfo(wParam, out var pointerInfo, out var pointerType, out var position, out var rawPosition);

		var modifiers = Win32Helper.GetKeyModifiers();

		PointerPointProperties properties;
		if (msg is PInvoke.WM_POINTERWHEEL or PInvoke.WM_POINTERHWHEEL)
		{
			properties = new()
			{
				MouseWheelDelta = Win32Helper.GET_WHEEL_DELTA_WPARAM(wParam),
				IsHorizontalMouseWheel = msg is PInvoke.WM_POINTERHWHEEL
			};
		}
		else
		{
			properties = new()
			{
				IsPrimary = Win32Helper.IS_POINTER_PRIMARY_WPARAM(wParam),
				IsInRange = Win32Helper.IS_POINTER_INRANGE_WPARAM(wParam),
				IsCanceled = Win32Helper.IS_POINTER_CANCELED_WPARAM(wParam),
				IsLeftButtonPressed = Win32Helper.IS_POINTER_FIRSTBUTTON_WPARAM(wParam),
				PointerUpdateKind = pointerInfo.ButtonChangeType switch
				{
					POINTER_BUTTON_CHANGE_TYPE.POINTER_CHANGE_NONE => PointerUpdateKind.Other,
					POINTER_BUTTON_CHANGE_TYPE.POINTER_CHANGE_FIRSTBUTTON_DOWN => PointerUpdateKind.LeftButtonPressed,
					POINTER_BUTTON_CHANGE_TYPE.POINTER_CHANGE_FIRSTBUTTON_UP => PointerUpdateKind.LeftButtonReleased,
					POINTER_BUTTON_CHANGE_TYPE.POINTER_CHANGE_SECONDBUTTON_DOWN => PointerUpdateKind.RightButtonPressed,
					POINTER_BUTTON_CHANGE_TYPE.POINTER_CHANGE_SECONDBUTTON_UP => PointerUpdateKind.RightButtonReleased,
					POINTER_BUTTON_CHANGE_TYPE.POINTER_CHANGE_THIRDBUTTON_DOWN => PointerUpdateKind.MiddleButtonPressed,
					POINTER_BUTTON_CHANGE_TYPE.POINTER_CHANGE_THIRDBUTTON_UP => PointerUpdateKind.MiddleButtonReleased,
					POINTER_BUTTON_CHANGE_TYPE.POINTER_CHANGE_FOURTHBUTTON_DOWN => PointerUpdateKind.XButton1Pressed,
					POINTER_BUTTON_CHANGE_TYPE.POINTER_CHANGE_FOURTHBUTTON_UP => PointerUpdateKind.XButton1Released,
					POINTER_BUTTON_CHANGE_TYPE.POINTER_CHANGE_FIFTHBUTTON_DOWN => PointerUpdateKind.XButton2Pressed,
					POINTER_BUTTON_CHANGE_TYPE.POINTER_CHANGE_FIFTHBUTTON_UP => PointerUpdateKind.XButton2Released,
					_ => throw new ArgumentOutOfRangeException()
				}
			};

			switch (pointerType)
			{
				case POINTER_INPUT_TYPE.PT_TOUCH:
					properties.TouchConfidence = Win32Helper.HAS_POINTER_CONFIDENCE_WPARAM(wParam);
					if (PInvoke.GetPointerTouchInfo(pointerId, out var pointerTouchInfo))
					{
						properties.ContactRect = (pointerTouchInfo.touchMask & PInvoke.TOUCH_MASK_CONTACTAREA) == 0 ? new Rect() : pointerTouchInfo.rcContact.ToRect();
						properties.Orientation = (pointerTouchInfo.touchMask & PInvoke.TOUCH_MASK_CONTACTAREA) == 0 ? 0 : pointerTouchInfo.orientation;
						properties.Pressure = (pointerTouchInfo.touchMask & PInvoke.TOUCH_MASK_PRESSURE) == 0 ? 0 : pointerTouchInfo.pressure;
					}
					else
					{
						this.LogError()?.Error($"{nameof(PInvoke.GetPointerTouchInfo)} failed: {Win32Helper.GetErrorMessage()}");
					}
					break;
				case POINTER_INPUT_TYPE.PT_PEN:
					properties.IsBarrelButtonPressed = Win32Helper.IS_POINTER_SECONDBUTTON_WPARAM(wParam);
					if (PInvoke.GetPointerPenInfo(pointerId, out var pointerPenInfo))
					{
						properties.XTilt = (pointerPenInfo.penMask & PInvoke.PEN_MASK_TILT_X) == 0 ? 0 : pointerPenInfo.tiltX;
						properties.YTilt = (pointerPenInfo.penMask & PInvoke.PEN_MASK_TILT_Y) == 0 ? 0 : pointerPenInfo.tiltY;
						properties.Pressure = (pointerPenInfo.penMask & PInvoke.PEN_MASK_PRESSURE) == 0 ? 0 : pointerPenInfo.pressure;
					}
					else
					{
						this.LogError()?.Error($"{nameof(PInvoke.GetPointerPenInfo)} failed: {Win32Helper.GetErrorMessage()}");
					}
					break;
				case POINTER_INPUT_TYPE.PT_MOUSE:
				case POINTER_INPUT_TYPE.PT_TOUCHPAD:
					properties.IsTouchPad = pointerType is POINTER_INPUT_TYPE.PT_TOUCHPAD;
					properties.IsRightButtonPressed = Win32Helper.IS_POINTER_SECONDBUTTON_WPARAM(wParam);
					properties.IsMiddleButtonPressed = Win32Helper.IS_POINTER_THIRDBUTTON_WPARAM(wParam);
					properties.IsXButton1Pressed = Win32Helper.IS_POINTER_FOURTHBUTTON_WPARAM(wParam);
					properties.IsXButton2Pressed = Win32Helper.IS_POINTER_FIFTHBUTTON_WPARAM(wParam);
					// TouchPad horizontal scrolling uses WM_POINTERHWHEEL and does not set POINTER_FLAG_HWHEEL
					// POINTER_FLAG_HWHEEL is set when mouse-scrolling with Shift held. We choose to handle this as
					// a vertical scroll + shift instead to keep behavior consistent between platforms, specially when
					// interacting with ScrollViewers
					properties.IsHorizontalMouseWheel = (modifiers & VirtualKeyModifiers.Shift) == 0 && (wParam & (ulong)POINTER_FLAGS.POINTER_FLAG_HWHEEL) != 0;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(pointerType));
			}
		}

		var point = new PointerPoint(
			frameId: Interlocked.Increment(ref _currentPointerFrameId),
			timestamp: (ulong)(PInvoke.GetMessageTime() * 1000), // GetMessageTime is in ms
			device: PointerDevice.For(pointerType switch
			{
				POINTER_INPUT_TYPE.PT_PEN => PointerDeviceType.Pen,
				POINTER_INPUT_TYPE.PT_TOUCH => PointerDeviceType.Touch,
				_ => PointerDeviceType.Mouse
			}),
			pointerId: pointerId,
			rawPosition: new Point(rawPosition.X, rawPosition.Y),
			position: new Point(position.X, position.Y),
			isInContact: msg is not (PInvoke.WM_POINTERWHEEL or PInvoke.WM_POINTERHWHEEL) && Win32Helper.IS_POINTER_INCONTACT_WPARAM(wParam),
			properties: properties
		);

		var (evt, msgName) = msg switch
		{
			PInvoke.WM_NCPOINTERDOWN => (PointerPressed, nameof(PInvoke.WM_NCPOINTERDOWN)),
			PInvoke.WM_NCPOINTERUP => (PointerReleased, nameof(PInvoke.WM_NCPOINTERUP)),
			PInvoke.WM_NCPOINTERUPDATE => (PointerMoved, nameof(PInvoke.WM_NCPOINTERUPDATE)),
			PInvoke.WM_NCLBUTTONDOWN => (PointerPressed, nameof(PInvoke.WM_NCLBUTTONDOWN)),
			PInvoke.WM_NCLBUTTONUP => (PointerReleased, nameof(PInvoke.WM_NCLBUTTONUP)),
			PInvoke.WM_NCMOUSEMOVE => (PointerMoved, nameof(PInvoke.WM_NCMOUSEMOVE)),
			PInvoke.WM_POINTERDOWN => (PointerPressed, nameof(PInvoke.WM_POINTERDOWN)),
			PInvoke.WM_POINTERUP => (PointerReleased, nameof(PInvoke.WM_POINTERUP)),
			PInvoke.WM_POINTERWHEEL => (PointerWheelChanged, nameof(PInvoke.WM_POINTERWHEEL)),
			PInvoke.WM_POINTERHWHEEL => (PointerWheelChanged, nameof(PInvoke.WM_POINTERHWHEEL)),
			PInvoke.WM_POINTERENTER => (PointerEntered, nameof(PInvoke.WM_POINTERENTER)),
			PInvoke.WM_POINTERLEAVE => (PointerExited, nameof(PInvoke.WM_POINTERLEAVE)),
			PInvoke.WM_POINTERUPDATE => (PointerMoved, nameof(PInvoke.WM_POINTERUPDATE)),
			_ => throw new ArgumentOutOfRangeException(nameof(msg), msg, null)
		};

		this.LogTrace()?.Trace($"WndProc received a {msgName} message.");
		evt?.Invoke(this, new PointerEventArgs(point, modifiers));
	}
}
