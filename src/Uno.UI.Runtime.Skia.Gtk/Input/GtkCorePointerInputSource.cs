#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Gdk;
using Gtk;
using Uno.UI.Runtime.Skia.Gtk.Extensions;
using Windows.Devices.Input;
using Windows.UI.Core;
using Windows.UI.Input;
using Uno.Foundation.Logging;
using static Windows.UI.Input.PointerUpdateKind;
using Exception = System.Exception;
using Windows.Foundation;
using Uno.UI.Runtime.Skia.Gtk.UI.Controls;
using Windows.UI.Xaml.Controls;
using Uno.UI.Hosting;
using Windows.Graphics.Display;
using Windows.UI.Xaml;
using Window = Gdk.Window;

namespace Uno.UI.Runtime.Skia.Gtk;

internal sealed class GtkCorePointerInputSource : IUnoCorePointerInputSource
{
	private const int _maxKnownDevices = 63;
	private const int _knownDeviceScavengeCount = 16;
	private readonly UnoGtkWindowHost _windowHost;
	private readonly Dictionary<PointerIdentifier, (Gdk.Device dev, uint ts)> _knownDevices = new(_maxKnownDevices + 1);
	private Gdk.Device? _lastUsedDevice;

	private readonly Logger _log;
	private readonly bool _isTraceEnabled;

	internal const Gdk.EventMask RequestedEvents =
		Gdk.EventMask.EnterNotifyMask
		| Gdk.EventMask.LeaveNotifyMask
		| Gdk.EventMask.ButtonPressMask
		| Gdk.EventMask.ButtonReleaseMask
		| Gdk.EventMask.PointerMotionMask // Move
		| Gdk.EventMask.SmoothScrollMask
		| Gdk.EventMask.TouchMask // Touch
		| Gdk.EventMask.ProximityInMask // Pen
		| Gdk.EventMask.ProximityOutMask // Pen
		| Gdk.EventMask.KeyPressMask
		| Gdk.EventMask.KeyReleaseMask;

#pragma warning disable CS0067 // Some event are not raised on GTK ... yet!
	public event TypedEventHandler<object, PointerEventArgs>? PointerCaptureLost;
	public event TypedEventHandler<object, PointerEventArgs>? PointerEntered;
	public event TypedEventHandler<object, PointerEventArgs>? PointerExited;
	public event TypedEventHandler<object, PointerEventArgs>? PointerMoved;
	public event TypedEventHandler<object, PointerEventArgs>? PointerPressed;
	public event TypedEventHandler<object, PointerEventArgs>? PointerReleased;
	public event TypedEventHandler<object, PointerEventArgs>? PointerWheelChanged;
	public event TypedEventHandler<object, PointerEventArgs>? PointerCancelled; // Uno Only
#pragma warning restore CS0067

	public GtkCorePointerInputSource(IXamlRootHost host)
	{
		_log = this.Log();
		_isTraceEnabled = _log.IsEnabled(LogLevel.Trace);

		if (host is not UnoGtkWindowHost windowHost)
		{
			throw new ArgumentException($"{nameof(host)} must be a Uno GTK Window host instance", nameof(host));
		}

		_windowHost = windowHost;

		// even though we are not going to use events directly in the window here maintain the masks
		_windowHost.GtkWindow.AddEvents((int)RequestedEvents);
		// add masks for the GtkEventBox
		_windowHost.EventBox.AddEvents((int)RequestedEvents);

		// Use GtkEventBox to fix Wayland titlebar events
		// Note: On some devices (e.g. raspberryPI - seems to be devices that are not supporting multi-touch?),
		//		 touch events are not going through the OnTouchEvent but are instead sent as "emulated" mouse (cf. EventHelper.GetPointerEmulated).
		//		 * For that kind of devices we are also supporting the touch in other handlers.
		//		   We don't have to inject Entered and Exited events as they are also simulated by the system.
		//		 * When a device properly send the touch events through the OnTouchEvent,
		//		   system does not "emulate the mouse" so this method should not be invoked.
		//		   That's the purpose of the UnoEventBox.
		_windowHost.EventBox.EnterNotifyEvent += OnEnterEvent;
		_windowHost.EventBox.LeaveNotifyEvent += OnLeaveEvent;
		_windowHost.EventBox.ButtonPressEvent += OnButtonPressEvent;
		_windowHost.EventBox.ButtonReleaseEvent += OnButtonReleaseEvent;
		_windowHost.EventBox.MotionNotifyEvent += OnMotionEvent;
		_windowHost.EventBox.ScrollEvent += OnScrollEvent;
		_windowHost.EventBox.Touched += OnTouchedEvent; //Note: we don't use the TouchEvent for the reason explained in the UnoEventBox!
		_windowHost.EventBox.ProximityInEvent += OnProximityInEvent;
		_windowHost.EventBox.ProximityOutEvent += OnProximityOutEvent;
	}

	/// <inheritdoc />
	[NotImplemented]
	public bool HasCapture => false;

	/// <inheritdoc />
	[NotImplemented]
	public Windows.Foundation.Point PointerPosition => default;

	/// <inheritdoc />
	public CoreCursor PointerCursor
	{
		get => _windowHost.GtkWindow.Window.Cursor.ToCoreCursor();
		set => _windowHost.GtkWindow.Window.Cursor = value.ToCursor();
	}

	/// <inheritdoc />
	public void SetPointerCapture()
	{
		if (_lastUsedDevice is not null)
		{
			global::Gtk.Device.GrabAdd(_windowHost.GtkWindow, _lastUsedDevice, block_others: false);
		}
		else if (this.Log().IsEnabled(LogLevel.Error))
		{
			this.Log().Error("No native device to capture.");
		}
	}

	/// <inheritdoc />
	public void SetPointerCapture(PointerIdentifier pointer)
	{
		if (_knownDevices.TryGetValue(pointer, out var entry))
		{
			global::Gtk.Device.GrabAdd(_windowHost.GtkWindow, entry.dev, block_others: false);
		}
		else if (this.Log().IsEnabled(LogLevel.Error))
		{
			this.Log().Error($"Unknown device id {pointer}, unable to natively capture it.");
		}
	}

	/// <inheritdoc />
	public void ReleasePointerCapture()
	{
		if (_lastUsedDevice is not null)
		{
			global::Gtk.Device.GrabAdd(_windowHost.GtkWindow, _lastUsedDevice, block_others: false);
		}
		else if (this.Log().IsEnabled(LogLevel.Error))
		{
			this.Log().Error("No native device to release.");
		}
	}

	/// <inheritdoc />
	public void ReleasePointerCapture(PointerIdentifier pointer)
	{
		if (_knownDevices.TryGetValue(pointer, out var entry))
		{
			global::Gtk.Device.GrabRemove(_windowHost.GtkWindow, entry.dev);
		}
		else if (this.Log().IsEnabled(LogLevel.Error))
		{
			this.Log().Error($"Unknown device id {pointer}, unable to natively capture it.");
		}
	}

	#region Native event handlers
	private void OnEnterEvent(object o, EnterNotifyEventArgs args)
	{
		try
		{
			if (AsPointerArgs(o, args.Event) is { } ptArgs)
			{
				RaisePointerEntered(ptArgs);
			}
		}
		catch (Exception e)
		{
			this.Log().Error("Failed to raise PointerEntered", e);
		}
	}

	private void OnLeaveEvent(object o, LeaveNotifyEventArgs args)
	{
		try
		{
			// The Ungrab mode event is triggered after clicking even when the pointer does not leave the window.
			// This may need to be removed when we implement native pointer capture support properly.
			if (args.Event.Mode != CrossingMode.Ungrab)
			{
				if (AsPointerArgs(o, args.Event) is { } ptArgs)
				{
					RaisePointerExited(ptArgs);
				}
			}
		}
		catch (Exception e)
		{
			this.Log().Error("Failed to raise PointerExited", e);
		}
	}

	private void OnButtonPressEvent(object o, ButtonPressEventArgs args)
	{
		// Double clicking on Gtk will produce an event sequence with the following event types on Gtk:
		// 1. EventType.ButtonPress
		// 2. EventType.ButtonRelease
		// 3. EventType.ButtonPress
		// 4. EventType.TwoButtonPress
		// 5. EventType.ButtonRelease
		// We want to receive the "single" presses only as receiving the second press twice is gonna be problematic.
		// So we skip TwoButtonPress (and also ThreeButtonPress as it has similar issue)
		if (args.Event.Type is EventType.TwoButtonPress or EventType.ThreeButtonPress)
		{
			return;
		}

		try
		{
			if (AsPointerArgs(o, args.Event) is { } ptArgs)
			{
				RaisePointerPressed(ptArgs);
			}
		}
		catch (Exception e)
		{
			this.Log().Error("Failed to raise PointerPressed", e);
		}
	}

	private void OnButtonReleaseEvent(object o, ButtonReleaseEventArgs args)
	{
		try
		{
			if (AsPointerArgs(o, args.Event) is { } ptArgs)
			{
				RaisePointerReleased(ptArgs);
			}
		}
		catch (Exception e)
		{
			this.Log().Error("Failed to raise PointerReleased", e);
		}
	}

	private void OnMotionEvent(object o, MotionNotifyEventArgs args) // a.k.a. move
	{
		try
		{
			if (AsPointerArgs(o, args.Event) is { } ptArgs)
			{
				RaisePointerMoved(ptArgs);
			}
		}
		catch (Exception e)
		{
			this.Log().Error("Failed to raise PointerMoved", e);
		}
	}

	private void OnScrollEvent(object o, ScrollEventArgs args)
	{
		try
		{
			if (AsPointerArgs(o, args.Event) is { } ptArgs)
			{
				RaisePointerWheelChanged(ptArgs);
			}
		}
		catch (Exception e)
		{
			this.Log().Error("Failed to raise PointerExited", e);
		}
	}

	private void OnTouchedEvent(object? o, EventTouch evt)
	{
		try
		{
			if (AsPointerArgs(o!, evt) is { } ptArgs)
			{
				switch (evt.Type)
				{
					case EventType.TouchBegin:
						RaisePointerEntered(ptArgs);
						RaisePointerPressed(ptArgs);
						break;

					case EventType.TouchEnd:
						RaisePointerReleased(ptArgs);
						RaisePointerExited(ptArgs);
						break;

					case EventType.TouchUpdate:
						RaisePointerMoved(ptArgs);
						break;

					case EventType.TouchCancel:
						RaisePointerCancelled(ptArgs);
						break;
				}
			}
		}
		catch (Exception e)
		{
			this.Log().Error("Failed to raise touch event (one of Enter/Pressed/Move/Release/Exited)", e);
		}
	}

	private void OnProximityOutEvent(object o, ProximityOutEventArgs args)
	{
		try
		{
			if (AsPointerArgs(o, args.Event) is { } ptArgs)
			{
				RaisePointerExited(ptArgs);
			}
		}
		catch (Exception e)
		{
			this.Log().Error("Failed to raise proximity out event", e);
		}
	}

	private void OnProximityInEvent(object o, ProximityInEventArgs args)
	{
		try
		{
			if (AsPointerArgs(o, args.Event) is { } ptArgs)
			{
				RaisePointerEntered(ptArgs);
			}
		}
		catch (Exception e)
		{
			this.Log().Error("Failed to raise proximity in event", e);
		}
	}
	#endregion

	private void UseDevice(PointerPoint pointer, Gdk.Device device)
	{
		_knownDevices[pointer.Pointer] = (device, pointer.FrameId);
		_lastUsedDevice = device;

		if (_knownDevices.Count > _maxKnownDevices)
		{
			// If we have too much devices in the cache, we significantly trim it to not have to do that every times.

			var oldest = _knownDevices
				.OrderBy(kvp => kvp.Value.ts)
				.Take(_knownDeviceScavengeCount)
				.Select(kvp => kvp.Key)
				.ToArray();

			foreach (var dev in oldest)
			{
				_knownDevices.Remove(dev);
			}
		}
	}

	#region Convert helpers
	private PointerEventArgs? AsPointerArgs(object o, EventTouch evt)
	{
		var (windowX, windowY) = GetWindowCoordinates();
		var x = evt.XRoot - windowX;
		var y = evt.YRoot - windowY;

		return AsPointerArgs(
			evt.Device, PointerDeviceType.Touch, (uint?)evt.Sequence?.Handle ?? 1u,
			evt.Type, evt.Time,
			evt.XRoot, evt.YRoot,
			x, y,
			(ModifierType)evt.State,
			evt: null);
	}

	private PointerEventArgs? AsPointerArgs(object o, Event evt)
	{
		// The coordinates received in the event args are relative to the widget that raised the event.
		// Usually, that's the native overlay (which embodies the entire window, minus the titlebar). However,
		// if another overlay (e.g. TextBox) raises an event, we still want the coordinates relative to the
		// top-left of the window. This method makes that adjustment
		var (windowX, windowY) = GetWindowCoordinates();

		var dev = EventHelper.GetSourceDevice(evt); // We use GetSourceDevice (and not GetDevice) in order to get the TouchScreen device
		var type = evt.Type;
		var time = EventHelper.GetTime(evt);
		EventHelper.GetRootCoords(evt, out var rootX, out var rootY);
		EventHelper.GetState(evt, out var state);

		var x = rootX - windowX;
		var y = rootY - windowY;

		return AsPointerArgs(
		dev, GetDeviceType(dev), 1u,
		type, time,
		rootX, rootY,
		x, y,
		state,
		evt);
	}

	private PointerEventArgs? AsPointerArgs(
		Gdk.Device dev, PointerDeviceType devType, uint pointerId,
		EventType evtType, uint time,
		double rootX, double rootY,
		double x, double y,
		ModifierType state,
		Event? evt)
	{
		var xamlRoot = GtkManager.XamlRootMap.GetRootForHost(_windowHost);
		var positionAdjustment = xamlRoot?.FractionalScaleAdjustment ?? 1.0;

		var pointerDevice = PointerDevice.For(devType);
		var rawPosition = new Windows.Foundation.Point(rootX / positionAdjustment, rootY / positionAdjustment);
		var position = new Windows.Foundation.Point(x / positionAdjustment, y / positionAdjustment);
		var modifiers = GtkKeyboardInputSource.GetKeyModifiers(state);
		var properties = new PointerPointProperties();

		switch (evtType)
		{
			case EventType.TouchBegin:
				properties.PointerUpdateKind = PointerUpdateKind.LeftButtonPressed;
				break;

			case EventType.TouchEnd:
			case EventType.TouchCancel:
				properties.PointerUpdateKind = PointerUpdateKind.LeftButtonReleased;
				break;

			case EventType.ButtonPress when EventHelper.GetButton(evt!, out var button):
				properties.PointerUpdateKind = button switch
				{
					1 => LeftButtonPressed,
					2 => MiddleButtonPressed,
					3 => RightButtonPressed,
					4 => XButton1Pressed,
					5 => XButton2Pressed,
					_ => Other
				};
				break;

			case EventType.ButtonRelease when EventHelper.GetButton(evt!, out var button):
				properties.PointerUpdateKind = button switch
				{
					1 => LeftButtonReleased,
					2 => MiddleButtonReleased,
					3 => RightButtonReleased,
					4 => XButton1Released,
					5 => XButton2Released,
					_ => Other
				};
				break;

			case EventType.Scroll when EventHelper.GetScrollDeltas(evt!, out var scrollX, out var scrollY):
				var isHorizontal = scrollY == 0;
				properties.IsHorizontalMouseWheel = isHorizontal;
				properties.MouseWheelDelta = (int)((isHorizontal ? scrollX : -scrollY) * ScrollContentPresenter.ScrollViewerDefaultMouseWheelDelta);
				break;

			case EventType.Scroll: // when no scroll value
				return null;
		}

		switch (pointerDevice.PointerDeviceType)
		{
			// https://gtk-rs.org/docs/gdk/struct.ModifierType.html

			case PointerDeviceType.Touch:
				properties.IsLeftButtonPressed = evtType is not EventType.TouchEnd and not EventType.TouchCancel;
				break;

			case PointerDeviceType.Mouse:
				properties.IsLeftButtonPressed = IsPressed(state, ModifierType.Button1Mask, properties.PointerUpdateKind, LeftButtonPressed, LeftButtonReleased);
				properties.IsMiddleButtonPressed = IsPressed(state, ModifierType.Button2Mask, properties.PointerUpdateKind, MiddleButtonPressed, MiddleButtonReleased);
				properties.IsRightButtonPressed = IsPressed(state, ModifierType.Button3Mask, properties.PointerUpdateKind, RightButtonPressed, RightButtonReleased);
				properties.IsXButton1Pressed = IsPressed(state, ModifierType.Button4Mask, properties.PointerUpdateKind, XButton1Pressed, XButton1Released);
				properties.IsXButton2Pressed = IsPressed(state, ModifierType.Button5Mask, properties.PointerUpdateKind, XButton1Pressed, XButton2Released);
				break;

			case PointerDeviceType.Pen:
				properties.IsBarrelButtonPressed = state.HasFlag(Gdk.ModifierType.Button1Mask);
				// On UWP the IsRight flag is set only when pen touch the screen when the barrel button is already pressed.
				// We accept it as a known limitation that with uno the flag is set as soon as the barrel is pressed,
				// not matter is the pen was already in contact with the screen or not.
				properties.IsRightButtonPressed = properties.IsBarrelButtonPressed;
				properties.IsEraser = dev.Source is InputSource.Eraser;
				if (EventHelper.GetAxis(evt!, AxisUse.Pressure, out var pressure))
				{
					properties.Pressure = (float)Math.Min(1.0, pressure);
				}
				if (EventHelper.GetAxis(evt!, AxisUse.Xtilt, out var xTilt))
				{
					properties.XTilt = (float)xTilt;
				}
				if (EventHelper.GetAxis(evt!, AxisUse.Ytilt, out var yTilt))
				{
					properties.YTilt = (float)yTilt;
				}
				break;
		}

		properties.IsInRange = true;

		var pointerPoint = new Windows.UI.Input.PointerPoint(
			frameId: time,
			timestamp: time * (ulong)TimeSpan.TicksPerMillisecond, // time is in ms, timestamp is in ticks
			device: pointerDevice,
			pointerId: pointerId,
			rawPosition: rawPosition,
			position: position,
			isInContact: properties.HasPressedButton,
			properties: properties
		);

		// This method is not pure as it should be for a 'AsXXXX' method due to the line below,
		// but doing so the 'dev' is contains only here.
		UseDevice(pointerPoint, dev);

		return new PointerEventArgs(pointerPoint, modifiers);
	}

	private static PointerDeviceType GetDeviceType(Gdk.Device sourceDevice)
	{
		switch (sourceDevice.Source)
		{
			// https://gtk-rs.org/docs/gdk/enum.InputSource.html

			case InputSource.Pen:
			case InputSource.Eraser:
			case InputSource.TabletPad: // the device is a "pad", a collection of buttons, rings and strips found in drawing tablets.
			case InputSource.Cursor: // the device is a graphics tablet “puck” or similar device.
				return PointerDeviceType.Pen;

			case InputSource.Touchscreen:
				return PointerDeviceType.Touch;

			case InputSource.Mouse:
			default:
				return PointerDeviceType.Mouse;
		}
	}

	private static bool IsPressed(ModifierType state, ModifierType mask, PointerUpdateKind update, PointerUpdateKind pressed, PointerUpdateKind released)
		=> update == pressed || (((state & mask) != 0) && update != released);
	#endregion

	private void RaisePointerEntered(PointerEventArgs ptArgs, [CallerMemberName] string caller = "")
	{
		if (_isTraceEnabled)
		{
			_log.Trace($"[Entered] ({caller}) {ptArgs}");
		}

		PointerEntered?.Invoke(this, ptArgs);
	}

	private void RaisePointerExited(PointerEventArgs ptArgs, [CallerMemberName] string caller = "")
	{
		if (_isTraceEnabled)
		{
			_log.Trace($"[Exited] ({caller}) {ptArgs}");
		}

		PointerExited?.Invoke(this, ptArgs);
	}

	private void RaisePointerPressed(PointerEventArgs ptArgs, [CallerMemberName] string caller = "")
	{
		if (_isTraceEnabled)
		{
			_log.Trace($"[Pressed] ({caller}) {ptArgs}");
		}

		PointerPressed?.Invoke(this, ptArgs);
	}

	private void RaisePointerReleased(PointerEventArgs ptArgs, [CallerMemberName] string caller = "")
	{
		if (_isTraceEnabled)
		{
			_log.Trace($"[Released] ({caller}) {ptArgs}");
		}

		PointerReleased?.Invoke(this, ptArgs);
	}

	private void RaisePointerMoved(PointerEventArgs ptArgs, [CallerMemberName] string caller = "")
	{
		if (_isTraceEnabled)
		{
			_log.Trace($"[Moved] ({caller}) {ptArgs}");
		}

		PointerMoved?.Invoke(this, ptArgs);
	}

	private void RaisePointerCancelled(PointerEventArgs ptArgs, [CallerMemberName] string caller = "")
	{
		if (_isTraceEnabled)
		{
			_log.Trace($"[Cancelled] ({caller}) {ptArgs}");
		}

		PointerCancelled?.Invoke(this, ptArgs);
	}

	private void RaisePointerWheelChanged(PointerEventArgs ptArgs, [CallerMemberName] string caller = "")
	{
		if (_isTraceEnabled)
		{
			_log.Trace($"[Wheel] ({caller}) {ptArgs}");
		}

		PointerWheelChanged?.Invoke(this, ptArgs);
	}

	private (int x, int y) GetWindowCoordinates()
	{
		// Calling GetGeometry or GetOrigin on any window reference such as e.Window or (o as Widget).Window will return wrong
		// values (usually just 0). Only UnoGtkWindow.Window will give us what we need.

		// GetGeometry returns different numbers between Windows and Linux.
		// FrameExtents (and a bunch of other methods/properties) will include the border and the title bar on Linux
		((UnoGtkWindow)(_windowHost.RootContainer)).Window.GetOrigin(out var x, out var y);

		// Window.GetOrigin returns shifted numbers on WSL, most likely due to window decoration differences
		// that might not be present on Linux itself. It might be because of differences between Gnome vs
		// non-Gnome desktops.
		var allocation = _windowHost.EventBox.Allocation;
		x += allocation.X;
		y += allocation.Y;

		return (x, y);
	}
}
