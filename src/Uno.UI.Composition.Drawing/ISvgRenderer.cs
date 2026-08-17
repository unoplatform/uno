#nullable enable

using Windows.Foundation;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// App-registerable seam for turning SVG markup into a renderable document. Independent of the graphics backend
/// (like <see cref="FontProvider"/> / <see cref="ImageEncoderDecoder"/> / <see cref="GeometryFactory"/>): the returned
/// <see cref="ISvgDocument"/> draws through the neutral <see cref="IDrawingSession"/>, so it works under any backend
/// and creates no backend resources itself. Register via the host builder; the framework defaults to its managed
/// (SkiaSharp-free) engine when none is set.
/// </summary>
public interface ISvgRenderer
{
	/// <summary>Parses SVG markup into a renderable <see cref="ISvgDocument"/>, or null when the bytes aren't SVG the renderer can handle.</summary>
	/// <param name="geometry">Registered geometry factory the renderer builds its shape/path geometry with.</param>
	/// <param name="drawing">Registered drawing factory the renderer mints its gradient shaders with.</param>
	ISvgDocument? Parse(byte[] svg, IGeometryFactory geometry, IDrawingFactory drawing);
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

/// <summary>
/// Holds the registered <see cref="ISvgRenderer"/>. When none was explicitly registered (host builder), the framework
/// lazily lights up its default — the Skia SVG engine when the Skia backend is present — on first access, exactly like
/// the <see cref="FontProvider"/> / <see cref="ImageEncoderDecoder"/> / <see cref="GeometryFactory"/> holders. An app
/// can override it by registering its own (e.g. the managed engine) via the host builder.
/// </summary>
public static class SvgRenderer
{
	private static ISvgRenderer? _current;

	/// <summary>
	/// The active SVG renderer, or null only on a head with no SVG renderer at all (SVG then simply doesn't render).
	/// Reading it lazily lights up the per-seam Skia default when nothing was explicitly registered.
	/// </summary>
	public static ISvgRenderer? Current
	{
		get
		{
			if (_current is null)
			{
				DrawingBackendFallback.EnsureSvgRenderer();
			}

			return _current;
		}
		internal set => _current = value;
	}

	/// <summary>Registers <paramref name="renderer"/> only if none is set yet (the per-seam fallback default).</summary>
	internal static void RegisterDefault(ISvgRenderer renderer) => _current ??= renderer;
}
