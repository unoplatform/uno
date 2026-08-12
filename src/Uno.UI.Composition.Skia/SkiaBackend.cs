#nullable enable

using SkiaSharp;
using Uno.Foundation.Extensibility;
using Uno.UI.Composition.Drawing;
using Uno.UI.Graphics;

namespace Uno.UI.Composition.Skia;

/// <summary>
/// Explicit entry point that installs the SkiaSharp drawing backend. A Skia host calls this during startup —
/// before the first layout/measure that resolves fonts/images through <see cref="DrawingFactory.Current"/> —
/// so the backend is registered independently of assembly load timing. (The backend also self-registers via
/// module initializers for standalone consumers that touch its types directly, e.g. tests and offscreen tools.)
/// </summary>
public static class SkiaBackend
{
	public static void Register()
	{
		// Calling into this assembly triggers its module initializers (they install the libSkiaSharp resolver
		// and register the backend). Registering again here is idempotent and makes the intent explicit.
		DrawingFactory.Register(new SkiaDrawingFactory());

		// Image decoding is an independent, render-backend-agnostic seam. When managed decoding is selected, install
		// the fully-managed backend (byte[]-backed IImage, no Skia object created) so an image-bearing app can run
		// with no native libSkiaSharp; otherwise the Skia codec (which still tries the managed parse in front).
		ImageDecoder.Current = DrawingBackendOptions.UseManagedImageDecoder
			? new ManagedImageDecoderBackend()
			: new SkiaImageDecoderBackend();

		// Font resolution is likewise render-backend-independent; install the Skia resolver (or a host override).
		FontProvider.Current = DrawingBackendOptions.FontProvider ?? new SkiaFontProvider();

		// The SKCanvasElement (raw-Skia) visual factory lives here because SKCanvasVisual reaches the concrete
		// SkiaDrawingSession; the public 2dsk package resolves it through the neutral factory abstraction.
		ApiExtensibility.Register(typeof(SKCanvasVisualBaseFactory), _ => new SKCanvasVisualFactory());

		// Composition-root backend selection for the pluggable graphics pipeline: register Skia as the
		// available backend. A host that drives the neutral loop (GraphicsRegistry.Initialize) picks it up; swap
		// this list to run a different backend. The choice lives here, not in the render loop or the host.
		GraphicsRegistry.Register(new IGraphicsProvider[] { new SkiaGraphicsProvider() });
	}
}
