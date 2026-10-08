#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.UI.Text
{
	internal readonly record struct TextRangeUnitSpan
	{
		internal TextRangeUnitSpan(int start, int end)
			: this(start, end, end, end)
		{
		}

		internal TextRangeUnitSpan(int start, int end, int containmentEnd)
			: this(start, end, containmentEnd, end)
		{
		}

		internal TextRangeUnitSpan(int start, int end, int containmentEnd, int operationEnd)
		{
			Start = start;
			End = end;
			ContainmentEnd = containmentEnd;
			OperationEnd = operationEnd;
		}

		internal int Start { get; }

		internal int End { get; }

		internal int ContainmentEnd { get; }

		internal int OperationEnd { get; }
	}

	internal sealed class TextRangeUnitBoundarySet
	{
		private readonly int _regularCount;
		private readonly TextRangeUnitSpan[]? _spans;
		private readonly int[]? _compactBoundaries;
		private readonly int _compactBoundaryCount;
		private readonly int _compactStoryEnd;

		internal TextRangeUnitBoundarySet(int regularCount)
		{
			_regularCount = regularCount;
		}

		internal TextRangeUnitBoundarySet(TextRangeUnitSpan[] spans, bool sparse = false)
		{
			_spans = spans;
			IsSparse = sparse;
		}

		internal TextRangeUnitBoundarySet(int[] boundaries, int count, int storyEnd)
		{
			_compactBoundaries = boundaries;
			_compactBoundaryCount = count;
			_compactStoryEnd = storyEnd;
		}

		internal bool IsSparse { get; }

		internal int OwnedStorageBytes => checked((_spans?.Length ?? 0) * 16);

		internal int Count
			=> _spans is not null
				? _spans.Length
				: _compactBoundaries is not null
					? _compactBoundaryCount
					: _regularCount;

		internal TextRangeUnitSpan this[int index]
			=> _spans is not null
				? _spans[index]
				: _compactBoundaries is not null
					? new(
						_compactBoundaries[index],
						index + 1 < _compactBoundaryCount
							? _compactBoundaries[index + 1]
							: _compactStoryEnd)
					: new(index, index + 1);

		internal int FindContaining(int position)
		{
			if (_spans is null && _compactBoundaries is null)
			{
				return position >= 0 && position < _regularCount ? position : -1;
			}

			var low = 0;
			var high = Count - 1;
			while (low <= high)
			{
				var middle = low + ((high - low) / 2);
				var span = this[middle];
				if (position < span.Start)
				{
					high = middle - 1;
				}
				else if (position >= span.ContainmentEnd)
				{
					low = middle + 1;
				}
				else
				{
					return middle;
				}
			}

			if (IsSparse && high >= 0 && position == this[high].End)
			{
				return high;
			}

			return -1;
		}

		internal bool HasStartAt(int position)
		{
			if (_spans is null && _compactBoundaries is null)
			{
				return position >= 0 && position < _regularCount;
			}

			var low = 0;
			var high = Count - 1;
			while (low <= high)
			{
				var middle = low + ((high - low) / 2);
				var start = this[middle].Start;
				if (position < start)
				{
					high = middle - 1;
				}
				else if (position > start)
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

		internal bool TryGetSpanEndingAt(int position, out TextRangeUnitSpan span)
		{
			if (_spans is null && _compactBoundaries is null)
			{
				span = default;
				return false;
			}

			var low = 0;
			var high = Count - 1;
			while (low <= high)
			{
				var middle = low + ((high - low) / 2);
				var end = this[middle].End;
				if (position < end)
				{
					high = middle - 1;
				}
				else if (position > end)
				{
					low = middle + 1;
				}
				else
				{
					span = this[middle];
					return true;
				}
			}

			span = default;
			return false;
		}

		internal bool TryGetSpanContainingForward(int position, out TextRangeUnitSpan span)
		{
			if (_spans is not null || _compactBoundaries is not null)
			{
				var index = FindContaining(position);
				if (index >= 0 && position < this[index].End)
				{
					span = this[index];
					return true;
				}
			}

			span = default;
			return false;
		}

		internal int CountEndingAtOrBefore(int position)
		{
			if (_spans is null && _compactBoundaries is null)
			{
				return Math.Clamp(position, 0, _regularCount);
			}

			var low = 0;
			var high = Count;
			while (low < high)
			{
				var middle = low + ((high - low) / 2);
				if (this[middle].End <= position)
				{
					low = middle + 1;
				}
				else
				{
					high = middle;
				}
			}
			return low;
		}

		internal int GetLeadingCompletedIndex(int position)
			=> (_spans is not null || _compactBoundaries is not null)
				&& Count > 0
				&& this[0].Start == 0
				&& this[0].End <= position
					? 1
					: 0;

		internal int Move(int position, int count, out int unitsMoved)
		{
			unitsMoved = 0;
			if (count == 0 || Count == 0)
			{
				return position;
			}

			if (_spans is null && _compactBoundaries is null)
			{
				var target = (int)Math.Clamp((long)position + count, 0, _regularCount - 1);
				unitsMoved = target - position;
				return target;
			}

			if (count > 0)
			{
				var first = FindFirstStartAfter(position);
				if (first < 0)
				{
					if (!IsSparse && Count > 0 && this[Count - 1].End > position)
					{
						unitsMoved = 1;
						return this[Count - 1].End;
					}
					return position;
				}

				var targetOrdinal = (long)first + count - 1;
				if (!IsSparse && targetOrdinal >= Count)
				{
					unitsMoved = Count - first + 1;
					return this[Count - 1].End;
				}

				var targetIndex = (int)Math.Min(targetOrdinal, Count - 1);
				unitsMoved = targetIndex - first + 1;
				return this[targetIndex].Start;
			}

			var previous = FindLastStartBefore(position);
			if (previous < 0)
			{
				return position;
			}

			var negativeCount = -(long)count;
			var previousTarget = (int)Math.Max((long)previous - (negativeCount - 1), 0);
			unitsMoved = -(previous - previousTarget + 1);
			return this[previousTarget].Start;
		}

		internal int[] GetMovementBoundaries()
		{
			if (_spans is null && _compactBoundaries is null)
			{
				var boundaries = new int[_regularCount];
				for (var i = 0; i < boundaries.Length; i++)
				{
					boundaries[i] = i;
				}
				return boundaries;
			}

			if (Count == 0)
			{
				return Array.Empty<int>();
			}

			var values = new List<int>(Count + 1);
			for (var i = 0; i < Count; i++)
			{
				if (values.Count == 0 || values[^1] != this[i].Start)
				{
					values.Add(this[i].Start);
				}
			}
			if (!IsSparse && values[^1] != this[Count - 1].End)
			{
				values.Add(this[Count - 1].End);
			}
			return values.ToArray();
		}

		private int FindFirstStartAfter(int position)
		{
			var low = 0;
			var high = Count;
			while (low < high)
			{
				var middle = low + ((high - low) / 2);
				if (this[middle].Start <= position)
				{
					low = middle + 1;
				}
				else
				{
					high = middle;
				}
			}
			return low < Count ? low : -1;
		}

		private int FindLastStartBefore(int position)
		{
			var low = 0;
			var high = Count;
			while (low < high)
			{
				var middle = low + ((high - low) / 2);
				if (this[middle].Start < position)
				{
					low = middle + 1;
				}
				else
				{
					high = middle;
				}
			}
			return low - 1;
		}
	}

	internal enum TextRangeUnitProviderKind
	{
		Character,
		Cluster,
		Word,
		Sentence,
		Paragraph,
		Line,
		Story,
		Screen,
		Window,
		CharacterFormat,
		ParagraphFormat,
		Object,
		Effect,
		UnsupportedOperation,
		UnsupportedEffect,
		ContentLink,
	}

	internal enum TextRangeUnitEffect
	{
		None,
		Bold,
		Italic,
		Underline,
		Strikethrough,
		ProtectedText,
		Link,
		SmallCaps,
		AllCaps,
		Hidden,
		Outline,
		Subscript,
		Superscript,
		FontBound,
		LinkProtected,
	}

	internal readonly record struct TextRangeUnitProviderDescriptor(
		TextRangeUnitProviderKind Kind,
		TextRangeUnitEffect Effect = TextRangeUnitEffect.None)
	{
		internal bool IsEffectUnit
			=> Kind is TextRangeUnitProviderKind.Effect or TextRangeUnitProviderKind.UnsupportedEffect;

		internal bool IsSparse
			=> Kind is TextRangeUnitProviderKind.Effect
				or TextRangeUnitProviderKind.UnsupportedEffect
				or TextRangeUnitProviderKind.Object;

		internal bool MoveCollapseConsumesUnit
			=> Kind is TextRangeUnitProviderKind.Cluster
				or TextRangeUnitProviderKind.CharacterFormat
				or TextRangeUnitProviderKind.Object
				or TextRangeUnitProviderKind.Effect
				or TextRangeUnitProviderKind.UnsupportedEffect
				or TextRangeUnitProviderKind.ContentLink;
	}

	internal static class TextRangeUnitBoundaryProvider
	{
		internal const int UnitCount = 33;

		private static readonly TextRangeUnitProviderDescriptor[] _providers =
		{
			new(TextRangeUnitProviderKind.Character),
			new(TextRangeUnitProviderKind.Word),
			new(TextRangeUnitProviderKind.Sentence),
			new(TextRangeUnitProviderKind.Paragraph),
			new(TextRangeUnitProviderKind.Line),
			new(TextRangeUnitProviderKind.Story),
			new(TextRangeUnitProviderKind.UnsupportedOperation), // Screen is not implemented by native RichEditBox TOM.
			new(TextRangeUnitProviderKind.UnsupportedOperation), // Section is not implemented by native RichEditBox TOM.
			new(TextRangeUnitProviderKind.Window),
			new(TextRangeUnitProviderKind.CharacterFormat),
			new(TextRangeUnitProviderKind.ParagraphFormat),
			new(TextRangeUnitProviderKind.Object),
			new(TextRangeUnitProviderKind.Paragraph), // HardParagraph differs only for table cells.
			new(TextRangeUnitProviderKind.Cluster),
			new(TextRangeUnitProviderKind.Effect, TextRangeUnitEffect.Bold),
			new(TextRangeUnitProviderKind.Effect, TextRangeUnitEffect.Italic),
			new(TextRangeUnitProviderKind.Effect, TextRangeUnitEffect.Underline),
			new(TextRangeUnitProviderKind.Effect, TextRangeUnitEffect.Strikethrough),
			new(TextRangeUnitProviderKind.Effect, TextRangeUnitEffect.ProtectedText),
			new(TextRangeUnitProviderKind.Effect, TextRangeUnitEffect.Link),
			new(TextRangeUnitProviderKind.Effect, TextRangeUnitEffect.SmallCaps),
			new(TextRangeUnitProviderKind.Effect, TextRangeUnitEffect.AllCaps),
			new(TextRangeUnitProviderKind.Effect, TextRangeUnitEffect.Hidden),
			new(TextRangeUnitProviderKind.Effect, TextRangeUnitEffect.Outline),
			new(TextRangeUnitProviderKind.UnsupportedEffect), // Shadow is not modeled.
			new(TextRangeUnitProviderKind.UnsupportedEffect), // Imprint is not modeled.
			new(TextRangeUnitProviderKind.UnsupportedEffect), // Disabled text is not modeled.
			new(TextRangeUnitProviderKind.UnsupportedEffect), // Revision marks are not modeled.
			new(TextRangeUnitProviderKind.Effect, TextRangeUnitEffect.Subscript),
			new(TextRangeUnitProviderKind.Effect, TextRangeUnitEffect.Superscript),
			new(TextRangeUnitProviderKind.Effect, TextRangeUnitEffect.FontBound),
			new(TextRangeUnitProviderKind.Effect, TextRangeUnitEffect.LinkProtected),
			new(TextRangeUnitProviderKind.ContentLink), // WinRT ContentLink objects are not modeled.
		};

		internal static TextRangeUnitProviderDescriptor GetDescriptor(global::Microsoft.UI.Text.TextRangeUnit unit)
			=> GetDescriptor(unit, throwOnUnsupportedOperation: true);

		internal static TextRangeUnitProviderDescriptor GetDescriptorForDelete(global::Microsoft.UI.Text.TextRangeUnit unit)
			=> GetDescriptor(unit, throwOnUnsupportedOperation: false);

		private static TextRangeUnitProviderDescriptor GetDescriptor(
			global::Microsoft.UI.Text.TextRangeUnit unit,
			bool throwOnUnsupportedOperation)
		{
			var value = (int)unit;
			if ((uint)value >= (uint)_providers.Length)
			{
				throw new ArgumentException("The text range unit is invalid.", nameof(unit));
			}

			var descriptor = _providers[value];
			if (throwOnUnsupportedOperation && descriptor.Kind == TextRangeUnitProviderKind.UnsupportedOperation)
			{
				throw new NotImplementedException();
			}
			return descriptor;
		}

		internal static TextRangeUnitBoundarySet? GetBoundaries(
			RichEditTextDocument document,
			global::Microsoft.UI.Text.TextRangeUnit unit)
		{
			var descriptor = GetDescriptor(unit);
			return CreateBoundaries(document, descriptor);
		}

		internal static TextRangeUnitBoundarySet? CreateBoundaries(
			RichEditTextDocument document,
			TextRangeUnitProviderDescriptor descriptor)
		{
			return descriptor.Kind switch
			{
				TextRangeUnitProviderKind.Character => new TextRangeUnitBoundarySet(document.StoryLength),
				TextRangeUnitProviderKind.Cluster => CreateClusterBoundaries(document),
				TextRangeUnitProviderKind.Word => CreateChunkBoundaries(document, global::Microsoft.UI.Text.TextRangeUnit.Word),
				TextRangeUnitProviderKind.Sentence => CreateChunkBoundaries(document, global::Microsoft.UI.Text.TextRangeUnit.Sentence),
				TextRangeUnitProviderKind.Paragraph => CreateChunkBoundaries(document, global::Microsoft.UI.Text.TextRangeUnit.Paragraph),
				TextRangeUnitProviderKind.Line => document.GetLineUnitBoundaries(),
				TextRangeUnitProviderKind.Story => new TextRangeUnitBoundarySet(new[] { new TextRangeUnitSpan(0, document.StoryLength) }),
				TextRangeUnitProviderKind.Screen or TextRangeUnitProviderKind.Window => document.GetVisibleUnitBoundaries(),
				TextRangeUnitProviderKind.CharacterFormat => document.GetCharacterFormatUnitBoundaries(),
				TextRangeUnitProviderKind.ParagraphFormat => document.GetParagraphFormatUnitBoundaries(),
				TextRangeUnitProviderKind.Object => document.GetObjectUnitBoundaries(),
				TextRangeUnitProviderKind.Effect => document.GetEffectUnitBoundaries(descriptor.Effect),
				TextRangeUnitProviderKind.UnsupportedEffect => new TextRangeUnitBoundarySet(Array.Empty<TextRangeUnitSpan>(), sparse: true),
				_ => null,
			};
		}

		private static TextRangeUnitBoundarySet CreateClusterBoundaries(RichEditTextDocument document)
		{
			var source = document.TextElementBoundaries;
			return new TextRangeUnitBoundarySet(source.Boundaries, source.Count, document.StoryLength);
		}

		internal static TextRangeUnitBoundarySet CreateChunkBoundaries(
			RichEditTextDocument document,
			global::Microsoft.UI.Text.TextRangeUnit unit)
		{
			var chunks = document.GetTextChunks(unit);
			if (chunks is null)
			{
				return new TextRangeUnitBoundarySet(Array.Empty<TextRangeUnitSpan>());
			}

			var finalEopBelongsToLastChunk =
				unit == global::Microsoft.UI.Text.TextRangeUnit.Sentence
				&& chunks.Count > 0
				&& chunks[^1].start + chunks[^1].length == document.TextLength;
			var addFinalEop = !finalEopBelongsToLastChunk && (chunks.Count == 0
				|| chunks[^1].start + chunks[^1].length <= document.TextLength);
			var spans = new TextRangeUnitSpan[chunks.Count + (addFinalEop ? 1 : 0)];
			for (var i = 0; i < chunks.Count; i++)
			{
				var end = finalEopBelongsToLastChunk && i == chunks.Count - 1
					? document.StoryLength
					: chunks[i].start + chunks[i].length;
				spans[i] = new(chunks[i].start, end);
			}
			if (addFinalEop)
			{
				spans[^1] = new(document.TextLength, document.StoryLength);
			}
			return new TextRangeUnitBoundarySet(spans);
		}
	}

	public partial class RichEditTextDocument
	{
		internal TextRangeUnitBoundarySet? GetUnitBoundaries(global::Microsoft.UI.Text.TextRangeUnit unit)
			=> _textRangeUnitBoundaryCache.Get(this, unit);

		internal TextRangeUnitBoundarySet? GetLineUnitBoundaries()
		{
			if (_owner.GetVisualLineUnitBoundaries() is { } boundaries)
			{
				return boundaries;
			}

			return TextRangeUnitBoundaryProvider.CreateChunkBoundaries(
				this,
				global::Microsoft.UI.Text.TextRangeUnit.Paragraph);
		}

		internal TextRangeUnitBoundarySet? GetVisibleUnitBoundaries()
			=> TryGetVisibleRange(out var start, out var end)
				? new TextRangeUnitBoundarySet(new[] { new TextRangeUnitSpan(start, end) })
				: null;

		internal TextRangeUnitBoundarySet GetCharacterFormatUnitBoundaries()
		{
			SyncRunsToLength(TextLength);
			if (_runs.Count == 0)
			{
				return new TextRangeUnitBoundarySet(new[] { new TextRangeUnitSpan(0, StoryLength) });
			}

			var spans = new TextRangeUnitSpan[_runs.Count];
			for (var i = 0; i < _runs.Count; i++)
			{
				spans[i] = new(GetRunStart(i), i + 1 < _runs.Count ? _runs.GetEnd(i) : StoryLength);
			}
			return new TextRangeUnitBoundarySet(spans);
		}

		internal TextRangeUnitBoundarySet GetParagraphFormatUnitBoundaries()
		{
			SyncParagraphRunsToLength(TextLength);
			var spans = new List<TextRangeUnitSpan>(_paragraphRuns.Count + 1);
			for (var i = 0; i < _paragraphRuns.Count; i++)
			{
				spans.Add(new(GetParagraphRunStart(i), _paragraphRuns.GetEnd(i)));
			}

			if (spans.Count > 0 && _paragraphRuns[^1].Format.Equals(_terminalParagraphFormat))
			{
				spans[^1] = new(spans[^1].Start, StoryLength);
			}
			else
			{
				spans.Add(new(TextLength, StoryLength));
			}
			return new TextRangeUnitBoundarySet(spans.ToArray());
		}

		internal TextRangeUnitBoundarySet GetObjectUnitBoundaries()
		{
			SyncRunsToLength(TextLength);
			var spans = new List<TextRangeUnitSpan>();
			var start = 0;
			foreach (var run in _runs)
			{
				if (run.Format.InlineImage is not null)
				{
					spans.Add(new(start, start + run.Length));
				}
				start += run.Length;
			}
			return new TextRangeUnitBoundarySet(spans.ToArray(), sparse: true);
		}

		internal bool IsInlineObjectRange(int start, int end)
		{
			var objects = GetObjectUnitBoundaries();
			var index = objects.FindContaining(start);
			if (index < 0)
			{
				return false;
			}

			var span = objects[index];
			return span.Start == start && span.End == end;
		}

		internal TextRangeUnitBoundarySet GetEffectUnitBoundaries(TextRangeUnitEffect effect)
		{
			SyncRunsToLength(TextLength);
			var spans = new List<TextRangeUnitSpan>();
			var position = 0;
			object? previousKey = null;
			var activeStart = -1;
			for (var i = 0; i < _runs.Count; i++)
			{
				var run = _runs[i];
				var key = GetEffectKey(run.Format, effect);
				if (key is null)
				{
					if (activeStart >= 0)
					{
						spans.Add(new(activeStart, position));
						activeStart = -1;
					}
					previousKey = null;
				}
				else if (activeStart < 0)
				{
					activeStart = position;
					previousKey = key;
				}
				else if (!Equals(previousKey, key))
				{
					spans.Add(new(activeStart, position));
					activeStart = position;
					previousKey = key;
				}
				position += run.Length;
			}
			if (activeStart >= 0)
			{
				spans.Add(new(activeStart, position == TextLength ? StoryLength : position));
			}
			return new TextRangeUnitBoundarySet(spans.ToArray(), sparse: true);
		}

		private static object? GetEffectKey(CharacterFormatState format, TextRangeUnitEffect effect)
			=> effect switch
			{
				TextRangeUnitEffect.Bold when format.Bold => true,
				TextRangeUnitEffect.Italic when format.Italic => true,
				TextRangeUnitEffect.Underline when format.Underline != global::Microsoft.UI.Text.UnderlineType.None => format.Underline,
				TextRangeUnitEffect.Strikethrough when format.Strikethrough => true,
				TextRangeUnitEffect.ProtectedText when format.ProtectedText => true,
				TextRangeUnitEffect.Link when !string.IsNullOrEmpty(format.Link) => format.Link,
				TextRangeUnitEffect.SmallCaps when format.SmallCaps => true,
				TextRangeUnitEffect.AllCaps when format.AllCaps => true,
				TextRangeUnitEffect.Hidden when format.Hidden => true,
				TextRangeUnitEffect.Outline when format.Outline => true,
				TextRangeUnitEffect.Subscript when format.Subscript => true,
				TextRangeUnitEffect.Superscript when format.Superscript => true,
				TextRangeUnitEffect.FontBound when !string.IsNullOrEmpty(format.Name) => format.Name,
				TextRangeUnitEffect.LinkProtected when !string.IsNullOrEmpty(format.Link) && format.ProtectedText => format.Link,
				_ => null,
			};
	}
}
