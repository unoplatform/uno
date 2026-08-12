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
				DrawingBackendFallback.EnsureRegistered();
			}

			return _current ?? throw new InvalidOperationException(
				"No IFontProvider registered. Set FontProvider.Current during app initialization (the Skia head does this in SkiaBackend.Register).");
		}
		set => _current = value;
	}
}
