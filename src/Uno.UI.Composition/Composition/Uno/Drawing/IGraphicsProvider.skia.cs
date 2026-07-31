#nullable enable

using System.Collections.Generic;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// The registerable unit of a pluggable 2D backend — a matched pair: a resource/content
/// <see cref="Drawing"/> factory and the <see cref="IRenderer"/> that consumes its handles. Registering
/// the pair together (see <see cref="GraphicsRegistry"/>) makes "a renderer without its matched factory"
/// unrepresentable, which is what keeps the opaque handles (<see cref="IShader"/> etc.) safe to downcast.
/// </summary>
public interface IGraphicsProvider
{
	/// <summary>The context kinds this backend can render on, most-preferred first. Negotiation tries them in order.</summary>
	IReadOnlyList<GraphicsContextKind> PreferredContexts { get; }

	/// <summary>What this backend needs from whatever context it is given (see <see cref="GraphicsRequirements"/>).</summary>
	GraphicsRequirements Requirements { get; }

	/// <summary>The resource/content factory. Installed as <see cref="DrawingFactory.Current"/> when this backend wins negotiation.</summary>
	IDrawingFactory Drawing { get; }

	/// <summary>Creates the render backend bound to a successfully-created <paramref name="context"/> (of one of <see cref="PreferredContexts"/>).</summary>
	IRenderer CreateRenderBackend(IGraphicsContext context);
}
