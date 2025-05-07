using System;
using System.Runtime.CompilerServices;
using Windows.Devices.Input;
using Windows.Foundation;
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
	private PointerPointProperties? _previousPointerPointProperties;

	public X11PointerInputSource(IXamlRootHost host)
	{
		if (host is not X11XamlRootHost)
		{
			throw new ArgumentException($"{nameof(host)} must be an X11 host instance");
		}

		_host = (X11XamlRootHost)host;
		_host.SetPointerSource(this);

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

			using var lockDiposable = X11Helper.XLock(_host.TopX11Window.Display);

			var cursor = XLib.XCreateFontCursor(_host.TopX11Window.Display, shape);
			_ = XLib.XDefineCursor(_host.TopX11Window.Display, _host.TopX11Window.Window, cursor);
			_ = XLib.XFreeCursor(_host.TopX11Window.Display, cursor);
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
}
