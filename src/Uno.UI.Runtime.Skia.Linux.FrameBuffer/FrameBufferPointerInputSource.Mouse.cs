// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// Base interactions with libinput derived from https://github.com/AvaloniaUI/Avalonia

#nullable enable

using System;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Xaml.Controls;
using Uno.UI.Runtime.Skia.Native;
using static Uno.UI.Runtime.Skia.Native.LibInput;
using static Windows.UI.Input.PointerUpdateKind;
using static Uno.UI.Runtime.Skia.Native.libinput_event_type;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia;

unsafe internal partial class FrameBufferPointerInputSource
{
	private Point _mousePosition;

	public void ProcessMouseEvent(IntPtr rawEvent, libinput_event_type type)
	{
		var rawPointerEvent = libinput_event_get_pointer_event(rawEvent);

		var timestamp = libinput_event_pointer_get_time_usec(rawPointerEvent);
		var properties = new PointerPointProperties();
		Action<PointerEventArgs>? raisePointerEvent = null;

		if (type == LIBINPUT_EVENT_POINTER_MOTION_ABSOLUTE)
		{
			_mousePosition = new Point(
				x: libinput_event_pointer_get_absolute_x_transformed(rawPointerEvent, (int)_displayInformation.ScreenWidthInRawPixels),
				y: libinput_event_pointer_get_absolute_y_transformed(rawPointerEvent, (int)_displayInformation.ScreenHeightInRawPixels));

			raisePointerEvent = RaisePointerMoved;
		}
		else if (type == LIBINPUT_EVENT_POINTER_AXIS)
		{
			double GetAxisValue(libinput_pointer_axis axis)
			{
				var source = libinput_event_pointer_get_axis_source(rawPointerEvent);
				return source == libinput_pointer_axis_source.Wheel
					? libinput_event_pointer_get_axis_value_discrete(rawPointerEvent, axis)
					: libinput_event_pointer_get_axis_value(rawPointerEvent, axis);
			}

			if (libinput_event_pointer_has_axis(rawPointerEvent, libinput_pointer_axis.ScrollHorizontal) != 0)
			{
				properties.IsHorizontalMouseWheel = true;
				properties.MouseWheelDelta = (int)(GetAxisValue(libinput_pointer_axis.ScrollHorizontal) * ScrollContentPresenter.ScrollViewerDefaultMouseWheelDelta);
				raisePointerEvent = RaisePointerWheelChanged;
			}
			else if (libinput_event_pointer_has_axis(rawPointerEvent, libinput_pointer_axis.ScrollVertical) != 0)
			{
				properties.IsHorizontalMouseWheel = false;
				properties.MouseWheelDelta = (int)(GetAxisValue(libinput_pointer_axis.ScrollVertical) * ScrollContentPresenter.ScrollViewerDefaultMouseWheelDelta);
				raisePointerEvent = RaisePointerWheelChanged;
			}
		}
		else if (type == LIBINPUT_EVENT_POINTER_BUTTON)
		{
			var button = libinput_event_pointer_get_button(rawPointerEvent);
			var buttonState = libinput_event_pointer_get_button_state(rawPointerEvent);

			if (buttonState == libinput_button_state.Pressed)
			{
				_pointerPressed.Add(button);

				properties.PointerUpdateKind = button switch
				{
					libinput_event_code.BTN_LEFT => LeftButtonPressed,
					libinput_event_code.BTN_MIDDLE => MiddleButtonPressed,
					libinput_event_code.BTN_RIGHT => RightButtonPressed,
					_ => Other
				};

				raisePointerEvent = RaisePointerPressed;
			}
			else
			{
				_pointerPressed.Remove(button);

				properties.PointerUpdateKind = button switch
				{
					libinput_event_code.BTN_LEFT => LeftButtonReleased,
					libinput_event_code.BTN_MIDDLE => MiddleButtonReleased,
					libinput_event_code.BTN_RIGHT => RightButtonReleased,
					_ => Other
				};

				raisePointerEvent = RaisePointerReleased;
			}
		}

		properties.IsLeftButtonPressed = _pointerPressed.Contains(libinput_event_code.BTN_LEFT);
		properties.IsMiddleButtonPressed = _pointerPressed.Contains(libinput_event_code.BTN_MIDDLE);
		properties.IsRightButtonPressed = _pointerPressed.Contains(libinput_event_code.BTN_RIGHT);

		var pointerPoint = new Windows.UI.Input.PointerPoint(
			frameId: (uint)timestamp, // UNO TODO: How should set the frame, timestamp may overflow.
			timestamp: timestamp * TimeSpan.TicksPerMicrosecond,
			device: PointerDevice.For(PointerDeviceType.Mouse),
			pointerId: 0,
			rawPosition: _mousePosition,
			position: _mousePosition,
			isInContact: properties.HasPressedButton,
			properties: properties
		);

		if (raisePointerEvent != null)
		{
			var args = new PointerEventArgs(pointerPoint, GetCurrentModifiersState());

			RaisePointerEvent(raisePointerEvent, args);
		}
		else
		{
			this.Log().LogWarning($"Pointer event type {type} was not handled");
		}
	}
}
