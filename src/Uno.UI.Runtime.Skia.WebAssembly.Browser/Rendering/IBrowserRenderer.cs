using SkiaSharp;

namespace Uno.UI.Runtime.Skia;

internal interface IBrowserRenderer
{
	void MakeCurrent();
	SKCanvas Resize(int width, int height);
	void Flush();
}
