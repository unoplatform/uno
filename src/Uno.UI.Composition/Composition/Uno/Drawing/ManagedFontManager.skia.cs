#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using Windows.UI.Text;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// SkiaSharp-free <see cref="IFontManager"/>: indexes the operating system's installed fonts by parsing each
/// face's <c>name</c>/<c>OS-2</c>/<c>head</c> tables, matches a family/style request against that index using a
/// CSS-style nearest-weight score, and produces <see cref="ManagedFont"/> handles. Codepoint fallback scans the
/// indexed faces' <c>cmap</c>s for coverage. Enabled via <see cref="DrawingBackendOptions.UseManagedFonts"/>.
/// </summary>
/// <remarks>
/// This is the option-A "managed system-font lookup": correct and fully portable, but it re-implements OS font
/// matching, so locale-specific family aliases and the platform's precise fallback ordering aren't reproduced.
/// The index is built lazily on first use (reads font-file headers once).
/// </remarks>
public sealed class ManagedFontManager : IFontManager
{
	private sealed record FaceEntry(string Path, int TtcIndex, int Weight, int WidthClass, bool Italic);

	private readonly object _gate = new();
	private Dictionary<string, List<FaceEntry>>? _byFamily; // family (lower-invariant) -> faces
	private List<FaceEntry>? _allFaces;

	// Loaded fonts, keyed by file + collection index + pixel size.
	private readonly Dictionary<(string Path, int TtcIndex, int Size), ManagedFont?> _loaded = new();

	// Codepoint fallback results, keyed by codepoint + requested style + size.
	private readonly Dictionary<(int Codepoint, int Weight, FontStretch Stretch, FontStyle Style, int Size), IFont?> _matchCharacterCache = new();

	// Default-font family preference, first present wins.
	private static readonly string[] _defaultFamilies =
	{
		"Segoe UI", "Arial", "Helvetica", "Roboto", "Noto Sans", "DejaVu Sans",
		"Liberation Sans", "Ubuntu", "Cantarell", "FreeSans",
	};

	public IFont? CreateFont(byte[] data, string? familyNameHint, FontWeight weight, FontStretch stretch, FontStyle style, float fontSize)
	{
		var ttcIndex = 0;
		if (!string.IsNullOrEmpty(familyNameHint))
		{
			// Select the collection face whose family matches the hint (matters for .ttc/.otc).
			foreach (var (_, family, _, _, _, index) in ParseFaces(data))
			{
				if (string.Equals(family, familyNameHint, StringComparison.OrdinalIgnoreCase))
				{
					ttcIndex = index;
					break;
				}
			}
		}

		return ManagedFont.TryCreate(data, ttcIndex, fontSize, out var font) ? font : null;
	}

	public IFont? MatchFamily(string familyName, FontWeight weight, FontStretch stretch, FontStyle style, float fontSize)
	{
		EnsureIndex();

		if (!_byFamily!.TryGetValue(familyName.ToLowerInvariant(), out var faces) || faces.Count == 0)
		{
			return null;
		}

		var best = PickBestFace(faces, weight.Weight, ToWidthClass(stretch), IsItalic(style));
		return best is null ? null : Load(best, fontSize);
	}

	public IFont? MatchCharacter(int codepoint, FontWeight weight, FontStretch stretch, FontStyle style, float fontSize)
	{
		var key = (codepoint, weight.Weight, stretch, style, (int)fontSize);
		lock (_gate)
		{
			if (_matchCharacterCache.TryGetValue(key, out var cached))
			{
				return cached;
			}
		}

		EnsureIndex();

		IFont? result = null;
		foreach (var face in _allFaces!)
		{
			if (Load(face, fontSize) is ManagedFont font && font.ContainsGlyph(codepoint))
			{
				result = font;
				break;
			}
		}

		lock (_gate)
		{
			_matchCharacterCache[key] = result;
		}

		return result;
	}

	public IFont GetDefaultFont(FontWeight weight, FontStretch stretch, FontStyle style, float fontSize)
	{
		EnsureIndex();

		foreach (var family in _defaultFamilies)
		{
			if (MatchFamily(family, weight, stretch, style, fontSize) is { } font)
			{
				return font;
			}
		}

		// Any usable face, in enumeration order.
		foreach (var face in _allFaces!)
		{
			if (Load(face, fontSize) is { } font)
			{
				return font;
			}
		}

		throw new InvalidOperationException("No usable system font was found for the managed font manager.");
	}

	private ManagedFont? Load(FaceEntry face, float fontSize)
	{
		var key = (face.Path, face.TtcIndex, (int)fontSize);
		lock (_gate)
		{
			if (_loaded.TryGetValue(key, out var cached))
			{
				return cached;
			}
		}

		ManagedFont? font = null;
		try
		{
			var bytes = File.ReadAllBytes(face.Path);
			if (ManagedFont.TryCreate(bytes, face.TtcIndex, fontSize, out var created))
			{
				font = created;
			}
		}
		catch
		{
			font = null;
		}

		lock (_gate)
		{
			_loaded[key] = font;
		}

		return font;
	}

	// Nearest match: an italic mismatch is the heaviest penalty, then weight distance, then width distance.
	private static FaceEntry? PickBestFace(List<FaceEntry> faces, int weight, int widthClass, bool italic)
	{
		FaceEntry? best = null;
		var bestScore = int.MaxValue;
		foreach (var face in faces)
		{
			var score = Math.Abs(face.Weight - weight)
				+ Math.Abs(face.WidthClass - widthClass) * 100
				+ (face.Italic == italic ? 0 : 100_000);
			if (score < bestScore)
			{
				bestScore = score;
				best = face;
			}
		}

		return best;
	}

	private void EnsureIndex()
	{
		lock (_gate)
		{
			if (_byFamily is not null)
			{
				return;
			}

			var byFamily = new Dictionary<string, List<FaceEntry>>();
			var allFaces = new List<FaceEntry>();

			foreach (var dir in GetFontDirectories())
			{
				IEnumerable<string> files;
				try
				{
					files = Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories);
				}
				catch
				{
					continue;
				}

				foreach (var file in files)
				{
					var ext = Path.GetExtension(file);
					if (!IsFontFile(ext))
					{
						continue;
					}

					byte[] data;
					try
					{
						data = File.ReadAllBytes(file);
					}
					catch
					{
						continue;
					}

					foreach (var (_, family, weight, widthClass, italic, ttcIndex) in ParseFaces(data))
					{
						if (family.Length == 0)
						{
							continue;
						}

						var entry = new FaceEntry(file, ttcIndex, weight, widthClass, italic);
						allFaces.Add(entry);

						var famKey = family.ToLowerInvariant();
						if (!byFamily.TryGetValue(famKey, out var list))
						{
							byFamily[famKey] = list = new List<FaceEntry>();
						}
						list.Add(entry);
					}
				}
			}

			_byFamily = byFamily;
			_allFaces = allFaces;
		}
	}

	private static bool IsFontFile(string ext) =>
		ext.Equals(".ttf", StringComparison.OrdinalIgnoreCase) ||
		ext.Equals(".otf", StringComparison.OrdinalIgnoreCase) ||
		ext.Equals(".ttc", StringComparison.OrdinalIgnoreCase) ||
		ext.Equals(".otc", StringComparison.OrdinalIgnoreCase);

	// Yields (sfntOffset, family, weight, widthClass, italic, ttcIndex) for each face in the file.
	private static IEnumerable<(int Offset, string Family, int Weight, int WidthClass, bool Italic, int TtcIndex)> ParseFaces(byte[] data)
	{
		var offsets = new List<int>();
		if (data.Length >= 16 && ManagedFont.U32(data, 0) == 0x74746366) // 'ttcf'
		{
			var count = (int)ManagedFont.U32(data, 8);
			for (var i = 0; i < count && 12 + i * 4 + 4 <= data.Length; i++)
			{
				offsets.Add((int)ManagedFont.U32(data, 12 + i * 4));
			}
		}
		else
		{
			offsets.Add(0);
		}

		for (var faceIndex = 0; faceIndex < offsets.Count; faceIndex++)
		{
			var baseOffset = offsets[faceIndex];
			if (baseOffset < 0 || baseOffset + 12 > data.Length)
			{
				continue;
			}

			int name = 0, os2 = 0, head = 0;
			var numTables = ManagedFont.U16(data, baseOffset + 4);
			var dir = baseOffset + 12;
			for (var i = 0; i < numTables && dir + 16 <= data.Length; i++, dir += 16)
			{
				var offset = (int)ManagedFont.U32(data, dir + 8);
				switch (ManagedFont.U32(data, dir))
				{
					case 0x6E616D65: name = offset; break; // 'name'
					case 0x4F532F32: os2 = offset; break;  // 'OS/2'
					case 0x68656164: head = offset; break; // 'head'
				}
			}

			var family = name != 0 ? ManagedFont.ParseFamilyName(data, name) : string.Empty;

			int weight = 400, widthClass = 5;
			var italic = false;
			if (os2 != 0 && os2 + 64 <= data.Length)
			{
				weight = ManagedFont.U16(data, os2 + 4);
				widthClass = ManagedFont.U16(data, os2 + 6);
				italic = (ManagedFont.U16(data, os2 + 62) & 0x01) != 0; // fsSelection ITALIC
			}
			else if (head != 0 && head + 46 <= data.Length)
			{
				italic = (ManagedFont.U16(data, head + 44) & 0x02) != 0; // macStyle italic
			}

			yield return (baseOffset, family, weight, widthClass, italic, faceIndex);
		}
	}

	private static bool IsItalic(FontStyle style) => style is FontStyle.Italic or FontStyle.Oblique;

	private static int ToWidthClass(FontStretch stretch) => stretch switch
	{
		FontStretch.UltraCondensed => 1,
		FontStretch.ExtraCondensed => 2,
		FontStretch.Condensed => 3,
		FontStretch.SemiCondensed => 4,
		FontStretch.SemiExpanded => 6,
		FontStretch.Expanded => 7,
		FontStretch.ExtraExpanded => 8,
		FontStretch.UltraExpanded => 9,
		_ => 5, // Normal / Undefined
	};

	private static IEnumerable<string> GetFontDirectories()
	{
		if (OperatingSystem.IsWindows())
		{
			yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts");
			var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			if (localAppData.Length > 0)
			{
				yield return Path.Combine(localAppData, "Microsoft", "Windows", "Fonts");
			}
		}
		else if (OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst() || OperatingSystem.IsIOS())
		{
			yield return "/System/Library/Fonts";
			yield return "/Library/Fonts";
			yield return Path.Combine(Home(), "Library", "Fonts");
		}
		else if (OperatingSystem.IsAndroid())
		{
			yield return "/system/fonts";
			yield return "/system/font";
			yield return "/data/fonts";
		}
		else // Linux and other Unix
		{
			yield return "/usr/share/fonts";
			yield return "/usr/local/share/fonts";
			yield return Path.Combine(Home(), ".fonts");
			yield return Path.Combine(Home(), ".local", "share", "fonts");
		}
	}

	private static string Home() => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
}
