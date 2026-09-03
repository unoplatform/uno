#nullable enable

using System;
using System.Collections.Generic;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// A bounded memo of <see cref="IFont.Shape"/> results for one font instance, for backends to consult before
/// shaping. UI text repeats heavily (labels, statuses, formatted numbers re-set every frame), and a cache hit
/// skips the HarfBuzz round-trip entirely. Only short runs are cached — long text rarely recurs and would
/// dominate the memory bound. Callers must treat the returned <see cref="GlyphRun"/> arrays as immutable,
/// as every existing consumer already does.
/// </summary>
public sealed class GlyphRunCache
{
	private const int MaxEntries = 4096;
	private const int MaxTextLength = 32;

	// One map per (direction, ligatures) combination so the text alone can key each map. Cleared when full:
	// crude, but O(1) amortized and the working set re-fills within a frame or two.
	private readonly Dictionary<string, GlyphRun>?[] _maps = new Dictionary<string, GlyphRun>?[4];

	private static int MapIndex(TextDirection direction, bool enableLigatures)
		=> (direction is TextDirection.RightToLeft ? 2 : 0) + (enableLigatures ? 1 : 0);

	public bool TryGet(ReadOnlySpan<char> text, TextDirection direction, bool enableLigatures, out GlyphRun run)
	{
		if (text.Length > MaxTextLength || _maps[MapIndex(direction, enableLigatures)] is not { } map)
		{
			run = default;
			return false;
		}

		lock (map)
		{
			return map.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(text, out run);
		}
	}

	public void Add(ReadOnlySpan<char> text, TextDirection direction, bool enableLigatures, in GlyphRun run)
	{
		if (text.Length > MaxTextLength)
		{
			return;
		}

		var map = _maps[MapIndex(direction, enableLigatures)] ??= new(StringComparer.Ordinal);
		lock (map)
		{
			if (map.Count >= MaxEntries)
			{
				map.Clear();
			}

			map[text.ToString()] = run;
		}
	}
}
