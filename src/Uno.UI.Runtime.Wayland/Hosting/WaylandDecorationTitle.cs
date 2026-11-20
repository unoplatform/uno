#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using Uno.UI.Composition.Drawing;

namespace Uno.UI.Runtime.Wayland;

// Dispatch thread. Hover and activation redraws reuse both the font and the fitted glyph run.
internal sealed class WaylandDecorationTitle(Func<IFont> createFont)
{
	private IFont? _font;
	private string? _title;
	private GlyphRun _fullRun;
	private int[]? _boundaries;
	private float _available = float.NaN;
	private (ushort[] Glyphs, Vector2[] Positions) _fitted = ([], []);

	internal IFont Font => _font ??= createFont();

	internal (ushort[] Glyphs, Vector2[] Positions) GetRun(string title, float available)
	{
		if (_title == title && _available == available)
		{
			return _fitted;
		}
		if (_title != title)
		{
			_title = title;
			_boundaries = null;
		}
		_available = available;
		_fitted = ([], []);
		if (available <= 0 || title.Length == 0)
		{
			return _fitted;
		}
		if (_boundaries is null)
		{
			_fullRun = Font.Shape(title, TextDirection.LeftToRight);
			// A shaping cluster can span several graphemes (a ligature); a grapheme can span several codepoints.
			var clusters = new HashSet<int>(_fullRun.Clusters);
			var boundaries = new List<int> { 0 };
			foreach (var boundary in StringInfo.ParseCombiningCharacters(title))
			{
				if (boundary > 0 && clusters.Contains(boundary))
				{
					boundaries.Add(boundary);
				}
			}
			_boundaries = boundaries.ToArray();
		}
		if (GetAdvance(_fullRun) <= available)
		{
			return _fitted = Layout(_fullRun);
		}
		const string ellipsis = "…";
		var best = Font.Shape(ellipsis, TextDirection.LeftToRight);
		if (GetAdvance(best) > available)
		{
			return _fitted;
		}
		var low = 1;
		var high = _boundaries.Length - 1;
		while (low <= high)
		{
			var middle = low + (high - low) / 2;
			var candidate = Font.Shape(string.Concat(title.AsSpan(0, _boundaries[middle]), ellipsis), TextDirection.LeftToRight);
			if (GetAdvance(candidate) <= available)
			{
				best = candidate;
				low = middle + 1;
			}
			else
			{
				high = middle - 1;
			}
		}
		return _fitted = Layout(best);
	}

	private static float GetAdvance(GlyphRun run)
	{
		var advance = 0f;
		foreach (var value in run.Advances)
		{
			advance += value;
		}
		return advance;
	}

	private static (ushort[] Glyphs, Vector2[] Positions) Layout(GlyphRun run)
	{
		var positions = new Vector2[run.Count];
		var x = 0f;
		for (var i = 0; i < run.Count; i++)
		{
			positions[i] = new Vector2(x + run.Offsets[i].X, run.Offsets[i].Y);
			x += run.Advances[i];
		}
		return (run.Glyphs, positions);
	}
}
