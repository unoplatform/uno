// Copyright 2013 The Flutter Authors. All rights reserved.

// Redistribution and use in source and binary forms, with or without modification,
// 	are permitted provided that the following conditions are met:
//
// * Redistributions of source code must retain the above copyright
// 	notice, this list of conditions and the following disclaimer.
// * Redistributions in binary form must reproduce the above
// copyright notice, this list of conditions and the following
// disclaimer in the documentation and/or other materials provided
// 	with the distribution.
// * Neither the name of Google Inc. nor the names of its
// 	contributors may be used to endorse or promote products derived
// 	from this software without specific prior written permission.
//
// 	THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// 	ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// 	WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
// ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// 	(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
// ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// 	(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// 	SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

// Loosely based on Flutter's implementation:
// https://github.com/flutter/engine/blob/ae5c3603d013477d37ae301993fc0967d4ad7ed2/lib/web_ui/lib/src/engine/canvaskit/fonts.dart
// https://github.com/flutter/engine/blob/ae5c3603d013477d37ae301993fc0967d4ad7ed2/lib/web_ui/dev/roll_fallback_fonts.dart
// https://github.com/flutter/engine/blob/main/lib/web_ui/lib/src/engine/font_fallbacks.dart

#nullable enable
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

namespace Microsoft.UI.Xaml.Documents.TextFormatting;

internal class NotoFontFallbackService : IFontFallbackService
{
	private readonly HashSet<int> _missingCodepoints = new();
	private Task? _fetchTask;
	private readonly Func<int, Task<string?>> _memoizedGetFontNameForCodepoint;
	private readonly Dictionary<string, SKTypeface?> _fetchedFonts = new();

	public static NotoFontFallbackService Instance { get; } = new NotoFontFallbackService();

	private NotoFontFallbackService()
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
				var locale = CultureInfo.CurrentCulture.Name.ToLowerInvariant();
				var split = locale.Split('-');
				var languageSubtag = split[0];
				var scriptSubtag = split.Length < 2 ? "" : split[1];
				var regionSubtag = split.Length < 3 ? "" : split[2];

				font = (languageSubtag, scriptSubtag, regionSubtag) switch
				{
					("zh", "hant", _) => "TraditionalChinese",
					("zh", "hans", _) => "SimplifiedChinese",
					("ja", _, _) => "Japanese",
					("ko", _, _) => "Korean",
					_ => "SimplifiedChinese"
				};
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
