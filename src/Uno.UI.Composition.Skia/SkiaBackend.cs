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
	/// <summary>
	/// Installs the whole SkiaSharp backend (every seam). Split into per-seam entry points so the implicit fallback
	/// (<c>DrawingBackendFallback</c>) can light up ONLY the seam that is actually empty — e.g. a WebGPU head that
	/// declared its own renderer still gets the Skia font/image defaults, but never the Skia renderer.
	/// </summary>
	public static void Register()
	{
		// Calling into this assembly triggers its module initializers (they install the libSkiaSharp resolver).
		RegisterDefaultFontProvider();
		RegisterDefaultImageDecoder();
		RegisterImageEncoder();
		RegisterDefaultGraphics();
	}

	/// <summary>Register-if-absent Skia font resolver — a render-independent content seam (an app that registered its
	/// own <see cref="IFontProvider"/>, e.g. the managed engine, wins).</summary>
	public static void RegisterDefaultFontProvider()
		=> FontProvider.RegisterDefault(new SkiaFontProvider());

	/// <summary>Register-if-absent Skia image decoder — a render-independent content seam (an app-registered
	/// <see cref="IImageDecoder"/> wins).</summary>
	public static void RegisterDefaultImageDecoder()
		=> ImageDecoder.RegisterDefault(new SkiaImageDecoderBackend());

	/// <summary>Registers the Skia image encoder for <c>BitmapEncoder</c> (Uno.UWP), an imaging-library-agnostic seam.</summary>
	public static void RegisterImageEncoder()
		=> ApiExtensibility.Register(typeof(global::Windows.Graphics.Imaging.IImageEncoderExtension), _ => new SkiaImageEncoderExtension());

	/// <summary>
	/// Installs the Skia graphics BACKEND — the matched (drawing factory, renderer) pair plus the raw-Skia
	/// SKCanvasElement factory. This is the seam a WebGPU/managed head OWNS by declaring its own backend, so the
	/// implicit fallback only calls this when no backend was declared (see <c>DrawingBackendFallback</c>).
	/// </summary>
	public static void RegisterDefaultGraphics()
	{
		// Geometry lives on the backend factory, so an app that registered its own IDrawingFactory path-implementor
		// before this call wins.
		DrawingFactory.RegisterDefault(new SkiaDrawingFactory());

		// The SKCanvasElement (raw-Skia) visual factory lives here because SKCanvasVisual reaches the concrete
		// SkiaDrawingSession; the public 2dsk package resolves it through the neutral factory abstraction.
		ApiExtensibility.Register(typeof(SKCanvasVisualBaseFactory), _ => new SKCanvasVisualFactory());

		// Composition-root backend selection for the pluggable graphics pipeline: register Skia as the available
		// backend (register-if-absent — a head that declared its own provider list wins).
		GraphicsRegistry.RegisterDefault(new IGraphicsProvider[] { new SkiaGraphicsProvider() });

		// The framework (Uno.UI) is backend-agnostic and no longer defaults CompositionTarget.Renderer to a Skia
		// type. Provide the Skia renderer as the neutral default so heads that don't install their own renderer
		// (e.g. the native Skia render path) still render; WebGPU heads override CompositionTarget.Renderer directly.
		DrawingRegistration.RegisterDefaultRenderer(new SkiaRenderer());
	}
}
