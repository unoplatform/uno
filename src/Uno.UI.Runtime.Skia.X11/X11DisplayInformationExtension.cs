// Mostly from the FrameBuffer implementation and xdpyinfo

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

using System;
using System.Globalization;
using Windows.Graphics.Display;
using Avalonia.X11;
using Uno.Foundation.Logging;

namespace Uno.WinUI.Runtime.Skia.X11
{
	internal class X11DisplayInformationExtension : IDisplayInformationExtension
	{
		private const string EnvironmentUnoDisplayScaleOverride = "UNO_DISPLAY_SCALE_OVERRIDE";
		private const double MillimetersToInches = 0.0393700787;

		private readonly float? _scaleOverride;
		private DisplayInformationDetails _details;

		private record DisplayInformationDetails(
			uint ScreenWidthInRawPixels,
			uint ScreenHeightInRawPixels,
			float LogicalDpi,
			double RawPixelsPerViewPixel,
			ResolutionScale ResolutionScale,
			double? DiagonalSizeInInches);

		public X11DisplayInformationExtension(object owner, float? scaleOverride)
		{
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
			_details = new DisplayInformationDetails(default, default, default, default, default, default); //
			UpdateDetails();
		}

		// TODO: this can probably be improved using the Xrandr extension
		private void UpdateDetails()
		{
			var x11Window = X11XamlRootHost.GetWindow();
			XWindowAttributes attributes = default;
			XLib.XGetWindowAttributes(x11Window.Display, x11Window.Window, ref attributes);
			var screen = attributes.screen;

			/*
			 * there are 2.54 centimeters to an inch; so there are 25.4 millimeters.
			 *
			 *     dpi = N pixels / (M millimeters / (25.4 millimeters / 1 inch))
			 *         = N pixels / (M inch / 25.4)
			 *         = N * 25.4 pixels / M inch
			 */

			// Using X<Width|Height>OfScreen calls seems to work more reliably than XDisplay<Width|Height>.
			// Mostly because XScreenNumberOfScreen (which we need for XDisplay<Width|Height>) isn't working reliably

			var xres = X11Helper.XWidthOfScreen(screen) * 25.4 / X11Helper.XWidthMMOfScreen(screen);
			var yres = X11Helper.XHeightOfScreen(screen) * 25.4 / X11Helper.XHeightMMOfScreen(screen);
			var dpi = (float)Math.Round(Math.Sqrt(xres * yres)); // TODO: what to do if dpi in the 2 normal directions is different??

			var rawScale = _scaleOverride ?? dpi / DisplayInformation.BaseDpi;

			var flooredScale = FloorScale(rawScale);

			// This returns very incorrect numbers as far as I've tested.
			var widthInInches = (uint)Math.Round(X11Helper.XWidthMMOfScreen(screen) / 25.4);
			var heightInInches = (uint)Math.Round(X11Helper.XHeightMMOfScreen(screen) / 25.4);

			_details = new(
				(uint)X11Helper.XWidthOfScreen(screen),
				(uint)X11Helper.XHeightOfScreen(screen),
				flooredScale * DisplayInformation.BaseDpi,
				flooredScale,
				(ResolutionScale)(int)(flooredScale * 100.0),
				Math.Sqrt(widthInInches * widthInInches + heightInInches * heightInInches)
			);
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

		// TODO: The obvious way to implement this is through polling, but that looks like overkill.
		public void StartDpiChanged() => this.Log().Error($"{nameof(StartDpiChanged)} not supported on MacOS.");

		public void StopDpiChanged() => this.Log().Error($"{nameof(StopDpiChanged)} not supported on MacOS.");

		private static float FloorScale(float rawDpi)
			=> rawDpi switch
			{
				> 5.00f => 5.00f,
				> 4.50f => 4.50f,
				> 4.00f => 4.00f,
				> 3.50f => 3.50f,
				> 3.00f => 3.00f,
				> 2.50f => 2.50f,
				> 2.25f => 2.25f,
				> 2.00f => 2.00f,
				> 1.80f => 1.80f,
				> 1.75f => 1.75f,
				> 1.60f => 1.60f,
				> 1.50f => 1.50f,
				> 1.40f => 1.40f,
				> 1.25f => 1.25f,
				> 1.20f => 1.20f,
				> 1.00f => 1.00f,
				_ => 1.00f,
			};
	}
}
