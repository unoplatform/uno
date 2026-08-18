#nullable enable

using Windows.Foundation;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// App-registerable seam for turning SVG markup into a renderable document, independent of the graphics backend:
/// the returned <see cref="ISvgDocument"/> draws through the neutral <see cref="IDrawingSession"/>. Register via
/// the host builder; the framework defaults to its managed engine when none is set.
/// </summary>
public interface ISvgRenderer
{
	/// <summary>Parses SVG markup into a renderable <see cref="ISvgDocument"/>, or null when the bytes aren't SVG the renderer can handle.</summary>
	/// <param name="geometry">Registered geometry factory the renderer builds its shape/path geometry with.</param>
	/// <param name="drawing">Registered drawing factory the renderer mints its gradient shaders with.</param>
	ISvgDocument? Parse(byte[] svg, IGeometryFactory geometry, IDrawingFactory drawing);
}

/// <summary>
/// A parsed SVG document — the retained vector representation. <see cref="Render"/> replays it into any
/// <see cref="IDrawingSession"/>. The document owns no backend resources; the caller supplies the session.
/// </summary>
public interface ISvgDocument
{
	/// <summary>The document's intrinsic size (from its viewBox / width-height).</summary>
	Size SourceSize { get; }

	/// <summary>Draws the document into <paramref name="session"/>, scaled to fit <paramref name="targetSize"/> (uniform, centered).</summary>
	void Render(IDrawingSession session, Size targetSize);
}

/// <summary>
/// Holds the registered <see cref="ISvgRenderer"/>. The host builder resolves the default at Build() time (the
/// Svg.Skia add-in when referenced, else the built-in managed engine); an app can override it via .SvgRenderer(...).
/// </summary>
public static class SvgRenderer
{
	private static ISvgRenderer? _current;

	/// <summary>The active SVG renderer, or null on a head with no SVG renderer at all (SVG then doesn't render).</summary>
	public static ISvgRenderer? Current
	{
		get => _current;
		internal set => _current = value;
	}

	/// <summary>Registers <paramref name="renderer"/> only if none is set yet (the per-seam fallback default).</summary>
	internal static void RegisterDefault(ISvgRenderer renderer) => _current ??= renderer;
}
