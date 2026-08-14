#nullable enable

using System;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Process-wide <see cref="IGeometryFactory"/>, set at the composition root independently of the graphics/render
/// backend — geometry construction is render-independent. Unset access throws (there is no hidden default); a
/// platform head registers its geometry engine at startup (the Skia head does this via the per-seam fallback).
/// </summary>
internal static class GeometryFactory
{
	private static IGeometryFactory? _current;

	public static IGeometryFactory Current
	{
		get
		{
			if (_current is null)
			{
				DrawingBackendFallback.EnsureGeometryFactory();
			}

			return _current ?? throw new InvalidOperationException(
				"No IGeometryFactory registered. Register a geometry engine via the host builder (.GeometryFactory), or rely on the per-seam Skia fallback.");
		}
		internal set => _current = value;
	}

	/// <summary>
	/// Registers <paramref name="factory"/> only if none is registered yet. Framework-internal (per-seam fallback);
	/// app-side geometry registration goes through the host builder's .GeometryFactory extension.
	/// </summary>
	internal static void RegisterDefault(IGeometryFactory factory)
		=> _current ??= factory ?? throw new ArgumentNullException(nameof(factory));
}
