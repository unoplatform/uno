#nullable enable

using SkiaSharp;
using Uno.Foundation.Extensibility;
using Uno.UI.Composition.Drawing;
using Uno.UI.Graphics;

namespace Uno.UI.Composition.Skia;

/// <summary>
/// The SkiaSharp drawing backend's reflective bootstrap surface, invoked BY REFLECTION from
/// <see cref="DrawingBackendFallback"/> in the neutral Drawing assembly (which keeps no compile-time dependency on
/// this backend). Each factory here returns a <em>public neutral-seam</em> instance; the framework then registers it
/// through its own internal per-seam registrars — so this backend reaches no Drawing internal and needs no
/// InternalsVisibleTo from Drawing. Apps register through the host builder, not here.
/// </summary>
public static class SkiaBackend
{
	// Per-seam Skia defaults — each RETURNS a public seam instance (IFontProvider / IImageDecoder / …); the caller
	// (DrawingBackendFallback) registers it via the framework's own internal RegisterDefault. Internal: reflection
	// reaches them with BindingFlags.NonPublic, no IVT required.
	internal static IFontProvider CreateFontProvider() => new SkiaFontProvider();

	internal static IImageDecoder CreateImageDecoder() => new SkiaImageDecoderBackend();

	internal static IGeometryFactory CreateGeometryFactory() => new SkiaGeometryFactory();

	/// <summary>The Skia graphics provider (the backend negotiation picks a context and builds its drawing factory).</summary>
	internal static IGraphicsProvider CreateGraphicsProvider() => new SkiaGraphicsProvider();

	/// <summary>The neutral default renderer for heads that don't install their own (e.g. the native Skia path).</summary>
	internal static IDrawingFactory CreateDefaultRenderer() => new SkiaDrawingFactory();

	/// <summary>Registers the Skia image encoder for <c>BitmapEncoder</c> (Uno.UWP) — a public, imaging-library-agnostic
	/// <see cref="ApiExtensibility"/> seam, so no Uno.UWP internal is touched.</summary>
	internal static void RegisterImageEncoder()
		=> ApiExtensibility.Register(typeof(global::Windows.Graphics.Imaging.IImageEncoderExtension), _ => new SkiaImageEncoderExtension());

	/// <summary>
	/// Registers the raw-Skia <c>SKCanvasElement</c> visual factory. This one genuinely reaches Composition-internal
	/// render-loop types (<c>SKCanvasVisualBase</c> + the internal paint hook), so it still requires the
	/// Composition→Skia InternalsVisibleTo — separate from the (now-removed) Drawing→Skia one.
	/// </summary>
	internal static void RegisterSKCanvasElementFactory()
		=> ApiExtensibility.Register(typeof(SKCanvasVisualBaseFactory), _ => new SKCanvasVisualFactory());
}
