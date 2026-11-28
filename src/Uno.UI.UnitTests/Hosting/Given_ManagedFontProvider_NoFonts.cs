#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Composition.Drawing;
using Windows.UI.Text;

namespace Uno.UI.Tests.Hosting;

/// <summary>
/// The browser and the iOS sandbox have no fonts to enumerate, and the app's default font arrives asynchronously,
/// so the default font is requested before anything is available to draw with.
/// </summary>
[TestClass]
public class Given_ManagedFontProvider_NoFonts
{
	private const float FontSize = 20;

	private static IFont GetDefaultFont()
		=> new ManagedFontProvider(Array.Empty<string>()).GetDefaultFont(new FontWeight(400), FontStretch.Normal, FontStyle.Normal, FontSize);

	[TestMethod]
	public void When_No_Fonts_Then_Default_Font_Has_A_Line_Height()
	{
		var font = GetDefaultFont();

		Assert.IsLessThan(0f, font.Ascent);
		Assert.IsGreaterThan(0f, font.Descent);
	}

	[TestMethod]
	public void When_No_Fonts_Then_Text_Shapes_To_Nothing_Visible()
	{
		var run = GetDefaultFont().Shape("Hello", TextDirection.LeftToRight);

		Assert.AreEqual(5, run.Count);
		CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4 }, run.Clusters);
		foreach (var advance in run.Advances)
		{
			Assert.AreEqual(0f, advance);
		}
	}

	[TestMethod]
	public void When_No_Fonts_Then_Building_A_Glyph_Run_Adds_No_Elements()
	{
		var font = GetDefaultFont();
		var run = font.Shape("Hello", TextDirection.LeftToRight);
		List<GlyphRunElement> elements = new();

		font.BuildGlyphRun(new ManagedGeometryFactory(), run.Glyphs, new Vector2[run.Count], 0, elements);

		Assert.IsEmpty(elements);
	}

	[TestMethod]
	public void When_No_Fonts_Then_Characters_Do_Not_Trigger_Fallback()
		=> Assert.IsTrue(GetDefaultFont().ContainsGlyph('A'));
}
