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
	// Per-seam Skia defaults, invoked BY REFLECTION from DrawingBackendFallback (in the neutral Drawing assembly,
	// which has no compile-time dependency on this backend) — one per empty seam, so a WebGPU head still gets the
	// Skia font/image defaults but never the Skia renderer. They are internal: apps register through the host builder,
	// never here. There is intentionally no public "install the whole backend" entry point.
	internal static void RegisterDefaultFontProvider()
		=> FontProvider.RegisterDefault(new SkiaFontProvider());

	internal static void RegisterDefaultImageDecoder()
		=> ImageDecoder.RegisterDefault(new SkiaImageDecoderBackend());

	/// <summary>Registers the Skia image encoder for <c>BitmapEncoder</c> (Uno.UWP), an imaging-library-agnostic seam.</summary>
	internal static void RegisterImageEncoder()
		=> ApiExtensibility.Register(typeof(global::Windows.Graphics.Imaging.IImageEncoderExtension), _ => new SkiaImageEncoderExtension());

	/// <summary>
	/// Installs the Skia graphics BACKEND — the matched (drawing factory, renderer) pair plus the raw-Skia
	/// SKCanvasElement factory. This is the seam a WebGPU/managed head OWNS by declaring its own backend, so the
	/// implicit fallback only calls this when no backend was declared (see <c>DrawingBackendFallback</c>).
	/// </summary>
	internal static void RegisterDefaultGraphics()
	{
		// The SKCanvasElement (raw-Skia) visual factory lives here because SKCanvasVisual reaches the concrete
		// SkiaDrawingSession; the public 2dsk package resolves it through the neutral factory abstraction.
		ApiExtensibility.Register(typeof(SKCanvasVisualBaseFactory), _ => new SKCanvasVisualFactory());

		// Composition-root backend selection for the pluggable graphics pipeline: register Skia as the available
		// backend (register-if-absent — a head that declared its own provider list wins). Negotiation
		// (GraphicsRegistry.Initialize) then installs this provider's own drawing factory as DrawingFactory.Current;
		// there is no separate DrawingFactory fallback (the provider carries the factory).
		GraphicsRegistry.RegisterDefault(new IGraphicsProvider[] { new SkiaGraphicsProvider() });

		// The framework (Uno.UI) is backend-agnostic and no longer defaults CompositionTarget.Renderer to a Skia
		// type. Provide the Skia renderer as the neutral default so heads that don't install their own renderer
		// (e.g. the native Skia render path) still render; WebGPU heads override CompositionTarget.Renderer directly.
		DrawingRegistration.RegisterDefaultRenderer(new SkiaRenderer());
	}
}
