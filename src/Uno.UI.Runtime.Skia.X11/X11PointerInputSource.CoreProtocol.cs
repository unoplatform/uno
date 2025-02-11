using System;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Input;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Hosting;

namespace Uno.WinUI.Runtime.Skia.X11;

internal partial class X11PointerInputSource
{
	private const int LEFT = 1;
	private const int MIDDLE = 2;
	private const int RIGHT = 3;
	private const int SCROLL_UP = 4;
	private const int SCROLL_DOWN = 5;
	private const int SCROLL_LEFT = 6;
	private const int SCROLL_RIGHT = 7;
	private const int XButton1 = 8;
	private const int XButton2 = 9;

	private Point _mousePosition;
	private int _pressedButtons; // // bit 0 is not used

	public void ProcessLeaveEvent(XCrossingEvent ev)
	{
		_mousePosition = new Point(ev.x, ev.y);

		var point = CreatePointFromCurrentState(ev.time);
		var modifiers = X11XamlRootHost.XModifierMaskToVirtualKeyModifiers(ev.state);

		var args = new PointerEventArgs(point, modifiers) { Handled = ev.window != _host.TopX11Window.Window };

		CreatePointFromCurrentState(ev.time);
		X11XamlRootHost.QueueAction(_host, () => RaisePointerExited(args));
	}

	public void ProcessEnterEvent(XCrossingEvent ev)
	{
		_mousePosition = new Point(ev.x, ev.y);

		var args = CreatePointerEventArgsFromCurrentState(ev.time, ev.state, ev.window);
		X11XamlRootHost.QueueAction(_host, () => RaisePointerEntered(args));
	}

	public void ProcessMotionNotifyEvent(XMotionEvent ev)
	{
		_mousePosition = new Point(ev.x, ev.y);

		var args = CreatePointerEventArgsFromCurrentState(ev.time, ev.state, ev.window);
		X11XamlRootHost.QueueAction(_host, () => RaisePointerMoved(args));
	}

	public void ProcessButtonPressedEvent(XButtonEvent ev)
	{
		_mousePosition = new Point(ev.x, ev.y);
		_pressedButtons = (byte)(_pressedButtons | 1 << ev.button);

		var args = CreatePointerEventArgsFromCurrentState(ev.time, ev.state, ev.window);

		if (ev.button is SCROLL_LEFT or SCROLL_RIGHT or SCROLL_UP or SCROLL_DOWN)
		{
			// These scrolling events are shown as a ButtonPressed with a corresponding ButtonReleased in succession.
			// We arbitrarily choose to handle this on the Pressed side and ignore the Released side.
			// Note that this makes scrolling discrete, i.e. there is no Scrolling delta. Instead, we get a separate
			// Pressed/Released pair for each scroll wheel "detent".

			var props = args.CurrentPoint.Properties;
			props.IsHorizontalMouseWheel = ev.button is SCROLL_LEFT or SCROLL_RIGHT;
			props.MouseWheelDelta = ev.button is SCROLL_LEFT or SCROLL_UP ?
				ScrollContentPresenter.ScrollViewerDefaultMouseWheelDelta :
				-ScrollContentPresenter.ScrollViewerDefaultMouseWheelDelta;

			X11XamlRootHost.QueueAction(_host, () => RaisePointerWheelChanged(args));
		}
		else
		{
			X11XamlRootHost.QueueAction(_host, () => RaisePointerPressed(args));
		}
	}

	// Note about removing devices: the server emits a ButtonRelease if a device is removed
	// while a button is held.
	public void ProcessButtonReleasedEvent(XButtonEvent ev)
	{
		// TODO: what if button released when not same_screen?
		if (ev.button is SCROLL_LEFT or SCROLL_RIGHT or SCROLL_UP or SCROLL_DOWN)
		{
			// Scroll events are already handled in ProcessButtonPressedEvent
			return;
		}

		_mousePosition = new Point(ev.x, ev.y);
		_pressedButtons = (byte)(_pressedButtons & ~(1 << ev.button));

		var args = CreatePointerEventArgsFromCurrentState(ev.time, ev.state, ev.window);
		X11XamlRootHost.QueueAction(_host, () => RaisePointerReleased(args));
	}

	private PointerEventArgs CreatePointerEventArgsFromCurrentState(IntPtr time, XModifierMask state, IntPtr eventWindow)
	{
		var point = CreatePointFromCurrentState(time);
		var modifiers = X11XamlRootHost.XModifierMaskToVirtualKeyModifiers(state);

		return new PointerEventArgs(point, modifiers) { Handled = eventWindow != _host.TopX11Window.Window };
	}

	/// <summary>
	/// Create a new PointerPoint from the current state of the PointerInputSource
	/// </summary>
	private PointerPoint CreatePointFromCurrentState(IntPtr time)
	{
		var properties = new PointerPointProperties
		{
			// TODO: fill this comprehensively
			IsLeftButtonPressed = (_pressedButtons & (1 << LEFT)) != 0,
			IsMiddleButtonPressed = (_pressedButtons & (1 << MIDDLE)) != 0,
			IsRightButtonPressed = (_pressedButtons & (1 << RIGHT)) != 0
		};

		var scale = ((IXamlRootHost)_host).RootElement?.XamlRoot is { } root
			? root.RasterizationScale
			: 1;

		var timeInMicroseconds = (ulong)(time * 1000); // Time is given in milliseconds since system boot. See also: https://github.com/unoplatform/uno/issues/14535
		var point = new PointerPoint(
			frameId: (uint)time, // UNO TODO: How should set the frame, timestamp may overflow.
			timestamp: timeInMicroseconds,
			PointerDevice.For(PointerDeviceType.Mouse),
			0, // TODO: XInput
			new Point(_mousePosition.X / scale, _mousePosition.Y / scale),
			new Point(_mousePosition.X / scale, _mousePosition.Y / scale),
			properties.HasPressedButton,
			properties.SetUpdateKindFromPrevious(_previousPointerPointProperties)
		);

		_previousPointerPointProperties = properties;

		return point;
	}
}
