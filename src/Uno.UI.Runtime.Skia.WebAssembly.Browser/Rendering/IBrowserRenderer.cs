using Uno.UI.Composition.Drawing;

namespace Uno.UI.Runtime.Skia;

internal interface IBrowserRenderer
{
	void MakeCurrent();
	// Returns a backend-neutral render target; no Skia type crosses this boundary.
	IRenderTarget Resize(int width, int height);
	void Flush();
	bool NeedsForceResize();
}
