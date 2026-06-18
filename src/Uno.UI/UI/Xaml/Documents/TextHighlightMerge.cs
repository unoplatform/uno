// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TextHighlightMerge.h, TextHighlightMerge.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Documents;

// This algorithm is meant to take a series of linear text ranges and remove any overlap such
// that all text ranges do not start or end with an intersection.  This is done iteratively by
// adding regions to the class and then iterating over the map when complete.  The reason for doing
// this is so that foreground color highlighting chooses the correct color to generate for the
// glyph primitive.  There are multiple cases to consider which are tested in the unit tests
// and commented in the algorithm.
//
// Algorithmic decisions:
//      - The most recent region addition always occludes previous regions.
//      - All ranges are inclusive [Start,End] since it makes more conceptual sense.
//        For instance: [1,2] would highlight text index 1 and 2.  [1,1] would highlight text index 1.
//
// The C++ original uses std::map (a red-black tree) keyed by startIndex for amortized O(NlogN).
// Uno uses SortedList<int, HighlightRegion> (an ordered key list, index-addressable) which keeps
// the same key-ordered iteration / prev / next / erase-returns-next semantics; insertion is O(N)
// so the whole operation is O(N^2), which is fine for the small N of highlight regions.
internal class TextHighlightMerge
{
	private readonly SortedList<int, HighlightRegion> _regions = new();

	public void AddRegion(HighlightRegion highlightRegion)
	{
		// The most recent region always wins.
		global::System.Diagnostics.Debug.Assert(highlightRegion.EndIndex >= highlightRegion.StartIndex);

		// OPTIMIZE: Doing a find which is logn followed by an emplace which is logn
		//                     is not the most optimal way of doing things.  The reason it is done this way
		//                     is because emplace will remove the previous entry if there is a collision.  See
		//                     about making an emplace that will return the previous entry.
		var emplaceIndex = _regions.IndexOfKey(highlightRegion.StartIndex);

		if (emplaceIndex < 0)
		{
			// Element doesn't exist yet, emplace it
			_regions.Add(highlightRegion.StartIndex, highlightRegion);
			emplaceIndex = _regions.IndexOfKey(highlightRegion.StartIndex);
			var emplacedHighlightRegion = _regions.Values[emplaceIndex];

			// First look at the item immediately before this one.
			// Since nothing can overlap the previous item, only one previous item
			// needs to be checked.
			if (emplaceIndex != 0)
			{
				var currentHighlightRegion = _regions.Values[emplaceIndex - 1];

				// Check for partial overlap case
				if (emplacedHighlightRegion.StartIndex <= currentHighlightRegion.EndIndex)
				{
					// Handle partial overlap case
					// === Example ===
					// BEFORE: ...BBB...
					// AFTER:  ...BBRR..

					if (emplacedHighlightRegion.EndIndex < currentHighlightRegion.EndIndex)
					{
						// The new range bisects the previous range so another range is necessary after
						// the new range is complete.  It is safe to add to the map here since it will
						// be placed after prevIter due to key ordering for std::map.
						// === Example ===
						// BEFORE: ...BBBB...
						// AFTER:  ...BRRB...
						var newRegion = new HighlightRegion(
							emplacedHighlightRegion.EndIndex + 1,
							currentHighlightRegion.EndIndex,
							currentHighlightRegion.ForegroundBrush,
							currentHighlightRegion.BackgroundBrush);

						_regions.Add(newRegion.StartIndex, newRegion);
					}

					currentHighlightRegion.EndIndex = emplacedHighlightRegion.StartIndex - 1;

					global::System.Diagnostics.Debug.Assert(currentHighlightRegion.EndIndex >= currentHighlightRegion.StartIndex);
				}
			}
		}
		else
		{
			var emplacedHighlightRegion = _regions.Values[emplaceIndex];

			// There as an index collision so split the node if necessary.
			// === Example ===
			// BEFORE: ...BBB...
			// AFTER:  ...RRB...
			if (highlightRegion.EndIndex < emplacedHighlightRegion.EndIndex)
			{
				var newRegion = new HighlightRegion(
					highlightRegion.EndIndex + 1,
					emplacedHighlightRegion.EndIndex,
					emplacedHighlightRegion.ForegroundBrush,
					emplacedHighlightRegion.BackgroundBrush);

				_regions.Add(newRegion.StartIndex, newRegion);
			}

			// Move the new highlight region into the found node
			_regions[highlightRegion.StartIndex] = highlightRegion;
		}

		// Next look after and do any necessary region splits.
		// (Re-resolve the index: inserts above may have shifted positions, but never
		// before the emplaced key, so its key still resolves to the emplaced region.)
		emplaceIndex = _regions.IndexOfKey(highlightRegion.StartIndex);
		var emplaced = _regions.Values[emplaceIndex];

		var nextIndex = emplaceIndex + 1;
		while (nextIndex < _regions.Count)
		{
			var currentHighlightRegion = _regions.Values[nextIndex];

			if (emplaced.EndIndex >= currentHighlightRegion.EndIndex)
			{
				// The highlighter completely occludes this region, so add it to the removal list.
				// === Example ===
				// BEFORE: ...BBB...
				// AFTER:  ..RRRRR..
				_regions.RemoveAt(nextIndex);
			}
			else
			{
				// Check for partial overlap case
				// === Example ===
				// BEFORE: ...BBB...
				// AFTER:  ..RRBB...
				if (emplaced.EndIndex >= currentHighlightRegion.StartIndex)
				{
					var newRegion = new HighlightRegion(
						emplaced.EndIndex + 1,
						currentHighlightRegion.EndIndex,
						currentHighlightRegion.ForegroundBrush,
						currentHighlightRegion.BackgroundBrush);

					// Replace the current entry with a new insertion
					_regions.RemoveAt(nextIndex);
					_regions.Add(newRegion.StartIndex, newRegion);

					// NOTE: It is not possible to bi-sect an element that follows after because the
					//       following regions are guaranteed to be greater based on the incremental
					//       nature of the algorithm guaranteeing no overlaps.
				}

				// End the iteration since endIndex cannot intersect anymore ranges
				break;
			}
		}
	}

	// Iteration over the merged regions in key (startIndex) order.
	public IList<HighlightRegion> Regions => _regions.Values;
}
