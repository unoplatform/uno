using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Windows.Devices.Input;
using AppKit;
using Windows.System;
using Foundation;
using Windows.UI.Input;
using CoreGraphics;

namespace Windows.UI.Xaml.Input
{
	partial class PointerRoutedEventArgs
	{
		private const int TabletPointEventSubtype = 1;
		private const int TabletProximityEventSubtype = 2;

		private const int LeftMouseButtonMask = 1;
		private const int RightMouseButtonMask = 2;

		private readonly NSEvent _nativeEvent;
		private readonly NSSet _nativeTouches;

		internal PointerRoutedEventArgs(NSSet touches, NSEvent nativeEvent, UIElement source) : this()
		{
			_nativeEvent = nativeEvent;
			_nativeTouches = touches;

			var pointerDeviceType = GetPointerDeviceType(nativeEvent);
			var pointerId = (uint)0;
			if (pointerDeviceType == PointerDeviceType.Pen)
			{
				pointerId = (uint)nativeEvent.PointingDeviceID();
			}

			var isInContact = GetIsInContact(nativeEvent);

			FrameId = ToFrameId(_nativeEvent.Timestamp);
			Pointer = new Pointer(pointerId, pointerDeviceType, isInContact, isInRange: true);
			KeyModifiers = GetVirtualKeyModifiers(nativeEvent);
			OriginalSource = source;
		}

		public PointerPoint GetCurrentPoint(UIElement relativeTo)
		{
			var device = PointerDevice.For(PointerDeviceType.Mouse);
			var rawPosition = _nativeEvent.LocationInWindow;
			var position = relativeTo != null ?
				relativeTo.ConvertPointFromView(_nativeEvent.LocationInWindow, null) :
				new CGPoint(rawPosition.X, Window.Current.Bounds.Height - rawPosition.Y); //flip Y - uses lower-left as origin and IsFlipped does not apply here

			var properties = new PointerPointProperties()
			{
				IsInRange = true,
				IsPrimary = true,
				IsLeftButtonPressed = ((int)NSEvent.CurrentPressedMouseButtons & LeftMouseButtonMask) == LeftMouseButtonMask,
				IsRightButtonPressed = ((int)NSEvent.CurrentPressedMouseButtons & RightMouseButtonMask) == RightMouseButtonMask,
			};

			if (Pointer.PointerDeviceType == PointerDeviceType.Pen)
			{
				properties.XTilt = (float)_nativeEvent.Tilt.X;
				properties.YTilt = (float)_nativeEvent.Tilt.Y;
				properties.Pressure = (float)_nativeEvent.Pressure;
			}

			if (_nativeEvent.Type == NSEventType.ScrollWheel)
			{
				var y = (int)_nativeEvent.ScrollingDeltaY;
				if (y == 0)
				{
					// Note: if X and Y are != 0, we should raise 2 events!
					properties.IsHorizontalMouseWheel = true;
					properties.MouseWheelDelta = (int)_nativeEvent.ScrollingDeltaX;
				}
				else
				{
					properties.MouseWheelDelta = -y;
				}
			}

			return new PointerPoint(
				FrameId,
				ToTimeStamp(_nativeEvent.Timestamp),
				device,
				Pointer.PointerId,
				rawPosition,
				position,
				Pointer.IsInContact,
				properties);
		}

		#region Misc static helpers
		private static long? _bootTime;

		private static bool GetIsInContact(NSEvent nativeEvent)
		{
			return
				nativeEvent.Type == NSEventType.LeftMouseDown ||
				nativeEvent.Type == NSEventType.LeftMouseDragged ||
				nativeEvent.Type == NSEventType.RightMouseDown ||
				nativeEvent.Type == NSEventType.RightMouseDragged ||
				nativeEvent.Type == NSEventType.OtherMouseDown ||
				nativeEvent.Type == NSEventType.OtherMouseDragged;
		}

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

		private VirtualKeyModifiers GetVirtualKeyModifiers(NSEvent nativeEvent)
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

			if(nativeEvent.ModifierFlags.HasFlag(NSEventModifierMask.CommandKeyMask))
			{
				modifiers |= VirtualKeyModifiers.Windows;
			}

			if (nativeEvent.ModifierFlags.HasFlag(NSEventModifierMask.ControlKeyMask))
			{
				modifiers |= VirtualKeyModifiers.Control;
			}

			return modifiers;
		}

		private static ulong ToTimeStamp(double timestamp)
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
		/// Taken from <see cref="https://github.com/xamarin/xamarin-macios/blob/bc492585d137d8c3d3a2ffc827db3cdaae3cc869/src/AppKit/NSEvent.cs#L127" />
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
		/// Inspiration from <see cref="https://github.com/xamarin/xamarin-macios/blob/bc492585d137d8c3d3a2ffc827db3cdaae3cc869/src/AppKit/NSEvent.cs#L148"/>
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
	}
}
