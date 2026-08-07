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
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Text;
using Uno.Helpers;

namespace Microsoft.UI.Xaml.Documents.TextFormatting;

internal static class NotoFontFallbackService
{
	public static IFontFallbackService Instance { get; } = CreateInstance();

	private static IFontFallbackService CreateInstance()
	{
		var coverage = FallbackFontMaps.CodepointsToFontFamilies
			.Select(t => new FontFallbackCoverageRange(t.start, t.end, t.fonts))
			.ToList();

		return new CoverageTableFontFallbackService(coverage, FetchNotoFontStream);
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

		if (!FallbackFontMaps.FontWeightsToRawUrls.TryGetValue(fontFamily, out var variants))
		{
			return null;
		}

		var uri = new Uri(variants["Regular"]);
		return await AppDataUriEvaluator.ToStream(uri, ct);
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
