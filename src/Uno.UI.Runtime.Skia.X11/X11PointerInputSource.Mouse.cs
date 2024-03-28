using Windows.Foundation;
using Windows.UI.Xaml.Controls;

namespace Uno.WinUI.Runtime.Skia.X11;

internal partial class X11PointerInputSource
{
	private const int LEFT = 1;
	private const int MIDDLE = 2;
	private const int RIGHT = 3;
	private const int SCROLL_DOWN = 4;
	private const int SCROLL_UP = 5;
	private const int SCROLL_LEFT = 6;
	private const int SCROLL_RIGHT = 7;

	private Point _mousePosition;
	private byte _pressedButtons; // // bit 0 is not used

	public void ProcessMotionNotifyEvent(XMotionEvent ev)
	{
		_mousePosition = new Point(ev.x, ev.y);

		var args = CreatePointerEventArgsFromCurrentState(ev.time, ev.state);
		X11XamlRootHost.QueueAction(_host, () => RaisePointerMoved(args));
	}

	public void ProcessButtonPressedEvent(XButtonEvent ev)
	{
		_mousePosition = new Point(ev.x, ev.y);
		_pressedButtons = (byte)(_pressedButtons | 1 << ev.button);

		var args = CreatePointerEventArgsFromCurrentState(ev.time, ev.state);

		if (ev.button is SCROLL_LEFT or SCROLL_RIGHT or SCROLL_UP or SCROLL_DOWN)
		{
			// These scrolling events are shown as a ButtonPressed with a corresponding ButtonReleased in succession.
			// We arbitrarily choose to handle this on the Pressed side and ignore the Released side.
			// Note that this makes scrolling discrete, i.e. there is no Scrolling delta. Instead, we get a separate
			// Pressed/Released pair for each scroll wheel "detent".

			var props = args.CurrentPoint.Properties;
			props.IsHorizontalMouseWheel = ev.button is SCROLL_LEFT or SCROLL_RIGHT;
			props.MouseWheelDelta = ev.button is SCROLL_LEFT or SCROLL_UP ?
				-ScrollContentPresenter.ScrollViewerDefaultMouseWheelDelta :
				ScrollContentPresenter.ScrollViewerDefaultMouseWheelDelta;

			X11XamlRootHost.QueueAction(_host, () => RaisePointerWheelChanged(args));
		}
		else
		{
			X11XamlRootHost.QueueAction(_host, () => RaisePointerPressed(args));
		}
	}

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

		var args = CreatePointerEventArgsFromCurrentState(ev.time, ev.state);
		X11XamlRootHost.QueueAction(_host, () => RaisePointerReleased(args));
	}
}
