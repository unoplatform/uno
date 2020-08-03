#nullable enable
using System;
using System.Runtime.InteropServices;
using Windows.Graphics.Display;

namespace Uno.UI.Skia.Platform
{
	internal class WpfDisplayInformationExtension : IDisplayInformationExtension
	{
		private readonly DisplayInformation _displayInformation;

		public WpfDisplayInformationExtension(object owner)
		{
			_displayInformation = (DisplayInformation)owner;
		}

		public DisplayOrientations CurrentOrientation => DisplayOrientations.Landscape;

		public uint ScreenHeightInRawPixels => 0;

		public uint ScreenWidthInRawPixels => 0;

		public float LogicalDpi
		{
			get => 96.0f;
		}

		public double RawPixelsPerViewPixel => LogicalDpi / 96.0f;

		public ResolutionScale ResolutionScale => (ResolutionScale)(int)(RawPixelsPerViewPixel * 100.0);

		public void StartDpiChanged()
		{
		}

		public void StopDpiChanged()
		{
		}
	}
}
