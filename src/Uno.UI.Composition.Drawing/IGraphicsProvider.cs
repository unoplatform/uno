#nullable enable

using System.Collections.Generic;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// The registerable unit of a pluggable 2D backend: it mints the single device-bound
/// <see cref="IDrawingFactory"/> that both manufactures resources and drives the render/present lifecycle,
/// bound to a negotiated context (see <see cref="GraphicsRegistry"/>). One object owns its own opaque handles
/// (<see cref="IShader"/> etc.), which is what keeps them safe to downcast.
/// </summary>
public interface IGraphicsProvider
{
	/// <summary>The context kinds this backend can render on, most-preferred first. Negotiation tries them in order.</summary>
	IReadOnlyList<GraphicsContextKind> PreferredContexts { get; }

	/// <summary>Mints the single device-bound <see cref="IDrawingFactory"/> backend for a successfully-created <paramref name="context"/> — it owns both resource creation and the render/present lifecycle, and is installed as <see cref="DrawingFactory.Current"/> when this backend wins negotiation.</summary>
	IDrawingFactory CreateGraphics(IGraphicsContext context);
}
