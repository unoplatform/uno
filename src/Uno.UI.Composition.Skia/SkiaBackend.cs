#nullable enable

using SkiaSharp;
using Uno.UI.Composition.Drawing;

namespace Uno.UI.Composition.Skia;

/// <summary>
/// The SkiaSharp backend's bootstrap surface, invoked by reflection from the host builder (which keeps no
/// compile-time dependency on this backend). Each factory returns a public neutral-seam instance that the
/// framework registers through its own internal registrars, so no Drawing internal or IVT is needed here.
/// </summary>
public static class SkiaBackend
{
	// Per-seam Skia defaults, each returning a public seam instance the host builder registers. Kept internal:
	// reflection reaches them with BindingFlags.NonPublic, so no IVT is required.
	internal static IFontProvider CreateFontProvider() => new SkiaFontProvider();

	internal static IImageEncoderDecoder CreateImageDecoder() => new SkiaImageDecoderBackend();

	internal static IGeometryFactory CreateGeometryFactory() => new SkiaGeometryFactory();

	/// <summary>The Skia graphics provider (the backend negotiation picks a context and builds its drawing factory).</summary>
	internal static IGraphicsProvider CreateGraphicsProvider() => new SkiaGraphicsProvider();

	/// <summary>The neutral default renderer for heads that don't install their own (e.g. the native Skia path).</summary>
	internal static IDrawingFactory CreateDefaultRenderer() => new SkiaDrawingFactory();
}
