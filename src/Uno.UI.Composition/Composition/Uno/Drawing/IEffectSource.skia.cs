#nullable enable

using System.Numerics;
using Windows.Foundation;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// A neutral input to an effect graph, resolved from an effect source-parameter name by the compositor and handed
/// to <see cref="IDrawingFactory.CreateEffectFilter"/>. It abstracts a bound brush so the backend seam never
/// references a <c>CompositionBrush</c>: the backend only needs to know whether the source is the live backdrop,
/// its intrinsic size (if any), and how to paint it into a session.
/// </summary>
public interface IEffectSource
{
	/// <summary>True when this source is the live backdrop (its content is the already-composited scene behind the element).</summary>
	bool IsBackdrop { get; }

	/// <summary>The source's intrinsic size, when it has one (used to bound the generated content); null otherwise.</summary>
	Vector2? Size { get; }

	/// <summary>Paints the source's content into <paramref name="session"/>. Returns false if nothing was painted.</summary>
	bool Paint(IDrawingSession session, float opacity, Rect bounds);
}
