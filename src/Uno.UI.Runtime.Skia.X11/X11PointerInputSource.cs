using System;
using System.Runtime.CompilerServices;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Windows.UI.Input;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;

namespace Uno.WinUI.Runtime.Skia.X11;

internal partial class X11PointerInputSource : IUnoCorePointerInputSource
{
#pragma warning disable CS0067 // Some event are not raised on X11 ... yet!
	public event TypedEventHandler<object, PointerEventArgs>? PointerCaptureLost;
	public event TypedEventHandler<object, PointerEventArgs>? PointerEntered;
	public event TypedEventHandler<object, PointerEventArgs>? PointerExited;
	public event TypedEventHandler<object, PointerEventArgs>? PointerMoved;
	public event TypedEventHandler<object, PointerEventArgs>? PointerPressed;
	public event TypedEventHandler<object, PointerEventArgs>? PointerReleased;
	public event TypedEventHandler<object, PointerEventArgs>? PointerWheelChanged;

	// X11 doesn't have the concept of a canceled Pointer
	public event TypedEventHandler<object, PointerEventArgs>? PointerCancelled; // Uno Only
#pragma warning restore CS0067

	private readonly X11XamlRootHost _host;
	private CoreCursor _pointerCursor;

	public X11PointerInputSource(IXamlRootHost host)
	{
		if (host is not X11XamlRootHost)
		{
			throw new ArgumentException($"{nameof(host)} must be an X11 host instance");
		}

		_host = (X11XamlRootHost)host;
		_host.SetPointerSource(this);

		DisplayInformation.GetForCurrentView();

		// Set this on startup in case a different global default was set beforehand
		PointerCursor = new(CoreCursorType.Arrow, 0);
		_pointerCursor = PointerCursor; // initialization is not needed, we're just keeping the compiler happy
	}

	[NotImplemented] public bool HasCapture => false;

	public CoreCursor PointerCursor
	{
		get => _pointerCursor;
		set
		{
			_pointerCursor = value;

			// These will have the look of the DE cursor themes if they exist (instead of the ugly x11 defaults)
			// using the XCURSOR extension. https://wiki.archlinux.org/title/Cursor_themes
			var shape = value.Type switch
			{
				CoreCursorType.Arrow => CursorFontShape.XC_arrow,
				CoreCursorType.Cross => CursorFontShape.XC_crosshair,
				CoreCursorType.Hand => CursorFontShape.XC_hand2, // subjective: XC_hand2 looks better than XC_hand1, XFCE renders both the same way
				CoreCursorType.Help => CursorFontShape.XC_question_arrow,
				CoreCursorType.IBeam => CursorFontShape.XC_xterm,
				CoreCursorType.SizeAll => CursorFontShape.XC_fleur,
				CoreCursorType.SizeNortheastSouthwest => CursorFontShape.XC_sizing, // this is the wrong direction, but it's all we have
				CoreCursorType.SizeNorthSouth => CursorFontShape.XC_sb_v_double_arrow, // this works better with XFCE than XC_double_arrow
				CoreCursorType.SizeNorthwestSoutheast => CursorFontShape.XC_sizing,
				CoreCursorType.SizeWestEast => CursorFontShape.XC_sb_h_double_arrow,
				CoreCursorType.UniversalNo => CursorFontShape.XC_circle, // maybe XC_pirate or XC_X_cursor? XFCE renders XC_circle as a red stop sign
				CoreCursorType.UpArrow => CursorFontShape.XC_sb_up_arrow,
				CoreCursorType.Wait => CursorFontShape.XC_watch, // this works better with XFCE than XC_exchange
				CoreCursorType.Person => CursorFontShape.XC_gumby, // sidenote: turns out this gumby is an ancient tv character
				CoreCursorType.Pin => CursorFontShape.XC_dot, // eh, not really
				_ => CursorFontShape.XC_arrow // including CoreCursorType.Custom
			};

			using var _1 = X11Helper.XLock(_host.X11Window.Display);

			var cursor = XLib.XCreateFontCursor(_host.X11Window.Display, shape);
			var _2 = XLib.XDefineCursor(_host.X11Window.Display, _host.X11Window.Window, cursor);
			var _3 = XLib.XFreeCursor(_host.X11Window.Display, cursor);
		}
	}

	public Point PointerPosition => _mousePosition;

	public void SetPointerCapture(PointerIdentifier pointer)
	{
		LogNotSupported();
		// XGrabPointer will globally lock pointer actions to the window, preventing any interaction elsewhere.
		// AFAICT, pointers are captured as long as a button is held.
		// var mask =
		// 	EventMask.ButtonPressMask |
		// 	EventMask.ButtonReleaseMask |
		// 	EventMask.PointerMotionMask |
		// 	EventMask.PointerMotionHintMask |
		// 	EventMask.Button1MotionMask |
		// 	EventMask.Button2MotionMask |
		// 	EventMask.Button3MotionMask |
		// 	EventMask.Button4MotionMask |
		// 	EventMask.Button5MotionMask |
		// 	EventMask.ButtonMotionMask;
		// XLib.XGrabPointer(_host.Display, _host.Window, false, mask,
		// 	GrabMode.GrabModeSync, GrabMode.GrabModeSync, /* None */ IntPtr.Zero, /* None */ IntPtr.Zero, /* CurrentTime */ IntPtr.Zero);
	}

	public void ReleasePointerCapture(PointerIdentifier pointer)
	{
		LogNotSupported();
		// XLib.XUngrabPointer(_host.Display, /* CurrentTime */ IntPtr.Zero);
	}

	public void ReleasePointerCapture() => LogNotSupported();
	public void SetPointerCapture() => LogNotSupported();

	private void RaisePointerMoved(PointerEventArgs args)
		=> PointerMoved?.Invoke(this, args);

	private void RaisePointerPressed(PointerEventArgs args)
		=> PointerPressed?.Invoke(this, args);

	private void RaisePointerReleased(PointerEventArgs args)
		=> PointerReleased?.Invoke(this, args);

	private void RaisePointerWheelChanged(PointerEventArgs args)
		=> PointerWheelChanged?.Invoke(this, args);

	private void RaisePointerExited(PointerEventArgs args)
		=> PointerExited?.Invoke(this, args);

	private void RaisePointerEntered(PointerEventArgs args)
		=> PointerEntered?.Invoke(this, args);

	private void LogNotSupported([CallerMemberName] string member = "")
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"{member} not supported on Skia for X11.");
		}
	}

	public void ProcessLeaveEvent(XCrossingEvent ev)
	{
		_mousePosition = new Point(ev.x, ev.y);

		var point = CreatePointFromCurrentState(ev.time);
		var modifiers = X11XamlRootHost.XModifierMaskToVirtualKeyModifiers(ev.state);

		var args = new PointerEventArgs(point, modifiers);

		CreatePointFromCurrentState(ev.time);
		X11XamlRootHost.QueueEvent(_host, () => RaisePointerExited(args));
	}

	public void ProcessEnterEvent(XCrossingEvent ev)
	{
		_mousePosition = new Point(ev.x, ev.y);

		var args = CreatePointerEventArgsFromCurrentState(ev.time, ev.state);
		X11XamlRootHost.QueueEvent(_host, () => RaisePointerEntered(args));
	}

	private PointerEventArgs CreatePointerEventArgsFromCurrentState(IntPtr time, XModifierMask state)
	{
		var point = CreatePointFromCurrentState(time);
		var modifiers = X11XamlRootHost.XModifierMaskToVirtualKeyModifiers(state);

		return new PointerEventArgs(point, modifiers);
	}

	/// <summary>
	/// Create a new PointerPoint from the current state of the PointerInputSource
	/// </summary>
	private PointerPoint CreatePointFromCurrentState(IntPtr time)
	{
		var properties = new PointerPointProperties
		{
			// TODO: fill this comprehensively like GTK's AsPointerArgs
			IsLeftButtonPressed = (_pressedButtons & (1 << LEFT)) != 0,
			IsMiddleButtonPressed = (_pressedButtons & (1 << MIDDLE)) != 0,
			IsRightButtonPressed = (_pressedButtons & (1 << RIGHT)) != 0
		};

		// Time is given in milliseconds since system boot
		// This matches the format of WinUI. See also: https://github.com/unoplatform/uno/issues/14535
		var point = new PointerPoint(
			frameId: (uint)time, // UNO TODO: How should set the frame, timestamp may overflow.
			timestamp: (uint)time,
			PointerDevice.For(PointerDeviceType.Mouse),
			0, // TODO: XInput
			_mousePosition,
			_mousePosition,
			// TODO: is isInContact correct?
			(_pressedButtons & 0b1111) != 0,
			properties
		);

		return point;
	}
}
