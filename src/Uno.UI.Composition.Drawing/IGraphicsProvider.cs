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
}

/// <summary>
/// The device-reading half of a provider, typed to the context device-face it consumes. A backend implements one
/// instantiation per device shape it serves (Skia: <see cref="IGLDeviceContext"/> / <see cref="IMetalDeviceContext"/>
/// / <see cref="IGraphicsContext"/> for software; WebGPU: <see cref="IGraphicsContext"/>, narrowing to its own
/// device context). The context arrives already typed — the backend reads its device details without casting a
/// neutral <see cref="IGraphicsContext"/> (the framework narrows, keyed on the closed kind).
/// </summary>
// Invariant on purpose: a backend implements one instantiation per exact device face it serves. Contravariance
// would make an IGraphicsProvider<IGraphicsContext> (software) also match IGraphicsProvider<IGLDeviceContext>,
// and the narrowing would dispatch the device-less software overload for a GL context.
public interface IGraphicsProvider<TContext> : IGraphicsProvider where TContext : IGraphicsContext
{
	/// <summary>Mints the single device-bound <see cref="IDrawingFactory"/> backend, reading device details from <paramref name="context"/>. Installed as <see cref="DrawingFactory.Current"/> when this backend wins negotiation.</summary>
	IDrawingFactory CreateGraphics(TContext context);
}
