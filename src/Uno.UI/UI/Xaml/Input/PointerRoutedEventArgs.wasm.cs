using System;
using System.Globalization;
using Windows.Foundation;
using Windows.System;
using Uno.Foundation;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Input;

using PointerIdentifier = Windows.Devices.Input.PointerIdentifier; // internal type (should be in Uno namespace)
#if HAS_UNO_WINUI
using Microsoft.UI.Input;
#else
using Windows.Devices.Input;
using Windows.UI.Input;
#endif

namespace Windows.UI.Xaml.Input
{
	partial class PointerRoutedEventArgs : IHtmlHandleableRoutedEventArgs
	{
		private readonly double _timestamp;
		private readonly Point _absolutePosition;
		private readonly WindowManagerInterop.HtmlPointerButtonsState _buttons;
		private readonly WindowManagerInterop.HtmlPointerButtonUpdate _buttonUpdate;
		private readonly double _pressure;
		private readonly (bool isHorizontalWheel, double delta) _wheel;

		internal PointerRoutedEventArgs(
			double timestamp,
			PointerIdentifier pointerUniqueId,
			Point absolutePosition,
			bool isInContact,
			bool isInRange,
			WindowManagerInterop.HtmlPointerButtonsState buttons,
			WindowManagerInterop.HtmlPointerButtonUpdate buttonUpdate,
			VirtualKeyModifiers keys,
			double pressure,
			(bool isHorizontalWheel, double delta) wheel,
			UIElement source)
			: this()
		{
			_timestamp = timestamp;
			_absolutePosition = absolutePosition;
			_buttons = buttons;
			_buttonUpdate = buttonUpdate;
			_pressure = pressure;
			_wheel = wheel;

			FrameId = ToFrameId(timestamp);
			Pointer = new Pointer(pointerUniqueId, isInContact, isInRange);
			KeyModifiers = keys;
			OriginalSource = source;
		}

		/// <inheritdoc />
		/// <remarks>Default value for pointers is <see cref="HtmlEventDispatchResult.StopPropagation"/>.</remarks>
		HtmlEventDispatchResult IHtmlHandleableRoutedEventArgs.HandledResult { get; set; } = HtmlEventDispatchResult.StopPropagation;

		public PointerPoint GetCurrentPoint(UIElement relativeTo)
		{
			var timestamp = ToTimeStamp(_timestamp);
			var device = global::Windows.Devices.Input.PointerDevice.For((global::Windows.Devices.Input.PointerDeviceType)Pointer.PointerDeviceType);
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

			props.IsHorizontalMouseWheel = _wheel.isHorizontalWheel;
			props.MouseWheelDelta = (int)_wheel.delta;

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
			_bootTime ??= (ulong)(WindowManagerInterop.GetBootTime() * TimeSpan.TicksPerMillisecond);

			return _bootTime.Value + (ulong)(timestamp * TimeSpan.TicksPerMillisecond);
		}

		internal static uint ToFrameId(double timestamp)
			// Known limitation: After 49 days, we will overflow the uint and frame IDs will restart at 0.
			=> (uint)(timestamp % uint.MaxValue);

		internal static Point ToRelativePosition(Point absolutePosition, UIElement relativeTo)
			=> relativeTo == null
				? absolutePosition
				: relativeTo.TransformToVisual(null).Inverse.TransformPoint(absolutePosition);

		private static PointerUpdateKind ToUpdateKind(WindowManagerInterop.HtmlPointerButtonUpdate update, PointerPointProperties props)
			=> update switch
			{
				WindowManagerInterop.HtmlPointerButtonUpdate.Left when props.IsLeftButtonPressed => PointerUpdateKind.LeftButtonPressed,
				WindowManagerInterop.HtmlPointerButtonUpdate.Left => PointerUpdateKind.LeftButtonReleased,
				WindowManagerInterop.HtmlPointerButtonUpdate.Middle when props.IsMiddleButtonPressed => PointerUpdateKind.MiddleButtonPressed,
				WindowManagerInterop.HtmlPointerButtonUpdate.Middle => PointerUpdateKind.MiddleButtonReleased,
				WindowManagerInterop.HtmlPointerButtonUpdate.Right when props.IsRightButtonPressed => PointerUpdateKind.RightButtonPressed,
				WindowManagerInterop.HtmlPointerButtonUpdate.Right => PointerUpdateKind.RightButtonReleased,
				WindowManagerInterop.HtmlPointerButtonUpdate.X1 when props.IsXButton1Pressed => PointerUpdateKind.XButton1Pressed,
				WindowManagerInterop.HtmlPointerButtonUpdate.X1 => PointerUpdateKind.XButton1Released,
				WindowManagerInterop.HtmlPointerButtonUpdate.X2 when props.IsXButton2Pressed => PointerUpdateKind.XButton1Pressed,
				WindowManagerInterop.HtmlPointerButtonUpdate.X2 => PointerUpdateKind.XButton1Released,
				_ => PointerUpdateKind.Other
			};
		#endregion
	}
}
