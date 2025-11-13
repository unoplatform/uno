// Copyright 1996-1997 by Frederic Lepied, France. <Frederic.Lepied@sugix.frmug.org>
//
// Permission to use, copy, modify, distribute, and sell this software and its
// documentation for any purpose is  hereby granted without fee, provided that
// the  above copyright   notice appear  in   all  copies and  that both  that
// copyright  notice   and   this  permission   notice  appear  in  supporting
// documentation, and that   the  name of  the authors   not  be  used  in
// advertising or publicity pertaining to distribution of the software without
// specific,  written      prior  permission.     The authors  make  no
// representations about the suitability of this software for any purpose.  It
// is provided "as is" without express or implied warranty.
//
// THE AUTHORS  DISCLAIMS ALL   WARRANTIES WITH REGARD  TO  THIS SOFTWARE,
// INCLUDING ALL IMPLIED   WARRANTIES OF MERCHANTABILITY  AND   FITNESS, IN NO
// EVENT  SHALL THE AUTHORS  BE   LIABLE   FOR ANY  SPECIAL, INDIRECT   OR
// CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF USE,
// DATA  OR PROFITS, WHETHER  IN  AN ACTION OF  CONTRACT,  NEGLIGENCE OR OTHER
// TORTIOUS  ACTION, ARISING    OUT OF OR   IN  CONNECTION  WITH THE USE    OR
// PERFORMANCE OF THIS SOFTWARE.
//
// Copyright © 2007 Peter Hutterer
// Copyright © 2009 Red Hat, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice (including the next
// paragraph) shall be included in all copies or substantial portions of the
// Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT SHALL
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

// The MIT License (MIT)
//
// Copyright (c) .NET Foundation and Contributors
// All Rights Reserved
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

// https://gitlab.freedesktop.org/xorg/app/xinput/-/blob/f550dfbb347ec62ca59e86f79a9e4fe43417ab39/src/xinput.c
// https://gitlab.freedesktop.org/xorg/app/xinput/-/blob/f550dfbb347ec62ca59e86f79a9e4fe43417ab39/src/test_xi2.c
// https://github.com/AvaloniaUI/Avalonia/blob/e0127c610c38701c3af34f580273f6efd78285b5/src/Avalonia.X11/XI2Manager.cs

using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.Collections;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;
using PointerEventArgs = Windows.UI.Core.PointerEventArgs;
namespace Uno.WinUI.Runtime.Skia.X11;

// Thanks to the amazing Peter Hutterer and Martin Kepplinger for creating evemu recordings
// for touchscreens
// https://github.com/whot/evemu-devices

internal partial class X11PointerInputSource
{
	private const int BitsPerByte = 8;

	// These are only written and read inside HandleXI2(), so no synchronization needed.
	private readonly Dictionary<int, Dictionary<int, double>> _valuatorValues = new(); // device id -> valuator number -> value
	private readonly Dictionary<int, DeviceInfo> _deviceInfoCache = new(); // device id -> device info

	// Excerpt from https://www.x.org/releases/X11R7.7/doc/inputproto/XI2proto.txt
	// [[multitouch-device-modes]]
	// Touch device modes
	// ~~~~~~~~~~~~~~~~~~
	//
	// Touch devices come in many different forms with varying capabilities. The
	// following device modes are defined for this protocol:
	//
	// 'DirectTouch':
	// These devices map their input region to a subset of the screen region. Touch
	// events are delivered to window at the location of the touch. "direct"
	// here refers to the user manipulating objects at their screen location.
	// An example of a DirectTouch device is a touchscreen.
	//
	// 'DependentTouch':
	// These devices do not have a direct correlation between a touch location and
	// a position on the screen. Touch events are delivered according to the
	// location of the device's cursor and often need to be interpreted
	// relative to the current position of that cursor. Such interactions are
	// usually the result of a gesture performed on the device, rather than
	// direct manipulation. An example of a DependentTouch device is a
	// trackpad.
	//
	// A device is identified as only one of the device modes above at any time, and
	// the touch mode may change at any time. If a device's touch mode changes, an
	// XIDeviceChangedEvent is generated.
	//
	// [[multitouch-processing]]
	// Touch event delivery
	// ~~~~~~~~~~~~~~~~~~~~
	//
	// For direct touch devices, the window set for event propagation is the set of
	// windows from the root window to the topmost window lying at the co-ordinates
	// of the touch.
	//
	// For dependent devices, the window set for event propagation is the set of
	// windows from the root window to the window that contains the device's
	// pointer. A dependent device may only have one window set at a time, for all
	// touches. Any future touch sequence will use the same window set. The window set
	// is cleared when all touch sequences on the device end.
	//
	// A window set is calculated on TouchBegin and remains constant until the end
	// of the sequence. Modifications to the window hierarchy, new grabs or changed
	// event selection do not affect the window set.
	//
	// Pointer control of dependent devices
	// ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
	// On a dependent device, the device may differ between a pointer-controlling
	// touch and a non-pointer-controlling touch. For example, on a touchpad the
	// first touch is pointer-controlling (i.e. serves only to move the visible
	// pointer). Multi-finger gestures on a touchpad cause all touches to be
	// non-pointer-controlling.
	//
	// For pointer-controlling touches, no touch events are sent; the touch
	// generates regular pointer events instead. Non-pointer-controlling touches
	// send touch events. A touch may change from pointer-controlling to
	// non-pointer-controlling, or vice versa.
	//
	// - If a touch changes from pointer-controlling to non-pointer-controlling,
	// a new touch ID is assigned and a TouchBegin is sent for the last known
	// position of the touch. Further events are sent as TouchUpdate events, or as
	// TouchEnd event if the touch terminates.
	//
	// - If a touch changes from non-pointer-controlling to pointer-controlling, a
	// TouchEnd is sent for that touch at the last known position of the touch.
	// Further events are sent as pointer events.
	//
	// The conditions to switch from pointer-controlling to non-pointer-controlling
	// touch is implementation-dependent. A device may support touches that are
	// both pointer-controlling and a touch event.
	//
	// In the dependent touch example event sequence below, touches are marked when
	// switching to pointer-controlling (pc) or to non-pointer-controlling (np).
	//
	// .Dependent touch example event sequence on a touchpad
	// [width="50%", options="header"]
	// |====================================================
	// | Finger 1 | Finger 2 | Event generated(touchid)
	// |  down    |          | Motion
	// |  move    |          | Motion
	// |  move    |          | Motion
	// |  (np)    |   down   | TouchBegin(0), TouchBegin(1)
	// |  move    |    --    | TouchUpdate(0)
	// |   --     |   move   | TouchUpdate(1)
	// |   up     |   (pc)   | TouchEnd(0), TouchEnd(1)
	// |          |   move   | Motion
	// |  down    |   (np)   | TouchBegin(2), TouchBegin(3)
	// |  move    |    --    | TouchUpdate(2)
	// |   up     |   (pc)   | TouchEnd(2), TouchEnd(3)
	// |          |    up    | Motion
	// |  down    |          | Motion
	// |  (np)    |   down   | TouchBegin(4), TouchBegin(5)
	// |  (pc)    |    up    | TouchEnd(4), TouchEnd(5)
	// |  move    |          | Motion
	// |   up     |          | Motion
	private unsafe PointerEventArgs CreatePointerEventArgsFromDeviceEvent(IntPtr display, XIDeviceEvent data)
	{
		(double wheelDelta, bool isHorizontalMouseWheel) = (0, false);
		if (data.evtype is XiEventType.XI_ButtonPress or XiEventType.XI_ButtonRelease)
		{
			// Check the similar implementation for scrolling using the core protocol for more
			// information on what this is.
			// Unlike the equivalent core protocol events, this only works for an actual mouse with
			// a real wheel. A touchpad does not behave the same way.
			(wheelDelta, isHorizontalMouseWheel) = data.detail switch
			{
				1 << SCROLL_RIGHT => (-ScrollContentPresenter.ScrollViewerDefaultMouseWheelDelta, true),
				1 << SCROLL_DOWN => (-ScrollContentPresenter.ScrollViewerDefaultMouseWheelDelta, false),
				1 << SCROLL_LEFT => (ScrollContentPresenter.ScrollViewerDefaultMouseWheelDelta, true),
				1 << SCROLL_UP => (ScrollContentPresenter.ScrollViewerDefaultMouseWheelDelta, false),
				_ => (0, false)
			};
		}

		var info = GetDevicePropertiesFromId(display, data.sourceid);

		// Touchpad scrolling manifests in a Motion event where the current scrolling "position" is recorded in the
		// valuator. There is no direct way to get the delta, so we have to record the old position and diff it
		// with the new position. This also means that the first "tick" will not result in a WheelChanged event,
		// but will only be used to set the initial wheel position.
		// Also, we can get a "diagonal scroll" where both the horizontal and the vertical positions change.
		// Our PointerEventArgs don't support this, so in that case, we arbitrarily choose the direction to
		// be the direction of the last scroller class to be present in data.valuators.
		// IMPORTANT: DO NOT FORGET TO RESET POSITIONS ON LEAVE.
		if (data.evtype is XiEventType.XI_Motion && info is { } info_)
		{
			var maskLen = data.valuators.MaskLen * BitsPerByte;
			var valuatorMask = data.valuators.Mask;
			var values = data.valuators.Values;
			for (var i = 0; i < maskLen; i++)
			{
				if (XLib.XIMaskIsSet(valuatorMask, i))
				{
					var value = *(values++);
					// Update valuator
					if (!_valuatorValues.TryGetValue(data.sourceid, out var valuatorsDict))
					{
						valuatorsDict = _valuatorValues[data.sourceid] = new Dictionary<int, double>();
					}
					var oldValueExisted = valuatorsDict.TryGetValue(i, out var oldValue);
					valuatorsDict[i] = value;

					// Act on new value
					if (oldValueExisted && info_.Scrollers.TryGetValue(i, out var scrollInfo))
					{
						isHorizontalMouseWheel = scrollInfo.ScrollType == XiScrollType.Horizontal;
						wheelDelta = (oldValue - value) * ScrollContentPresenter.ScrollViewerDefaultMouseWheelDelta / scrollInfo.Increment;
					}
					// We currently don't support Linux's MT protocol valuators.
					// More at https://www.kernel.org/doc/html/v4.14/input/multi-touch-protocol.html
					// else if (info_.Valuators.TryGetValue(i, out var valuatorInfo) && valuatorInfo.Label == X11Helper.GetAtom(display, "Abs MT Pressure"))
					// {
					// 	pressureValuator = valuatorInfo.Value;
					// }
				}
			}
		}

		var buttonMask = 0;
		for (var i = 0; i < data.buttons.MaskLen; i++) // masklen <= 4
		{
			buttonMask |= data.buttons.Mask[i] << (BitsPerByte * i);
		}

		if (data.evtype is XiEventType.XI_ButtonPress)
		{
			buttonMask |= (1 << data.detail); // the newly pressed button is not added to the mask yet.
		}
		else if (data.evtype is XiEventType.XI_ButtonRelease)
		{
			buttonMask &= ~(1 << data.detail); // the newly released button is not removed from the mask yet.
		}
		else if (data.evtype is XiEventType.XI_TouchBegin or XiEventType.XI_TouchUpdate)
		{
			buttonMask = 1 << data.detail; // touch events only have one button.
		}

		var properties = new PointerPointProperties
		{
			IsLeftButtonPressed = (buttonMask & (1 << LEFT)) != 0 || (data.evtype is XiEventType.XI_TouchBegin or XiEventType.XI_TouchUpdate),
			IsMiddleButtonPressed = (buttonMask & (1 << MIDDLE)) != 0,
			IsRightButtonPressed = (buttonMask & (1 << RIGHT)) != 0,
			IsXButton1Pressed = (buttonMask & (1 << XButton1)) != 0,
			IsXButton2Pressed = (buttonMask & (1 << XButton2)) != 0,
			IsHorizontalMouseWheel = isHorizontalMouseWheel,
			IsInRange = true,
			IsPrimary = true,
			IsTouchPad = info?.IsTouchpad ?? false,
			MouseWheelDelta = (int)Math.Round(wheelDelta),
			PointerUpdateKind = data.detail switch
			{
				LEFT when data.evtype == XiEventType.XI_ButtonPress => PointerUpdateKind.LeftButtonPressed,
				LEFT when data.evtype == XiEventType.XI_ButtonRelease => PointerUpdateKind.LeftButtonReleased,
				RIGHT when data.evtype == XiEventType.XI_ButtonPress => PointerUpdateKind.RightButtonPressed,
				RIGHT when data.evtype == XiEventType.XI_ButtonRelease => PointerUpdateKind.RightButtonReleased,
				MIDDLE when data.evtype == XiEventType.XI_ButtonPress => PointerUpdateKind.MiddleButtonPressed,
				MIDDLE when data.evtype == XiEventType.XI_ButtonRelease => PointerUpdateKind.MiddleButtonReleased,
				XButton1 when data.evtype == XiEventType.XI_ButtonPress => PointerUpdateKind.XButton1Pressed,
				XButton1 when data.evtype == XiEventType.XI_ButtonRelease => PointerUpdateKind.XButton1Released,
				XButton2 when data.evtype == XiEventType.XI_ButtonPress => PointerUpdateKind.XButton2Pressed,
				XButton2 when data.evtype == XiEventType.XI_ButtonRelease => PointerUpdateKind.XButton2Released,
				_ when data.evtype == XiEventType.XI_TouchBegin => PointerUpdateKind.LeftButtonPressed,
				_ when data.evtype == XiEventType.XI_TouchEnd => PointerUpdateKind.LeftButtonReleased,
				_ => PointerUpdateKind.Other
			}
		};

		var scale = ((IXamlRootHost)_host).RootElement?.XamlRoot is { } root
			? XamlRoot.GetDisplayInformation(root).RawPixelsPerViewPixel
			: 1;

		_ = XLib.XTranslateCoordinates(display, data.EventWindow, _host!.TopX11Window.Window, (int)data.event_x, (int)data.event_y, out var dataEventX, out var dataEventY, out _);

		var timeInMicroseconds = (ulong)(data.time * 1000); // Time is given in milliseconds since system boot. See also: https://github.com/unoplatform/uno/issues/14535
		var deviceType = data.evtype is XiEventType.XI_TouchBegin or XiEventType.XI_TouchEnd or XiEventType.XI_TouchUpdate ? PointerDeviceType.Touch : PointerDeviceType.Mouse;
		var pointerId = (uint)(data.evtype is XiEventType.XI_TouchBegin or XiEventType.XI_TouchEnd or XiEventType.XI_TouchUpdate ? data.detail : data.sourceid); // for touch, data.detail is the touch ID
		var point = new PointerPoint(
			frameId: (uint)data.time, // UNO TODO: How should we set the frame, timestamp may overflow.
			timestamp: timeInMicroseconds,
			PointerDevice.For(deviceType),
			pointerId,
			new Point(dataEventX / scale, dataEventY / scale),
			new Point(dataEventX / scale, dataEventY / scale),
			properties.HasPressedButton,
			properties // We don't SetUpdateKind here. We already did that above
		);

		_previousPointerPointProperties = properties;

		var modifiers = X11XamlRootHost.XModifierMaskToVirtualKeyModifiers((XModifierMask)(data.mods.Base & 0xffff));

		return new PointerEventArgs(point, modifiers) { Handled = data.EventWindow != _host.TopX11Window.Window };
	}

	// This method is comment-free. See the very similar (but more involved) implementation of
	// CreatePointerEventArgsFromDeviceEvent for comments.
	private unsafe PointerEventArgs CreatePointerEventArgsFromEnterLeaveEvent(IntPtr display, XIEnterLeaveEvent data, PointerDeviceType pointerType)
	{
		var mask = 0;
		for (var i = 0; i < data.buttons.MaskLen; i++) // masklen <= 4
		{
			mask |= data.buttons.Mask[i] << (BitsPerByte * i);
		}

		// No need to translate coords. We only receive Leave/Enter on the uno window

		var properties = new PointerPointProperties
		{
			IsLeftButtonPressed = (mask & (1 << LEFT)) != 0,
			IsMiddleButtonPressed = (mask & (1 << MIDDLE)) != 0,
			IsRightButtonPressed = (mask & (1 << RIGHT)) != 0,
			IsXButton1Pressed = (mask & (1 << XButton1)) != 0,
			IsXButton2Pressed = (mask & (1 << XButton2)) != 0,
			IsInRange = true,
			IsTouchPad = GetDevicePropertiesFromId(display, data.sourceid)?.IsTouchpad ?? false,
			IsHorizontalMouseWheel = false,
		};

		var scale = ((IXamlRootHost)_host).RootElement?.XamlRoot is { } root
					? XamlRoot.GetDisplayInformation(root).RawPixelsPerViewPixel
					: 1;

		var timestampInMicroseconds = (ulong)(data.time * 1000); // Time is given in milliseconds since system boot. See also: https://github.com/unoplatform/uno/issues/14535
		var point = new PointerPoint(
			frameId: (uint)data.time, // UNO TODO: How should we set the frame, timestamp may overflow.
			timestamp: timestampInMicroseconds,
			PointerDevice.For(PointerDeviceType.Mouse),
			(uint)data.sourceid,
			new Point(data.event_x / scale, data.event_y / scale),
			new Point(data.event_x / scale, data.event_y / scale),
			properties.HasPressedButton,
			properties.SetUpdateKindFromPrevious(_previousPointerPointProperties)
		);

		_previousPointerPointProperties = properties;

		var modifiers = X11XamlRootHost.XModifierMaskToVirtualKeyModifiers((XModifierMask)(data.mods.Base & 0xffff));

		return new PointerEventArgs(point, modifiers) { Handled = data.EventWindow != _host!.TopX11Window.Window };
	}

	// Note about removing devices: the server emits a ButtonRelease if a device is removed
	// while a button is held.
	public void HandleXI2Event(IntPtr display, XEvent ev)
	{
		var evtype = (XiEventType)ev.GenericEventCookie.evtype;

		switch (evtype)
		{
			case XiEventType.XI_Enter:
			case XiEventType.XI_Leave:
				{
					var enterLeaveEvent = ev.GenericEventCookie.GetEvent<XIEnterLeaveEvent>();

					if (this.Log().IsEnabled(LogLevel.Trace))
					{
						this.Log().Trace($"Received {enterLeaveEvent}");
					}

					_valuatorValues.Remove(enterLeaveEvent.sourceid);

					var args = CreatePointerEventArgsFromEnterLeaveEvent(
						display,
						enterLeaveEvent,
						PointerDeviceType.Mouse); // https://www.x.org/releases/X11R7.7/doc/inputproto/XI2proto.txt: Touch events do not generate enter/leave events.

					if (this.Log().IsEnabled(LogLevel.Trace))
					{
						this.Log().Trace($"Created event args: {args}");
					}

					if (evtype is XiEventType.XI_Enter)
					{
						X11XamlRootHost.QueueAction(_host, () => RaisePointerEntered(args));
					}
					else
					{
						X11XamlRootHost.QueueAction(_host, () => RaisePointerExited(args));
					}
				}
				break;
			case XiEventType.XI_Motion:
			case XiEventType.XI_ButtonPress:
			case XiEventType.XI_ButtonRelease:
			case XiEventType.XI_TouchBegin:
			case XiEventType.XI_TouchEnd:
			case XiEventType.XI_TouchUpdate:
				{
					var data = ev.GenericEventCookie.GetEvent<XIDeviceEvent>();
					if (data.deviceid == data.sourceid)
					{
						// The X Server sends 2 events. One for the master device and one for
						// the slave device. We only want the slave device. This happens even for
						// motion events. Here's an example I logged from the xinput utility.
						// EVENT type 4 (ButtonPress)
						// device: 11 (11)
						// time: 15331402
						// detail: 1
						// flags:
						// root: 991.92/597.13
						// event: 131.92/163.13
						// buttons:
						// modifiers: locked 0x10 latched 0 base 0 effective: 0x10
						// group: locked 0 latched 0 base 0 effective: 0
						// valuators:
						// windows: root 0x533 event 0x4c00001 child 0x0
						// EVENT type 4 (ButtonPress)
						// device: 2 (11)
						// time: 15331402
						// detail: 1
						// flags:
						// root: 991.92/597.13
						// event: 131.92/163.13
						// buttons:
						// modifiers: locked 0x10 latched 0 base 0 effective: 0x10
						// group: locked 0 latched 0 base 0 effective: 0
						// valuators:
						// windows: root 0x533 event 0x4c00001 child 0x0
						break;
					}

					// The spec mandates that X servers and devices supporting XI2 touch events and/or smooth scrolling
					// must keep backward compatibility by emulating corresponding core-protocol events for these XI2-specific
					// features. This means emulated ButtonPress/ButtonRelease/Motion events for touch and emulated
					// ButtonPress/ButtonRelease on button 4,5,6 and 7. We make sure to ignore such emulated events.
					// Excerpts from the spec:
					// Smooth scrolling
					// ~~~~~~~~~~~~~~~~
					//
					// Historically, X implemented scrolling events by using button press events:
					// button 4 was one â€œclickâ€ of the scroll wheel upwards, button 5 was downwards,
					// button 6 was one unit of scrolling left, and button 7 was one unit of scrolling
					// right.  This is insufficient for e.g. touchpads which are able to provide
					// scrolling events through multi-finger drag gestures, or simply dragging your
					// finger along a designated strip along the side of the touchpad.
					//
					// Newer X servers may provide scrolling information through valuators to
					// provide clients with more precision than the legacy button events. This
					// scrolling information is part of the valuator data in device events.
					// Scrolling events do not have a specific event type.
					//
					// Valuators for axes sending scrolling information must have one
					// ScrollClass for each scrolling axis. If scrolling valuators are present on a
					// device, the server must provide two-way emulation between these valuators
					// and the legacy button events for each delta unit of scrolling.
					//
					// One unit of scrolling in either direction is considered to be equivalent to
					// one button event, e.g. for a unit size of 1.0, -2.0 on an valuator type
					// Vertical sends two button press/release events for button 4. Likewise, a
					// button press event for button 7 generates an event on the Horizontal
					// valuator with a value of +1.0. The server may accumulate deltas of less than
					// one unit of scrolling.
					//
					// Any server providing this behaviour marks emulated button or valuator events
					// with the XIPointerEmulated flag for DeviceEvents, and the XIRawEmulated flag
					// for raw events, to hint at applications which event is a hardware event.
					//
					// If more than one scroll valuator of the same type is present on a device,
					// the valuator marked with Preferred for the same scroll direction is used to
					// convert legacy button events into scroll valuator events. If no valuator is
					// marked Preferred or more than one valuator is marked with Preferred for this
					// scroll direction, this should be considered a driver bug and the behaviour
					// is implementation-dependent.
					// ------------------------------------------------------------------
					// [[multitouch-emulation]]
					// Pointer emulation from multitouch events
					// ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
					//
					// Touch sequences from direct touch devices may emulate pointer events. Only one
					// touch sequence from a device may emulate pointer events at a time; which touch
					// sequence emulates pointer events is implementation-dependent.
					//
					// Pointer events are emulated as follows:
					//
					// - A TouchBegin event generates a pointer motion event to the location of the
					// touch with the same axis values of the touch event, followed by a button press
					// event for button 1.
					// - A TouchUpdate event generates a pointer motion event to the location of the
					// touch and/or to update axis values of the pointer device. The button state
					// as seen from the protocol includes button 1 set.
					// - A TouchEnd event generates a pointer motion event to the location of the touch
					// and/or to update the axis values if either have changed, followed by a button
					// release event for button 1. The button state as seen from the protocol
					// includes button 1 set.
					//
					// If a touch sequence emulates pointer events and an emulated pointer event
					// triggers the activation of a passive grab, the grabbing client becomes the
					// owner of the touch sequence.
					//
					// The touch sequence is considered to have been accepted if
					//
					// - the grab mode is asynchronous, or
					// - the grab mode is synchronous and the device is thawed as a result of
					// AllowEvents with AsyncPointer or AsyncDevice
					//
					// Otherwise, if the button press is replayed by the client, the touch sequence
					// is considered to be rejected.
					//
					// Touch event delivery precedes pointer event delivery. A touch event emulating
					// pointer events is delivered:
					//
					// - as a touch event to the top-most window of the current window set if a
					// client has a touch grab on this window,
					// - otherwise, as a pointer event to the top-most window of the current window
					// set if a client has a pointer grab on this window,
					// - otherwise, to the next child window in the window set until a grab has been
					// found.
					//
					// If no touch or pointer grab on any window is active and the last window in the
					// window set has been reached, the event is delivered:
					//
					// - as a touch event to the window if a client has selected for touch events
					// on this window
					// - otherwise, as a pointer event to the window if a client has selected for
					// pointer events.
					// - otherwise, to the next parent window in the window set until a selection has
					// been found.
					//
					// Emulated pointer events will have the PointerEmulated flag set. A touch
					// event that emulates pointer events has the TouchEmulatingPointer flag set.
					if ((data.flags & XiDeviceEventFlags.XIPointerEmulated) != 0)
					{
						if (this.Log().IsEnabled(LogLevel.Trace))
						{
							this.Log().Trace($"Ignoring emulated {evtype} event.");
						}

						return;
					}

					if (this.Log().IsEnabled(LogLevel.Trace))
					{
						this.Log().Trace($"Received {data}");
					}

					var args = CreatePointerEventArgsFromDeviceEvent(display, data);
					if (this.Log().IsEnabled(LogLevel.Trace))
					{
						this.Log().Trace($"Created event args: {args}");
					}
					switch (evtype)
					{
						case XiEventType.XI_Motion when args.CurrentPoint.Properties.MouseWheelDelta != 0:
							X11XamlRootHost.QueueAction(_host, () => RaisePointerWheelChanged(args));
							break;
						case XiEventType.XI_Motion:
							X11XamlRootHost.QueueAction(_host, () => RaisePointerMoved(args));
							break;
						case XiEventType.XI_ButtonPress when args.CurrentPoint.Properties.MouseWheelDelta != 0:
							X11XamlRootHost.QueueAction(_host, () => RaisePointerWheelChanged(args));
							break;
						case XiEventType.XI_ButtonPress:
							X11XamlRootHost.QueueAction(_host, () => RaisePointerPressed(args));
							break;
						case XiEventType.XI_ButtonRelease when args.CurrentPoint.Properties.MouseWheelDelta == 0:
							// if delta != 0, then this is the ButtonRelease of the (ButtonPress,ButtonRelease) pair
							// used for scrolling. We arbitrarily choose to handle it on the ButtonPress side.
							X11XamlRootHost.QueueAction(_host, () => RaisePointerReleased(args));
							break;
						case XiEventType.XI_TouchBegin:
							X11XamlRootHost.QueueAction(_host, () => RaisePointerEntered(args));
							X11XamlRootHost.QueueAction(_host, () => RaisePointerPressed(args));
							break;
						case XiEventType.XI_TouchEnd:
							X11XamlRootHost.QueueAction(_host, () => RaisePointerReleased(args));
							X11XamlRootHost.QueueAction(_host, () => RaisePointerExited(args));
							break;
						case XiEventType.XI_TouchUpdate:
							X11XamlRootHost.QueueAction(_host, () => RaisePointerMoved(args));
							break;
					}
				}
				break;
			case XiEventType.XI_DeviceChanged:
				{
					var data = ev.GenericEventCookie.GetEvent<XIDeviceEvent>();
					if (this.Log().IsEnabled(LogLevel.Error))
					{
						this.Log().Error($"XI2 {evtype} EVENT: {data.event_x}x{data.event_y} {data.buttons}");
					}
					_deviceInfoCache.Remove(data.sourceid);
					_valuatorValues.Remove(data.sourceid);
					break;
				}
			default:
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"XI2 ERROR: received an unexpected {evtype} event");
				}
				break;
		}
	}

	private static bool IsTouchpad(IEnumerable<string> props)
	{
		// X Input cannot distinguish between trackpads and mice. We do this
		// by testing for properties that should be available on trackpads.
		//
		// For libinput, here's the output of `xinput list-props` on my Lenovo trackpad
		// Device 'ELAN0001:00 04F3:3140 Touchpad':
		// Device Enabled (168):	1
		// Coordinate Transformation Matrix (170):	1.000000, 0.000000, 0.000000, 0.000000, 1.000000, 0.000000, 0.000000, 0.000000, 1.000000
		// libinput Tapping Enabled (324):	1
		// libinput Tapping Enabled Default (325):	0
		// libinput Tapping Drag Enabled (326):	1
		// libinput Tapping Drag Enabled Default (327):	1
		// libinput Tapping Drag Lock Enabled (328):	0
		// libinput Tapping Drag Lock Enabled Default (329):	0
		// libinput Tapping Button Mapping Enabled (330):	1, 0
		// libinput Tapping Button Mapping Default (331):	1, 0
		// libinput Natural Scrolling Enabled (297):	0
		// libinput Natural Scrolling Enabled Default (298):	0
		// libinput Disable While Typing Enabled (332):	1
		// libinput Disable While Typing Enabled Default (333):	1
		// libinput Scroll Methods Available (299):	1, 1, 0
		// libinput Scroll Method Enabled (300):	1, 0, 0
		// libinput Scroll Method Enabled Default (301):	1, 0, 0
		// libinput Click Methods Available (334):	1, 1
		// libinput Click Method Enabled (335):	1, 0
		// libinput Click Method Enabled Default (336):	1, 0
		// libinput Middle Emulation Enabled (337):	0
		// libinput Middle Emulation Enabled Default (338):	0
		// libinput Accel Speed (306):	0.000000
		// libinput Accel Speed Default (307):	0.000000
		// libinput Accel Profiles Available (308):	1, 1, 1
		// libinput Accel Profile Enabled (309):	1, 0, 0
		// libinput Accel Profile Enabled Default (310):	1, 0, 0
		// libinput Accel Custom Fallback Points (311):	<no items>
		// libinput Accel Custom Fallback Step (312):	0.000000
		// libinput Accel Custom Motion Points (313):	<no items>
		// libinput Accel Custom Motion Step (314):	0.000000
		// libinput Accel Custom Scroll Points (315):	<no items>
		// libinput Accel Custom Scroll Step (3g16):	0.000000
		// libinput Left Handed Enabled (317):	0
		// libinput Left Handed Enabled Default (318):	0
		// libinput Send Events Modes Available (282):	1, 1
		// libinput Send Events Mode Enabled (283):	0, 0
		// libinput Send Events Mode Enabled Default (284):	0, 0
		// Device Node (285):	"/dev/input/event7"
		// Device Product ID (286):	1267, 12608
		// libinput Drag Lock Buttons (319):	<no items>
		// libinput Horizontal Scroll Enabled (320):	1
		// libinput Scrolling Pixel Distance (321):	15
		// libinput Scrolling Pixel Distance Default (322):	15
		// libinput High Resolution Wheel Scroll Enabled (323):	1

		// here's the output when swapping out libinput for synaptics
		// Device 'ELAN0001:00 04F3:3140 Touchpad':
		// Device Enabled (168):	1
		// Coordinate Transformation Matrix (170):	1.000000, 0.000000, 0.000000, 0.000000, 1.000000, 0.000000, 0.000000, 0.000000, 1.000000
		// Device Accel Profile (293):	1
		// Device Accel Constant Deceleration (294):	2.500000
		// Device Accel Adaptive Deceleration (295):	1.000000
		// Device Accel Velocity Scaling (296):	12.500000
		// Synaptics Edges (327):	128, 3081, 113, 1984
		// Synaptics Finger (328):	25, 30, 0
		// Synaptics Tap Time (329):	180
		// Synaptics Tap Move (330):	168
		// Synaptics Tap Durations (331):	180, 180, 100
		// Synaptics ClickPad (332):	1
		// Synaptics Middle Button Timeout (333):	0
		// Synaptics Two-Finger Pressure (334):	282
		// Synaptics Two-Finger Width (335):	7
		// Synaptics Scrolling Distance (336):	76, 76
		// Synaptics Edge Scrolling (337):	0, 0, 0
		// Synaptics Two-Finger Scrolling (338):	1, 0
		// Synaptics Move Speed (339):	1.000000, 1.750000, 0.052178, 0.000000
		// Synaptics Off (340):	0
		// Synaptics Locked Drags (341):	0
		// Synaptics Locked Drags Timeout (342):	5000
		// Synaptics Tap Action (343):	0, 0, 0, 0, 0, 0, 0
		// Synaptics Click Action (344):	1, 3, 2
		// Synaptics Circular Scrolling (345):	0
		// Synaptics Circular Scrolling Distance (346):	0.100000
		// Synaptics Circular Scrolling Trigger (347):	0
		// Synaptics Circular Pad (348):	0
		// Synaptics Palm Detection (349):	0
		// Synaptics Palm Dimensions (350):	10, 200
		// Synaptics Coasting Speed (351):	20.000000, 50.000000
		// Synaptics Pressure Motion (352):	30, 160
		// Synaptics Pressure Motion Factor (353):	1.000000, 1.000000
		// Synaptics Grab Event Device (354):	0
		// Synaptics Gestures (355):	1
		// Synaptics Capabilities (356):	1, 0, 0, 1, 1, 0, 0
		// Synaptics Pad Resolution (357):	32, 32
		// Synaptics Area (358):	0, 0, 0, 0
		// Synaptics Soft Button Areas (359):	1604, 0, 1719, 0, 0, 0, 0, 0
		// Synaptics Noise Cancellation (360):	19, 19
		// Device Product ID (286):	1267, 12608
		// Device Node (285):	"/dev/input/event8"
		return props.Any(p => p is "Synaptics Tap Time" or "libinput Tapping Enabled");
	}

	private unsafe DeviceInfo? GetDevicePropertiesFromId(IntPtr display, int id)
	{
		if (_deviceInfoCache.TryGetValue(id, out var result))
		{
			return result;
		}
		var infos = XLib.XIQueryDevice(display, (int)XiPredefinedDeviceId.XIAllDevices, out var ndevices);
		using var deviceInfoDisposable = Disposable.Create(() => XLib.XIFreeDeviceInfo(infos));

		for (var i = 0; i < ndevices; i++)
		{
			if (infos[i].Deviceid == id)
			{
				var info = infos[i];

				IntPtr* props = X11Helper.XIListProperties(display, info.Deviceid, out var nprops);
				using var propsDisposable = Disposable.Create(() =>
				{
					_ = XLib.XFree((IntPtr)props);
				});
				var propsResult = new List<string>();
				for (var index = 0; index < nprops; index++)
				{
					var name = XLib.GetAtomName(display, props[index]);
					if (name is { })
					{
						propsResult.Add(name);
					}
				}

				var classes = new Span<IntPtr>(info.Classes, info.NumClasses).ToArray();
				var scollers = classes
					.Where(classPointer => ((XIAnyClassInfo*)classPointer)->Type == XiDeviceClass.XIScrollClass)
					.Select(classPointer => *(XIScrollClassInfo*)classPointer)
					.Select(scrollClassInfo => (scrollClassInfo.Number, scrollClassInfo))
					.ToDictionary();
				var valuators = classes
					.Where(classPointer => ((XIAnyClassInfo*)classPointer)->Type == XiDeviceClass.XIValuatorClass)
					.Select(classPointer => *(XIValuatorClassInfo*)classPointer)
					.Select(valuatorClassInfo => (valuatorClassInfo.Number, scrollClassInfo: valuatorClassInfo))
					.ToDictionary();
				var @out = new DeviceInfo(new ImmutableList<string>(propsResult), IsTouchpad(propsResult), scollers, valuators);
				return _deviceInfoCache[id] = @out;
			}
		}

		return null;
	}

	private record struct DeviceInfo(ImmutableList<string> Properties, bool IsTouchpad, Dictionary<int, XIScrollClassInfo> Scrollers, Dictionary<int, XIValuatorClassInfo> Valuators);
}
