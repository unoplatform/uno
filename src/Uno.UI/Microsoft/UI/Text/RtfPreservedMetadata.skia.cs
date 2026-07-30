#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.UI.Text;

internal sealed record RtfPreservedEntry(
	string Rtf,
	int Anchor,
	int ProjectedLength,
	int ValidationStart,
	int ValidationLength,
	int RegionId,
	int ParentRegionId,
	int Sequence);

internal sealed class RtfPreservedMetadata
{
	internal static readonly RtfPreservedMetadata Empty = new(Array.Empty<RtfPreservedEntry>());

	private readonly IReadOnlyList<RtfPreservedEntry> _entries;

	internal RtfPreservedMetadata(IReadOnlyList<RtfPreservedEntry> entries)
	{
		_entries = entries;
	}

	internal IReadOnlyList<RtfPreservedEntry> Entries => _entries;

	internal bool IsEmpty => _entries.Count == 0;

	internal RtfPreservedMetadata Slice(int start, int length)
	{
		if (_entries.Count == 0)
		{
			return Empty;
		}

		var end = checked(start + length);
		var result = new List<RtfPreservedEntry>();
		foreach (var entry in _entries)
		{
			var validationEnd = entry.ValidationStart + entry.ValidationLength;
			var validationIsContained = entry.ValidationLength == 0
				? entry.Anchor >= start && entry.Anchor <= end
				: entry.ValidationStart >= start && validationEnd <= end;
			if (validationIsContained && entry.Anchor >= start && entry.Anchor + entry.ProjectedLength <= end)
			{
				result.Add(entry with
				{
					Anchor = entry.Anchor - start,
					ValidationStart = entry.ValidationStart - start,
				});
			}
		}

		return result.Count == 0 ? Empty : new RtfPreservedMetadata(result);
	}

	internal RtfPreservedMetadata ApplyEdit(
		int start,
		int removeLength,
		int insertLength,
		RtfPreservedMetadata? inserted = null)
	{
		if (_entries.Count == 0 && (inserted is null || inserted.IsEmpty))
		{
			return Empty;
		}

		var end = checked(start + removeLength);
		var invalidRegions = new HashSet<int>();
		foreach (var entry in _entries)
		{
			if (entry.ValidationLength == 0)
			{
				if (removeLength != 0 && entry.Anchor >= start && entry.Anchor < end)
				{
					invalidRegions.Add(entry.RegionId);
				}
				continue;
			}

			if (removeLength != 0
				&& entry.ProjectedLength != 0
				&& start < entry.Anchor + entry.ProjectedLength
				&& end > entry.Anchor)
			{
				invalidRegions.Add(entry.RegionId);
			}
		}
		bool added;
		do
		{
			added = false;
			foreach (var entry in _entries)
			{
				if (entry.ParentRegionId != 0
					&& invalidRegions.Contains(entry.ParentRegionId)
					&& invalidRegions.Add(entry.RegionId))
				{
					added = true;
				}
			}
		}
		while (added);

		var delta = insertLength - removeLength;
		var result = new List<RtfPreservedEntry>(_entries.Count + (inserted?._entries.Count ?? 0));
		foreach (var entry in _entries)
		{
			if (invalidRegions.Contains(entry.RegionId))
			{
				continue;
			}

			if (entry.ValidationLength == 0)
			{
				result.Add(entry with
				{
					Anchor = RebaseOpaqueAnchor(entry.Anchor),
					ValidationStart = RebaseBoundary(entry.ValidationStart),
				});
				continue;
			}

			var validationStart = RebaseBoundary(entry.ValidationStart);
			var validationEnd = RebaseBoundary(entry.ValidationStart + entry.ValidationLength);
			result.Add(entry with
			{
				Anchor = RebaseTableAnchor(entry),
				ValidationStart = validationStart,
				ValidationLength = Math.Max(0, validationEnd - validationStart),
			});
		}

		if (inserted is { IsEmpty: false })
		{
			var nextRegionId = 1;
			var nextSequence = 0;
			foreach (var entry in result)
			{
				nextRegionId = Math.Max(nextRegionId, entry.RegionId + 1);
				nextSequence = Math.Max(nextSequence, entry.Sequence + 1);
			}
			var remappedRegions = new Dictionary<int, int>();
			var insertedEntries = new List<RtfPreservedEntry>(inserted._entries.Count);
			foreach (var entry in inserted._entries)
			{
				if (!remappedRegions.TryGetValue(entry.RegionId, out var regionId))
				{
					regionId = nextRegionId++;
					remappedRegions.Add(entry.RegionId, regionId);
				}
				insertedEntries.Add(entry with
				{
					Anchor = checked(start + entry.Anchor),
					ValidationStart = checked(start + entry.ValidationStart),
					RegionId = regionId,
					ParentRegionId = entry.ParentRegionId == 0
						? 0
						: remappedRegions.TryGetValue(entry.ParentRegionId, out var parentRegionId)
							? parentRegionId
							: 0,
					Sequence = nextSequence++,
				});
			}

			var merged = new List<RtfPreservedEntry>(result.Count + insertedEntries.Count);
			var existingIndex = 0;
			var insertedIndex = 0;
			while (existingIndex < result.Count && insertedIndex < insertedEntries.Count)
			{
				if (Compare(result[existingIndex], insertedEntries[insertedIndex]) <= 0)
				{
					merged.Add(result[existingIndex++]);
				}
				else
				{
					merged.Add(insertedEntries[insertedIndex++]);
				}
			}
			while (existingIndex < result.Count)
			{
				merged.Add(result[existingIndex++]);
			}
			while (insertedIndex < insertedEntries.Count)
			{
				merged.Add(insertedEntries[insertedIndex++]);
			}
			result = merged;
		}

		return result.Count == 0 ? Empty : new RtfPreservedMetadata(result);

		static int Compare(RtfPreservedEntry left, RtfPreservedEntry right)
		{
			var anchor = left.Anchor.CompareTo(right.Anchor);
			return anchor != 0 ? anchor : left.Sequence.CompareTo(right.Sequence);
		}

		int RebaseOpaqueAnchor(int position)
		{
			if (position < start || removeLength == 0 && position == start)
			{
				return position;
			}
			if (position >= end)
			{
				return checked(position + delta);
			}
			return checked(start + insertLength);
		}

		int RebaseTableAnchor(RtfPreservedEntry entry)
		{
			var position = entry.Anchor;
			if (position < start
				|| position == start && entry.ProjectedLength == 0)
			{
				return position;
			}
			if (removeLength == 0)
			{
				return checked(position + insertLength);
			}
			if (position >= end)
			{
				return checked(position + delta);
			}
			return checked(start + insertLength);
		}

		int RebaseBoundary(int position)
		{
			if (removeLength == 0)
			{
				return position <= start ? position : checked(position + insertLength);
			}
			if (position <= start)
			{
				return position;
			}
			if (position >= end)
			{
				return checked(position + delta);
			}
			return checked(start + insertLength);
		}
	}
}
