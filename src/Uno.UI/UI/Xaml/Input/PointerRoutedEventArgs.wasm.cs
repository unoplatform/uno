using System;
using System.Globalization;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Input;
using Uno.Foundation;
using Uno.UI.Xaml;

namespace Windows.UI.Xaml.Input
{
	partial class PointerRoutedEventArgs
	{
		private readonly double _timestamp;
		private readonly Point _absolutePosition;
		private readonly WindowManagerInterop.HtmlPointerButtonsState _buttons;
		private readonly WindowManagerInterop.HtmlPointerButtonUpdate _buttonUpdate;
		private readonly double _pressure;

		internal PointerRoutedEventArgs(
			double timestamp,
			uint pointerId,
			PointerDeviceType pointerType,
			Point absolutePosition,
			bool isInContact,
			WindowManagerInterop.HtmlPointerButtonsState buttons,
			WindowManagerInterop.HtmlPointerButtonUpdate buttonUpdate,
			VirtualKeyModifiers keys,
			double pressure,
			UIElement source,
			bool canBubbleNatively)
			: this()
		{
			_timestamp = timestamp;
			_absolutePosition = absolutePosition;
			_buttons = buttons;
			_buttonUpdate = buttonUpdate;
			_pressure = pressure;

			FrameId = ToFrameId(timestamp);
			Pointer = new Pointer(pointerId, pointerType, isInContact, isInRange: true);
			KeyModifiers = keys;
			OriginalSource = source;
			CanBubbleNatively = canBubbleNatively;
		}

		public PointerPoint GetCurrentPoint(UIElement relativeTo)
		{
			var timestamp = ToTimeStamp(_timestamp);
			var device = PointerDevice.For(Pointer.PointerDeviceType);
			var rawPosition = _absolutePosition;
			var position = relativeTo == null
				? rawPosition
				: relativeTo.TransformToVisual(null).Inverse.TransformPoint(_absolutePosition);
			var properties = GetProperties();

			return new PointerPoint(FrameId, timestamp, device, Pointer.PointerId, rawPosition, position, Pointer.IsInContact, properties);
		}

		private PointerPointProperties GetProperties()
		{
			var props = new PointerPointProperties()
			{
				IsPrimary = true,
				IsInRange = Pointer.IsInRange,
			};

			props.IsLeftButtonPressed = _buttons.HasFlag(WindowManagerInterop.HtmlPointerButtonsState.Left);
			props.IsMiddleButtonPressed = _buttons.HasFlag(WindowManagerInterop.HtmlPointerButtonsState.Middle);
			props.IsRightButtonPressed = _buttons.HasFlag(WindowManagerInterop.HtmlPointerButtonsState.Right);
			props.IsXButton1Pressed = _buttons.HasFlag(WindowManagerInterop.HtmlPointerButtonsState.X1);
			props.IsXButton2Pressed = _buttons.HasFlag(WindowManagerInterop.HtmlPointerButtonsState.X2);
			props.IsEraser = _buttons.HasFlag(WindowManagerInterop.HtmlPointerButtonsState.Eraser);

			switch (Pointer.PointerDeviceType)
			{
				// For touch and mouse, we keep the default pressure of .5, as WinUI

				case PointerDeviceType.Pen:
					// !!! WARNING !!! Here we have a slight different behavior compared to WinUI:
					// On WinUI we will get IsRightButtonPressed (with IsBarrelButtonPressed) only if the user is pressing
					// the barrel button when pen goes "in contact" (i.e. touches the screen), otherwise we will get
					// IsLeftButtonPressed and IsBarrelButtonPressed.
					// Here we set IsRightButtonPressed as soon as the barrel button was pressed, no matter
					// if the pen was already in contact or not.
					// This is acceptable since the UIElement pressed state is **per pointer** (not buttons of pointer)
					// and GestureRecognizer always checks that pressed buttons didn't changed for a single gesture.
					props.IsBarrelButtonPressed = props.IsRightButtonPressed;
					props.Pressure = (float)_pressure;
					break;
			}

			props.PointerUpdateKind = ToUpdateKind(_buttonUpdate, props);

			return props;
		}

		#region Misc static helpers
		private static ulong? _bootTime;

		private static ulong ToTimeStamp(double timestamp)
		{
			if (!_bootTime.HasValue)
			{
				_bootTime = (ulong)(double.Parse(WebAssemblyRuntime.InvokeJS("Date.now() - performance.now()"), CultureInfo.InvariantCulture) * TimeSpan.TicksPerMillisecond);
			}

			return _bootTime.Value + (ulong)(timestamp * TimeSpan.TicksPerMillisecond);
		}

		private static uint ToFrameId(double timestamp)
		{
			// Known limitation: After 49 days, we will overflow the uint and frame IDs will restart at 0.
			return (uint)(timestamp % uint.MaxValue);
		}

		private static PointerUpdateKind ToUpdateKind(WindowManagerInterop.HtmlPointerButtonUpdate update, PointerPointProperties props)
		{
			switch (update)
			{
				case WindowManagerInterop.HtmlPointerButtonUpdate.Left when props.IsLeftButtonPressed: return PointerUpdateKind.LeftButtonPressed;
				case WindowManagerInterop.HtmlPointerButtonUpdate.Left: return PointerUpdateKind.LeftButtonReleased;
				case WindowManagerInterop.HtmlPointerButtonUpdate.Middle when props.IsMiddleButtonPressed: return PointerUpdateKind.MiddleButtonPressed;
				case WindowManagerInterop.HtmlPointerButtonUpdate.Middle: return PointerUpdateKind.MiddleButtonReleased;
				case WindowManagerInterop.HtmlPointerButtonUpdate.Right when props.IsRightButtonPressed: return PointerUpdateKind.RightButtonPressed;
				case WindowManagerInterop.HtmlPointerButtonUpdate.Right: return PointerUpdateKind.RightButtonReleased;
				case WindowManagerInterop.HtmlPointerButtonUpdate.X1 when props.IsXButton1Pressed: return PointerUpdateKind.XButton1Pressed;
				case WindowManagerInterop.HtmlPointerButtonUpdate.X1: return PointerUpdateKind.XButton1Released;
				case WindowManagerInterop.HtmlPointerButtonUpdate.X2 when props.IsXButton2Pressed: return PointerUpdateKind.XButton1Pressed;
				case WindowManagerInterop.HtmlPointerButtonUpdate.X2: return PointerUpdateKind.XButton1Released;
				default: return PointerUpdateKind.Other;
			}
		}
		#endregion
	}
}
