// Generates tables for font fallback based on notofonts GitHub repository.
// Typical usage: dotnet run -- --summary-only > ../Uno.UI/UI/Xaml/Documents/FontFallbackMaps.skia.cs

using System.CommandLine;
using System.Diagnostics;

return await AppHost.RunAsync(args);

static class AppHost
{
	private static readonly FontSource DefaultFontSource =
		new FontSource(
			"notofonts/notofonts.github.io",
			"main",
			"fonts",
			["hinted", "ttf"],
			"Noto Fonts"
		);

	private static readonly FontSource CJKFontSource =
		new FontSource(
			"notofonts/noto-cjk",
			"main",
			"Sans/OTF",
			[],
			"Noto CJK Sans/OTF"
		);

	public static async Task<int> RunAsync(string[] args)
	{
		var summaryOption = new Option<bool>("--summary-only", "Skip per-font logging and show only merged results.");
		summaryOption.AddAlias("-s");

		var rootCommand = new RootCommand("Inspects notofonts GitHub fonts to determine supported Unicode ranges.")
		{
			summaryOption
		};

		rootCommand.SetHandler(async summaryOnly =>
		{
			var exitCode = await RunApplicationAsync(summaryOnly);
			Environment.ExitCode = exitCode;
		}, summaryOption);

		return await rootCommand.InvokeAsync(args);
	}

	private static async Task<int> RunApplicationAsync(bool summaryOnly)
	{
		var fontRangeMap = new Dictionary<string, List<(int start, int end)>>();
		var fontWeightMap = new Dictionary<string, List<FontVariant>>(StringComparer.Ordinal);

		foreach (var source in (FontSource[])[DefaultFontSource, CJKFontSource])
		{
			List<FontFamilyDownload> downloadResult;
			try
			{
				downloadResult = await GithubFontFetcher.DownloadFontsAsync(source);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Failed to download fonts from {source.DisplayName}: {ex.Message}");
				return 1;
			}

			foreach (var family in downloadResult)
			{
				if (!FontWhitelist.AllowedFamilies.TryGetValue(family.FamilyName.ToLowerInvariant().Replace(" ", ""), out var displayName))
				{
					Console.Error.WriteLine($"Skipping '{family.FamilyName}' because it is not in the allowed whitelist.");
					continue;
				}

				fontWeightMap[displayName] = family.Variants.ToList();

				if (FontWhitelist.CjkFonts.Contains(family.FamilyName) && fontRangeMap.ContainsKey(FontWhitelist.CjkUnifiedDisplayName))
				{
					Console.Error.WriteLine($"Skipping font coverage analysis for {displayName} since another CJK font has already been processed.");
					continue;
				}

				var fontPath = family.RepresentativePath;
				if (!summaryOnly)
					Console.Error.WriteLine($"Processing family: {displayName} ({Path.GetFileName(fontPath)})");

				try
				{
					var codepoints = TtfReader.GetSupportedCodepoints(fontPath);
					if (!summaryOnly)
						Console.Error.WriteLine($"Total supported codepoints: {codepoints.Count}");

					var ranges = TtfReader.GetCodepointRanges(codepoints);
					if (!summaryOnly)
					{
						Console.Error.WriteLine($"Total ranges: {ranges.Count}");
						Console.Error.WriteLine("Ranges:");
						foreach (var range in ranges)
						{
							if (range.Start == range.End)
								Console.Error.WriteLine($"U+{range.Start:X4}");
							else
								Console.Error.WriteLine($"U+{range.Start:X4} - U+{range.End:X4}");
						}
					}

					fontRangeMap[FontWhitelist.CjkFonts.Contains(family.FamilyName) ? FontWhitelist.CjkUnifiedDisplayName : displayName] = ranges;
				}
				catch (Exception ex)
				{
					Console.Error.WriteLine($"Error reading font {displayName}: {ex.Message}");
				}

				if (!summaryOnly)
					Console.Error.WriteLine(new string('-', 20));
			}
		}

		if (fontRangeMap.Count == 0)
		{
			Console.Error.WriteLine("No usable font ranges detected across the inspected fonts.");
			return 1;
		}

		var mergedRanges = FontRangeMerger.Merge(fontRangeMap);
		if (mergedRanges.Count == 0)
		{
			Console.Error.WriteLine("No combined coverage ranges to report.");
		}
		else
		{
			Console.WriteLine("using System.Collections.Generic;");
			Console.WriteLine();
			Console.WriteLine("namespace Microsoft.UI.Xaml.Documents.TextFormatting;");
			Console.WriteLine();
			Console.WriteLine("public static class FallbackFontMaps");
			Console.WriteLine("{");
			Console.WriteLine("\tpublic static readonly IReadOnlyList<(int start, int end, List<string> fonts)> CodepointsToFontFamilies = new List<(int, int, List<string>)>");
			Console.WriteLine("\t{");
			foreach (var span in mergedRanges)
			{
				var startLiteral = $"0x{span.Start:X4}";
				var endLiteral = $"0x{span.End:X4}";
				var fontsLiteral = string.Join(", ", span.Fonts.Select(font => $"\"{font}\""));
				Console.WriteLine($"\t\t({startLiteral}, {endLiteral}, [{fontsLiteral}]),");
			}
			Console.WriteLine("\t};");
			Console.WriteLine();
			Console.WriteLine("\tpublic static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> FontWeightsToRawUrls = new Dictionary<string, IReadOnlyDictionary<string, string>>");
			Console.WriteLine("\t{");
			foreach (var (family, variants) in fontWeightMap.OrderBy(kvp => kvp.Key, StringComparer.Ordinal))
			{
				Console.WriteLine($"\t\t[\"{family}\"] = new Dictionary<string, string>");
				Console.WriteLine("\t\t{");
				foreach (var variant in variants.OrderBy(v => v.Weight, StringComparer.OrdinalIgnoreCase))
				{
					Console.WriteLine($"\t\t\t[\"{variant.Weight}\"] = \"{variant.RawUrl}\",");
				}
				Console.WriteLine("\t\t},");
			}
			Console.WriteLine("\t};\n}");
		}

		return 0;
	}
}

public static class TtfReader
{
	public static List<(int Start, int End)> GetCodepointRanges(List<int> codepoints)
	{
		var ranges = new List<(int Start, int End)>();
		if (codepoints == null || codepoints.Count == 0)
			return ranges;

		codepoints.Sort();

		int start = codepoints[0];
		int end = codepoints[0];

		for (int i = 1; i < codepoints.Count; i++)
		{
			if (codepoints[i] == end + 1)
			{
				end = codepoints[i];
			}
			else
			{
				ranges.Add((start, end));
				start = codepoints[i];
				end = codepoints[i];
			}
		}
		ranges.Add((start, end));

		return ranges;
	}

	public static List<int> GetSupportedCodepoints(string filePath)
	{
		using var fs = File.OpenRead(filePath);
		using var reader = new BinaryReader(fs);

		// 1. Read Offset Table
		// Offset Table: ScalerType(4), NumTables(2), SearchRange(2), EntrySelector(2), RangeShift(2)
		fs.Seek(4, SeekOrigin.Begin);
		ushort numTables = ReadUInt16Be(reader);
		fs.Seek(6, SeekOrigin.Current); // Skip rest of offset table

		// 2. Find 'cmap' table
		uint cmapOffset = 0;
		for (int i = 0; i < numTables; i++)
		{
			uint tag = ReadUInt32Be(reader);
			_ = ReadUInt32Be(reader); // checkSum
			uint offset = ReadUInt32Be(reader);
			_ = ReadUInt32Be(reader); // length

			if (tag == 0x636D6170) // 'cmap'
			{
				cmapOffset = offset;
				break;
			}
		}

		if (cmapOffset == 0)
			throw new Exception("cmap table not found");

		// 3. Read cmap table header
		fs.Seek(cmapOffset, SeekOrigin.Begin);
		_ = ReadUInt16Be(reader); // version
		ushort numSubtables = ReadUInt16Be(reader);

		uint selectedSubtableOffset = 0;
		int selectedPlatformId = -1;
		int selectedEncodingId = -1;

		// We want Platform 3 (Windows) and Encoding 1 (Unicode BMP) or 10 (Unicode Full)
		// Or Platform 0 (Unicode)
		for (int i = 0; i < numSubtables; i++)
		{
			ushort platformId = ReadUInt16Be(reader);
			ushort encodingId = ReadUInt16Be(reader);
			uint offset = ReadUInt32Be(reader);

			if (platformId == 3 && (encodingId == 1 || encodingId == 10))
			{
				if (selectedPlatformId != 3 || (encodingId == 10 && selectedEncodingId == 1))
				{
					selectedSubtableOffset = offset;
					selectedPlatformId = platformId;
					selectedEncodingId = encodingId;
				}
			}
			else if (platformId == 0 && selectedPlatformId != 3)
			{
				selectedSubtableOffset = offset;
				selectedPlatformId = platformId;
				selectedEncodingId = encodingId;
			}
		}

		if (selectedSubtableOffset == 0)
			throw new Exception("No supported cmap subtable found (Platform 3/Encoding 1 or 10, or Platform 0)");

		// 4. Parse subtable
		// The offset in the encoding record is relative to the beginning of the cmap table.
		long subtablePosition = cmapOffset + selectedSubtableOffset;
		fs.Seek(subtablePosition, SeekOrigin.Begin);
		ushort format = ReadUInt16Be(reader);

		if (format == 4)
		{
			return ReadFormat4(reader);
		}
		else if (format == 12)
		{
			return ReadFormat12(reader);
		}
		else
		{
			throw new Exception($"cmap format {format} not supported (only 4 and 12)");
		}
	}

	private static List<int> ReadFormat4(BinaryReader reader)
	{
		// Format 4 Header
		// length(2), language(2)
		_ = ReadUInt16Be(reader); // length
		_ = ReadUInt16Be(reader); // language

		// segCountX2(2), searchRange(2), entrySelector(2), rangeShift(2)
		ushort segCountX2 = ReadUInt16Be(reader);
		ushort segCount = (ushort)(segCountX2 / 2);
		reader.ReadBytes(6); // Skip searchRange, entrySelector, rangeShift

		// Arrays
		ushort[] endCounts = new ushort[segCount];
		for (int i = 0; i < segCount; i++) endCounts[i] = ReadUInt16Be(reader);

		reader.ReadUInt16(); // reservedPad

		ushort[] startCounts = new ushort[segCount];
		for (int i = 0; i < segCount; i++) startCounts[i] = ReadUInt16Be(reader);

		short[] idDeltas = new short[segCount];
		for (int i = 0; i < segCount; i++) idDeltas[i] = (short)ReadUInt16Be(reader);

		// idRangeOffsets location
		long idRangeOffsetsPosition = reader.BaseStream.Position;
		ushort[] idRangeOffsets = new ushort[segCount];
		for (int i = 0; i < segCount; i++) idRangeOffsets[i] = ReadUInt16Be(reader);

		var codepoints = new List<int>();

		for (int i = 0; i < segCount; i++)
		{
			int start = startCounts[i];
			int end = endCounts[i];

			if (start == 0xFFFF) break; // End of segments

			for (int c = start; c <= end; c++)
			{
				int glyphId = 0;
				if (idRangeOffsets[i] == 0)
				{
					glyphId = (c + idDeltas[i]) & 0xFFFF;
				}
				else
				{
					// Calculate address of the glyph index
					// idRangeOffset[i] is number of bytes from the location of idRangeOffset[i] to the glyph array value
					long currentRangeOffsetPtr = idRangeOffsetsPosition + (i * 2);
					long glyphIndexAddress = currentRangeOffsetPtr + idRangeOffsets[i] + 2 * (c - start);

					// Save current position
					long currentPos = reader.BaseStream.Position;

					reader.BaseStream.Seek(glyphIndexAddress, SeekOrigin.Begin);
					int index = ReadUInt16Be(reader);

					// Restore position
					reader.BaseStream.Seek(currentPos, SeekOrigin.Begin);

					if (index != 0)
						glyphId = (index + idDeltas[i]) & 0xFFFF;
				}

				if (glyphId != 0)
				{
					codepoints.Add(c);
				}
			}
		}
		return codepoints;
	}

	private static List<int> ReadFormat12(BinaryReader reader)
	{
		reader.ReadUInt16(); // reserved
		_ = ReadUInt32Be(reader); // length
		_ = ReadUInt32Be(reader); // language
		uint nGroups = ReadUInt32Be(reader);

		var codepoints = new List<int>();
		for (int i = 0; i < nGroups; i++)
		{
			uint startCharCode = ReadUInt32Be(reader);
			uint endCharCode = ReadUInt32Be(reader);
			_ = ReadUInt32Be(reader); // startGlyphID

			for (uint c = startCharCode; c <= endCharCode; c++)
			{
				codepoints.Add((int)c);
			}
		}
		return codepoints;
	}

	private static ushort ReadUInt16Be(BinaryReader reader)
	{
		byte[] bytes = reader.ReadBytes(2);
		if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
		return BitConverter.ToUInt16(bytes, 0);
	}

	private static uint ReadUInt32Be(BinaryReader reader)
	{
		byte[] bytes = reader.ReadBytes(4);
		if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
		return BitConverter.ToUInt32(bytes, 0);
	}
}

public static class FontRangeMerger
{
	public static List<(int Start, int End, List<string> Fonts)> Merge(Dictionary<string, List<(int Start, int End)>> fontRanges)
	{
		var boundaries = new SortedDictionary<int, List<(string Font, bool IsStart)>>();
		foreach (var (font, ranges) in fontRanges)
		{
			if (ranges == null || ranges.Count == 0) continue;
			foreach (var range in ranges)
			{
				AddEvent(range.Start, font, true);
				if (range.End == int.MaxValue)
					AddEvent(range.End, font, false);
				else
					AddEvent(range.End + 1, font, false);
			}
		}

		var activeFonts = new SortedSet<string>(StringComparer.Ordinal);
		var merged = new List<(int Start, int End, List<string> Fonts)>();
		int? previousPosition = null;

		foreach (var (position, events) in boundaries)
		{
			if (previousPosition.HasValue && previousPosition.Value < position && activeFonts.Count > 0)
			{
				AppendSegment(previousPosition.Value, position, activeFonts, merged);
			}

			foreach (var (font, isStart) in events)
			{
				if (isStart)
					activeFonts.Add(font);
				else
					activeFonts.Remove(font);
			}

			previousPosition = position;
		}

		return merged;

		void AddEvent(int pos, string font, bool isStart)
		{
			if (!boundaries.TryGetValue(pos, out var list))
			{
				list = new List<(string Font, bool IsStart)>();
				boundaries[pos] = list;
			}

			list.Add((font, isStart));
		}
	}

	private static void AppendSegment(int start, int endExclusive, SortedSet<string> activeFonts, List<(int Start, int End, List<string> Fonts)> merged)
	{
		var snapshot = activeFonts.ToList();
		if (merged.Count > 0)
		{
			var last = merged[^1];
			if (last.End == start && last.Fonts.SequenceEqual(snapshot))
			{
				merged[^1] = (last.Start, endExclusive, last.Fonts);
				return;
			}
		}

		merged.Add((start, endExclusive, snapshot));
	}
}

public sealed record FontSource(string Repo, string Branch, string RootPath, IReadOnlyList<string> FamilySubdirectorySegments, string DisplayName);

static class FontWhitelist
{
	public const string CjkUnifiedDisplayName = "Noto Sans CJK";
	public static string[] NonCjkFonts { get; } =
	[
		"Noto Sans",
		"Noto Music",
		"Noto Sans Symbols",
		"Noto Sans Adlam",
		"Noto Sans Anatolian Hieroglyphs",
		"Noto Sans Arabic",
		"Noto Sans Armenian",
		"Noto Sans Avestan",
		"Noto Sans Balinese",
		"Noto Sans Bamum",
		"Noto Sans Bassa Vah",
		"Noto Sans Batak",
		"Noto Sans Bengali",
		"Noto Sans Bhaiksuki",
		"Noto Sans Brahmi",
		"Noto Sans Buginese",
		"Noto Sans Buhid",
		"Noto Sans Canadian Aboriginal",
		"Noto Sans Carian",
		"Noto Sans Caucasian Albanian",
		"Noto Sans Chakma",
		"Noto Sans Cham",
		"Noto Sans Cherokee",
		"Noto Sans Coptic",
		"Noto Sans Cypriot",
		"Noto Sans Deseret",
		"Noto Sans Devanagari",
		"Noto Sans Elbasan",
		"Noto Sans Elymaic",
		"Noto Sans Ethiopic",
		"Noto Sans Georgian",
		"Noto Sans Glagolitic",
		"Noto Sans Gothic",
		"Noto Sans Grantha",
		"Noto Sans Gujarati",
		"Noto Sans Gunjala Gondi",
		"Noto Sans Gurmukhi",
		"Noto Sans Hanunoo",
		"Noto Sans Hatran",
		"Noto Sans Hebrew",
		"Noto Sans Imperial Aramaic",
		"Noto Sans Indic Siyaq Numbers",
		"Noto Sans Inscriptional Pahlavi",
		"Noto Sans Inscriptional Parthian",
		"Noto Sans Javanese",
		"Noto Sans Kaithi",
		"Noto Sans Kannada",
		"Noto Sans Kayah Li",
		"Noto Sans Kharoshthi",
		"Noto Sans Khmer",
		"Noto Sans Khojki",
		"Noto Sans Khudawadi",
		"Noto Sans Lao",
		"Noto Sans Lepcha",
		"Noto Sans Limbu",
		"Noto Sans Linear A",
		"Noto Sans Linear B",
		"Noto Sans Lisu",
		"Noto Sans Lycian",
		"Noto Sans Lydian",
		"Noto Sans Mahajani",
		"Noto Sans Malayalam",
		"Noto Sans Mandaic",
		"Noto Sans Manichaean",
		"Noto Sans Marchen",
		"Noto Sans Masaram Gondi",
		"Noto Sans Math",
		"Noto Sans Mayan Numerals",
		"Noto Sans Medefaidrin",
		"Noto Sans Meetei Mayek",
		"Noto Sans Meroitic",
		"Noto Sans Miao",
		"Noto Sans Modi",
		"Noto Sans Mongolian",
		"Noto Sans Mro",
		"Noto Sans Multani",
		"Noto Sans Myanmar",
		"Noto Sans NKo",
		"Noto Sans Nabataean",
		"Noto Sans New Tai Lue",
		"Noto Sans Newa",
		"Noto Sans Nushu",
		"Noto Sans Ogham",
		"Noto Sans Ol Chiki",
		"Noto Sans Old Hungarian",
		"Noto Sans Old Italic",
		"Noto Sans Old North Arabian",
		"Noto Sans Old Permic",
		"Noto Sans Old Persian",
		"Noto Sans Old Sogdian",
		"Noto Sans Old South Arabian",
		"Noto Sans Old Turkic",
		"Noto Sans Oriya",
		"Noto Sans Osage",
		"Noto Sans Osmanya",
		"Noto Sans Pahawh Hmong",
		"Noto Sans Palmyrene",
		"Noto Sans Pau Cin Hau",
		"Noto Sans Phags Pa",
		"Noto Sans Phoenician",
		"Noto Sans Psalter Pahlavi",
		"Noto Sans Rejang",
		"Noto Sans Runic",
		"Noto Sans Saurashtra",
		"Noto Sans Sharada",
		"Noto Sans Shavian",
		"Noto Sans Siddham",
		"Noto Sans Sinhala",
		"Noto Sans Sogdian",
		"Noto Sans Sora Sompeng",
		"Noto Sans Soyombo",
		"Noto Sans Sundanese",
		"Noto Sans Syloti Nagri",
		"Noto Sans Syriac",
		"Noto Sans Tagalog",
		"Noto Sans Tagbanwa",
		"Noto Sans Tai Le",
		"Noto Sans Tai Tham",
		"Noto Sans Tai Viet",
		"Noto Sans Takri",
		"Noto Sans Tamil",
		"Noto Sans Tamil Supplement",
		"Noto Sans Telugu",
		"Noto Sans Thaana",
		"Noto Sans Thai",
		"Noto Sans Tifinagh",
		"Noto Sans Tirhuta",
		"Noto Sans Ugaritic",
		"Noto Sans Vai",
		"Noto Sans Wancho",
		"Noto Sans Warang Citi",
		"Noto Sans Yi",
		"Noto Sans Zanabazar Square",
		"Noto Serif Tibetan"
	];

	public static string[] CjkFonts { get; } =
	[
		"Japanese",
		"Korean",
		"SimplifiedChinese",
		"TraditionalChinese",
		"TraditionalChineseHK"
	];

	public static IReadOnlyDictionary<string, string> AllowedFamilies { get; } = NonCjkFonts.Concat(CjkFonts).ToDictionary(name => name.ToLowerInvariant().Replace(" ", ""), StringComparer.OrdinalIgnoreCase);
}

public sealed record FontFamilyDownload(
	string FamilyName,
	string RepresentativePath,
	IReadOnlyList<FontVariant> Variants);

public class FontVariant
{
	public string Weight { get; set; } = null!;
	public string RawUrl { get; set; } = null!;
	public string LocalPath { get; set; } = null!;
}

public static class GithubFontFetcher
{
	private const string CacheFolderName = "fonts_cache";

	public static Task<List<FontFamilyDownload>> DownloadFontsAsync(FontSource source)
	{
		return Task.Run(() =>
		{
			var repoPath = PrepareCache(source.Repo, source.Branch);
			var sanitizedPath = (source.RootPath).Trim('/', '\\');
			var targetRoot = string.IsNullOrEmpty(sanitizedPath)
				? repoPath
				: Path.Combine(repoPath, sanitizedPath.Replace('/', Path.DirectorySeparatorChar));

			var families = new List<FontFamilyDownload>();

			var familyDirectories = Directory.GetDirectories(targetRoot, "*", SearchOption.TopDirectoryOnly);
			foreach (var familyDir in familyDirectories)
			{
				var familyName = Path.GetFileName(familyDir);
				var dir = source.FamilySubdirectorySegments.Prepend(familyDir).Aggregate("", Path.Combine);
				var files = Directory.EnumerateFiles(dir, "*.*", SearchOption.TopDirectoryOnly)
					.ToList();

				var variants = files
					.Select(file => new FontVariant
					{
						Weight = Path.GetFileNameWithoutExtension(file).Split('-')[^1],
						RawUrl = BuildRawUrl(source.Repo, source.Branch, repoPath, file),
						LocalPath = file
					})
					.ToList();

				var representative = variants.First(v => string.Equals(v.Weight, "Regular", StringComparison.OrdinalIgnoreCase));
				families.Add(new FontFamilyDownload(familyName, representative.LocalPath, variants));
			}

			return families;
		});
	}

	private static string BuildRawUrl(string repo, string branch, string repoRoot, string filePath)
	{
		var relativePath = Path.GetRelativePath(repoRoot, filePath)
			.Replace(Path.DirectorySeparatorChar, '/');
		return $"https://raw.githubusercontent.com/{repo}/{branch}/{relativePath}";
	}

	private static string PrepareCache(string repo, string branch)
	{
		var cacheDir = Path.Combine(Path.GetTempPath(), CacheFolderName, repo.Replace('/', '_'));
		Directory.CreateDirectory(cacheDir);

		var repoPath = Path.Combine(cacheDir, "repo");

		if (!Directory.Exists(Path.Combine(repoPath, ".git")))
		{
			CloneRepo(repo, branch, repoPath);
		}
		else
		{
			FetchRepo(branch, repoPath);
		}

		CheckoutBranch(repoPath, branch);
		return repoPath;
	}

	private static void CloneRepo(string repo, string branch, string destination)
	{
		Console.Error.WriteLine($"Cloning {repo} ({branch})...");
		RunGit($"clone --depth=1 --branch {branch} https://github.com/{repo}.git \"{destination}\"");
	}

	private static void FetchRepo(string branch, string repoPath)
	{
		Console.Error.WriteLine($"Fetching latest changes for {branch}...");
		RunGit($"-C \"{repoPath}\" fetch origin {branch}");
	}

	private static void CheckoutBranch(string repoPath, string branch)
	{
		RunGit($"-C \"{repoPath}\" checkout {branch}");
		RunGit($"-C \"{repoPath}\" reset --hard origin/{branch}");
	}

	private static void RunGit(string arguments)
	{
		var processStart = new ProcessStartInfo
		{
			FileName = "git",
			Arguments = arguments,
			CreateNoWindow = true,
			UseShellExecute = false,
			RedirectStandardError = true,
			RedirectStandardOutput = true
		};

		using var process = Process.Start(processStart) ?? throw new InvalidOperationException("Failed to start git process.");
		process.WaitForExit();

		if (process.ExitCode != 0)
		{
			var error = process.StandardError.ReadToEnd();
			throw new InvalidOperationException($"git {arguments} failed: {error}");
		}
	}
}
