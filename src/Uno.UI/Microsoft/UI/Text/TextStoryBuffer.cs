#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Microsoft.UI.Text;

// An implicit-treap piece table over immutable string sources. Subtree lengths index exact UTF-16
// positions; bounded local compaction limits fragmentation without copying the full story.
internal sealed class TextStoryBuffer
{
	private const int LocalCompactionEditInterval = 32;
	private const int LocalCompactionPieceThreshold = 64;
	private const int MaxLocalCompactionLength = 32 * 1024;

	private Node? _root;
	private string? _materializedText = string.Empty;
	private long _materializedVersion;
	private long _version;
	private uint _prioritySequence;
	private int _editsSinceCompaction;
	private int _fullMaterializationCount;
	private int _compactionCount;
	private long _compactedCharacterCount;

	internal int Length => GetLength(_root);

	internal int PieceCount => GetPieceCount(_root);

	internal int TreeHeight => GetHeight(_root);

	internal long Version => _version;

	internal int FullMaterializationCount => _fullMaterializationCount;

	internal int CompactionCount => _compactionCount;

	internal long CompactedCharacterCount => _compactedCharacterCount;

	internal char this[int index]
	{
		get
		{
			if ((uint)index >= (uint)Length)
			{
				throw new ArgumentOutOfRangeException(nameof(index));
			}

			var node = _root;
			while (node is not null)
			{
				var leftLength = GetLength(node.Left);
				if (index < leftLength)
				{
					node = node.Left;
				}
				else if (index < leftLength + node.Piece.Length)
				{
					return node.Piece.Source.Text[node.Piece.Start + index - leftLength];
				}
				else
				{
					index -= leftLength + node.Piece.Length;
					node = node.Right;
				}
			}

			throw new InvalidOperationException("The story index is not covered by a piece.");
		}
	}

	internal bool Reset(string text)
	{
		ArgumentNullException.ThrowIfNull(text);
		if (ContentEquals(text))
		{
			return false;
		}

		var source = new SourceBuffer(text);
		_prioritySequence = 0;
		_root = text.Length == 0
			? null
			: CreateNode(new Piece(source, 0, text.Length));
		_version++;
		_materializedText = text;
		_materializedVersion = _version;
		_editsSinceCompaction = 0;
		AssertInvariants();
		return true;
	}

	internal bool Replace(int start, int removeLength, string insert)
	{
		ArgumentNullException.ThrowIfNull(insert);
		ValidateRange(start, removeLength);
		_ = checked(Length - removeLength + insert.Length);
		if (removeLength == insert.Length && RangeEquals(start, insert))
		{
			return false;
		}

		ReplaceTree(start, removeLength, insert);
		_version++;
		_materializedText = null;
		_materializedVersion = -1;
		_editsSinceCompaction++;
		TryCompactLocal(start, insert.Length);
		AssertInvariants();
		return true;
	}

	internal string Slice(int start, int length)
	{
		ValidateRange(start, length);
		if (length == 0)
		{
			return string.Empty;
		}
		if (start == 0 && length == Length)
		{
			return GetText();
		}

		return SliceUncached(start, length);
	}

	internal string GetText()
	{
		if (_materializedText is not null && _materializedVersion == _version)
		{
			return _materializedText;
		}

		_materializedText = Length == 0
			? string.Empty
			: string.Create(
				Length,
				this,
				static (destination, buffer) => buffer.CopyTo(0, destination));
		_materializedVersion = _version;
		_fullMaterializationCount++;
		return _materializedText;
	}

	internal void AppendTo(StringBuilder builder, int start, int length)
	{
		ArgumentNullException.ThrowIfNull(builder);
		ValidateRange(start, length);
		foreach (var segment in EnumerateSegments(start, length))
		{
			builder.Append(segment.Span);
		}
	}

	internal int IndexOf(char value, int start, int count)
	{
		ValidateRange(start, count);
		return IndexOfCore(_root, 0, start, start + count, value);
	}

	internal int LastIndexOf(char value, int start, int count)
	{
		ValidateRange(start, count);
		return LastIndexOfCore(_root, 0, start, start + count, value);
	}

	internal int FindParagraphStart(int position)
	{
		position = Math.Clamp(position, 0, Length);
		if (position == Length && TextUnitNavigation.EndsInParagraphBreak(this))
		{
			return position;
		}

		var candidate = LastIndexOfBreak(_root, 0, 0, position, BreakKind.Paragraph);
		while (candidate >= 0)
		{
			var end = candidate + 1;
			if (TextUnitNavigation.IsParagraphBreakAt(this, end))
			{
				return end;
			}
			candidate = LastIndexOfBreak(_root, 0, 0, candidate, BreakKind.Paragraph);
		}

		return 0;
	}

	internal int FindParagraphEnd(int position)
	{
		position = Math.Clamp(position, 0, Length);
		if (position == Length)
		{
			return position;
		}

		var candidate = IndexOfBreak(_root, 0, position, Length, BreakKind.Paragraph);
		if (candidate < 0)
		{
			return Length;
		}
		if (this[candidate] == '\r'
			&& candidate + 1 < Length
			&& this[candidate + 1] == '\n')
		{
			return candidate + 2;
		}

		return candidate + 1;
	}

	internal int FindHardLineStart(int position)
	{
		position = Math.Clamp(position, 0, Length);
		var candidate = LastIndexOfBreak(_root, 0, 0, position, BreakKind.HardLine);
		while (candidate >= 0)
		{
			var end = candidate + 1;
			if (TextUnitNavigation.GetHardLineBreakLengthEndingAt(this, end) != 0)
			{
				return end;
			}
			candidate = LastIndexOfBreak(_root, 0, 0, candidate, BreakKind.HardLine);
		}

		return 0;
	}

	internal int FindHardLineEnd(int position)
	{
		position = Math.Clamp(position, 0, Length);
		if (position == Length)
		{
			return position;
		}

		var candidate = IndexOfBreak(_root, 0, position, Length, BreakKind.HardLine);
		if (candidate < 0)
		{
			return Length;
		}
		if (this[candidate] == '\r'
			&& candidate + 1 < Length
			&& this[candidate + 1] == '\n')
		{
			return candidate + 2;
		}

		return candidate + 1;
	}

	internal int IndexOf(string value, int start, int count, StringComparison comparison)
	{
		const int searchChunkLength = 4096;
		ArgumentNullException.ThrowIfNull(value);
		ValidateRange(start, count);
		if (value.Length == 0)
		{
			return start;
		}
		if (value.Length > count)
		{
			return -1;
		}

		var searchEnd = start + count;
		var position = start;
		while (position <= searchEnd - value.Length)
		{
			var primaryLength = Math.Min(searchChunkLength, searchEnd - position);
			var windowLength = (int)Math.Min(
				searchEnd - position,
				(long)primaryLength + value.Length - 1);
			var window = SliceUncached(position, windowLength);
			var match = window.IndexOf(value, comparison);
			if (match >= 0 && match < primaryLength)
			{
				return position + match;
			}
			position += primaryLength;
		}

		return -1;
	}

	internal bool RangeEquals(int start, string value, StringComparison comparison = StringComparison.Ordinal)
	{
		ArgumentNullException.ThrowIfNull(value);
		if (start < 0 || start > Length - value.Length)
		{
			return false;
		}
		if (comparison != StringComparison.Ordinal)
		{
			return SliceUncached(start, value.Length).Equals(value, comparison);
		}

		var compared = 0;
		foreach (var segment in EnumerateSegments(start, value.Length))
		{
			if (!segment.Span.SequenceEqual(value.AsSpan(compared, segment.Length)))
			{
				return false;
			}
			compared += segment.Length;
		}

		return compared == value.Length;
	}

	internal bool TryGetRuneAt(int index, out Rune value)
	{
		if ((uint)index >= (uint)Length)
		{
			value = default;
			return false;
		}

		var first = this[index];
		if (char.IsHighSurrogate(first)
			&& index + 1 < Length
			&& char.IsLowSurrogate(this[index + 1]))
		{
			value = new Rune(first, this[index + 1]);
			return true;
		}
		if (!char.IsSurrogate(first))
		{
			value = new Rune(first);
			return true;
		}

		value = Rune.ReplacementChar;
		return false;
	}

	internal int GetTextElementStart(int position)
		=> GetTextElementBoundary(position, getEnd: false);

	internal int GetTextElementEnd(int position)
		=> GetTextElementBoundary(position, getEnd: true);

	internal IEnumerable<ReadOnlyMemory<char>> EnumerateSegments(int start, int length)
	{
		ValidateRange(start, length);
		return EnumerateSegmentsCore(_root, 0, start, start + length);
	}

	internal bool AreInvariantsValid()
	{
		if (!ValidateNode(_root, out var length, out var count))
		{
			return false;
		}
		if (length != Length || count != PieceCount)
		{
			return false;
		}

		var enumeratedLength = 0;
		var enumeratedCount = 0;
		foreach (var segment in EnumerateSegments(0, Length))
		{
			if (segment.Length <= 0)
			{
				return false;
			}
			enumeratedLength = checked(enumeratedLength + segment.Length);
			enumeratedCount++;
		}

		return enumeratedLength == Length && enumeratedCount == PieceCount;
	}

	internal void ResetDiagnostics()
	{
		_fullMaterializationCount = 0;
		_compactionCount = 0;
		_compactedCharacterCount = 0;
	}

	private string SliceUncached(int start, int length)
		=> string.Create(
			length,
			(this, start),
			static (destination, state) => state.Item1.CopyTo(state.start, destination));

	private void CopyTo(int start, Span<char> destination)
	{
		ValidateRange(start, destination.Length);
		var copied = 0;
		foreach (var segment in EnumerateSegments(start, destination.Length))
		{
			segment.Span.CopyTo(destination[copied..]);
			copied += segment.Length;
		}
		Debug.Assert(copied == destination.Length);
	}

	private bool ContentEquals(string text)
		=> text.Length == Length && RangeEquals(0, text);

	private int GetTextElementBoundary(int position, bool getEnd)
	{
		position = Math.Clamp(position, 0, Length);
		if (position == 0 || position == Length)
		{
			return position;
		}

		var radius = 64;
		while (true)
		{
			var windowStart = Math.Max(0, position - radius);
			var windowEnd = Math.Min(Length, position + radius);
			var window = SliceUncached(windowStart, windowEnd - windowStart);
			var localPosition = position - windowStart;
			var enumerator = StringInfo.GetTextElementEnumerator(window);
			var elementStart = 0;
			var elementEnd = window.Length;
			while (enumerator.MoveNext())
			{
				var nextStart = enumerator.ElementIndex;
				if (nextStart == localPosition)
				{
					return position;
				}
				if (nextStart > localPosition)
				{
					elementEnd = nextStart;
					break;
				}
				elementStart = nextStart;
			}

			if (elementStart == 0 && windowStart > 0
				|| elementEnd == window.Length && windowEnd < Length)
			{
				radius = radius >= Length / 2 ? Length : radius * 2;
				continue;
			}

			return windowStart + (getEnd ? elementEnd : elementStart);
		}
	}

	private void ReplaceTree(int start, int removeLength, string insert)
	{
		Split(_root, start, out var left, out var tail);
		Split(tail, removeLength, out _, out var right);
		var inserted = insert.Length == 0
			? null
			: CreateNode(new Piece(new SourceBuffer(insert), 0, insert.Length));
		_root = Join(Join(left, inserted), right);
	}

	private void TryCompactLocal(int editStart, int insertLength)
	{
		if (_editsSinceCompaction < LocalCompactionEditInterval
			|| PieceCount < LocalCompactionPieceThreshold
			|| Length <= 2)
		{
			return;
		}

		_editsSinceCompaction = 0;
		var compactLength = Math.Min(MaxLocalCompactionLength, Length - 1);
		var center = Math.Clamp(editStart + insertLength / 2, 0, Length);
		var compactStart = Math.Clamp(center - compactLength / 2, 0, Length - compactLength);
		if (CountPieces(_root, 0, compactStart, compactStart + compactLength, LocalCompactionPieceThreshold)
			< LocalCompactionPieceThreshold)
		{
			return;
		}

		var compacted = SliceUncached(compactStart, compactLength);
		ReplaceTree(compactStart, compactLength, compacted);
		_compactionCount++;
		_compactedCharacterCount = checked(_compactedCharacterCount + compactLength);
	}

	private Node CreateNode(Piece piece) => new(piece, NextPriority());

	private uint NextPriority()
	{
		var value = ++_prioritySequence;
		value ^= value >> 16;
		value *= 0x7feb352d;
		value ^= value >> 15;
		value *= 0x846ca68b;
		value ^= value >> 16;
		return value;
	}

	private uint NextPriorityAtLeast(uint minimum)
	{
		var value = NextPriority();
		if (value >= minimum)
		{
			return value;
		}

		var range = (ulong)uint.MaxValue - minimum + 1;
		return minimum + (uint)((ulong)value * range >> 32);
	}

	private void Split(Node? node, int position, out Node? left, out Node? right)
	{
		if (node is null)
		{
			left = null;
			right = null;
			return;
		}

		var leftLength = GetLength(node.Left);
		var pieceEnd = leftLength + node.Piece.Length;
		if (position < leftLength)
		{
			Split(node.Left, position, out left, out var splitRight);
			node.Left = splitRight;
			Update(node);
			right = node;
		}
		else if (position > pieceEnd)
		{
			Split(node.Right, position - pieceEnd, out var splitLeft, out right);
			node.Right = splitLeft;
			Update(node);
			left = node;
		}
		else if (position == leftLength)
		{
			left = node.Left;
			node.Left = null;
			Update(node);
			right = node;
		}
		else if (position == pieceEnd)
		{
			right = node.Right;
			node.Right = null;
			Update(node);
			left = node;
		}
		else
		{
			var offset = position - leftLength;
			var leftTree = node.Left;
			var rightTree = node.Right;
			var leftPiece = node.Piece with { Length = offset };
			var rightPiece = node.Piece with
			{
				Start = node.Piece.Start + offset,
				Length = node.Piece.Length - offset,
			};
			// Split descendants cannot outrank the original node or they would violate an ancestor's heap.
			left = Merge(leftTree, new Node(leftPiece, NextPriorityAtLeast(node.Priority)));
			right = Merge(new Node(rightPiece, NextPriorityAtLeast(node.Priority)), rightTree);
		}
	}

	private Node? Join(Node? left, Node? right)
		=> Merge(left, right);

	private static Node? Merge(Node? left, Node? right)
	{
		if (left is null)
		{
			return right;
		}
		if (right is null)
		{
			return left;
		}

		if (left.Priority <= right.Priority)
		{
			left.Right = Merge(left.Right, right);
			Update(left);
			return left;
		}

		right.Left = Merge(left, right.Left);
		Update(right);
		return right;
	}

	private static IEnumerable<ReadOnlyMemory<char>> EnumerateSegmentsCore(
		Node? node,
		int subtreeStart,
		int rangeStart,
		int rangeEnd)
	{
		if (node is null || rangeStart >= rangeEnd)
		{
			yield break;
		}

		var pieceStart = subtreeStart + GetLength(node.Left);
		var pieceEnd = pieceStart + node.Piece.Length;
		if (rangeStart < pieceStart)
		{
			foreach (var segment in EnumerateSegmentsCore(node.Left, subtreeStart, rangeStart, Math.Min(rangeEnd, pieceStart)))
			{
				yield return segment;
			}
		}

		var intersectionStart = Math.Max(rangeStart, pieceStart);
		var intersectionEnd = Math.Min(rangeEnd, pieceEnd);
		if (intersectionStart < intersectionEnd)
		{
			yield return node.Piece.Source.Text.AsMemory(
				node.Piece.Start + intersectionStart - pieceStart,
				intersectionEnd - intersectionStart);
		}

		if (rangeEnd > pieceEnd)
		{
			foreach (var segment in EnumerateSegmentsCore(node.Right, pieceEnd, Math.Max(rangeStart, pieceEnd), rangeEnd))
			{
				yield return segment;
			}
		}
	}

	private static int IndexOfCore(
		Node? node,
		int subtreeStart,
		int rangeStart,
		int rangeEnd,
		char value)
	{
		if (node is null || rangeStart >= rangeEnd)
		{
			return -1;
		}

		var pieceStart = subtreeStart + GetLength(node.Left);
		var pieceEnd = pieceStart + node.Piece.Length;
		if (rangeStart < pieceStart)
		{
			var leftResult = IndexOfCore(
				node.Left,
				subtreeStart,
				rangeStart,
				Math.Min(rangeEnd, pieceStart),
				value);
			if (leftResult >= 0)
			{
				return leftResult;
			}
		}

		var intersectionStart = Math.Max(rangeStart, pieceStart);
		var intersectionEnd = Math.Min(rangeEnd, pieceEnd);
		if (intersectionStart < intersectionEnd)
		{
			var sourceStart = node.Piece.Start + intersectionStart - pieceStart;
			var sourceResult = node.Piece.Source.IndexOf(value, sourceStart, intersectionEnd - intersectionStart);
			if (sourceResult >= 0)
			{
				return pieceStart + sourceResult - node.Piece.Start;
			}
		}

		return rangeEnd > pieceEnd
			? IndexOfCore(node.Right, pieceEnd, Math.Max(rangeStart, pieceEnd), rangeEnd, value)
			: -1;
	}

	private static int LastIndexOfCore(
		Node? node,
		int subtreeStart,
		int rangeStart,
		int rangeEnd,
		char value)
	{
		if (node is null || rangeStart >= rangeEnd)
		{
			return -1;
		}

		var pieceStart = subtreeStart + GetLength(node.Left);
		var pieceEnd = pieceStart + node.Piece.Length;
		if (rangeEnd > pieceEnd)
		{
			var rightResult = LastIndexOfCore(
				node.Right,
				pieceEnd,
				Math.Max(rangeStart, pieceEnd),
				rangeEnd,
				value);
			if (rightResult >= 0)
			{
				return rightResult;
			}
		}

		var intersectionStart = Math.Max(rangeStart, pieceStart);
		var intersectionEnd = Math.Min(rangeEnd, pieceEnd);
		if (intersectionStart < intersectionEnd)
		{
			var sourceStart = node.Piece.Start + intersectionStart - pieceStart;
			var sourceResult = node.Piece.Source.LastIndexOf(value, sourceStart, intersectionEnd - intersectionStart);
			if (sourceResult >= 0)
			{
				return pieceStart + sourceResult - node.Piece.Start;
			}
		}

		return rangeStart < pieceStart
			? LastIndexOfCore(node.Left, subtreeStart, rangeStart, Math.Min(rangeEnd, pieceStart), value)
			: -1;
	}

	private static int IndexOfBreak(
		Node? node,
		int subtreeStart,
		int rangeStart,
		int rangeEnd,
		BreakKind kind)
	{
		if (node is null || rangeStart >= rangeEnd)
		{
			return -1;
		}

		var pieceStart = subtreeStart + GetLength(node.Left);
		var pieceEnd = pieceStart + node.Piece.Length;
		if (rangeStart < pieceStart)
		{
			var leftResult = IndexOfBreak(
				node.Left,
				subtreeStart,
				rangeStart,
				Math.Min(rangeEnd, pieceStart),
				kind);
			if (leftResult >= 0)
			{
				return leftResult;
			}
		}

		var intersectionStart = Math.Max(rangeStart, pieceStart);
		var intersectionEnd = Math.Min(rangeEnd, pieceEnd);
		if (intersectionStart < intersectionEnd)
		{
			var sourceStart = node.Piece.Start + intersectionStart - pieceStart;
			var sourceResult = node.Piece.Source.IndexOfBreak(kind, sourceStart, intersectionEnd - intersectionStart);
			if (sourceResult >= 0)
			{
				return pieceStart + sourceResult - node.Piece.Start;
			}
		}

		return rangeEnd > pieceEnd
			? IndexOfBreak(node.Right, pieceEnd, Math.Max(rangeStart, pieceEnd), rangeEnd, kind)
			: -1;
	}

	private static int LastIndexOfBreak(
		Node? node,
		int subtreeStart,
		int rangeStart,
		int rangeEnd,
		BreakKind kind)
	{
		if (node is null || rangeStart >= rangeEnd)
		{
			return -1;
		}

		var pieceStart = subtreeStart + GetLength(node.Left);
		var pieceEnd = pieceStart + node.Piece.Length;
		if (rangeEnd > pieceEnd)
		{
			var rightResult = LastIndexOfBreak(
				node.Right,
				pieceEnd,
				Math.Max(rangeStart, pieceEnd),
				rangeEnd,
				kind);
			if (rightResult >= 0)
			{
				return rightResult;
			}
		}

		var intersectionStart = Math.Max(rangeStart, pieceStart);
		var intersectionEnd = Math.Min(rangeEnd, pieceEnd);
		if (intersectionStart < intersectionEnd)
		{
			var sourceStart = node.Piece.Start + intersectionStart - pieceStart;
			var sourceResult = node.Piece.Source.LastIndexOfBreak(kind, sourceStart, intersectionEnd - intersectionStart);
			if (sourceResult >= 0)
			{
				return pieceStart + sourceResult - node.Piece.Start;
			}
		}

		return rangeStart < pieceStart
			? LastIndexOfBreak(node.Left, subtreeStart, rangeStart, Math.Min(rangeEnd, pieceStart), kind)
			: -1;
	}

	private static int CountPieces(
		Node? node,
		int subtreeStart,
		int rangeStart,
		int rangeEnd,
		int limit)
	{
		if (node is null || rangeStart >= rangeEnd || limit <= 0)
		{
			return 0;
		}

		var pieceStart = subtreeStart + GetLength(node.Left);
		var pieceEnd = pieceStart + node.Piece.Length;
		var count = rangeStart < pieceStart
			? CountPieces(node.Left, subtreeStart, rangeStart, Math.Min(rangeEnd, pieceStart), limit)
			: 0;
		if (count >= limit)
		{
			return count;
		}
		if (Math.Max(rangeStart, pieceStart) < Math.Min(rangeEnd, pieceEnd))
		{
			count++;
		}
		if (count >= limit || rangeEnd <= pieceEnd)
		{
			return count;
		}

		return count + CountPieces(
			node.Right,
			pieceEnd,
			Math.Max(rangeStart, pieceEnd),
			rangeEnd,
			limit - count);
	}

	private static bool ValidateNode(Node? node, out int length, out int count)
	{
		if (node is null)
		{
			length = 0;
			count = 0;
			return true;
		}
		if (node.Piece.Length <= 0
			|| node.Piece.Start < 0
			|| node.Piece.Start > node.Piece.Source.Text.Length - node.Piece.Length
			|| node.Left is not null && node.Left.Priority < node.Priority
			|| node.Right is not null && node.Right.Priority < node.Priority
			|| !ValidateNode(node.Left, out var leftLength, out var leftCount)
			|| !ValidateNode(node.Right, out var rightLength, out var rightCount))
		{
			length = 0;
			count = 0;
			return false;
		}

		length = checked(leftLength + node.Piece.Length + rightLength);
		count = checked(leftCount + 1 + rightCount);
		return node.SubtreeLength == length && node.SubtreePieceCount == count;
	}

	private static void Update(Node node)
	{
		node.SubtreeLength = checked(GetLength(node.Left) + node.Piece.Length + GetLength(node.Right));
		node.SubtreePieceCount = checked(GetPieceCount(node.Left) + 1 + GetPieceCount(node.Right));
	}

	private static int GetLength(Node? node) => node?.SubtreeLength ?? 0;

	private static int GetPieceCount(Node? node) => node?.SubtreePieceCount ?? 0;

	private static int GetHeight(Node? node)
		=> node is null ? 0 : 1 + Math.Max(GetHeight(node.Left), GetHeight(node.Right));

	private void ValidateRange(int start, int length)
	{
		if ((uint)start > (uint)Length)
		{
			throw new ArgumentOutOfRangeException(nameof(start));
		}
		if ((uint)length > (uint)(Length - start))
		{
			throw new ArgumentOutOfRangeException(nameof(length));
		}
	}

	[Conditional("DEBUG")]
	private void AssertInvariants()
	{
		var error = GetInvariantError();
		Debug.Assert(error is null, $"TextStoryBuffer piece indexes are inconsistent: {error}");
	}

	private string? GetInvariantError()
	{
		if (GetNodeInvariantError(_root, "root") is { } nodeError)
		{
			return nodeError;
		}

		var enumeratedLength = 0;
		var enumeratedCount = 0;
		foreach (var segment in EnumerateSegments(0, Length))
		{
			enumeratedLength = checked(enumeratedLength + segment.Length);
			enumeratedCount++;
		}

		return enumeratedLength != Length
			? $"enumerated length {enumeratedLength} != {Length}"
			: enumeratedCount != PieceCount
				? $"enumerated count {enumeratedCount} != {PieceCount}"
				: null;
	}

	private static string? GetNodeInvariantError(Node? node, string path)
	{
		if (node is null)
		{
			return null;
		}
		if (node.Piece.Length <= 0)
		{
			return $"{path} has non-positive piece length {node.Piece.Length}";
		}
		if (node.Piece.Start < 0 || node.Piece.Start > node.Piece.Source.Text.Length - node.Piece.Length)
		{
			return $"{path} piece [{node.Piece.Start}, {node.Piece.Start + node.Piece.Length}) exceeds source length {node.Piece.Source.Text.Length}";
		}
		if (node.Left is not null && node.Left.Priority < node.Priority)
		{
			return $"{path}.left priority {node.Left.Priority} precedes parent {node.Priority}";
		}
		if (node.Right is not null && node.Right.Priority < node.Priority)
		{
			return $"{path}.right priority {node.Right.Priority} precedes parent {node.Priority}";
		}
		if (GetNodeInvariantError(node.Left, path + ".left") is { } leftError)
		{
			return leftError;
		}
		if (GetNodeInvariantError(node.Right, path + ".right") is { } rightError)
		{
			return rightError;
		}

		var length = checked(GetLength(node.Left) + node.Piece.Length + GetLength(node.Right));
		var count = checked(GetPieceCount(node.Left) + 1 + GetPieceCount(node.Right));
		return node.SubtreeLength != length
			? $"{path} subtree length {node.SubtreeLength} != {length}"
			: node.SubtreePieceCount != count
				? $"{path} subtree count {node.SubtreePieceCount} != {count}"
				: null;
	}

	private readonly record struct Piece(SourceBuffer Source, int Start, int Length);

	private enum BreakKind
	{
		Paragraph,
		HardLine,
	}

	[Flags]
	private enum SourceBreakKind : byte
	{
		CarriageReturn = 1,
		Paragraph = 2,
		HardLine = 4,
	}

	private sealed class SourceBuffer
	{
		private readonly int[] _breakPositions;
		private readonly byte[] _breakKinds;

		internal SourceBuffer(string text)
		{
			Text = text;
			List<int>? breakPositions = null;
			List<byte>? breakKinds = null;
			for (var i = 0; i < text.Length; i++)
			{
				var value = text[i];
				var kind = default(SourceBreakKind);
				if (value == '\r')
				{
					kind |= SourceBreakKind.CarriageReturn;
				}
				if (value is '\r' or '\n' or '\u2029')
				{
					kind |= SourceBreakKind.Paragraph;
				}
				if (value is '\n' or '\v' or '\f' or '\r' or '\u0085' or '\u2028' or '\u2029')
				{
					kind |= SourceBreakKind.HardLine;
				}
				if (kind != 0)
				{
					(breakPositions ??= new()).Add(i);
					(breakKinds ??= new()).Add((byte)kind);
				}
			}

			_breakPositions = breakPositions?.ToArray() ?? Array.Empty<int>();
			_breakKinds = breakKinds?.ToArray() ?? Array.Empty<byte>();
		}

		internal string Text { get; }

		internal int IndexOf(char value, int start, int count)
		{
			if (value == '\r')
			{
				return IndexOf(SourceBreakKind.CarriageReturn, start, count);
			}

			var index = Text.AsSpan(start, count).IndexOf(value);
			return index < 0 ? -1 : start + index;
		}

		internal int LastIndexOf(char value, int start, int count)
		{
			if (value == '\r')
			{
				return LastIndexOf(SourceBreakKind.CarriageReturn, start, count);
			}

			var index = Text.AsSpan(start, count).LastIndexOf(value);
			return index < 0 ? -1 : start + index;
		}

		internal int IndexOfBreak(BreakKind kind, int start, int count)
			=> IndexOf(
				kind == BreakKind.Paragraph ? SourceBreakKind.Paragraph : SourceBreakKind.HardLine,
				start,
				count);

		internal int LastIndexOfBreak(BreakKind kind, int start, int count)
			=> LastIndexOf(
				kind == BreakKind.Paragraph ? SourceBreakKind.Paragraph : SourceBreakKind.HardLine,
				start,
				count);

		private int IndexOf(SourceBreakKind kind, int start, int count)
		{
			var index = Array.BinarySearch(_breakPositions, start);
			index = index >= 0 ? index : ~index;
			var end = start + count;
			while (index < _breakPositions.Length && _breakPositions[index] < end)
			{
				if (((SourceBreakKind)_breakKinds[index] & kind) != 0)
				{
					return _breakPositions[index];
				}
				index++;
			}
			return -1;
		}

		private int LastIndexOf(SourceBreakKind kind, int start, int count)
		{
			var end = start + count;
			var index = Array.BinarySearch(_breakPositions, end);
			index = index >= 0 ? index - 1 : ~index - 1;
			while (index >= 0 && _breakPositions[index] >= start)
			{
				if (((SourceBreakKind)_breakKinds[index] & kind) != 0)
				{
					return _breakPositions[index];
				}
				index--;
			}
			return -1;
		}
	}

	private sealed class Node
	{
		internal Node(Piece piece, uint priority)
		{
			Piece = piece;
			Priority = priority;
			SubtreeLength = piece.Length;
			SubtreePieceCount = 1;
		}

		internal Piece Piece;
		internal readonly uint Priority;
		internal Node? Left;
		internal Node? Right;
		internal int SubtreeLength;
		internal int SubtreePieceCount;
	}
}
