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
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Text;
using Uno.Helpers;

namespace Microsoft.UI.Xaml.Documents.TextFormatting;

internal static class NotoFontFallbackService
{
	private const string EmojiFontFamily = "Noto Color Emoji";
	private const string EmojiFontUrl = "https://raw.githubusercontent.com/googlefonts/noto-emoji/8998f5dd683424a73e2314a8c1f1e359c19e8742/fonts/Noto-COLRv1.ttf";
	private const string EmojiFontSha256 = "0ae57fe58645638523ba35f388d93739d292539a9acb84df5700c81b1e1a28d2";
	private const int EmojiFontMaxBytes = 6 * 1024 * 1024;

	// Exact standalone emoji/base-character coverage from Noto-COLRv1's Unicode cmap. Sequence-only
	// components (ASCII keycap bases, space, ZWJ, tag characters and private glyph IDs) deliberately
	// do not trigger fallback by themselves; once a base triggers, UnicodeText keeps the whole
	// extended grapheme cluster in this font's shaping run.
	private static readonly (int Start, int End)[] _emojiRanges =
	[
		(0x00A9, 0x00AA), (0x00AE, 0x00AF), (0x203C, 0x203D), (0x2049, 0x204A),
		(0x20E3, 0x20E4), (0x2122, 0x2123), (0x2139, 0x213A), (0x2194, 0x219A),
		(0x21A9, 0x21AB), (0x231A, 0x231C), (0x2328, 0x2329), (0x23CF, 0x23D0),
		(0x23E9, 0x23F4), (0x23F8, 0x23FB), (0x24C2, 0x24C3), (0x25AA, 0x25AC),
		(0x25B6, 0x25B7), (0x25C0, 0x25C1), (0x25FB, 0x25FF), (0x2600, 0x2605),
		(0x260E, 0x260F), (0x2611, 0x2612), (0x2614, 0x2616), (0x2618, 0x2619),
		(0x261D, 0x261E), (0x2620, 0x2621), (0x2622, 0x2624), (0x2626, 0x2627),
		(0x262A, 0x262B), (0x262E, 0x2630), (0x2638, 0x263B), (0x2640, 0x2641),
		(0x2642, 0x2643), (0x2648, 0x2654), (0x265F, 0x2661), (0x2663, 0x2664),
		(0x2665, 0x2667), (0x2668, 0x2669), (0x267B, 0x267C), (0x267E, 0x2680),
		(0x2692, 0x2698), (0x2699, 0x269A), (0x269B, 0x269D), (0x26A0, 0x26A2),
		(0x26A7, 0x26A8), (0x26AA, 0x26AC), (0x26B0, 0x26B2), (0x26BD, 0x26BF),
		(0x26C4, 0x26C6), (0x26C8, 0x26C9), (0x26CE, 0x26D0), (0x26D1, 0x26D2),
		(0x26D3, 0x26D5), (0x26E9, 0x26EB), (0x26F0, 0x26F6), (0x26F7, 0x26FB),
		(0x26FD, 0x26FE), (0x2702, 0x2703), (0x2705, 0x2706), (0x2708, 0x270E),
		(0x270F, 0x2710), (0x2712, 0x2713), (0x2714, 0x2715), (0x2716, 0x2717),
		(0x271D, 0x271E), (0x2721, 0x2722), (0x2728, 0x2729), (0x2733, 0x2735),
		(0x2744, 0x2745), (0x2747, 0x2748), (0x274C, 0x274D), (0x274E, 0x274F),
		(0x2753, 0x2756), (0x2757, 0x2758), (0x2763, 0x2765), (0x2795, 0x2798),
		(0x27A1, 0x27A2), (0x27B0, 0x27B1), (0x27BF, 0x27C0), (0x2934, 0x2936),
		(0x2B05, 0x2B08), (0x2B1B, 0x2B1D), (0x2B50, 0x2B51), (0x2B55, 0x2B56),
		(0x3030, 0x3031), (0x303D, 0x303E), (0x3297, 0x3298), (0x3299, 0x329A),
		(0x1F004, 0x1F005), (0x1F0CF, 0x1F0D0), (0x1F170, 0x1F172), (0x1F17E, 0x1F180),
		(0x1F18E, 0x1F18F), (0x1F191, 0x1F19B), (0x1F1E6, 0x1F200), (0x1F201, 0x1F203),
		(0x1F21A, 0x1F21B), (0x1F22F, 0x1F230), (0x1F232, 0x1F23B), (0x1F250, 0x1F252),
		(0x1F300, 0x1F322), (0x1F324, 0x1F394), (0x1F396, 0x1F398), (0x1F399, 0x1F39C),
		(0x1F39E, 0x1F3F1), (0x1F3F3, 0x1F3F6), (0x1F3F7, 0x1F4FE), (0x1F4FF, 0x1F53E),
		(0x1F549, 0x1F54F), (0x1F550, 0x1F568), (0x1F56F, 0x1F571), (0x1F573, 0x1F57B),
		(0x1F587, 0x1F588), (0x1F58A, 0x1F58E), (0x1F590, 0x1F591), (0x1F595, 0x1F597),
		(0x1F5A4, 0x1F5A6), (0x1F5A8, 0x1F5A9), (0x1F5B1, 0x1F5B3), (0x1F5BC, 0x1F5BD),
		(0x1F5C2, 0x1F5C5), (0x1F5D1, 0x1F5D4), (0x1F5DC, 0x1F5DF), (0x1F5E1, 0x1F5E2),
		(0x1F5E3, 0x1F5E4), (0x1F5E8, 0x1F5E9), (0x1F5EF, 0x1F5F0), (0x1F5F3, 0x1F5F4),
		(0x1F5FA, 0x1F650), (0x1F680, 0x1F6C6), (0x1F6CB, 0x1F6D3), (0x1F6D5, 0x1F6D9),
		(0x1F6DC, 0x1F6E6), (0x1F6E9, 0x1F6EA), (0x1F6EB, 0x1F6ED), (0x1F6F0, 0x1F6F1),
		(0x1F6F3, 0x1F6FD), (0x1F7E0, 0x1F7EC), (0x1F7F0, 0x1F7F1), (0x1F90C, 0x1F93B),
		(0x1F93C, 0x1F946), (0x1F947, 0x1FA00), (0x1FA70, 0x1FA7D), (0x1FA80, 0x1FA8B),
		(0x1FA8E, 0x1FAC7), (0x1FAC8, 0x1FAC9), (0x1FACD, 0x1FADD), (0x1FADF, 0x1FAEB),
		(0x1FAEF, 0x1FAF9),
	];

	public static IFontFallbackService Instance { get; } = CreateInstance();

	private static IFontFallbackService CreateInstance()
	{
		var coverage = FallbackFontMaps.CodepointsToFontFamilies
			.Select(t => new FontFallbackCoverageRange(t.start, t.end, t.fonts))
			.ToList();

		var textService = new CoverageTableFontFallbackService(coverage, FetchNotoFontStream);
		var emojiCoverage = _emojiRanges
			.Select(range => new FontFallbackCoverageRange(range.Start, range.End, [EmojiFontFamily]))
			.ToList();
		var emojiService = new CoverageTableFontFallbackService(emojiCoverage, FetchNotoFontStream);
		return new EmojiAwareFallbackService(textService, emojiService);
	}

	internal static bool IsEmojiCodepoint(int codepoint)
	{
		var low = 0;
		var high = _emojiRanges.Length - 1;
		while (low <= high)
		{
			var middle = low + (high - low) / 2;
			var range = _emojiRanges[middle];
			if (codepoint < range.Start)
			{
				high = middle - 1;
			}
			else if (codepoint >= range.End)
			{
				low = middle + 1;
			}
			else
			{
				return true;
			}
		}

		return false;
	}

	private static async Task<Stream?> FetchNotoFontStream(
		string fontFamily,
		FontWeight weight,
		FontStretch stretch,
		FontStyle style,
		CancellationToken ct)
	{
		if (fontFamily == "Noto Sans CJK")
		{
			// All CJK fonts have the same codepoint coverage; pick a regional variant by current culture.
			fontFamily = SelectCjkVariantForCurrentCulture();
		}
		else if (fontFamily == EmojiFontFamily)
		{
			using var source = await AppDataUriEvaluator.ToStream(new Uri(EmojiFontUrl), ct);
			using var bounded = new MemoryStream();
			var buffer = new byte[81920];
			while (true)
			{
				var read = await source.ReadAsync(buffer, 0, buffer.Length, ct);
				if (read == 0)
				{
					break;
				}
				if (bounded.Length + read > EmojiFontMaxBytes)
				{
					throw new InvalidDataException("The emoji fallback font exceeds its maximum permitted size.");
				}
				await bounded.WriteAsync(buffer, 0, read, ct);
			}

			var bytes = bounded.ToArray();
			using var sha256 = SHA256.Create();
			var hash = sha256.ComputeHash(bytes);
			var hashText = new StringBuilder(hash.Length * 2);
			foreach (var value in hash)
			{
				hashText.Append(value.ToString("x2", CultureInfo.InvariantCulture));
			}
			if (!string.Equals(hashText.ToString(), EmojiFontSha256, StringComparison.Ordinal))
			{
				throw new InvalidDataException("The emoji fallback font failed integrity validation.");
			}

			return new MemoryStream(bytes, writable: false);
		}

		if (!FallbackFontMaps.FontWeightsToRawUrls.TryGetValue(fontFamily, out var variants))
		{
			return null;
		}

		var uri = new Uri(variants["Regular"]);
		return await AppDataUriEvaluator.ToStream(uri, ct);
	}

	private sealed class EmojiAwareFallbackService : IFontFallbackService
	{
		private readonly IFontFallbackService _textService;
		private readonly IFontFallbackService _emojiService;

		internal EmojiAwareFallbackService(IFontFallbackService textService, IFontFallbackService emojiService)
		{
			_textService = textService;
			_emojiService = emojiService;
		}

		public async Task<string?> GetFontFamilyForCodepoint(int codepoint)
		{
			if (IsEmojiCodepoint(codepoint)
				&& await _emojiService.GetFontFamilyForCodepoint(codepoint) is { } emojiFamily)
			{
				return emojiFamily;
			}

			return await _textService.GetFontFamilyForCodepoint(codepoint);
		}

		public Task<Stream?> GetFontStreamForFontFamily(
			string fontFamily,
			FontWeight weight,
			FontStretch stretch,
			FontStyle style)
			=> fontFamily == EmojiFontFamily
				? _emojiService.GetFontStreamForFontFamily(fontFamily, weight, stretch, style)
				: _textService.GetFontStreamForFontFamily(fontFamily, weight, stretch, style);
	}

	private static string SelectCjkVariantForCurrentCulture()
	{
		var locale = CultureInfo.CurrentCulture.Name.ToLowerInvariant();
		var split = locale.Split('-');
		var languageSubtag = split[0];
		var scriptSubtag = split.Length < 2 ? "" : split[1];
		var regionSubtag = split.Length < 3 ? "" : split[2];

		return (languageSubtag, scriptSubtag, regionSubtag) switch
		{
			("zh", "hant", _) => "TraditionalChinese",
			("zh", "hans", _) => "SimplifiedChinese",
			("ja", _, _) => "Japanese",
			("ko", _, _) => "Korean",
			_ => "SimplifiedChinese"
		};
	}
}
