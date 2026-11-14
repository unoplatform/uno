#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Stands in when there is no font to draw with yet: no system fonts, no bundled default, and the app's default font
/// still loading (the browser, the iOS sandbox). Text takes up a line but draws nothing until the real font replaces it.
/// </summary>
internal sealed class ManagedPlaceholderFont : IFont
{
	// Open Sans, Uno's default text font, so the line height doesn't jump when it arrives.
	private const float AscentRatio = 1.069f;
	private const float DescentRatio = 0.293f;
	private const float CapHeightRatio = 0.714f;

	private readonly float _fontSize;

	public ManagedPlaceholderFont(float fontSize) => _fontSize = fontSize;

	public GlyphRun Shape(ReadOnlySpan<char> text, TextDirection direction, bool enableLigatures = true)
	{
		var clusters = new int[text.Length];
		for (var i = 0; i < clusters.Length; i++)
		{
			clusters[i] = i;
		}

		return new GlyphRun(new ushort[text.Length], new Vector2[text.Length], new float[text.Length], clusters);
	}

	public void BuildGlyphRun(IGeometryFactory geometry, ReadOnlySpan<ushort> glyphs, ReadOnlySpan<Vector2> positions, float baselineY, IList<GlyphRunElement> elements)
	{
	}

	public float Ascent => -AscentRatio * _fontSize;

	public float Descent => DescentRatio * _fontSize;

	public float CapHeight => CapHeightRatio * _fontSize;

	public float? UnderlinePosition => null;

	public float? UnderlineThickness => null;

	public float? StrikeoutPosition => null;

	public float? StrikeoutThickness => null;

	public ushort GetGlyphIndex(int codepoint) => 0;

	// Reporting a miss would send every character to the fallback, which downloads Noto in the browser.
	public bool ContainsGlyph(int codepoint) => true;

	public float GetGlyphAdvance(ushort glyph) => 0;

	public string FamilyName => string.Empty;
}
