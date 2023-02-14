#nullable enable
//#define TRACE_NATIVE_POINTER_EVENTS

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Gdk;
using Gtk;
using Uno.Extensions;
using Uno.UI.Runtime.Skia.GTK.Extensions;
using Windows.Devices.Input;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Input;
using Uno.Foundation.Logging;
using static Windows.UI.Input.PointerUpdateKind;
using Device = Gtk.Device;
using Exception = System.Exception;
using Microsoft.UI.Xaml;
using Uno.UI.Xaml.Core;

namespace Uno.UI.Runtime.Skia
{
	internal partial class GtkCoreWindowExtension : ICoreWindowExtension
	{
		private readonly CoreWindow _owner;
		private readonly ICoreWindowEvents _ownerEvents;

		private const int _maxKnownDevices = 63;
		private const int _knownDeviceScavengeCount = 16;
		private readonly Dictionary<PointerIdentifier, (Gdk.Device dev, uint ts)> _knownDevices = new(_maxKnownDevices + 1);

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

		public CoreCursor PointerCursor
		{
			get => GtkHost.Window.Window.Cursor.ToCoreCursor();
			set => GtkHost.Window.Window.Cursor = value.ToCursor();
		}

		public GtkCoreWindowExtension(object owner)
		{
			_owner = (CoreWindow)owner;
			_ownerEvents = (ICoreWindowEvents)owner;

			// even though we are not going to use events directly in the window here maintain the masks
			GtkHost.Window.AddEvents((int)RequestedEvents);
			// add masks for the GtkEventBox
			GtkHost.EventBox.AddEvents((int)RequestedEvents);

			// Use GtkEventBox to fix Wayland titlebar events
			// Note: On some devices (e.g. raspberryPI - seems to be devices that are not supporting multi-touch?),
			//		 touch events are not going through the OnTouchEvent but are instead sent as "emulated" mouse (cf. EventHelper.GetPointerEmulated).
			//		 * For that kind of devices we are also supporting the touch in other handlers.
			//		   We don't have to inject Entered and Exited events as they are also simulated by the system.
			//		 * When a device properly send the touch events through the OnTouchEvent,
			//		   system does not "emulate the mouse" so this method should not be invoked.
			//		   That's the purpose of the UnoEventBox.
			GtkHost.EventBox.EnterNotifyEvent += OnEnterEvent;
			GtkHost.EventBox.LeaveNotifyEvent += OnLeaveEvent;
			GtkHost.EventBox.ButtonPressEvent += OnButtonPressEvent;
			GtkHost.EventBox.ButtonReleaseEvent += OnButtonReleaseEvent;
			GtkHost.EventBox.MotionNotifyEvent += OnMotionEvent;
			GtkHost.EventBox.ScrollEvent += OnScrollEvent;
			GtkHost.EventBox.Touched += OnTouchedEvent; //Note: we don't use the TouchEvent for the reason explained in the UnoEventBox!
			GtkHost.EventBox.ProximityInEvent += OnProximityInEvent;
			GtkHost.EventBox.ProximityOutEvent += OnProximityOutEvent;

			InitializeKeyboard();
		}

		partial void InitializeKeyboard();

		/// <inheritdoc />
		public void SetPointerCapture(PointerIdentifier pointer)
		{
			if (_knownDevices.TryGetValue(pointer, out var entry))
			{
				Gtk.Device.GrabAdd(GtkHost.Window, entry.dev, block_others: false);
			}
			else if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"Unknown device id {pointer}, unable to natively capture it.");
			}
		}

		/// <inheritdoc />
		public void ReleasePointerCapture(PointerIdentifier pointer)
		{
			if (_knownDevices.TryGetValue(pointer, out var entry))
			{
				Gtk.Device.GrabRemove(GtkHost.Window, entry.dev);
			}
			else if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"Unknown device id {pointer}, unable to natively capture it.");
			}
		}

		private void OnEnterEvent(object o, EnterNotifyEventArgs args)
		{
			try
			{
				if (AsPointerArgs(args.Event) is { } ptArgs)
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
				// The Ungrab mode event is triggered after click even when the pointer does not leave the window.
				// This may need to be removed when we implement native pointer capture support properly.
				if (args.Event.Mode != CrossingMode.Ungrab)
				{
					if (AsPointerArgs(args.Event) is { } ptArgs)
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
			try
			{
				if (AsPointerArgs(args.Event) is { } ptArgs)
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
				if (AsPointerArgs(args.Event) is { } ptArgs)
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
				if (AsPointerArgs(args.Event) is { } ptArgs)
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
				if (AsPointerArgs(args.Event) is { } ptArgs)
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
				if (AsPointerArgs(evt) is { } ptArgs)
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
				if (AsPointerArgs(args.Event) is { } ptArgs)
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
				if (AsPointerArgs(args.Event) is { } ptArgs)
				{
					RaisePointerEntered(ptArgs);
				}
			}
			catch (Exception e)
			{
				this.Log().Error("Failed to raise proximity in event", e);
			}
		}

		private void UseDevice(PointerPoint pointer, Gdk.Device device)
		{
			_knownDevices[pointer.Pointer] = (device, pointer.FrameId);

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

		private PointerEventArgs? AsPointerArgs(EventTouch evt)
			=> AsPointerArgs(
				evt.Device, PointerDeviceType.Touch, (uint?)evt.Sequence?.Handle ?? 1u,
				evt.Type, evt.Time,
				evt.XRoot, evt.YRoot,
				evt.X, evt.Y,
				(ModifierType)evt.State,
				evt: null);

		private PointerEventArgs? AsPointerArgs(Event evt)
		{
			var dev = EventHelper.GetSourceDevice(evt); // We use GetSourceDevice (and not GetDevice) in order to get the TouchScreen device
			var type = evt.Type;
			var time = EventHelper.GetTime(evt);
			EventHelper.GetRootCoords(evt, out var rootX, out var rootY);
			EventHelper.GetCoords(evt, out var x, out var y);
			EventHelper.GetState(evt, out var state);

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
			var pointerDevice = PointerDevice.For(devType);
			var rawPosition = new Windows.Foundation.Point(rootX, rootY);
			var position = new Windows.Foundation.Point(x, y);
			var modifiers = GetKeyModifiers(state);
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
					properties.MouseWheelDelta = (int)(isHorizontal ? scrollX : scrollY);
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
				timestamp: time,
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

		private static VirtualKeyModifiers GetKeyModifiers(Gdk.ModifierType state)
		{
			var modifiers = VirtualKeyModifiers.None;
			if (state.HasFlag(Gdk.ModifierType.ShiftMask))
			{
				modifiers |= VirtualKeyModifiers.Shift;
			}
			if (state.HasFlag(Gdk.ModifierType.ControlMask))
			{
				modifiers |= VirtualKeyModifiers.Control;
			}
			if (state.HasFlag(Gdk.ModifierType.Mod1Mask))
			{
				modifiers |= VirtualKeyModifiers.Menu;
			}
			return modifiers;
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
			=> update == pressed || (state.HasFlag(mask) && update != released);


#if TRACE_NATIVE_POINTER_EVENTS
		private void RaisePointerEntered(PointerEventArgs ptArgs, [CallerMemberName] string caller = null)
		{
			Trace($"[Entered] ({caller}) {ptArgs}");
			_ownerEvents.RaisePointerEntered(ptArgs);
		}

		private void RaisePointerExited(PointerEventArgs ptArgs, [CallerMemberName] string caller = null)
		{
			Trace($"[Exited] ({caller}) {ptArgs}");
			_ownerEvents.RaisePointerExited(ptArgs);
		}

		private void RaisePointerPressed(PointerEventArgs ptArgs, [CallerMemberName] string caller = null)
		{
			Trace($"[Pressed] ({caller}) {ptArgs}");
			_ownerEvents.RaisePointerPressed(ptArgs);
		}

		private void RaisePointerReleased(PointerEventArgs ptArgs, [CallerMemberName] string caller = null)
		{
			Trace($"[Released] ({caller}) {ptArgs}");
			_ownerEvents.RaisePointerReleased(ptArgs);
		}

		private void RaisePointerMoved(PointerEventArgs ptArgs, [CallerMemberName] string caller = null)
		{
			Trace($"[Moved] ({caller}) {ptArgs}");
			_ownerEvents.RaisePointerMoved(ptArgs);
		}

		private void RaisePointerCancelled(PointerEventArgs ptArgs, [CallerMemberName] string caller = null)
		{
			Trace($"[Cancelled] ({caller}) {ptArgs}");
			_ownerEvents.RaisePointerCancelled(ptArgs);
		}

		private void RaisePointerWheelChanged(PointerEventArgs ptArgs, [CallerMemberName] string caller = null)
		{
			Trace($"[Wheel] ({caller}) {ptArgs}");
			_ownerEvents.RaisePointerWheelChanged(ptArgs);
		}

		private void Trace(string text)
			=> Console.WriteLine(text);

#else
		private void RaisePointerEntered(PointerEventArgs ptArgs)
			=> _ownerEvents.RaisePointerEntered(ptArgs);

		private void RaisePointerExited(PointerEventArgs ptArgs)
			=> _ownerEvents.RaisePointerExited(ptArgs);

		private void RaisePointerPressed(PointerEventArgs ptArgs)
			=> _ownerEvents.RaisePointerPressed(ptArgs);

		private void RaisePointerReleased(PointerEventArgs ptArgs)
			=> _ownerEvents.RaisePointerReleased(ptArgs);

		private void RaisePointerMoved(PointerEventArgs ptArgs)
			=> _ownerEvents.RaisePointerMoved(ptArgs);

		private void RaisePointerCancelled(PointerEventArgs ptArgs)
			=> _ownerEvents.RaisePointerCancelled(ptArgs);

		private void RaisePointerWheelChanged(PointerEventArgs ptArgs)
			=> _ownerEvents.RaisePointerWheelChanged(ptArgs);
#endif
	}
}
