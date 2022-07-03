using SkiaSharp;

namespace Uno.UI.Runtime.Skia
{
	internal interface IRenderSurface
	{
		void TakeScreenshot(string filePath);


		/// <summary>
		/// Background color for the render surface
		/// </summary>
		SKColor BackgroundColor { get; set; }
	}
}
