using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Windows.Devices.Input;
using AppKit;
using Windows.System;
using Foundation;
using Windows.UI.Input;

namespace Windows.UI.Xaml.Input
{
	partial class PointerRoutedEventArgs
	{
		private const int LeftMouseButtonMask = 1;
		private const int RightMouseButtonMask = 2;

		private readonly NSEvent _nativeEvent;
		private readonly NSSet _nativeTouches;

		internal PointerRoutedEventArgs(NSSet touches, NSEvent nativeEvent, UIElement source) : this()
		{
			_nativeEvent = nativeEvent;
			_nativeTouches = touches;

			var pointerId = (uint)0;
			var pointerDeviceType = GetPointerDeviceType(nativeEvent.Type);

			var isInContact = GetIsInContact(nativeEvent);

			FrameId = ToFrameId(_nativeEvent.Timestamp);
			Pointer = new Pointer(pointerId, pointerDeviceType, isInContact, isInRange: true);
			KeyModifiers = GetVirtualKeyModifiers(nativeEvent);
			OriginalSource = source;
			CanBubbleNatively = true;
		}

		public PointerPoint GetCurrentPoint(UIElement relativeTo)
		{
			var device = PointerDevice.For(PointerDeviceType.Mouse);
			var rawPosition = _nativeEvent.LocationInWindow;
			var position = relativeTo != null ?
				relativeTo.ConvertPointFromView(_nativeEvent.LocationInWindow, null) :
				rawPosition;
			
			var properties = new PointerPointProperties()
			{
				IsInRange = true,
				IsPrimary = true,
				IsLeftButtonPressed = ((int)NSEvent.CurrentPressedMouseButtons & LeftMouseButtonMask) == LeftMouseButtonMask,
				IsRightButtonPressed = ((int)NSEvent.CurrentPressedMouseButtons & RightMouseButtonMask) == RightMouseButtonMask
			};

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

		private static PointerDeviceType GetPointerDeviceType(NSEventType eventType)
		{
			switch (eventType)
			{
				case AppKit.NSEventType.DirectTouch:
					return PointerDeviceType.Touch;
				case AppKit.NSEventType.TabletPoint:
				case AppKit.NSEventType.TabletProximity:
					return PointerDeviceType.Pen;
				default:
					return PointerDeviceType.Mouse;
			}
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
		#endregion
	}
}
