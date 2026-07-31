#nullable enable

using System;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Holds the process-wide <see cref="IDrawingFactory"/>. The core has no built-in backend: an
/// implementation registers itself via <see cref="Register"/> (the Skia backend does so on load), so the
/// core never references a concrete backend.
/// </summary>
public static class DrawingFactory
{
	private static IDrawingFactory? _current;

	/// <summary>The active drawing backend.</summary>
	/// <exception cref="InvalidOperationException">No backend has been registered yet.</exception>
	public static IDrawingFactory Current
		=> _current ?? throw new InvalidOperationException(
			"No IDrawingFactory has been registered. The Skia backend registers itself when its assembly " +
			"loads; a host using a different backend must call DrawingFactory.Register(...) during initialization.");

	/// <summary>Registers the active drawing backend. Intended to be called during host/backend initialization.</summary>
	public static void Register(IDrawingFactory backend)
		=> _current = backend ?? throw new ArgumentNullException(nameof(backend));
}
