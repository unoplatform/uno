using Uno.UI.Rendering;

namespace Uno.UI.Runtime.Skia.Gtk;

internal interface IGtkRenderer : IRenderer
{
	void InvalidateRender();

	void TakeScreenshot(string filePath);
}
