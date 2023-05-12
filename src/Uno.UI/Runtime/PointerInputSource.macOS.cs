using System;
using System.Linq;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Input;
using AppKit;
using Foundation;
using Uno.Foundation.Logging;
using System.Runtime.CompilerServices;

namespace Uno.UI.Runtime.MacOS;

internal partial class MacOSPointerInputSource : IUnoCorePointerInputSource
{
	private const int TabletPointEventSubtype = 1;
	private const int TabletProximityEventSubtype = 2;

	private const int LeftMouseButtonMask = 1;
	private const int RightMouseButtonMask = 2;


	private bool _cursorHidden;
	private CoreCursor _pointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
	private PointerEventArgs _previous;

#pragma warning disable CS0067 // Some event are not raised on MacOS ... yet!
	public event TypedEventHandler<object, PointerEventArgs> PointerCaptureLost;
	public event TypedEventHandler<object, PointerEventArgs> PointerEntered;
	public event TypedEventHandler<object, PointerEventArgs> PointerExited;
	public event TypedEventHandler<object, PointerEventArgs> PointerMoved;
	public event TypedEventHandler<object, PointerEventArgs> PointerPressed;
	public event TypedEventHandler<object, PointerEventArgs> PointerReleased;
	public event TypedEventHandler<object, PointerEventArgs> PointerWheelChanged;
	public event TypedEventHandler<object, PointerEventArgs> PointerCancelled; // Uno only
#pragma warning restore CS0067

	public MacOSPointerInputSource(Uno.UI.Controls.Window window)
	{
		window.OnSendEvent += OnWindowEvent;
	}

	/// <inheritdoc />
	public CoreCursor PointerCursor
	{
		get => _pointerCursor;
		set
		{
			_pointerCursor = value;
			RefreshCursor();
		}
	}

	public Point PointerPosition => _previous.CurrentPoint.Position;

	[NotImplemented] public bool HasCapture => false;
	[NotImplemented] public void ReleasePointerCapture() => LogNotSupported();
	[NotImplemented] public void ReleasePointerCapture(PointerIdentifier pointer) => LogNotSupported();
	[NotImplemented] public void SetPointerCapture() => LogNotSupported();
	[NotImplemented] public void SetPointerCapture(PointerIdentifier pointer) => LogNotSupported();

	private void OnWindowEvent(Uno.UI.Controls.Window window, Uno.UI.Controls.Window.SendEventArgs args)
	{
		var evt = args.Event;

		// The effective location in top/left coordinates.
		var posInWindow = new Point(evt.LocationInWindow.X, window.VisibleFrame.Height - evt.LocationInWindow.Y);
		if (posInWindow.Y < 0)
		{
			// We are in the titlebar, let send the event to native code ... so close button will continue to work
			return;
		}

		switch (evt.Type)
		{
			case NSEventType.MouseEntered:
				PointerEntered?.Invoke(this, BuildPointerArgs(evt, posInWindow));
				break;

			case NSEventType.MouseExited:
				PointerExited?.Invoke(this, BuildPointerArgs(evt, posInWindow));
				break;

			case NSEventType.LeftMouseDown:
			case NSEventType.OtherMouseDown:
			case NSEventType.RightMouseDown:
				PointerPressed?.Invoke(this, BuildPointerArgs(evt, posInWindow));
				break;

			case NSEventType.LeftMouseUp:
			case NSEventType.OtherMouseUp:
			case NSEventType.RightMouseUp:
				PointerReleased?.Invoke(this, BuildPointerArgs(evt, posInWindow));
				break;

			case NSEventType.MouseMoved:
			case NSEventType.LeftMouseDragged:
			case NSEventType.OtherMouseDragged:
			case NSEventType.RightMouseDragged:
			case NSEventType.TabletPoint:
			case NSEventType.TabletProximity:
			case NSEventType.DirectTouch:
				PointerMoved?.Invoke(this, BuildPointerArgs(evt, posInWindow));
				break;

			case NSEventType.ScrollWheel:
				PointerWheelChanged?.Invoke(this, BuildPointerArgs(evt, posInWindow));
				break;
		}
	}

	private PointerEventArgs BuildPointerArgs(NSEvent nativeEvent, Point posInWindow)
	{
		var frameId = ToFrameId(nativeEvent.Timestamp);
		var timestamp = ToTimestamp(nativeEvent.Timestamp);
		var pointerDeviceType = GetPointerDeviceType(nativeEvent);
		var pointerDevice = PointerDevice.For(pointerDeviceType);
		var pointerId = pointerDeviceType == PointerDeviceType.Pen
			? (uint)nativeEvent.PointingDeviceID()
			: (uint)1;
		var isInContact = GetIsInContact(nativeEvent);
		var properties = GetPointerProperties(nativeEvent, pointerDeviceType).SetUpdateKindFromPrevious(_previous?.CurrentPoint.Properties);
		var modifiers = GetVirtualKeyModifiers(nativeEvent);

		var point = new PointerPoint(frameId, timestamp, pointerDevice, pointerId, posInWindow, posInWindow, isInContact, properties);
		var args = new PointerEventArgs(point, modifiers);

		_previous = args;

		return args;
	}

	private static PointerPointProperties GetPointerProperties(NSEvent nativeEvent, PointerDeviceType pointerType)
	{
		var properties = new PointerPointProperties()
		{
			IsInRange = true,
			IsPrimary = true,
			IsLeftButtonPressed = ((int)NSEvent.CurrentPressedMouseButtons & LeftMouseButtonMask) == LeftMouseButtonMask,
			IsRightButtonPressed = ((int)NSEvent.CurrentPressedMouseButtons & RightMouseButtonMask) == RightMouseButtonMask,
		};

		if (pointerType == PointerDeviceType.Pen)
		{
			properties.XTilt = (float)nativeEvent.Tilt.X;
			properties.YTilt = (float)nativeEvent.Tilt.Y;
			properties.Pressure = (float)nativeEvent.Pressure;
		}

		if (nativeEvent.Type == NSEventType.ScrollWheel)
		{
			var y = (int)nativeEvent.ScrollingDeltaY;
			if (y == 0)
			{
				// Note: if X and Y are != 0, we should raise 2 events!
				properties.IsHorizontalMouseWheel = true;
				properties.MouseWheelDelta = (int)nativeEvent.ScrollingDeltaX;
			}
			else
			{
				properties.MouseWheelDelta = -y;
			}
		}

		return properties;
	}

	#region Misc static helpers
	private static long? _bootTime;

	private static bool GetIsInContact(NSEvent nativeEvent)
		=> nativeEvent.Type == NSEventType.LeftMouseDown
			|| nativeEvent.Type == NSEventType.LeftMouseDragged
			|| nativeEvent.Type == NSEventType.RightMouseDown
			|| nativeEvent.Type == NSEventType.RightMouseDragged
			|| nativeEvent.Type == NSEventType.OtherMouseDown
			|| nativeEvent.Type == NSEventType.OtherMouseDragged;

	private static PointerDeviceType GetPointerDeviceType(NSEvent nativeEvent)
	{
		if (nativeEvent.Type == NSEventType.DirectTouch)
		{
			return PointerDeviceType.Touch;
		}
		if (IsTabletPointingEvent(nativeEvent))
		{
			return PointerDeviceType.Pen;
		}
		return PointerDeviceType.Mouse;
	}

	private static VirtualKeyModifiers GetVirtualKeyModifiers(NSEvent nativeEvent)
	{
		var modifiers = VirtualKeyModifiers.None;

		if (nativeEvent.ModifierFlags.HasFlag(NSEventModifierMask.AlphaShiftKeyMask) ||
			nativeEvent.ModifierFlags.HasFlag(NSEventModifierMask.ShiftKeyMask))
		{
			modifiers |= VirtualKeyModifiers.Shift;
		}

		if (nativeEvent.ModifierFlags.HasFlag(NSEventModifierMask.AlternateKeyMask))
		{
			modifiers |= VirtualKeyModifiers.Menu;
		}

		if (nativeEvent.ModifierFlags.HasFlag(NSEventModifierMask.CommandKeyMask))
		{
			modifiers |= VirtualKeyModifiers.Windows;
		}

		if (nativeEvent.ModifierFlags.HasFlag(NSEventModifierMask.ControlKeyMask))
		{
			modifiers |= VirtualKeyModifiers.Control;
		}

		return modifiers;
	}

	private static ulong ToTimestamp(double timestamp)
	{
		if (!_bootTime.HasValue)
		{
			_bootTime = DateTime.UtcNow.Ticks - (long)(TimeSpan.TicksPerSecond * new NSProcessInfo().SystemUptime);
		}

		return (ulong)_bootTime.Value + (ulong)(TimeSpan.TicksPerSecond * timestamp);
	}

	private static uint ToFrameId(double timestamp)
	{
		// The precision of the frameId is 10 frame per ms ... which should be enough
		return (uint)(timestamp * 1000.0 * 10.0);
	}

	/// <summary>
	/// Taken from <see href="https://github.com/xamarin/xamarin-macios/blob/bc492585d137d8c3d3a2ffc827db3cdaae3cc869/src/AppKit/NSEvent.cs#L127" />
	/// </summary>
	/// <param name="nativeEvent">Native event</param>
	/// <returns>Value indicating whether the event is recognized as a "mouse" event.</returns>
	private static bool IsMouseEvent(NSEvent nativeEvent)
	{
		switch (nativeEvent.Type)
		{
			case NSEventType.LeftMouseDown:
			case NSEventType.LeftMouseUp:
			case NSEventType.RightMouseDown:
			case NSEventType.RightMouseUp:
			case NSEventType.MouseMoved:
			case NSEventType.LeftMouseDragged:
			case NSEventType.RightMouseDragged:
			case NSEventType.MouseEntered:
			case NSEventType.MouseExited:
			case NSEventType.OtherMouseDown:
			case NSEventType.OtherMouseUp:
			case NSEventType.OtherMouseDragged:
				return true;
			default:
				return false;
		}
	}

	/// <summary>
	/// Inspiration from <see href="https://github.com/xamarin/xamarin-macios/blob/bc492585d137d8c3d3a2ffc827db3cdaae3cc869/src/AppKit/NSEvent.cs#L148"/>
	/// with some modifications.
	/// </summary>
	/// <param name="nativeEvent">Native event</param>
	/// <returns>Value indicating whether the event is in fact coming from a tablet device.</returns>
	private static bool IsTabletPointingEvent(NSEvent nativeEvent)
	{
		//limitation - mouse entered event currently throws for Subtype
		//(selector not working, although it should, according to docs)
		if (IsMouseEvent(nativeEvent) &&
			nativeEvent.Type != NSEventType.MouseEntered &&
			nativeEvent.Type != NSEventType.MouseExited)
		{
			//Xamarin debugger proxy for NSEvent incorrectly says Subtype
			//works only for Custom events, but that is not the case
			return
				nativeEvent.Subtype == TabletPointEventSubtype ||
				nativeEvent.Subtype == TabletProximityEventSubtype;
		}
		return nativeEvent.Type == NSEventType.TabletPoint;
	}

	#endregion

	internal void RefreshCursor()
	{
		if (PointerCursor == null)
		{
			if (!_cursorHidden)
			{
				NSCursor.Hide();
				_cursorHidden = true;
			}
		}
		else
		{
			if (_cursorHidden)
			{
				NSCursor.Unhide();
				_cursorHidden = false;
			}
			switch (_pointerCursor.Type)
			{
				case CoreCursorType.Arrow:
					NSCursor.ArrowCursor.Set();
					break;
				case CoreCursorType.Cross:
					NSCursor.CrosshairCursor.Set();
					break;
				case CoreCursorType.Hand:
					NSCursor.PointingHandCursor.Set();
					break;
				case CoreCursorType.IBeam:
					NSCursor.IBeamCursor.Set();
					break;
				case CoreCursorType.SizeNorthSouth:
					NSCursor.ResizeUpDownCursor.Set();
					break;
				case CoreCursorType.SizeWestEast:
					NSCursor.ResizeLeftRightCursor.Set();
					break;
				default:
					if (this.Log().IsEnabled(LogLevel.Warning))
					{
						this.Log().LogWarning($"Cursor type '{_pointerCursor.Type}' is not supported on macOS. Default cursor is used instead.");
					}
					NSCursor.ArrowCursor.Set();
					break;
			}
		}
	}

	private void LogNotSupported([CallerMemberName] string member = "")
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"{member} not supported on MacOS.");
		}
	}
}
