#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml.Documents.TextFormatting;

namespace Uno.UI.Tests.Helpers;

[TestClass]
public class Given_NotoFontFallbackService
{
	// Minimal synthetic range list used by all greedy-selection tests.
	private static readonly IReadOnlyList<(int Start, int End, List<string> Fonts)> _ranges = new List<(int, int, List<string>)>
	{
		(0x0041, 0x005B, new List<string> { "FontA" }),           // A-Z
		(0x0600, 0x0650, new List<string> { "FontB", "FontA" }), // Arabic subset (both fonts cover it)
		(0x4E00, 0x4E10, new List<string> { "FontC" }),           // CJK subset
	};

	[TestMethod]
	public void WhenNoCodepoints_ThenEmptyListReturned()
	{
		var result = NotoFontFallbackService.GetMinimalFontsForCodepoints([], _ranges);
		result.Should().BeEmpty();
	}

	[TestMethod]
	public void WhenSingleRangeCodepoint_ThenSingleFontReturned()
	{
		var result = NotoFontFallbackService.GetMinimalFontsForCodepoints([0x0041], _ranges);
		result.Should().HaveCount(1).And.Contain("FontA");
	}

	[TestMethod]
	public void WhenCodepointsSpanTwoDisjointFonts_ThenBothFontsReturned()
	{
		var result = NotoFontFallbackService.GetMinimalFontsForCodepoints([0x0041, 0x4E00], _ranges);
		result.Should().HaveCount(2).And.Contain("FontA").And.Contain("FontC");
	}

	[TestMethod]
	public void WhenCodepointsCoveredByTwoFonts_ThenGreedyPicksSingleCoveringFont()
	{
		// 0x0600 is in the range covered by both FontB and FontA; greedy should pick one.
		var result = NotoFontFallbackService.GetMinimalFontsForCodepoints([0x0600], _ranges);
		result.Should().HaveCount(1);
	}

	[TestMethod]
	public void WhenFontACoversMoreCodepoints_ThenGreedyPrefersIt()
	{
		// FontA covers A-Z and the Arabic range; FontB covers only the Arabic range.
		// Give it both A-Z and an Arabic codepoint — FontA alone should cover both.
		var result = NotoFontFallbackService.GetMinimalFontsForCodepoints([0x0041, 0x0600], _ranges);
		result.Should().HaveCount(1).And.Contain("FontA");
	}

	[TestMethod]
	public void WhenCodepointOutsideAllRanges_ThenEmptyListReturned()
	{
		var result = NotoFontFallbackService.GetMinimalFontsForCodepoints([0x9999], _ranges);
		result.Should().BeEmpty();
	}

	[TestMethod]
	public void WhenDuplicateCodepointsSupplied_ThenResultDeduplicates()
	{
		var result = NotoFontFallbackService.GetMinimalFontsForCodepoints([0x0041, 0x0041, 0x0041], _ranges);
		result.Should().HaveCount(1).And.Contain("FontA");
	}

	// ── LocalFontUriOverrides ────────────────────────────────────────────────

	[TestMethod]
	public void WhenLocalFontUriOverridesIsEmpty_ThenItIsNotNull()
	{
		NotoFontFallbackService.LocalFontUriOverrides.Should().NotBeNull();
	}

	[TestMethod]
	public void WhenUriIsRegistered_ThenItCanBeRetrieved()
	{
		var key = $"TestFont_{Guid.NewGuid()}";
		var uri = new Uri("ms-appx:///Assets/Fonts/Test.ttf");

		NotoFontFallbackService.LocalFontUriOverrides[key] = uri;

		NotoFontFallbackService.LocalFontUriOverrides.Should().ContainKey(key)
			.WhoseValue.Should().Be(uri);

		NotoFontFallbackService.LocalFontUriOverrides.Remove(key);
	}

	// ── DisableRemoteFontFallback ────────────────────────────────────────────

	[TestMethod]
	public void WhenDisableRemoteFontFallbackIsNotSet_ThenItDefaultsToFalse()
	{
		NotoFontFallbackService.DisableRemoteFontFallback.Should().BeFalse();
	}

	[TestMethod]
	public void WhenDisableRemoteFontFallbackIsSet_ThenItCanBeToggled()
	{
		var original = NotoFontFallbackService.DisableRemoteFontFallback;
		try
		{
			NotoFontFallbackService.DisableRemoteFontFallback = true;
			NotoFontFallbackService.DisableRemoteFontFallback.Should().BeTrue();

			NotoFontFallbackService.DisableRemoteFontFallback = false;
			NotoFontFallbackService.DisableRemoteFontFallback.Should().BeFalse();
		}
		finally
		{
			NotoFontFallbackService.DisableRemoteFontFallback = original;
		}
	}
}
