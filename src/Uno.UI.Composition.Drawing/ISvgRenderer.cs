#nullable enable

using Windows.Foundation;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// App-registerable seam for turning SVG markup into a renderable document. Independent of the graphics backend
/// (like <see cref="FontProvider"/> / <see cref="ImageDecoder"/> / <see cref="GeometryFactory"/>): the returned
/// <see cref="ISvgDocument"/> draws through the neutral <see cref="IDrawingSession"/>, so it works under any backend
/// and creates no backend resources itself. Register via the host builder; the framework defaults to its managed
/// (SkiaSharp-free) engine when none is set.
/// </summary>
public interface ISvgRenderer
{
	/// <summary>Parses SVG markup into a renderable <see cref="ISvgDocument"/>, or null when the bytes aren't SVG the renderer can handle.</summary>
	ISvgDocument? Parse(byte[] svg);
}

/// <summary>
/// A parsed SVG document — the <em>retained</em> vector representation. <see cref="Render"/> replays it into any
/// <see cref="IDrawingSession"/>: an offscreen session to rasterize at a chosen size, or a live per-frame session to
/// draw it directly. The document owns no backend resources; the caller supplies the session (and thus the backend).
/// </summary>
public interface ISvgDocument
{
	/// <summary>The document's intrinsic size (from its viewBox / width-height).</summary>
	Size SourceSize { get; }

	/// <summary>Draws the document into <paramref name="session"/>, scaled to fit <paramref name="targetSize"/> (uniform, centered).</summary>
	void Render(IDrawingSession session, Size targetSize);
}

/// <summary>Holds the registered <see cref="ISvgRenderer"/>. Set once at startup (host builder / app).</summary>
public static class SvgRenderer
{
	/// <summary>The active SVG renderer, or null when none is registered (the SVG consumer falls back to its default).</summary>
	public static ISvgRenderer? Current { get; set; }
}
