using Gtk;
using SkiaSharp;

namespace Uno.UI.Runtime.Skia
{
	internal interface IRenderSurface
	{
		Widget Widget { get; }

		void TakeScreenshot(string filePath);

		/// <summary>
		/// Background color for the render surface
		/// </summary>
		SKColor BackgroundColor { get; set; }

		void InvalidateRender();
	}
}
