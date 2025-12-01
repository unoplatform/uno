// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// See the LICENSE file in the project root for more information.
//
// Base interactions with libinput derived from https://github.com/AvaloniaUI/Avalonia

#nullable enable

using System;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Input;
using Uno.UI.Runtime.Skia.Native;
using static Uno.UI.Runtime.Skia.Native.LibInput;
using static Windows.UI.Input.PointerUpdateKind;
using static Uno.UI.Runtime.Skia.Native.libinput_event_type;
using Uno.Foundation.Logging;
using System.Collections.Generic;
using Windows.Graphics.Display;
using Uno.WinUI.Runtime.Skia.Linux.FrameBuffer.UI;

namespace Uno.UI.Runtime.Skia;

unsafe internal partial class FrameBufferPointerInputSource
{
	private readonly Dictionary<uint, Point> _activePointers = new();
	private readonly HashSet<libinput_event_code> _pointerPressed = new();

	public void ProcessTouchEvent(IntPtr rawEvent, libinput_event_type rawEventType)
	{
		var rawTouchEvent = libinput_event_get_touch_event(rawEvent);

		if (rawTouchEvent != IntPtr.Zero
			&& rawEventType < LIBINPUT_EVENT_TOUCH_FRAME)
		{
			var properties = new PointerPointProperties();
			var timestamp = libinput_event_touch_get_time_usec(rawTouchEvent);
			var pointerId = (uint)libinput_event_touch_get_slot(rawTouchEvent);
			Action<PointerEventArgs>? raisePointerEvent = null;
			Point currentPosition;

			if (rawEventType == LIBINPUT_EVENT_TOUCH_DOWN
				|| rawEventType == LIBINPUT_EVENT_TOUCH_MOTION)
			{
				double x, y;
				switch (FrameBufferWindowWrapper.Instance.Orientation)
				{
					case DisplayOrientations.None:
					case DisplayOrientations.Landscape:
						x = libinput_event_touch_get_x_transformed(rawTouchEvent, (int)FrameBufferWindowWrapper.Instance.Bounds.Width);
						y = libinput_event_touch_get_y_transformed(rawTouchEvent, (int)FrameBufferWindowWrapper.Instance.Bounds.Height);
						break;
					case DisplayOrientations.Portrait:
						y = FrameBufferWindowWrapper.Instance.Bounds.Height - libinput_event_touch_get_x_transformed(rawTouchEvent, (int)FrameBufferWindowWrapper.Instance.Bounds.Height);
						x = libinput_event_touch_get_y_transformed(rawTouchEvent, (int)FrameBufferWindowWrapper.Instance.Bounds.Width);
						break;
					case DisplayOrientations.LandscapeFlipped:
						x = libinput_event_touch_get_x_transformed(rawTouchEvent, (int)FrameBufferWindowWrapper.Instance.Bounds.Width);
						y = FrameBufferWindowWrapper.Instance.Bounds.Height - libinput_event_touch_get_y_transformed(rawTouchEvent, (int)FrameBufferWindowWrapper.Instance.Bounds.Height);
						break;
					case DisplayOrientations.PortraitFlipped:
						y = libinput_event_touch_get_x_transformed(rawTouchEvent, (int)FrameBufferWindowWrapper.Instance.Bounds.Height);
						x = FrameBufferWindowWrapper.Instance.Bounds.Width - libinput_event_touch_get_y_transformed(rawTouchEvent, (int)FrameBufferWindowWrapper.Instance.Bounds.Width);
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
				currentPosition = new Point(x, y);
				_activePointers[pointerId] = currentPosition;
			}
			else
			{
				_activePointers.TryGetValue(pointerId, out currentPosition);
				_activePointers.Remove(pointerId);
			}

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"ProcessTouchEvent: {rawEventType}, pointerId:{pointerId}, currentPosition:{currentPosition}, timestamp:{timestamp}");
			}

			switch (rawEventType)
			{
				case LIBINPUT_EVENT_TOUCH_MOTION:
					raisePointerEvent = RaisePointerMoved;
					break;

				case LIBINPUT_EVENT_TOUCH_DOWN:
					properties.PointerUpdateKind = LeftButtonPressed;
					raisePointerEvent = RaisePointerPressed;
					break;

				case LIBINPUT_EVENT_TOUCH_UP:
					properties.PointerUpdateKind = LeftButtonReleased;
					raisePointerEvent = RaisePointerReleased;
					break;

				case LIBINPUT_EVENT_TOUCH_CANCEL:
					properties.PointerUpdateKind = LeftButtonReleased;
					raisePointerEvent = RaisePointerCancelled;
					break;
			}

			properties.IsLeftButtonPressed = rawEventType != LIBINPUT_EVENT_TOUCH_UP && rawEventType != LIBINPUT_EVENT_TOUCH_CANCEL;

			var timestampInMicroseconds = timestamp;
			var pointerPoint = new Windows.UI.Input.PointerPoint(
				frameId: (uint)timestamp, // UNO TODO: How should set the frame, timestamp may overflow.
				timestamp: timestampInMicroseconds,
				device: PointerDevice.For(PointerDeviceType.Touch),
				pointerId: pointerId,
				rawPosition: currentPosition,
				position: currentPosition,
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
				this.Log().LogWarning($"Touch event type {rawEventType} was not handled");
			}
		}
	}
}
