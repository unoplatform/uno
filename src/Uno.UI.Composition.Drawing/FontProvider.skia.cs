#nullable enable

using System;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Process-wide font resolver (family/style/bytes/codepoint → <see cref="IFont"/>), set at the composition root
/// independently of the graphics/render backend — font resolution and shaping are render-independent content
/// production. Unset access throws (there is no hidden default); a platform head registers its font manager at
/// startup (the Skia head does this in SkiaBackend.Register).
/// </summary>
public static class FontProvider
{
	private static IFontProvider? _current;

	public static IFontProvider Current
	{
		get
		{
			if (_current is null)
			{
				DrawingBackendFallback.EnsureFontProvider();
			}

			return _current ?? throw new InvalidOperationException(
				"No IFontProvider registered. Register a font provider via the host builder (.FontProvider), or rely on the per-seam Skia fallback.");
		}
		internal set => _current = value;
	}

	/// <summary>
	/// Registers <paramref name="provider"/> only if none is registered yet. Framework-internal (per-seam fallback);
	/// app-side font registration goes through the host builder's .FontProvider extension.
	/// </summary>
	internal static void RegisterDefault(IFontProvider provider)
		=> _current ??= provider ?? throw new ArgumentNullException(nameof(provider));
}
