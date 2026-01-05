using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Text;
using SkiaSharp;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.Helpers;
using Uno.UI.Dispatching;
using Uno.UI.Runtime.Skia.Win32;

namespace Microsoft.UI.Xaml.Documents.TextFormatting;

internal class Win32FontFallbackService : IFontFallbackService
{
	private readonly HashSet<int> _missingCodepoints = new();
	private Task? _fetchTask;
	private readonly Func<int, Task<string?>> _memoizedGetFontNameForCodepoint;
	private readonly Dictionary<string, SKTypeface?> _fetchedFonts = new();

	public static Win32FontFallbackService Instance { get; } = new Win32FontFallbackService();

	private Win32FontFallbackService()
	{
		_memoizedGetFontNameForCodepoint = ((Func<int, Task<string?>>)GetFontNameForCodepointInternal).AsMemoized();
	}

	public Task<string?> GetFontNameForCodepoint(int codepoint) => _memoizedGetFontNameForCodepoint(codepoint);

	private async Task<string?> GetFontNameForCodepointInternal(int codepoint)
	{
		NativeDispatcher.CheckThreadAccess();
		var (name, typeface) = _fetchedFonts.AsEnumerable().FirstOrDefault(t => t.Value != null && t.Value.ContainsGlyph(codepoint));
		if (typeface is null)
		{
			_missingCodepoints.Add(codepoint);
			if (_fetchTask is null)
			{
				var fetchTask = FetchFontsForMissingCodepoints();
				if (!fetchTask.IsCompleted)
				{
					_fetchTask = fetchTask;
					await fetchTask;
				}
			}
			else
			{
				await _fetchTask;
			}
			return _fetchedFonts.AsEnumerable().FirstOrDefault(t => t.Value != null && t.Value.ContainsGlyph(codepoint)).Key;
		}
		else
		{
			return name;
		}
	}

	private async Task FetchFontsForMissingCodepoints()
	{
		NativeDispatcher.CheckThreadAccess();

		var missingCodepoints = _missingCodepoints.ToList();
		_missingCodepoints.Clear();

		var fonts = GetMinimalFontsForCodepoints(missingCodepoints, FallbackFontMaps.CodepointsToFontFamilies);
		foreach (var rawFont in fonts)
		{
			var font = rawFont;
			if (font is "Noto Sans CJK")
			{
				// all CJK fonts have the same codepoint coverage, so we pick one based on locale
				font = "SimplifiedChinese";

				var locale = CultureInfo.CurrentCulture.Name;
				if (locale.StartsWith("ja", StringComparison.InvariantCultureIgnoreCase) || locale.StartsWith("jp", StringComparison.InvariantCultureIgnoreCase))
				{
					font = "Japanese";
				}
				else if (locale.StartsWith("ko", StringComparison.InvariantCultureIgnoreCase))
				{
					font = "Korean";
				}
				else if (locale.StartsWith("zh", StringComparison.InvariantCultureIgnoreCase))
				{
					if (locale.Contains("cn", StringComparison.InvariantCultureIgnoreCase))
					{
						font = "SimplifiedChinese";
					}
					else if (locale.Contains("hk", StringComparison.InvariantCultureIgnoreCase))
					{
						font = "TraditionalChineseHK";
					}
					else
					{
						font = "TraditionalChinese";
					}
				}
			}

			var map = FallbackFontMaps.FontWeightsToRawUrls[font];
			// TODO: use weight/stretch/style to pick the best match
			var uri = new Uri(map["Regular"]);
			try
			{
				var stream = await AppDataUriEvaluator.ToStream(uri, CancellationToken.None);
				var typeface = SKTypeface.FromStream(stream);
				_fetchedFonts[font] = typeface;
			}
			catch (Exception e)
			{
				this.LogError()?.Error($"Failed to create an SKTypeface for {font} using {uri.OriginalString}", e);
				_fetchedFonts[font] = null;
			}
		}

		if (_missingCodepoints.Count > 0)
		{
			await FetchFontsForMissingCodepoints();
		}
		else
		{
			_fetchTask = null;
		}
	}

	public async Task<SKTypeface?> GetTypefaceForFontName(string fontName, FontWeight weight, FontStretch stretch, FontStyle style)
	{
		NativeDispatcher.CheckThreadAccess();
		if (_fetchedFonts.TryGetValue(fontName, out var typeface))
		{
			return typeface;
		}

		if (_fetchTask is null)
		{
			return null;
		}
		else
		{
			await _fetchTask;
			return _fetchedFonts.GetValueOrDefault(fontName);
		}
	}

	public static List<string> GetMinimalFontsForCodepoints(IEnumerable<int> codepoints, IReadOnlyList<(int Start, int End, List<string> Fonts)> mergedRanges)
	{
		var requested = codepoints.Distinct().ToList();
		requested.Sort();
		if (requested.Count == 0)
			return [];

		var uncovered = new HashSet<int>(requested);
		var coverageByFont = BuildCoverageMap(requested, mergedRanges);
		var selectedFonts = new List<string>();

		while (uncovered.Count > 0)
		{
			var (bestFont, coveredCount) = FindBestCoveringFont(uncovered, coverageByFont);
			if (bestFont == null || coveredCount == 0)
				break;

			selectedFonts.Add(bestFont);
			var handled = coverageByFont[bestFont];
			foreach (var cp in handled)
				uncovered.Remove(cp);

			coverageByFont.Remove(bestFont);
		}

		return selectedFonts;
	}

	private static Dictionary<string, HashSet<int>> BuildCoverageMap(IEnumerable<int> requested, IReadOnlyList<(int Start, int End, List<string> Fonts)> mergedRanges)
	{
		var map = new Dictionary<string, HashSet<int>>(StringComparer.Ordinal);
		foreach (var codepoint in requested)
		{
			var fonts = GetFontsForCodepoint(codepoint, mergedRanges);
			foreach (var font in fonts)
			{
				if (!map.TryGetValue(font, out var covered))
				{
					covered = new HashSet<int>();
					map[font] = covered;
				}

				covered.Add(codepoint);
			}
		}

		return map;
	}

	private static List<string> GetFontsForCodepoint(int codepoint, IReadOnlyList<(int Start, int End, List<string> Fonts)> mergedRanges)
	{
		for (int i = 0; i < mergedRanges.Count; i++)
		{
			var span = mergedRanges[i];
			if (codepoint < span.Start)
				break;

			if (codepoint >= span.Start && codepoint < span.End)
				return span.Fonts;
		}

		return new List<string>();
	}

	private static (string? Font, int CoveredCount) FindBestCoveringFont(HashSet<int> uncovered, Dictionary<string, HashSet<int>> coverageByFont)
	{
		string? bestFont = null;
		int bestCoverage = 0;

		foreach (var (font, covered) in coverageByFont)
		{
			int current = 0;
			foreach (var cp in covered)
			{
				if (uncovered.Contains(cp))
					current++;
			}

			if (current > bestCoverage)
			{
				bestCoverage = current;
				bestFont = font;
			}
		}

		return (bestFont, bestCoverage);
	}
}
