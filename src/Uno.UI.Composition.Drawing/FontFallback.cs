#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Text;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// The default providers' codepoint fallback: what to render when neither the requested family nor the backend's own
/// installed-font lookup covers a codepoint. Backend-agnostic — it drives the given <see cref="IFontProvider"/>'s own
/// <see cref="IFontProvider.CreateFont"/>/<see cref="IFont.ContainsGlyph"/>, so both the Skia and managed providers get
/// the same fallback. Platform-selected at runtime: the browser fetches Noto fonts on demand (no broadly-covering
/// installed fonts there); Android scans its bundled system fonts; other platforms rely on the backend lookup and add
/// nothing here. Public so a backend provider (which stands only on this assembly's public seam) can reuse it from its
/// <see cref="IFontProvider.MatchCharacterAsync"/>; a third-party provider may call it or supply its own fallback.
/// </summary>
public static class FontFallback
{
	private static IFontFallbackService? _noto;
	private static (byte[] bytes, IFont probe)[]? _androidSystemFonts;
	private static readonly object _androidGate = new();

	/// <summary>
	/// Resolves a font that can render <paramref name="codepoint"/> via the platform fallback, or <c>null</c>. Callers
	/// try their own installed-font lookup first and only reach here on a miss. Completes synchronously except for the
	/// browser's on-demand Noto fetch.
	/// </summary>
	public static async ValueTask<IFont?> MatchCharacterAsync(IFontProvider provider, int codepoint, FontWeight weight, FontStretch stretch, FontStyle style, float fontSize)
	{
		if (OperatingSystem.IsBrowser())
		{
			var noto = _noto ??= NotoFontFallbackService.Instance;
			var family = await noto.GetFontFamilyForCodepoint(codepoint);
			if (family is null)
			{
				return null;
			}

			using var stream = await noto.GetFontStreamForFontFamily(family, weight, stretch, style);
			return stream is null ? null : provider.CreateFont(ReadAllBytes(stream), family, weight, stretch, style, fontSize);
		}

		if (OperatingSystem.IsAndroid())
		{
			foreach (var (bytes, probe) in GetAndroidSystemFonts(provider))
			{
				if (probe.ContainsGlyph(codepoint))
				{
					return provider.CreateFont(bytes, null, weight, stretch, style, fontSize);
				}
			}
		}

		return null;
	}

	// Loads Android's bundled system fonts once, using the provider to build a coverage probe per file. Kept in memory
	// so subsequent codepoint misses resolve synchronously (matching the previous HarfBuzz-based service's caching).
	private static (byte[] bytes, IFont probe)[] GetAndroidSystemFonts(IFontProvider provider)
	{
		lock (_androidGate)
		{
			if (_androidSystemFonts is null)
			{
				var loaded = new List<(byte[], IFont)>();
				foreach (var path in SafeEnumerateSystemFonts())
				{
					try
					{
						var bytes = File.ReadAllBytes(path);
						// The probe is only used for ContainsGlyph, so the style/size are immaterial.
						if (provider.CreateFont(bytes, null, FontWeights.Normal, FontStretch.Normal, FontStyle.Normal, 16f) is { } probe)
						{
							loaded.Add((bytes, probe));
						}
					}
					catch
					{
						// non-font file or unreadable — skip in coverage lookups
					}
				}
				_androidSystemFonts = loaded.ToArray();
			}

			return _androidSystemFonts;
		}
	}

	private static IEnumerable<string> SafeEnumerateSystemFonts()
	{
		try
		{
			return Directory.EnumerateFiles("/system/fonts").ToArray();
		}
		catch
		{
			return Array.Empty<string>();
		}
	}

	private static byte[] ReadAllBytes(Stream stream)
	{
		if (stream is MemoryStream ms && ms.TryGetBuffer(out var seg) && seg.Offset == 0 && seg.Count == seg.Array!.Length)
		{
			return seg.Array;
		}

		using var copy = new MemoryStream();
		stream.CopyTo(copy);
		return copy.ToArray();
	}
}
