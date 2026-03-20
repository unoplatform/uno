// Mostly from the FrameBuffer implementation, xdpyinfo, xrandr, xrdb and Avalonia

// Copyright 1988, 1998  The Open Group
// Copyright 2005 Hitachi, Ltd.
//
// Permission to use, copy, modify, distribute, and sell this software and its
// documentation for any purpose is hereby granted without fee, provided that
// the above copyright notice appear in all copies and that both that
// copyright notice and this permission notice appear in supporting
// documentation.
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT SHALL THE
// OPEN GROUP BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN
// AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
// Except as contained in this notice, the name of The Open Group shall not be
// used in advertising or otherwise to promote the sale, use or other dealings
// in this Software without prior written authorization from The Open Group.

// Copyright © 2001 Keith Packard, member of The XFree86 Project, Inc.
// Copyright © 2002 Hewlett Packard Company, Inc.
// Copyright © 2006 Intel Corporation
//
// Permission to use, copy, modify, distribute, and sell this software and its
// documentation for any purpose is hereby granted without fee, provided that
// the above copyright notice appear in all copies and that both that copyright
// notice and this permission notice appear in supporting documentation, and
// that the name of the copyright holders not be used in advertising or
// publicity pertaining to distribution of the software without specific,
// written prior permission.  The copyright holders make no representations
// about the suitability of this software for any purpose.  It is provided "as
// is" without express or implied warranty.
//
// THE COPYRIGHT HOLDERS DISCLAIM ALL WARRANTIES WITH REGARD TO THIS SOFTWARE,
// INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS, IN NO
// EVENT SHALL THE COPYRIGHT HOLDERS BE LIABLE FOR ANY SPECIAL, INDIRECT OR
// CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF USE,
// DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER
// TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE
// OF THIS SOFTWARE.

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

// COPYRIGHT 1987, 1991
// DIGITAL EQUIPMENT CORPORATION
// MAYNARD, MASSACHUSETTS
// MASSACHUSETTS INSTITUTE OF TECHNOLOGY
// CAMBRIDGE, MASSACHUSETTS
// ALL RIGHTS RESERVED.
//
// THE INFORMATION IN THIS SOFTWARE IS SUBJECT TO CHANGE WITHOUT NOTICE AND
// SHOULD NOT BE CONSTRUED AS A COMMITMENT BY DIGITAL EQUIPMENT CORPORATION.
// DIGITAL MAKES NO REPRESENTATIONS ABOUT THE SUITABILITY OF THIS SOFTWARE FOR
// ANY PURPOSE.  IT IS SUPPLIED "AS IS" WITHOUT EXPRESS OR IMPLIED WARRANTY.
//
// IF THE SOFTWARE IS MODIFIED IN A MANNER CREATING DERIVATIVE COPYRIGHT RIGHTS,
// APPROPRIATE LEGENDS MAY BE PLACED ON THE DERIVATIVE WORK IN ADDITION TO THAT
// SET FORTH ABOVE.
//
// Permission to use, copy, modify, and distribute this software and its
// documentation for any purpose and without fee is hereby granted, provided
// that the above copyright notice appear in all copies and that both that
// copyright notice and this permission notice appear in supporting
// documentation, and that the name of Digital Equipment Corporation not be
// used in advertising or publicity pertaining to distribution of the software
// without specific, written prior permission.
//
// ----------------------------------------------------------------
//
// Copyright 1991, Digital Equipment Corporation.
// Copyright 1991, 1994, 1998  The Open Group
//
// Permission to use, copy, modify, distribute, and sell this software and its
// documentation for any purpose is hereby granted without fee, provided that
// the above copyright notice appear in all copies and that both that
// copyright notice and this permission notice appear in supporting
// documentation.
//
// The above copyright notice and this permission notice shall be included
// in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
// OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE OPEN GROUP BE LIABLE FOR ANY CLAIM, DAMAGES OR
// OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
// ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
//
// Except as contained in this notice, the name of The Open Group shall
// not be used in advertising or otherwise to promote the sale, use or
// other dealings in this Software without prior written authorization
// from The Open Group.
//

// https://gitlab.freedesktop.org/xorg/app/xdpyinfo/-/blob/d14333b852377f1e43ee2fe0fc737453e6dfccd9/xdpyinfo.c
// https://gitlab.freedesktop.org/xorg/app/xrandr/-/blob/71ab94418ead8f59c6124e8b3e53f8df7340f095/xrandr.c
// https://github.com/AvaloniaUI/Avalonia/blob/5fa3ffaeab7e5cd2662ef02d03e34b9d4cb1a489/src/Avalonia.X11/Screens/X11Screen.Providers.cs
// https://gitlab.freedesktop.org/xorg/app/xrdb/-/blob/ff688ceacaddb8e2f345caadfe33e408d97782a0/xrdb.c

using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using Windows.Graphics.Display;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI;

namespace Uno.WinUI.Runtime.Skia.X11
{
	internal class X11DisplayInformationExtension : IDisplayInformationExtension
	{
		private const string EnvironmentUnoDisplayScaleOverride = "UNO_DISPLAY_SCALE_OVERRIDE";
		private const double InchesToMilliMeters = 25.4;
		private const string XftDotdpi = "Xft.dpi";

		private readonly float? _scaleOverride;
		private readonly DisplayInformation _owner;
		private readonly X11XamlRootHost _host;
		private DisplayInformationDetails _details;

		private record DisplayInformationDetails(
			uint ScreenWidthInRawPixels,
			uint ScreenHeightInRawPixels,
			float LogicalDpi,
			double RawPixelsPerViewPixel,
			ResolutionScale ResolutionScale,
			double? DiagonalSizeInInches);

		public X11DisplayInformationExtension(object owner)
		{
			_owner = (DisplayInformation)owner;

			if (float.TryParse(
				Environment.GetEnvironmentVariable(EnvironmentUnoDisplayScaleOverride),
				NumberStyles.Any,
				CultureInfo.InvariantCulture,
				out var environmentScaleOverride))
			{
				_scaleOverride = environmentScaleOverride;
			}

			// initialization is redundant, just keeping the compiler happy
			_details = new DisplayInformationDetails(default, default, default, default, default, default);

			if (!X11Helper.XamlRootHostFromDisplayInformation(_owner, out var host))
			{
				throw new InvalidOperationException($"{nameof(X11DisplayInformationExtension)} couldn't find a {nameof(X11XamlRootHost)}.");
			}
			_host = host;
			_host.SetDisplayInformationExtension(this);

			UpdateDetails();
		}

		internal void UpdateDetails()
		{
			var window = _host.RootX11Window.Window;
			var display = _host.RootX11Window.Display;

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"updating DisplayInformation details for X11 window: {display.ToString("X", CultureInfo.InvariantCulture)}, {window.ToString("X", CultureInfo.InvariantCulture)}");
			}

			var oldDetails = _details;
			var xLock = X11Helper.XLock(display);
			using var notifyDisposable = Disposable.Create(() =>
			{
				// dispose lock before raising DpiChanged in case a user defined callback takes too long.
				xLock.Dispose();
				if (_details != oldDetails)
				{
					_owner.NotifyDpiChanged();
				}
			});

			if (_host.Closed.IsCompleted)
			{
				return;
			}

			if (XLib.XRRQueryExtension(display, out _, out _) != 0 &&
				XLib.XRRQueryVersion(display, out var major, out var minor) != 0 &&
				(major > 1 || (major == 1 && minor >= 3)) &&
				GetDisplayInformationXRandR1_3(display, window) is { details: var details, fps: var fps })
			{
				_details = details;
				if (fps is not null)
				{
					_host.UpdateRenderTimerFps(fps.Value);
				}
			}
			else // naive implementation from xdpyinfo
			{
				XWindowAttributes attributes = default;
				_ = XLib.XGetWindowAttributes(display, window, ref attributes);
				var screen = attributes.screen;
				// Using X<Width|Height>OfScreen calls seems to work more reliably than XDisplay<Width|Height>.
				// Mostly because XScreenNumberOfScreen (which we need for XDisplay<Width|Height>) isn't working reliably
				var xres = X11Helper.XWidthOfScreen(screen) * InchesToMilliMeters / X11Helper.XWidthMMOfScreen(screen);
				var yres = X11Helper.XHeightOfScreen(screen) * InchesToMilliMeters / X11Helper.XHeightMMOfScreen(screen);
				var dpi = (float)Math.Round(Math.Sqrt(xres * yres)); // TODO: what to do if dpi in the 2 normal directions is different??

				var rawScale = _scaleOverride ?? (TryGetXResource(XftDotdpi, out var xrdbScaling) ? xrdbScaling.Value : dpi / DisplayInformation.BaseDpi);

				// This returns very incorrect numbers as far as I've tested.
				var widthInInches = (uint)Math.Round(X11Helper.XWidthMMOfScreen(screen) / InchesToMilliMeters);
				var heightInInches = (uint)Math.Round(X11Helper.XHeightMMOfScreen(screen) / InchesToMilliMeters);

				_details = new(
					(uint)X11Helper.XWidthOfScreen(screen),
					(uint)X11Helper.XHeightOfScreen(screen),
					(float)(rawScale * DisplayInformation.BaseDpi),
					rawScale,
					(ResolutionScale)(int)(rawScale * 100),
					Math.Sqrt(widthInInches * widthInInches + heightInInches * heightInInches)
				);
			}
		}

		public DisplayOrientations CurrentOrientation
			=> DisplayOrientations.Landscape;

		public uint ScreenHeightInRawPixels
			=> _details.ScreenHeightInRawPixels;

		public uint ScreenWidthInRawPixels
			=> _details.ScreenWidthInRawPixels;

		public float LogicalDpi => _details.LogicalDpi;

		public double RawPixelsPerViewPixel => _details.RawPixelsPerViewPixel;

		public ResolutionScale ResolutionScale => _details.ResolutionScale;

		public double? DiagonalSizeInInches => _details.DiagonalSizeInInches;

		public void StartDpiChanged() { }
		public void StopDpiChanged() { }

		private bool TryGetXResource(string resourceName, [NotNullWhen(true)] out double? scaling)
		{
			// For some reason, querying the resources with a preexisting display yields outdated values, so
			// we have to open a new display here.
			IntPtr display = XLib.XOpenDisplay(IntPtr.Zero);
			using var displayDisposable = new DisposableStruct<IntPtr>(static d => { _ = XLib.XCloseDisplay(d); }, display);
			var xdefs = X11Helper.XResourceManagerString(display);
			if (xdefs != IntPtr.Zero)
			{
				IntPtr xrdb = X11Helper.XrmGetStringDatabase(xdefs);
				using var databaseDisposable = new DisposableStruct<IntPtr>(X11Helper.XrmDestroyDatabase, xrdb);
				var resourceNamePtr = Marshal.StringToHGlobalAnsi(resourceName);
				using var resourceNameDisposable = new DisposableStruct<IntPtr>(Marshal.FreeHGlobal, resourceNamePtr);
				var found = X11Helper.XrmGetResource(xrdb, resourceNamePtr, resourceNamePtr, out _, out X11Helper.XrmValue value);
				// don't free value.addr. It's managed by the X server.
				if (found && value.addr != IntPtr.Zero)
				{
					if (Marshal.PtrToStringAnsi(value.addr, (int)value.size) is { } str && int.TryParse(str, out var result))
					{
						scaling = result / DisplayInformation.BaseDpi;
						return true;
					}
				}
			}
			scaling = null;
			return false;
		}

		// START OF EXCERPT 1
		// 1.2 Introduction to version 1.2 of the extension
		//
		// One of the significant limitations found in version 1.1 of the RandR
		// protocol was the inability to deal with the Xinerama model where multiple
		// monitors display portions of a common underlying screen. In this environment,
		// zero or more video outputs are associated with each CRT controller which
		// defines both a set of video timings and a 'viewport' within the larger
		// screen. This viewport is independent of the overall size of the screen, and
		// may be located anywhere within the screen.
		//
		// The effect is to decouple the reported size of the screen from the size
		// presented by each video output, and to permit multiple outputs to present
		// information for a single screen.
		//
		// To extend RandR for this model, we separate out the output, CRTC and screen
		// configuration information and permit them to be configured separately. For
		// compatibility with the 1.1 version of the protocol, we make the 1.1 requests
		// simultaneously affect both the screen and the (presumably sole) CRTC and
		// output. The set of available outputs are presented with UTF-8 encoded names
		// and may be connected to CRTCs as permitted by the underlying hardware. CRTC
		// configuration is now done with full mode information instead of just size
		// and refresh rate, and these modes have names. These names also use UTF-8
		// encoding. New modes may also be added by the user.
		//
		// Additional requests and events are provided for this new functionality.
		//
		// ┌────────────────────────────────┬──────────┐
		// ┏━━━━━━━┳───────────────┐       ╔════════╗ ╔════════╗
		// ┃   1   ┃               │       ║   A    ║ ║   B    ║
		// ┃   ┏━━━╋━━━━━━━━━━━━━━━┫       ║        ║ ║        ║
		// ┣━━━╋━━━┛               ┃       ╚════════╝ ╚════════╝
		// │   ┃         2         ┃─────────────────┐
		// │   ┃                   ┃        ╔═══════════════════╗
		// │   ┃                   ┃        ║                   ║
		// │   ┗━━━━━━━━━━━━━━━━━━━┫        ║        C          ║
		// └───────────────────────┘        ║                   ║
		// ┌──────┐  ┏━━━━┓  ╔══════╗       ║                   ║
		// │screen│  ┃CRTC┃  ║output║       ╚═══════════════════╝
		// └──────┘  ┗━━━━┛  ╚══════╝
		//
		// In this picture, the screen is covered (incompletely) by two CRTCs. CRTC1
		// is connected to two outputs, A and B. CRTC2 is connected to output C.
		// Outputs A and B will present exactly the same region of the screen using
		// the same mode line. Output C will present a different (larger) region of
		// the screen using a different mode line.
		// END OF EXCERPT 1

		// START OF EXCERPT 2
		// Version 1.5 adds monitors
		//
		// • A 'Monitor' is a rectangular subset of the screen which represents
		// a coherent collection of pixels presented to the user.
		//
		// • Each Monitor is be associated with a list of outputs (which may be
		// empty).
		//
		// • When clients define monitors, the associated outputs are removed from
		// existing Monitors. If removing the output causes the list for that
		// monitor to become empty, that monitor will be deleted.
		//
		// • For active CRTCs that have no output associated with any
		// client-defined Monitor, one server-defined monitor will
		// automatically be defined of the first Output associated with them.
		//
		// • When defining a monitor, setting the geometry to all zeros will
		// cause that monitor to dynamically track the bounding box of the
		// active outputs associated with them
		// END OF EXCERPT 2

		// START OF EXCERPT 3
		// RandR provides information about each available CRTC and output; the
		// connection between CRTC and output is under application control, although
		// the hardware will probably impose restrictions on the possible
		// configurations. The protocol doesn't try to describe these restrictions,
		// instead it provides a mechanism to find out what combinations are supported.
		// END OF EXCERPT 3

		// 2009 spec, notably does not use monitors. Relies on "transforms" which may or may not be be part of RandR 1.2
		private unsafe (DisplayInformationDetails details, double? fps)? GetDisplayInformationXRandR1_3(IntPtr display, IntPtr window)
		{
			using var lockDiposable = X11Helper.XLock(display);

			var resources = X11Helper.XRRGetScreenResourcesCurrent(display, window);
			using var resourcesDiposable = Disposable.Create(() => X11Helper.XRRFreeScreenResources(resources));

			_ = XLib.XQueryTree(display, XLib.XDefaultRootWindow(display), out IntPtr root, out _, out var children, out _);
			_ = XLib.XFree(children);
			XWindowAttributes windowAttrs = default;
			_ = XLib.XGetWindowAttributes(display, window, ref windowAttrs);
			XLib.XTranslateCoordinates(display, window, root, windowAttrs.x, windowAttrs.y, out var rootx, out var rooty, out _);

			X11Helper.XRRCrtcInfo* crtcInfo = default;
			IntPtr crtc = default;
			double bestArea = 1;
			foreach (var crtc_ in new Span<IntPtr>(resources->crtcs.ToPointer(), resources->ncrtc))
			{
				var crtcInfo_ = X11Helper.XRRGetCrtcInfo(display, new IntPtr(resources), crtc_);
				// We decide that the crtc we want is the one that overlaps the most with the window
				var intersection = Rectangle.Intersect(
					new Rectangle(crtcInfo_->x, crtcInfo_->y, (int)crtcInfo_->width, (int)crtcInfo_->height),
					new Rectangle(rootx, rooty, windowAttrs.width, windowAttrs.height));
				var area = intersection.Height * intersection.Width;
				if (area > bestArea)
				{
					if (crtcInfo != default)
					{
						X11Helper.XRRFreeCrtcInfo(crtcInfo);
					}
					bestArea = area;
					crtcInfo = crtcInfo_;
					crtc = crtc_;
				}
				else
				{
					X11Helper.XRRFreeCrtcInfo(crtcInfo_);
				}
			}

			using var crtcInfoDisposable = Disposable.Create(() => X11Helper.XRRFreeCrtcInfo(crtcInfo));

			if (crtcInfo == default || crtcInfo->noutput == 0)
			{
				return null;
			}

			// We assume each CRTC is connected to one physical output. If not, we take the first one.
			// This means that if two physical displays duplicate the same pixels, we will use
			// the first one we find for the numbers

			var outputInfo = X11Helper.XRRGetOutputInfo(display, new IntPtr(resources), *(IntPtr*)crtcInfo->outputs.ToPointer());
			using var outputInfoDisposable = Disposable.Create(() => X11Helper.XRRFreeOutputInfo(outputInfo));

			X11Helper.XRRCrtcTransformAttributes* transformInfo = default;
			_ = X11Helper.XRRGetCrtcTransform(display, crtc, ref transformInfo);
			using var transformInfoDisposable = Disposable.Create(() =>
			{
				_ = XLib.XFree(new IntPtr(transformInfo));
			});

			// Assume no fancy transforms. We only support simple scaling transforms.
			// e.g. for 1.5x1.5 we should see a similar affine transformation to:
			// 98304     0      0
			//   0     98304    0
			//   0        0     65536
			var xScaling = transformInfo->currentTransform._1_1 * 1.0 / transformInfo->currentTransform._3_3;
			var yScaling = transformInfo->currentTransform._2_2 * 1.0 / transformInfo->currentTransform._3_3;

			var logicalWidth = crtcInfo->width;
			var logicalHeight = crtcInfo->height;
			var rawWidth = (uint)Math.Round(logicalWidth * 1.0 / xScaling);
			var rawHeight = (uint)Math.Round(logicalHeight * 1.0 / yScaling);

			// With XRandR, we don't use the xScaling and yScaling values, since the server will "stretch" the window to
			// the required scaling. We don't need to do any scale by <x|y>Scaling ourselves.
			var rawScale = _scaleOverride ?? (TryGetXResource(XftDotdpi, out var xrdbScaling) ? xrdbScaling.Value : 1);

			// Note: This returns nondeterministic values when testing on WSL or on a VM. Testing on a real device
			// with the same Ubuntu version returns accurate results.
			static double? mode_refresh(X11Helper.XRRModeInfo* mode_info)
			{
				double? rate;
				double vTotal = mode_info->vTotal;

				if ((mode_info->modeFlags & /* RR_DoubleScan */ 0x00000020) != 0)
				{
					/* doublescan doubles the number of lines */
					vTotal *= 2;
				}

				if ((mode_info->modeFlags & /* RR_Interlace */ 0x00000010) != 0)
				{
					/* interlace splits the frame into two fields */
					/* the field rate is what is typically reported by monitors */
					vTotal /= 2;
				}

				if (mode_info->hTotal != 0 && vTotal != 0)
				{
					rate = (mode_info->dotClock / (mode_info->hTotal * vTotal));
				}
				else
				{
					rate = null;
				}

				return rate;
			}

			// xrandr does something similar to this, except it reads the XRRModeInfo from outputInfo->modes by picking
			// a "best fit" mode and reading it, but this can actually result in dotClock == 0 in some cases and this
			// shows when calling xrandr. Reading from the CRTC info struct returns a more accurate result
			var modeSpan = new Span<X11Helper.XRRModeInfo>((void*)resources->modes, resources->nmode);
			var fps = FeatureConfiguration.CompositionTarget.FrameRate;
			foreach (var testMode in modeSpan)
			{
				if (crtcInfo->mode == testMode.id && mode_refresh(&testMode) is { } testFps)
				{
					fps = (float)testFps;
					break;
				}
			}

			return (new DisplayInformationDetails(
				rawWidth,
				rawHeight,
				(float)(rawScale * DisplayInformation.BaseDpi),
				rawScale,
				(ResolutionScale)(rawScale * 100),
				Math.Sqrt(outputInfo->mm_width * outputInfo->mm_width + outputInfo->mm_height * outputInfo->mm_height) / InchesToMilliMeters), fps);
		}
	}
}
