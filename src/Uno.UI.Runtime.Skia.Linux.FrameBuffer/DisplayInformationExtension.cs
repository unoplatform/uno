using Uno.WinUI.Runtime.Skia.LinuxFB;
using Windows.Graphics.Display;

namespace Uno.UI.Runtime.Skia
{
	internal class DisplayInformationExtension : IDisplayInformationExtension
	{
		private readonly DisplayInformation _displayInformation;
		public Renderer? Renderer { get; set; }

		public DisplayInformationExtension(object owner)
		{
			_displayInformation = (DisplayInformation)owner;
		}

		public DisplayOrientations CurrentOrientation => DisplayOrientations.Landscape;

		public uint ScreenHeightInRawPixels
			=> (uint)(Renderer?.PixelSize.Height ?? 0);

		public uint ScreenWidthInRawPixels
			=> (uint)(Renderer?.PixelSize.Width ?? 0);

		public float LogicalDpi => DisplayInformation.BaseDpi;

		public double RawPixelsPerViewPixel => LogicalDpi / DisplayInformation.BaseDpi;

		public ResolutionScale ResolutionScale => (ResolutionScale)(int)(RawPixelsPerViewPixel * 100.0);

		public void StartDpiChanged()
		{

		}

		public void StopDpiChanged()
		{

		}
	}
}
