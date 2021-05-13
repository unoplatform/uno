using System;
using System.Linq;
using Gdk;
using Gtk;
using Uno.Extensions;
using Uno.Logging;
using Uno.UI.Runtime.Skia.GTK.Extensions;
using Windows.Devices.Input;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Input;
using static Windows.UI.Input.PointerUpdateKind;
using Exception = System.Exception;

namespace Uno.UI.Runtime.Skia
{
	internal partial class GtkCoreWindowExtension : ICoreWindowExtension
	{
		private readonly CoreWindow _owner;
		private ICoreWindowEvents _ownerEvents;

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

		/// <inheritdoc />
		public void ReleasePointerCapture()
			=> this.Log().Warn("Pointer capture release is not supported on GTK");

		/// <inheritdoc />
		public void SetPointerCapture()
			=> this.Log().Warn("Pointer capture is not supported on GTK");

		public GtkCoreWindowExtension(object owner)
		{
			_owner = (CoreWindow)owner;
			_ownerEvents = (ICoreWindowEvents)owner;

			// even though we are not going to use events directly in the window here maintain the masks
			GtkHost.Window.AddEvents((int)RequestedEvents);
			// add masks for the GtkEventBox
			GtkHost.EventBox.AddEvents((int)RequestedEvents);

			// Use GtkEventBox to fix Wayland titlebar events
			GtkHost.EventBox.EnterNotifyEvent += OnWindowEnterEvent;
			GtkHost.EventBox.LeaveNotifyEvent += OnWindowLeaveEvent;
			GtkHost.EventBox.ButtonPressEvent += OnWindowButtonPressEvent;
			GtkHost.EventBox.ButtonReleaseEvent += OnWindowButtonReleaseEvent;
			GtkHost.EventBox.MotionNotifyEvent += OnWindowMotionEvent;
			GtkHost.EventBox.ScrollEvent += OnWindowScrollEvent;
			GtkHost.EventBox.TouchEvent += OnWindowTouchEvent;
			GtkHost.EventBox.ProximityInEvent += OnWindowProximityInEvent;
			GtkHost.EventBox.ProximityOutEvent += OnWindowProximityOutEvent;

			InitializeKeyboard();
		}

		partial void InitializeKeyboard();

		private void OnWindowEnterEvent(object o, EnterNotifyEventArgs args)
		{
			try
			{
				if (AsPointerArgs(args.Event) is { } ptArgs)
				{
					_ownerEvents.RaisePointerEntered(ptArgs);
				}
			}
			catch (Exception e)
			{
				this.Log().Error("Failed to raise PointerEntered", e);
			}
		}

		private void OnWindowLeaveEvent(object o, LeaveNotifyEventArgs args)
		{
			try
			{
				// The Ungrab mode event is triggered after click 
				// even when the pointer does not leave the window.
				// This may need to be removed when we implement
				// native pointer capture support properly.
				if (args.Event.Mode != CrossingMode.Ungrab)
				{
					if (AsPointerArgs(args.Event) is { } ptArgs)
					{
						_ownerEvents.RaisePointerExited(ptArgs);
					}
				}
			}
			catch (Exception e)
			{
				this.Log().Error("Failed to raise PointerExited", e);
			}
		}

		private void OnWindowButtonPressEvent(object o, ButtonPressEventArgs args)
		{
			try
			{
				if (AsPointerArgs(args.Event) is { } ptArgs)
				{
					_ownerEvents.RaisePointerPressed(ptArgs);
				}
			}
			catch (Exception e)
			{
				this.Log().Error("Failed to raise PointerPressed", e);
			}
		}

		private void OnWindowButtonReleaseEvent(object o, ButtonReleaseEventArgs args)
		{
			try
			{
				if (AsPointerArgs(args.Event) is { } ptArgs)
				{
					_ownerEvents.RaisePointerReleased(ptArgs);
				}
			}
			catch (Exception e)
			{
				this.Log().Error("Failed to raise PointerReleased", e);
			}
		}

		private void OnWindowMotionEvent(object o, MotionNotifyEventArgs args) // a.k.a. move
		{
			try
			{
				if (AsPointerArgs(args.Event) is { } ptArgs)
				{
					_ownerEvents.RaisePointerMoved(ptArgs);
				}
			}
			catch (Exception e)
			{
				this.Log().Error("Failed to raise PointerMoved", e);
			}
		}

		private void OnWindowScrollEvent(object o, ScrollEventArgs args)
		{
			try
			{
				if (AsPointerArgs(args.Event) is { } ptArgs)
				{
					_ownerEvents.RaisePointerWheelChanged(ptArgs);
				}
			}
			catch (Exception e)
			{
				this.Log().Error("Failed to raise PointerExited", e);
			}
		}

		private void OnWindowTouchEvent(object o, TouchEventArgs args)
		{
			try
			{
				// Note: We DO NOT used the args.Event as it's causing an InvalidCastException as of 2021-03-09
				if (args.Args.FirstOrDefault() is Gdk.Event evt
					&& AsPointerArgs(evt) is { } ptArgs)
				{
					switch (evt.Type)
					{
						case EventType.TouchBegin:
							_ownerEvents.RaisePointerEntered(ptArgs);
							_ownerEvents.RaisePointerPressed(ptArgs);
							break;

						case EventType.TouchEnd:
							_ownerEvents.RaisePointerReleased(ptArgs);
							_ownerEvents.RaisePointerExited(ptArgs);
							break;

						case EventType.TouchCancel:
							_ownerEvents.RaisePointerMoved(ptArgs);
							break;
					}

					_ownerEvents.RaisePointerMoved(ptArgs);
				}
			}
			catch (Exception e)
			{
				this.Log().Error("Failed to raise touch event (one of Enter/Pressed/Move/Release/Exited)", e);
			}
		}

		private void OnWindowProximityOutEvent(object o, ProximityOutEventArgs args)
		{
			try
			{
				if (AsPointerArgs(args.Event) is { } ptArgs)
				{
					_ownerEvents.RaisePointerExited(ptArgs);
				}
			}
			catch (Exception e)
			{
				this.Log().Error("Failed to raise proximity out event", e);
			}
		}

		private void OnWindowProximityInEvent(object o, ProximityInEventArgs args)
		{
			try
			{
				if (AsPointerArgs(args.Event) is { } ptArgs)
				{
					_ownerEvents.RaisePointerEntered(ptArgs);
				}
			}
			catch (Exception e)
			{
				this.Log().Error("Failed to raise proximity in event", e);
			}
		}

		private static PointerEventArgs AsPointerArgs(Event evt)
		{
			var dev = EventHelper.GetSourceDevice(evt); // We use GetSourceDevice (and not GetDevice) in order to get the TouchScreen device
			var pointerDevice = ToPointerDevice(dev);
			var pointerDeviceType = pointerDevice.PointerDeviceType;

			if (pointerDeviceType == PointerDeviceType.Touch)
			{
				var type = evt.Type;
				if (type != EventType.TouchBegin
					&& type != EventType.TouchUpdate
					&& type != EventType.TouchEnd
					&& type != EventType.TouchCancel)
				{
					// Touch events are sent twice by the OnWindowTouchEvent and the ButtonPressed/Released and MotionEvent.
					// As the pointerId (a.k.a sequence) is available only for touch events, we mute events coming from the mouse handlers.
					return null;
				}
			}

			var time = EventHelper.GetTime(evt);
			EventHelper.GetRootCoords(evt, out var x, out var y);
			var rawPosition = new Windows.Foundation.Point(x, y);
			EventHelper.GetCoords(evt, out x, out y);
			var position = new Windows.Foundation.Point(x, y);
			EventHelper.GetState(evt, out var state);

			var pointerId = 1u;
			var modifiers = GetKeyModifiers(state);
			var properties = new PointerPointProperties();

			switch (evt.Type)
			{
				case EventType.TouchBegin:
					properties.PointerUpdateKind = PointerUpdateKind.LeftButtonPressed;
					break;

				case EventType.TouchEnd:
				case EventType.TouchCancel:
					properties.PointerUpdateKind = PointerUpdateKind.LeftButtonReleased;
					break;

				case EventType.ButtonPress when EventHelper.GetButton(evt, out var button):
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

				case EventType.ButtonRelease when EventHelper.GetButton(evt, out var button):
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

				case EventType.Scroll when EventHelper.GetScrollDeltas(evt, out var scrollX, out var scrollY):
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
					properties.IsLeftButtonPressed = evt.Type != EventType.TouchEnd && evt.Type != EventType.TouchCancel;
					pointerId = ((uint?)EventHelper.GetEventSequence(evt)?.Handle) ?? 0u;
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
					properties.IsEraser = dev.Source == InputSource.Eraser;
					if (EventHelper.GetAxis(evt, AxisUse.Pressure, out var pressure))
					{
						properties.Pressure = (float)Math.Min(1.0, pressure);
					}
					break;
			}

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

		private static PointerDevice ToPointerDevice(Gdk.Device sourceDevice)
		{
			switch (sourceDevice.Source)
			{
				// https://gtk-rs.org/docs/gdk/enum.InputSource.html

				case InputSource.Pen:
				case InputSource.Eraser:
				case InputSource.TabletPad: // the device is a "pad", a collection of buttons, rings and strips found in drawing tablets.
				case InputSource.Cursor: // the device is a graphics tablet “puck” or similar device.
					return PointerDevice.For(PointerDeviceType.Pen);

				case InputSource.Touchscreen:
					return PointerDevice.For(PointerDeviceType.Touch);

				case InputSource.Mouse:
				default:
					return PointerDevice.For(PointerDeviceType.Mouse);
			}
		}

		private static bool IsPressed(ModifierType state, ModifierType mask, PointerUpdateKind update, PointerUpdateKind pressed, PointerUpdateKind released)
			=> update == pressed || (state.HasFlag(mask) && update != released);
	}
}
