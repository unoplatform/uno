#nullable enable

using Windows.UI;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Transient, inline paint configuration passed by value on the <see cref="IDrawingSession"/> draw verbs.
/// This is the "paint" half of the Track 1 boundary: unlike geometry (an <see cref="IGeometry"/> handle),
/// paint is cheap draw-time state and does not need cross-frame identity, so it crosses the boundary as a
/// value rather than a backend object.
/// </summary>
/// <remarks>
/// Only the simple, universally-supported properties live here. Expensive resources that need caching
/// (shaders, color/mask filters) will be added as opaque <see cref="IDrawingBackend"/> handles.
/// </remarks>
internal readonly struct PaintParams
{
	public PaintParams(Color color)
	{
		Color = color;
		Opacity = 1f;
	}

	/// <summary>The paint color. Multiplied by <see cref="Opacity"/> by the backend.</summary>
	public Color Color { get; init; }

	/// <summary>An additional alpha multiplier applied to <see cref="Color"/>. Defaults to 1 via the constructor.</summary>
	public float Opacity { get; init; }

	public PaintStyle Style { get; init; }

	public float StrokeWidth { get; init; }

	public StrokeCap StrokeCap { get; init; }

	public StrokeJoin StrokeJoin { get; init; }

	public float StrokeMiter { get; init; }

	public bool IsAntialias { get; init; }

	public BlendMode BlendMode { get; init; }

	/// <summary>Optional shader (e.g. a gradient) applied to the paint.</summary>
	public IShader? Shader { get; init; }

	/// <summary>Optional color filter (e.g. opacity modulation) applied to the paint.</summary>
	public IColorFilter? ColorFilter { get; init; }
}
