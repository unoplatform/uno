#nullable enable

using System;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Holds the process-wide <see cref="IDrawingFactory"/>. The core has no built-in backend; the factory is
/// installed exactly once, by <see cref="GraphicsRegistry"/> negotiation, at the winning backend (see
/// <see cref="Register"/>). There is deliberately no if-absent fallback: the graphics backend is registered up
/// front (via the host builder's <c>.GraphicsBackend</c>, or the implicit Skia default the registry lights up
/// before it negotiates), so a "fallback factory, then real factory later" sequence can never occur. Reading
/// <see cref="Current"/> before negotiation has run is a bug, and throws.
/// </summary>
internal static class DrawingFactory
{
	private static IDrawingFactory? _current;

	/// <summary>The active drawing factory, installed by negotiation.</summary>
	/// <summary>The active drawing factory, or null if none is negotiated yet (a probe that never throws).</summary>
	internal static IDrawingFactory? CurrentOrNull => _current;

	/// <exception cref="InvalidOperationException">Read before a backend was negotiated (registration must come first).</exception>
	public static IDrawingFactory Current
		=> _current ?? throw new InvalidOperationException(
			"No IDrawingFactory has been registered. A graphics backend must be registered up front (the host builder's " +
			".GraphicsBackend, or the implicit Skia default) and negotiated (GraphicsRegistry.Initialize) before the " +
			"drawing factory is used — reading it earlier is a bug.");

	/// <summary>
	/// Installs the negotiated backend's drawing factory (called by <see cref="GraphicsRegistry"/> at the winning
	/// backend). Unconditional so a re-negotiation (e.g. an Android GL context loss) re-binds the current factory —
	/// but it is never used as an if-absent fallback; registration precedes the first use.
	/// </summary>
	internal static void Register(IDrawingFactory backend)
		=> _current = backend ?? throw new ArgumentNullException(nameof(backend));
}
