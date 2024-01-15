// Mostly from the FrameBuffer implementation, xdpyinfo, xrandr and Avalonia

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

using System;
using System.Drawing;
using System.Globalization;
using Windows.Graphics.Display;
using Avalonia.X11;
using Uno.Disposables;

namespace Uno.WinUI.Runtime.Skia.X11
{
	internal class X11DisplayInformationExtension : IDisplayInformationExtension
	{
		private const string EnvironmentUnoDisplayScaleOverride = "UNO_DISPLAY_SCALE_OVERRIDE";
		private const double InchesToMilliMeters = 25.4;

		private readonly float? _scaleOverride;
		private DisplayInformationDetails _details;
		private DisplayInformation _owner;

		private record DisplayInformationDetails(
			uint ScreenWidthInRawPixels,
			uint ScreenHeightInRawPixels,
			float LogicalDpi,
			double RawPixelsPerViewPixel,
			ResolutionScale ResolutionScale,
			double? DiagonalSizeInInches);

		public X11DisplayInformationExtension(object owner, float? scaleOverride)
		{
			_owner = (DisplayInformation)owner;
			_scaleOverride = scaleOverride;

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

			var x11Window = X11XamlRootHost.GetWindow();
			var host = X11XamlRootHost.GetXamlRootHostFromX11Window(x11Window);
			host?.SetDisplayInformationExtension(this);
		}

		// TODO: this can probably be improved using the Xrandr extension
		internal void UpdateDetails()
		{
			var x11Window = X11XamlRootHost.GetWindow();

			var oldDetails = _details;
			var xLock = X11Helper.XLock(x11Window.Display);
			using var __ = Disposable.Create(() =>
			{
				// dispose lock before raising DpiChanged in case a user defined callback takes too long.
				xLock.Dispose();
				if (_details != oldDetails)
				{
					_owner.NotifyDpiChanged();
				}
			});

			XWindowAttributes attributes = default;
			XLib.XGetWindowAttributes(x11Window.Display, x11Window.Window, ref attributes);
			var screen = attributes.screen;

			if (XLib.XRRQueryExtension(x11Window.Display, out _, out _) != 0 &&
				XLib.XRRQueryVersion(x11Window.Display, out var major, out var minor) != 0 &&
				(major > 1 || (major == 1 && minor >= 3)) &&
				GetDisplayInformationXRandR1_3(x11Window.Display, x11Window.Window) is { }  details)
			{
					_details = details;
			}
			else // naive implementation from xdpyinfo
			{
				// Using X<Width|Height>OfScreen calls seems to work more reliably than XDisplay<Width|Height>.
				// Mostly because XScreenNumberOfScreen (which we need for XDisplay<Width|Height>) isn't working reliably
				var xres = X11Helper.XWidthOfScreen(screen) * InchesToMilliMeters / X11Helper.XWidthMMOfScreen(screen);
				var yres = X11Helper.XHeightOfScreen(screen) * InchesToMilliMeters / X11Helper.XHeightMMOfScreen(screen);
				var dpi = (float)Math.Round(Math.Sqrt(xres * yres)); // TODO: what to do if dpi in the 2 normal directions is different??

				var rawScale = _scaleOverride ?? dpi / DisplayInformation.BaseDpi;

				var flooredScale = FloorScale(rawScale);

				// This returns very incorrect numbers as far as I've tested.
				var widthInInches = (uint)Math.Round(X11Helper.XWidthMMOfScreen(screen) / InchesToMilliMeters);
				var heightInInches = (uint)Math.Round(X11Helper.XHeightMMOfScreen(screen) / InchesToMilliMeters);

				_details = new(
					(uint)X11Helper.XWidthOfScreen(screen),
					(uint)X11Helper.XHeightOfScreen(screen),
					flooredScale * DisplayInformation.BaseDpi,
					flooredScale,
					(ResolutionScale)(int)(flooredScale * 100.0),
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

		private static float FloorScale(double rawDpi)
			=> rawDpi switch
			{
				>= 5.00f => 5.00f,
				>= 4.50f => 4.50f,
				>= 4.00f => 4.00f,
				>= 3.50f => 3.50f,
				>= 3.00f => 3.00f,
				>= 2.50f => 2.50f,
				>= 2.25f => 2.25f,
				>= 2.00f => 2.00f,
				>= 1.80f => 1.80f,
				>= 1.75f => 1.75f,
				>= 1.60f => 1.60f,
				>= 1.50f => 1.50f,
				>= 1.40f => 1.40f,
				>= 1.25f => 1.25f,
				>= 1.20f => 1.20f,
				>= 1.00f => 1.00f,
				_ => 1.00f,
			};

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
		private unsafe DisplayInformationDetails? GetDisplayInformationXRandR1_3(IntPtr display, IntPtr window)
		{
			using var _1 = X11Helper.XLock(display);

			var resources = X11Helper.XRRGetScreenResourcesCurrent(display, window);
			using var _2 = Disposable.Create(() => X11Helper.XRRFreeScreenResources(resources));

			XLib.XQueryTree(display, XLib.XDefaultRootWindow(display), out IntPtr root, out _, out _, out _);
			XWindowAttributes windowAttrs = default;
			XLib.XGetWindowAttributes(display, window, ref windowAttrs);
			XLib.XTranslateCoordinates(display, window, root, windowAttrs.x, windowAttrs.y, out var rootx, out var rooty, out _);

			X11Helper.XRRCrtcInfo* crtcInfo = default;
			IntPtr crtc = default;
			double bestArea = 1;
			foreach (var crtc_ in new Span<IntPtr>(resources->crtcs.ToPointer(), resources->ncrtc)) {
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

			using var _3 = Disposable.Create(() => X11Helper.XRRFreeCrtcInfo(crtcInfo));

			if (crtcInfo == default || crtcInfo->noutput == 0)
			{
				return null;
			}

			// We assume each CRTC is connected to one physical output. If not, we take the first one.
			// This means that if two physical displays duplicate the same pixels, we will use
			// the first one we find for the numbers

			var outputInfo = X11Helper.XRRGetOutputInfo(display, new IntPtr(resources), *(IntPtr*)crtcInfo->outputs.ToPointer());
			using var _4 = Disposable.Create(() => X11Helper.XRRFreeOutputInfo(outputInfo));

			X11Helper.XRRCrtcTransformAttributes* transformInfo = default;
			X11Helper.XRRGetCrtcTransform(display, crtc, ref transformInfo);
			using var _5 = Disposable.Create(() => XLib.XFree(new IntPtr(transformInfo)));

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

			// TODO: how to reconcile different x and y scaling? for now just take max
			var rawScale = _scaleOverride ?? (1 / Math.Min(xScaling, yScaling));
			var flooredScale = FloorScale(rawScale);

			return new DisplayInformationDetails(
				rawWidth,
				rawHeight,
				flooredScale * DisplayInformation.BaseDpi,
				flooredScale,
				(ResolutionScale)(FloorScale(flooredScale) * 100),
				Math.Sqrt(outputInfo->mm_width * outputInfo->mm_width + outputInfo->mm_height * outputInfo->mm_height) / InchesToMilliMeters
			);
		}
	}
}
