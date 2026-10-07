#nullable enable

using SkiaSharp;
using Uno.UI.Composition.Drawing;

namespace Uno.UI.Composition.Skia;

/// <summary>
/// Entry point to the SkiaSharp backend. Each factory returns the neutral seam, for an app to register on the host
/// builder (e.g. <c>builder.FontProvider(SkiaBackend.CreateFontProvider())</c>, as a trimmed or AOT head must); the
/// host builder's own defaults are created through the same methods, by name. The implementations stay internal.
/// </summary>
public static class SkiaBackend
{
	public static IFontProvider CreateFontProvider() => new SkiaFontProvider();

	public static IImageEncoderDecoder CreateImageDecoder() => new SkiaImageDecoderBackend();

	public static IGeometryFactory CreateGeometryFactory() => new SkiaGeometryFactory();

	/// <summary>The Skia graphics provider (the backend negotiation picks a context and builds its drawing factory).</summary>
	public static IGraphicsProvider CreateGraphicsProvider() => new SkiaGraphicsProvider();

	/// <summary>The Skia graphics provider, negotiating the given context kinds in this order (e.g.
	/// <see cref="GraphicsContextKind.Software"/> alone forces software rendering).</summary>
	public static IGraphicsProvider CreateGraphicsProvider(params GraphicsContextKind[] preferred) => new SkiaGraphicsProvider(preferred);

	/// <summary>The neutral default renderer for heads that don't install their own (e.g. the native Skia path).</summary>
	internal static IDrawingFactory CreateDefaultRenderer() => new SkiaDrawingFactory();
}
