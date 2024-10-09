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
	private readonly Dictionary<int, double> _lastHorizontalTouchpadWheelPosition = new();
	private readonly Dictionary<int, double> _lastVerticalTouchpadWheelPosition = new();
	private readonly Dictionary<int, DeviceInfo> _deviceInfoCache = new();

	public unsafe PointerEventArgs CreatePointerEventArgsFromDeviceEvent(XIDeviceEvent data)
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

		var info = GetDevicePropertiesFromId(data.sourceid);

		// Touchpad scrolling manifests in a Motion event where the current scrolling "position" is recorded in the
		// valuator. There is no direct way to get the delta, so we have to record the old position and diff it
		// with the new position. This also means that the first "tick" will not result in a WheelChanged event,
		// but will only be used to set the initial wheel position.
		// Also, we can get a "diagonal scroll" where both the horizontal and the vertical positions change.
		// Our PointerEventArgs don't support this, so we arbitrarily choose to make diagonal scrolling
		// result in a vertical scroll.
		// IMPORTANT: DO NOT FORGET TO RESET POSITIONS ON LEAVE.
		if (data.evtype is XiEventType.XI_Motion && info is { } info_)
		{
			var valuators = data.valuators;
			var values = valuators.Values;
			for (var i = 0; i < valuators.MaskLen * 8; i++)
			{
				if (XLib.XIMaskIsSet(valuators.Mask, i))
				{
					var (valuator, value) = (i, *values++);
					if (valuator == info_.HorizontalValuator || valuator == info_.VerticalValuator)
					{
						isHorizontalMouseWheel = valuator == info_.HorizontalValuator;
						var (dict, increment) = isHorizontalMouseWheel ?
							(_lastHorizontalTouchpadWheelPosition, info_.HorizontalIncrement!.Value) :
							(_lastVerticalTouchpadWheelPosition, info_.VerticalIncrement!.Value);
						if (dict.TryGetValue(data.sourceid, out var oldValue))
						{
							wheelDelta = (oldValue - value) * ScrollContentPresenter.ScrollViewerDefaultMouseWheelDelta / increment;
						}
						dict[data.sourceid] = value;
					}
					// DO NOT BREAK OUT OF THE FOR LOOP. We still need to update the other valuator positions.
				}
			}
		}

		var mask = 0;
		for (var i = 0; i < data.buttons.MaskLen; i++) // masklen <= 4
		{
			mask |= data.buttons.Mask[i] << (8 * i);
		}

		if (data.evtype is XiEventType.XI_ButtonPress)
		{
			mask |= (1 << data.detail); // the newly pressed button is not added to the mask yet.
		}
		else if (data.evtype is XiEventType.XI_ButtonRelease)
		{
			mask &= ~(1 << data.detail); // the newly released button is not removed from the mask yet.
		}

		var properties = new PointerPointProperties
		{
			IsLeftButtonPressed = (mask & (1 << LEFT)) != 0,
			IsMiddleButtonPressed = (mask & (1 << MIDDLE)) != 0,
			IsRightButtonPressed = (mask & (1 << RIGHT)) != 0,
			IsXButton1Pressed = (mask & (1 << XButton1)) != 0,
			IsXButton2Pressed = (mask & (1 << XButton2)) != 0,
			IsHorizontalMouseWheel = isHorizontalMouseWheel,
			IsInRange = true,
			IsPrimary = true,
			IsTouchPad = info is { } && IsTouchpad(info.Value.Properties),
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
				_ => PointerUpdateKind.Other
			}
		};

		var scale = ((IXamlRootHost)_host).RootElement?.XamlRoot is { } root
			? XamlRoot.GetDisplayInformation(root).RawPixelsPerViewPixel
			: 1;

		var point = new PointerPoint(
			frameId: (uint)data.time, // UNO TODO: How should we set the frame, timestamp may overflow.
			timestamp: (uint)(data.time * TimeSpan.TicksPerMillisecond), // Time is given in milliseconds since system boot. See also: https://github.com/unoplatform/uno/issues/14535
			data.evtype is XiEventType.XI_TouchBegin or XiEventType.XI_TouchEnd or XiEventType.XI_TouchUpdate ? PointerDevice.For(PointerDeviceType.Touch) : PointerDevice.For(PointerDeviceType.Mouse),
			(uint)data.sourceid,
			new Point(data.event_x / scale, data.event_y / scale),
			new Point(data.event_x / scale, data.event_y / scale),
			properties.HasPressedButton,
			properties // We don't SetUpdateKind here. We already did that above
		);

		_previousPointerPointProperties = properties;

		var modifiers = X11XamlRootHost.XModifierMaskToVirtualKeyModifiers((XModifierMask)(data.mods.Base & 0xffff));

		return new PointerEventArgs(point, modifiers);
	}

	// This method is comment-free. See the very similar (but more involved) implementation of
	// CreatePointerEventArgsFromDeviceEvent for comments.
	public unsafe PointerEventArgs CreatePointerEventArgsFromEnterLeaveEvent(XIEnterLeaveEvent data, PointerDeviceType pointerType)
	{
		var mask = 0;
		for (var i = 0; i < data.buttons.MaskLen; i++) // masklen <= 4
		{
			mask |= data.buttons.Mask[i] << (8 * i);
		}

		var properties = new PointerPointProperties
		{
			IsLeftButtonPressed = (mask & (1 << LEFT)) != 0,
			IsMiddleButtonPressed = (mask & (1 << MIDDLE)) != 0,
			IsRightButtonPressed = (mask & (1 << RIGHT)) != 0,
			IsXButton1Pressed = (mask & (1 << XButton1)) != 0,
			IsXButton2Pressed = (mask & (1 << XButton2)) != 0,
			IsInRange = true,
			IsTouchPad = GetDevicePropertiesFromId(data.sourceid) is { } info && IsTouchpad(info.Properties),
			IsHorizontalMouseWheel = false,
		};

		var point = new PointerPoint(
			frameId: (uint)data.time, // UNO TODO: How should we set the frame, timestamp may overflow.
			timestamp: (ulong)data.time,
			PointerDevice.For(PointerDeviceType.Mouse),
			(uint)data.sourceid,
			new Point(data.event_x, data.event_y),
			new Point(data.event_x, data.event_y),
			properties.HasPressedButton,
			properties.SetUpdateKindFromPrevious(_previousPointerPointProperties)
		);

		_previousPointerPointProperties = properties;

		var modifiers = X11XamlRootHost.XModifierMaskToVirtualKeyModifiers((XModifierMask)(data.mods.Base & 0xffff));

		return new PointerEventArgs(point, modifiers);
	}

	// Note about removing devices: the server emits a ButtonRelease if a device is removed
	// while a button is held.
	public void HandleXI2Event(XEvent ev)
	{
		var evtype = (XiEventType)ev.GenericEventCookie.evtype;
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"XI2 EVENT: {evtype}");
		}
		switch (evtype)
		{
			case XiEventType.XI_Enter:
			case XiEventType.XI_Leave:
				{
					_lastHorizontalTouchpadWheelPosition.Clear();
					_lastVerticalTouchpadWheelPosition.Clear();
					var args = CreatePointerEventArgsFromEnterLeaveEvent(
						ev.GenericEventCookie.GetEvent<XIEnterLeaveEvent>(),
						PointerDeviceType.Mouse); // https://www.x.org/releases/X11R7.7/doc/inputproto/XI2proto.txt: Touch events do not generate enter/leave events.
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

					if ((data.flags & XiDeviceEventFlags.XIPointerEmulated) != 0)
					{
						if (this.Log().IsEnabled(LogLevel.Trace))
						{
							this.Log().Trace($"Ignoring emulated {evtype} event.");
						}
					}

					var args = CreatePointerEventArgsFromDeviceEvent(data);
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
					_deviceInfoCache.Remove(data.sourceid);
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

	// for some stupid reason, the device id can be reused for other devices if
	// the original device is unplugged. For example, if device D1 is assigned device id 1
	// but is then unplugged, another device D2 can be assigned id 1. This means that we
	// can't cache the lookups, and instead are forced to make this call every single time :(
	private bool IsTouchpad(IEnumerable<string> props)
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

	private unsafe DeviceInfo? GetDevicePropertiesFromId(int id)
	{
		if (_deviceInfoCache.TryGetValue(id, out var result))
		{
			return result;
		}
		var display = _host.TopX11Window.Display;
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

				int? pressureValuator = null;
				int? horizontalValuator = null;
				double? horizontalIncrement = null;
				int? verticalValuator = null;
				double? verticalIncrement = null;
				for (var index = 0; index < info.NumClasses; index++)
				{
					var xiAnyClassInfo = info.Classes[index];
					if (xiAnyClassInfo->Type == XiDeviceClass.XIScrollClass)
					{
						var classInfo = (XIScrollClassInfo*)xiAnyClassInfo;
						if (classInfo->ScrollType == XiScrollType.Horizontal)
						{
							(horizontalValuator, horizontalIncrement) = (classInfo->Number, classInfo->Increment);
						}
						else
						{
							(verticalValuator, verticalIncrement) = (classInfo->Number, classInfo->Increment);
						}
					}
					else if (xiAnyClassInfo->Type == XiDeviceClass.XIValuatorClass &&
						((XIValuatorClassInfo*)xiAnyClassInfo)->Label == X11Helper.GetAtom(display, "Abs MT Pressure"))
					{
						pressureValuator = ((XIValuatorClassInfo*)xiAnyClassInfo)->Number;
					}
				}

				var @out = new DeviceInfo(new ImmutableList<string>(propsResult), pressureValuator, horizontalValuator, horizontalIncrement, verticalValuator, verticalIncrement);
				_deviceInfoCache[id] = @out;
				return @out;
			}
		}

		return null;
	}

	private record struct DeviceInfo(ImmutableList<string> Properties, int? PressureValuator, int? HorizontalValuator, double? HorizontalIncrement, int? VerticalValuator, double? VerticalIncrement);
}
