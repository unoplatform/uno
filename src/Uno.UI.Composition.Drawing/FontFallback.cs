#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
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
	// One IFont per resolved family+style: text segmentation breaks wherever the font instance changes, so
	// minting a new one per codepoint would split a fallback run into single-character segments and lose the
	// shaping across them (joining scripts render disconnected).
	private static readonly Dictionary<(IFontProvider, string, FontWeight, FontStretch, FontStyle, float), IFont?> _fetched = new();
	private static readonly object _fetchedGate = new();
	private static (string path, IFont probe)[]? _androidSystemFonts;
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

			var key = (provider, family, weight, stretch, style, fontSize);
			lock (_fetchedGate)
			{
				if (_fetched.TryGetValue(key, out var cached))
				{
					return cached;
				}
			}

			using var stream = await noto.GetFontStreamForFontFamily(family, weight, stretch, style);
			var font = stream is null ? null : provider.CreateFont(ReadAllBytes(stream), family, weight, stretch, style, fontSize);
			lock (_fetchedGate)
			{
				// A concurrent miss may have raced us here; keep whichever instance got stored first so every
				// caller segments against the same one.
				if (_fetched.TryGetValue(key, out var raced))
				{
					return raced;
				}

				_fetched[key] = font;
			}

			return font;
		}

		if (OperatingSystem.IsAndroid())
		{
			var fonts = GetAndroidSystemFonts(provider);
			for (var i = 0; i < fonts.Length; i++)
			{
				if (!fonts[i].probe.ContainsGlyph(codepoint))
				{
					continue;
				}

				// Keyed by index rather than family: these are bare font files, so the provider gets no name to
				// key on and every call would otherwise mint a distinct instance.
				var androidKey = (provider, i.ToString(CultureInfo.InvariantCulture), weight, stretch, style, fontSize);
				lock (_fetchedGate)
				{
					if (_fetched.TryGetValue(androidKey, out var cachedAndroid))
					{
						return cachedAndroid;
					}

					// Re-read on the hit rather than holding every system font's bytes: /system/fonts carries the
					// CJK and emoji faces, tens of MB each, and a miss only reaches here once per style.
					IFont? created = null;
					try
					{
						created = provider.CreateFont(File.ReadAllBytes(fonts[i].path), null, weight, stretch, style, fontSize);
					}
					catch
					{
						// unreadable since enumeration — fall through to the next covering font
					}

					if (created is null)
					{
						break;
					}

					_fetched[androidKey] = created;
					return created;
				}
			}
		}

		return null;
	}

	// Builds a coverage probe per bundled system font once, so subsequent codepoint misses resolve synchronously.
	// Only the path is kept alongside it: the bytes are re-read when a font is actually selected.
	private static (string path, IFont probe)[] GetAndroidSystemFonts(IFontProvider provider)
	{
		lock (_androidGate)
		{
			if (_androidSystemFonts is null)
			{
				var loaded = new List<(string, IFont)>();
				foreach (var path in SafeEnumerateSystemFonts())
				{
					try
					{
						var bytes = File.ReadAllBytes(path);
						// The probe is only used for ContainsGlyph, so the style/size are immaterial.
						if (provider.CreateFont(bytes, null, FontWeights.Normal, FontStretch.Normal, FontStyle.Normal, 16f) is { } probe)
						{
							loaded.Add((path, probe));
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
