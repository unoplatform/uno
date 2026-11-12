#nullable enable

using Uno.UI.Composition.Drawing;

namespace Uno.UI.Composition.Managed;

/// <summary>
/// Entry point to the SkiaSharp-free managed engines. Each factory returns the neutral seam, for an app to register
/// on the host builder (e.g. <c>builder.SvgRenderer(ManagedBackend.CreateSvgRenderer())</c>); the host builder's own
/// defaults are created through the same methods. The implementations stay internal.
/// </summary>
public static class ManagedBackend
{
	public static IGeometryFactory CreateGeometryFactory() => new ManagedGeometryFactory();

	public static IFontProvider CreateFontProvider() => new ManagedFontProvider();

	/// <param name="bundledDefaultFont">
	/// A font file used as the default face, for a platform with no enumerable system fonts (the iOS sandbox, the
	/// browser), where the provider would otherwise have no guaranteed default.
	/// </param>
	/// <param name="enableFallback">
	/// <c>false</c> renders only from the requested fonts: a missing glyph stays missing instead of coming from an
	/// installed font or, in the browser, a downloaded Noto font.
	/// </param>
	public static IFontProvider CreateFontProvider(byte[]? bundledDefaultFont = null, bool enableFallback = true)
		=> new ManagedFontProvider(bundledDefaultFont, enableFallback);

	public static IImageEncoderDecoder CreateImageDecoder() => new ManagedImageDecoderBackend();

	public static ISvgRenderer CreateSvgRenderer() => new ManagedSvgRenderer();

	public static ILottieRenderer CreateLottieRenderer() => new ManagedLottieRenderer();
}
