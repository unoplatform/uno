#nullable enable

using System;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Holds the process-wide <see cref="IDrawingBackend"/>. Defaults to the SkiaSharp backend; a host or
/// experiment can replace it before the first frame via <see cref="Register"/>.
/// </summary>
public static class DrawingBackend
{
	private static IDrawingBackend? _current;

	/// <summary>The active drawing backend. Resolves to the SkiaSharp backend when none was registered.</summary>
	public static IDrawingBackend Current => _current ??= new SkiaDrawingBackend();

	/// <summary>Replaces the active drawing backend. Intended to be called during host initialization.</summary>
	public static void Register(IDrawingBackend backend)
		=> _current = backend ?? throw new ArgumentNullException(nameof(backend));
}
