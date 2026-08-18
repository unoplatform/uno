#nullable enable

using System;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Process-wide <see cref="IGeometryFactory"/>, set at the composition root independently of the render backend.
/// Unset access throws (there is no hidden default); a platform head registers its geometry engine at startup.
/// </summary>
internal static class GeometryFactory
{
	private static IGeometryFactory? _current;

	public static IGeometryFactory Current
	{
		get => _current ?? throw new InvalidOperationException(
			"No IGeometryFactory registered. Register a geometry engine via the host builder (.GeometryFactory), or reference the Skia backend for the built-in default.");
		internal set => _current = value;
	}

	/// <summary>Whether a geometry engine has been registered (used by the host builder's fail-fast seam check).</summary>
	internal static bool IsRegistered => _current is not null;

	/// <summary>Registers <paramref name="factory"/> only if none is registered yet (framework-internal per-seam fallback).</summary>
	internal static void RegisterDefault(IGeometryFactory factory)
		=> _current ??= factory ?? throw new ArgumentNullException(nameof(factory));
}
