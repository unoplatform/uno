#nullable enable

using SkiaSharp;
using Uno.UI.Composition.Drawing;

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
	// Per-seam Skia defaults — each RETURNS a public seam instance (IFontProvider / IImageEncoderDecoder / …); the caller
	// (DrawingBackendFallback) registers it via the framework's own internal RegisterDefault. Internal: reflection
	// reaches them with BindingFlags.NonPublic, no IVT required.
	internal static IFontProvider CreateFontProvider() => new SkiaFontProvider();

	internal static IImageEncoderDecoder CreateImageDecoder() => new SkiaImageDecoderBackend();

	internal static IGeometryFactory CreateGeometryFactory() => new SkiaGeometryFactory();

	/// <summary>The Skia graphics provider (the backend negotiation picks a context and builds its drawing factory).</summary>
	internal static IGraphicsProvider CreateGraphicsProvider() => new SkiaGraphicsProvider();

	/// <summary>The neutral default renderer for heads that don't install their own (e.g. the native Skia path).</summary>
	internal static IDrawingFactory CreateDefaultRenderer() => new SkiaDrawingFactory();
}
