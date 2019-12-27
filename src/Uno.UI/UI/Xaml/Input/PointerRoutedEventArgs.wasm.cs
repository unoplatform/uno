using System;
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

		internal PointerRoutedEventArgs(
			double timestamp,
			uint pointerId,
			PointerDeviceType pointerType,
			Point absolutePosition,
			bool isInContact,
			WindowManagerInterop.HtmlPointerButtonsState buttons,
			WindowManagerInterop.HtmlPointerButtonUpdate buttonUpdate,
			VirtualKeyModifiers keys,
			UIElement source,
			bool canBubbleNatively)
			: this()
		{
			_timestamp = timestamp;
			_absolutePosition = absolutePosition;
			_buttons = buttons;
			_buttonUpdate = buttonUpdate;

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
			if (_buttons.HasFlag(WindowManagerInterop.HtmlPointerButtonsState.Right))
			{
				switch (Pointer.PointerDeviceType)
				{
					case PointerDeviceType.Mouse:
						props.IsMiddleButtonPressed = true;
						break;
					case PointerDeviceType.Pen:
						props.IsBarrelButtonPressed = true;
						break;
				}
			}
			props.IsXButton1Pressed = _buttons.HasFlag(WindowManagerInterop.HtmlPointerButtonsState.X1);
			props.IsXButton2Pressed = _buttons.HasFlag(WindowManagerInterop.HtmlPointerButtonsState.X2);
			props.IsEraser = _buttons.HasFlag(WindowManagerInterop.HtmlPointerButtonsState.Eraser);

			props.PointerUpdateKind = ToUpdateKind(_buttonUpdate, props);

			return props;
		}

		#region Misc static helpers
		private static ulong? _bootTime;

		private static ulong ToTimeStamp(double timestamp)
		{
			if (!_bootTime.HasValue)
			{
				_bootTime = (ulong) (double.Parse(WebAssemblyRuntime.InvokeJS("Date.now() - performance.now()")) * TimeSpan.TicksPerMillisecond);
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
