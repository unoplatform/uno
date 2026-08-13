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
	/// <exception cref="InvalidOperationException">No backend has been registered and none could be auto-registered.</exception>
	public static IDrawingFactory Current
	{
		get
		{
			if (_current is null)
			{
				// The drawing factory is part of the graphics BACKEND (matched with the renderer): light up the Skia
				// factory only if no backend was declared. A WebGPU/managed head owns this seam via its own backend
				// and supplies the factory itself (possibly after async init) — never implicitly filled by Skia here.
				DrawingBackendFallback.EnsureGraphicsBackend();
			}

			return _current ?? throw new InvalidOperationException(
				"No IDrawingFactory has been registered. Reference the Skia backend (it auto-registers when present) " +
				"or call DrawingFactory.Register(...) / ManagedBackend.Register() during initialization.");
		}
	}

	/// <summary>
	/// Registers the active drawing backend. Framework-internal: app-side registration goes through the host builder
	/// (a graphics backend provider supplies its drawing factory), not this low-level entry.
	/// </summary>
	internal static void Register(IDrawingFactory backend)
		=> _current = backend ?? throw new ArgumentNullException(nameof(backend));

	/// <summary>
	/// Registers <paramref name="backend"/> only if none is registered yet. Framework-internal; used by a backend's
	/// module-initializer self-registration so it acts as a fallback without clobbering an explicit registration.
	/// </summary>
	internal static void RegisterDefault(IDrawingFactory backend)
		=> _current ??= backend ?? throw new ArgumentNullException(nameof(backend));
}
