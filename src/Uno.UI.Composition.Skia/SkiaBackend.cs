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
		// Calling into this assembly triggers its module initializers (they install the libSkiaSharp resolver and
		// register the backend). Install the Skia drawing backend as a register-if-absent DEFAULT (geometry lives on
		// the backend, so an app that registered its own IDrawingFactory path-implementor before this call wins).
		DrawingFactory.RegisterDefault(new SkiaDrawingFactory());

		// Image decoding and font resolution ARE backend-independent content seams: each is installed as a
		// register-if-absent DEFAULT so an app that registered its own implementor (any IImageDecoder / IFontProvider
		// — e.g. the SkiaSharp-free managed engines) before this call wins. Otherwise the Skia-backed defaults apply.
		ImageDecoder.RegisterDefault(new SkiaImageDecoderBackend());
		FontProvider.RegisterDefault(new SkiaFontProvider());

		// The SKCanvasElement (raw-Skia) visual factory lives here because SKCanvasVisual reaches the concrete
		// SkiaDrawingSession; the public 2dsk package resolves it through the neutral factory abstraction.
		ApiExtensibility.Register(typeof(SKCanvasVisualBaseFactory), _ => new SKCanvasVisualFactory());

		// BitmapEncoder (Uno.UWP) is imaging-library-agnostic and resolves its encoder through this seam.
		ApiExtensibility.Register(typeof(global::Windows.Graphics.Imaging.IImageEncoderExtension), _ => new SkiaImageEncoderExtension());

		// Composition-root backend selection for the pluggable graphics pipeline: register Skia as the
		// available backend. A host that drives the neutral loop (GraphicsRegistry.Initialize) picks it up; swap
		// this list to run a different backend. The choice lives here, not in the render loop or the host.
		GraphicsRegistry.Register(new IGraphicsProvider[] { new SkiaGraphicsProvider() });

		// The framework (Uno.UI) is backend-agnostic and no longer defaults CompositionTarget.Renderer to a Skia
		// type. Provide the Skia renderer as the neutral default so heads that don't install their own renderer
		// (e.g. the native Skia render path) still render; WebGPU heads override CompositionTarget.Renderer directly.
		DrawingRegistration.DefaultRenderer = new SkiaRenderer();
	}
}
