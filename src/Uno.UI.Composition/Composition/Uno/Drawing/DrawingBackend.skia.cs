#nullable enable

using System;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Holds the process-wide <see cref="IDrawingBackend"/>. The core has no built-in backend: an
/// implementation registers itself via <see cref="Register"/> (the Skia backend does so on load), so the
/// core never references a concrete backend.
/// </summary>
public static class DrawingBackend
{
	private static IDrawingBackend? _current;

	/// <summary>The active drawing backend.</summary>
	/// <exception cref="InvalidOperationException">No backend has been registered yet.</exception>
	public static IDrawingBackend Current
		=> _current ?? throw new InvalidOperationException(
			"No IDrawingBackend has been registered. The Skia backend registers itself when its assembly " +
			"loads; a host using a different backend must call DrawingBackend.Register(...) during initialization.");

	/// <summary>Registers the active drawing backend. Intended to be called during host/backend initialization.</summary>
	public static void Register(IDrawingBackend backend)
		=> _current = backend ?? throw new ArgumentNullException(nameof(backend));
}
