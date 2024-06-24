using SkiaSharp;

namespace Uno.WinUI.Runtime.Skia.X11;

internal interface IX11Renderer
{
	void Render();

	void SetBackgroundColor(SKColor color);
}
