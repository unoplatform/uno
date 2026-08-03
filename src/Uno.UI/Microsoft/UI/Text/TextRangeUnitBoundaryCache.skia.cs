#nullable enable

using System;

namespace Microsoft.UI.Text;

internal sealed class TextRangeUnitBoundaryCache
{
	private readonly CacheEntry?[] _entries = new CacheEntry[TextRangeUnitBoundaryProvider.UnitCount];
	private readonly int[] _rebuildCounts = new int[TextRangeUnitBoundaryProvider.UnitCount];

	internal int RebuildCount { get; private set; }

	internal TextRangeUnitBoundarySet? Get(RichEditTextDocument document, global::Microsoft.UI.Text.TextRangeUnit unit)
	{
		var descriptor = TextRangeUnitBoundaryProvider.GetDescriptor(unit);
		if (!IsCacheable(descriptor))
		{
			return TextRangeUnitBoundaryProvider.CreateBoundaries(document, descriptor);
		}

		var unitIndex = (int)unit;
		var stamp = GetStamp(document, descriptor);
		var entry = _entries[unitIndex];
		if (entry is not null && entry.Stamp.Equals(stamp))
		{
			return entry.Boundaries;
		}

		var boundaries = TextRangeUnitBoundaryProvider.CreateBoundaries(document, descriptor);
		_entries[unitIndex] = new CacheEntry(stamp, boundaries);
		_rebuildCounts[unitIndex]++;
		RebuildCount++;
		return boundaries;
	}

	internal int GetRebuildCount(global::Microsoft.UI.Text.TextRangeUnit unit)
	{
		var index = (int)unit;
		return (uint)index < (uint)_rebuildCounts.Length ? _rebuildCounts[index] : 0;
	}

	private static bool IsCacheable(TextRangeUnitProviderDescriptor descriptor)
		=> descriptor.Kind is not TextRangeUnitProviderKind.Screen
			and not TextRangeUnitProviderKind.Window
			and not TextRangeUnitProviderKind.UnsupportedOperation;

	private static BoundaryCacheStamp GetStamp(
		RichEditTextDocument document,
		TextRangeUnitProviderDescriptor descriptor)
	{
		return descriptor.Kind switch
		{
			TextRangeUnitProviderKind.Character
				or TextRangeUnitProviderKind.Cluster
				or TextRangeUnitProviderKind.Word
				or TextRangeUnitProviderKind.Sentence
				or TextRangeUnitProviderKind.Paragraph
				or TextRangeUnitProviderKind.Story
				=> new(document.TextVersion, 0, 0, 0),
			TextRangeUnitProviderKind.Line
				=> document.TryGetLineLayoutStamp(out var layoutVersion, out var width)
					? new(document.TextVersion, layoutVersion, BitConverter.DoubleToInt64Bits(width), 0)
					: new(document.TextVersion, -1, 0, 0),
			TextRangeUnitProviderKind.CharacterFormat
				or TextRangeUnitProviderKind.Object
				or TextRangeUnitProviderKind.Effect
				or TextRangeUnitProviderKind.UnsupportedEffect
				or TextRangeUnitProviderKind.ContentLink
				=> new(document.TextVersion, document.CharacterFormatVersion, 0, 0),
			TextRangeUnitProviderKind.ParagraphFormat
				=> new(document.TextVersion, document.ParagraphFormatVersion, 0, 0),
			_ => default,
		};
	}

	private readonly record struct BoundaryCacheStamp(long First, long Second, long Third, long Fourth);

	private sealed record CacheEntry(BoundaryCacheStamp Stamp, TextRangeUnitBoundarySet? Boundaries);
}
