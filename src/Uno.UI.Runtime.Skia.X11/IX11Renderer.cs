using SkiaSharp;

namespace Uno.WinUI.Runtime.Skia.X11;

internal interface IX11Renderer
{
	void Render(SKPicture picture, SKPath nativeClippingPath, float scaleX, float scaleY);

	void SetBackgroundColor(SKColor color);
}
