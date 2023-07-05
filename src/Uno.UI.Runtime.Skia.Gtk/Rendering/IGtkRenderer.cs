#nullable disable

using Uno.UI.Rendering;

namespace Uno.UI.Runtime.Skia;

internal interface IGtkRenderer : IRenderer
{
	void InvalidateRender();

	void TakeScreenshot(string filePath);
}
