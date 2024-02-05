using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;

namespace Uno.WinUI.Runtime.Skia.X11;

internal partial class X11XamlRootHost
{
	private static int ThreadCount = 0;

	private Thread? _eventsThread;
	private X11PointerInputSource? _pointerSource;
	private X11KeyboardInputSource? _keyboardSource;
	private X11DisplayInformationExtension? _displayInformationExtension;
	private Action<Size> _resizeCallback;
	private Action _closeCallback;
	private Action<bool> _focusCallback;
	private Action<bool> _visibilityCallback;

	private void InitializeX11EventsThread()
	{
		_eventsThread = new Thread(Run)
		{
			Name = $"Uno XEvents {Interlocked.Increment(ref ThreadCount) - 1}",
			IsBackground = true
		};

		_eventsThread.Start();
	}

	public void SetPointerSource(X11PointerInputSource pointerSource)
	{
		if (_pointerSource is not null)
		{
			throw new InvalidOperationException($"{nameof(X11PointerInputSource)} is set twice.");
		}
		_pointerSource = pointerSource;
	}

	public void SetKeyboardSource(X11KeyboardInputSource keyboardSource)
	{
		if (_keyboardSource is not null)
		{
			throw new InvalidOperationException($"{nameof(X11KeyboardInputSource)} is set twice.");
		}
		_keyboardSource = keyboardSource;
	}

	public void SetDisplayInformationExtension(X11DisplayInformationExtension extension)
	{
		if (_displayInformationExtension is not null)
		{
			throw new InvalidOperationException($"{nameof(X11DisplayInformationExtension)} is set twice.");
		}
		_displayInformationExtension = extension;
	}

	[DoesNotReturn]
	private void Run()
	{
		while (true)
		{
			// can probably be optimized with epoll but at the cost of thread preemption
			SpinWait.SpinUntil(() =>
			{
				using (X11Helper.XLock(X11Window.Display))
				{
					return X11Helper.XPending(X11Window.Display) > 0;
				}
			});

			using (X11Helper.XLock(X11Window.Display))
			{
				XLib.XNextEvent(X11Window.Display, out var event_);

				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"XLIB EVENT: {event_.type}");
				}

				switch (event_.type)
				{
					case XEventName.ClientMessage:
						// TODO: where does INativeWindowWrapper.Closing fit in all of this?
						IntPtr deleteWindow = X11Helper.GetAtom(X11Window.Display, X11Helper.WM_DELETE_WINDOW);
						if (event_.ClientMessageEvent.ptr1 == deleteWindow)
						{
							// TODO: how to detect if the window is force-killed? (e.g. xkill)
							_closeCallback();
						}
						break;
					case XEventName.ConfigureNotify:
						var configureEvent = event_.ConfigureEvent;
						_displayInformationExtension?.UpdateDetails();
						QueueEvent(this, () => _resizeCallback.Invoke(new Size(configureEvent.width, configureEvent.height)));
						break;
					case XEventName.FocusIn:
						QueueEvent(this, () => _focusCallback.Invoke(true));
						break;
					case XEventName.FocusOut:
						QueueEvent(this, () => _focusCallback.Invoke(false));
						break;
					case XEventName.VisibilityNotify:
						QueueEvent(this, () => _visibilityCallback.Invoke(event_.VisibilityEvent.state != /* VisibilityFullyObscured */ 2));
						break;
					case XEventName.Expose:
						QueueEvent(this, () => ((IXamlRootHost)this).InvalidateRender());
						break;
					case XEventName.MotionNotify:
						_pointerSource?.ProcessMotionNotifyEvent(event_.MotionEvent);
						break;
					case XEventName.ButtonPress:
						_pointerSource?.ProcessButtonPressedEvent(event_.ButtonEvent);
						break;
					case XEventName.ButtonRelease:
						_pointerSource?.ProcessButtonReleasedEvent(event_.ButtonEvent);
						break;
					case XEventName.LeaveNotify:
						_pointerSource?.ProcessLeaveEvent(event_.CrossingEvent);
						break;
					case XEventName.EnterNotify:
						_pointerSource?.ProcessEnterEvent(event_.CrossingEvent);
						break;
					case XEventName.KeyPress:
						_keyboardSource?.ProcessKeyboardEvent(event_.KeyEvent, true);
						break;
					case XEventName.KeyRelease:
						_keyboardSource?.ProcessKeyboardEvent(event_.KeyEvent, false);
						break;
					default:
						if (this.Log().IsEnabled(LogLevel.Error))
						{
							this.Log().Error($"XLIB ERROR: received an unexpected {event_.type} event");
						}
						break;
				}
			}
		}
		// ReSharper disable once FunctionNeverReturns
	}

	public static void QueueEvent(IXamlRootHost host, Action raisePointerEvent)
		=> host.RootElement?.Dispatcher.RunAsync(CoreDispatcherPriority.High, new DispatchedHandler(raisePointerEvent));

	public static VirtualKeyModifiers XModifierMaskToVirtualKeyModifiers(XModifierMask state)
	{
		var modifiers = VirtualKeyModifiers.None;
		if ((state & XModifierMask.ShiftMask) != 0)
		{
			modifiers |= VirtualKeyModifiers.Shift;
		}
		if ((state & XModifierMask.Mod1Mask) != 0)
		{
			// TODO: Modifier keys can be mapped to different keys. What to do?
			modifiers |= VirtualKeyModifiers.Shift;
		}
		if ((state & XModifierMask.ControlMask) != 0)
		{
			modifiers |= VirtualKeyModifiers.Control;
		}
		if ((state & XModifierMask.ControlMask) != 0)
		{
			modifiers |= VirtualKeyModifiers.Control;
		}
		if ((state & XModifierMask.Mod4Mask) != 0)
		{
			// TODO: Modifier keys can be mapped to different keys. What to do?
			modifiers |= VirtualKeyModifiers.Windows;
		}

		return modifiers;
	}
}
