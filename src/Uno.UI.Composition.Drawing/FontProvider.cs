#nullable enable

using System;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Process-wide font resolver (family/style/bytes/codepoint → <see cref="IFont"/>), set at the composition root
/// independently of the render backend. Unset access throws (there is no hidden default); a platform head
/// registers its font manager at startup.
/// </summary>
internal static class FontProvider
{
	private static IFontProvider? _current;

	public static IFontProvider Current
	{
		get => _current ?? throw new InvalidOperationException(
			"No IFontProvider registered. Register a font provider via the host builder (.FontProvider), or reference the Skia backend for the built-in default.");
		internal set => _current = value;
	}

	/// <summary>Whether a font provider has been registered (used by the host builder's fail-fast seam check).</summary>
	internal static bool IsRegistered => _current is not null;

	/// <summary>Registers <paramref name="provider"/> only if none is registered yet (framework-internal per-seam fallback).</summary>
	internal static void RegisterDefault(IFontProvider provider)
		=> _current ??= provider ?? throw new ArgumentNullException(nameof(provider));
}
