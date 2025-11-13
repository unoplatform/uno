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
using System.Runtime.InteropServices;
using Uno.Foundation.Logging;
namespace Uno.WinUI.Runtime.Skia.X11;

// Excerpt from the spec :
// Interoperability between version 1.x and 2.0
// --------------------------------------------
//
// There is little interaction between 1.x and 2.x versions of the X Input
// Extension. Clients are requested to avoid mixing XI1.x and XI2 code as much as
// possible. Several direct incompatibilities are observable:

// Accordingly, we only use version 2 of the extension and default to the core input events if
// version 2 is not present. Note that XInput 2 was first released in 2009.
internal partial class X11XamlRootHost
{
	private enum XIVersion
	{
		Unsupported,
		XI2_0,
		XI2_1,
		XI2_2,
		XI2_3,
		XI2_4
	}

	// These should match X11XamlRootHost.EventsHandledByXI2Mask
	private const int XI2Mask =
		(1 << (int)XiEventType.XI_ButtonPress) |
		(1 << (int)XiEventType.XI_ButtonRelease) |
		(1 << (int)XiEventType.XI_Motion) |
		(1 << (int)XiEventType.XI_Enter) |
		(1 << (int)XiEventType.XI_Leave) |
		(1 << (int)XiEventType.XI_DeviceChanged);

	private const int XI2_2Mask =
		(1 << (int)XiEventType.XI_TouchBegin) |
		(1 << (int)XiEventType.XI_TouchUpdate) |
		(1 << (int)XiEventType.XI_TouchEnd);

	private (XIVersion, int)? _xi2Details;

	private unsafe (XIVersion version, int opcode) GetXI2Details(IntPtr display)
	{
		if (_xi2Details is { } d)
		{
			return d;
		}

		if (!XLib.XQueryExtension(display, "XInputExtension", out var _xi2Opcode, out _, out _))
		{
			return (_xi2Details = (XIVersion.Unsupported, _xi2Opcode)).Value;
		}

		var version = X11Helper.XGetExtensionVersion(display, "XInputExtension");
		if (version->major_version != 2)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().LogError($"X server does not support X Input Extension 2. Falling back to the core protocol implementation for pointer inputs. (For more information see: https://aka.platform.uno/skia-desktop");
			}
			return (_xi2Details = (XIVersion.Unsupported, _xi2Opcode)).Value;
		}

		// Just because XI2 is supported, doesn't mean that libXi is present, so we check explicitly for it.
		// https://github.com/unoplatform/private/issues/600
		if (NativeLibrary.TryLoad(XLib.libXInput, out var xiHandle))
		{
			NativeLibrary.Free(xiHandle);
		}
		else
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().LogError($"X server supports X Input Extension 2, but libXi.so.6 was not found. Falling back to the core protocol implementation for pointer inputs. For more information see: https://aka.platform.uno/skia-desktop");
			}
			return (_xi2Details = (XIVersion.Unsupported, _xi2Opcode)).Value;
		}

		_xi2Details = version->minor_version switch
		{
			0 => (XIVersion.XI2_0, _xi2Opcode),
			1 => (XIVersion.XI2_1, _xi2Opcode),
			2 => (XIVersion.XI2_2, _xi2Opcode),
			3 => (XIVersion.XI2_3, _xi2Opcode),
			4 => (XIVersion.XI2_4, _xi2Opcode),
			_ => throw new ArgumentException("XI2 version is not between 2.0 and 2.4. There should be no 2.5 or above.")
		};

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"Using X Input Extension 2.{version->minor_version}.");
		}

		return _xi2Details.Value;
	}

	private unsafe void SetXIEventMask(IntPtr display, IntPtr window, int mask)
	{
		var m = stackalloc XIEventMask[1];
		m->Deviceid = (int)XiPredefinedDeviceId.XIAllDevices;
		m->MaskLen = (int)XiEventType.XI_LASTEVENT;
		// from XI2.h:
		//		#define XIMaskLen(event)        (((event) >> 3) + 1)
		//		#define XI_GestureSwipeEnd               32
		//		#define XI_LASTEVENT                     XI_GestureSwipeEnd
		// So XIMaskLen(XI_LASTEVENT) is always 4
		// m->mask = calloc(m->mask_len, sizeof(char));
		var maskPtr = stackalloc int[1];
		*maskPtr = mask;
		m->Mask = maskPtr;
		m->MaskLen = 4;
		m->Deviceid = (int)XiPredefinedDeviceId.XIAllDevices;
		var _1 = XLib.XISelectEvents(display, window, m, 1);
		var _2 = XLib.XSync(display, false);
	}
}
