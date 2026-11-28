#nullable enable
using System;
using System.Collections.Generic;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Documents;

/// <summary>A line of a <see cref="UnicodeText"/> layout, in the layout's coordinates.</summary>
/// <param name="Start">The index of the line's first character.</param>
/// <param name="End">The index after the line's last character, its line break included.</param>
/// <param name="X0">The left edge of the line's content, trailing whitespace excluded.</param>
/// <param name="AlignmentOffset">The left edge of the line, trailing whitespace included.</param>
/// <param name="Width">The content's width, trailing whitespace excluded.</param>
/// <param name="WidthIncludingTrailingWhitespace">The line's width.</param>
/// <param name="Top">The top of the line box.</param>
/// <param name="Height">The height of the line box.</param>
/// <param name="Baseline">The baseline's distance from <paramref name="Top"/>.</param>
/// <param name="NewlineLength">The length of the line break that ends the line, 0 if it wraps or ends the text.</param>
/// <param name="TrailingWhitespaceLength">The number of whitespace characters after the line's last other character,
/// not counting the line break (as <c>TextLine.TrailingWhitespaceLength</c>). 0 on a line that ends with an ellipsis,
/// whose last character the ellipsis is.</param>
internal readonly record struct UnicodeTextLineInfo(
	int Start,
	int End,
	float X0,
	float AlignmentOffset,
	float Width,
	float WidthIncludingTrailingWhitespace,
	float Top,
	float Height,
	float Baseline,
	bool RightToLeft,
	int NewlineLength,
	int TrailingWhitespaceLength,
	bool HasEllipsis,
	bool IsEndingLine);

/// <summary>A visually contiguous span of clusters of one direction.</summary>
internal readonly record struct UnicodeTextSpan(float Left, float Right, bool RightToLeft);

// Reads one line at a time, for callers that position each line themselves. Lines are numbered as in GetVisualLine:
// the engine lines, then the empty ending line after a trailing line break when the layout has one.
internal readonly partial struct UnicodeText
{
	internal UnicodeTextLineInfo GetLineInfo(int lineIndex)
	{
		if ((uint)lineIndex >= (uint)VisualLineCount)
		{
			throw new ArgumentOutOfRangeException(nameof(lineIndex));
		}

		if (lineIndex < _lines.Count)
		{
			var line = _lines[lineIndex];
			var alignmentOffset = GetAlignmentOffsetForLine(line);
			var rtl = IsLineRightToLeft(line);
			return new UnicodeTextLineInfo(
				line.start,
				line.end,
				alignmentOffset + (rtl ? line.width - line.widthWithoutTrailingSpaces : 0),
				alignmentOffset,
				line.widthWithoutTrailingSpaces,
				line.width,
				GetLineContentTop(lineIndex),
				line.lineHeight,
				line.baselineOffset,
				rtl,
				TrailingCRLFInLine(line),
				line.hasEllipsis ? 0 : TrailingWhitespaceLength(_text, line with { end = line.end - TrailingCRLFInLine(line) }),
				line.hasEllipsis,
				false);
		}

		var start = _lines.Count == 0 ? 0 : _lines[^1].end;
		var endingAlignmentOffset = GetAlignmentOffsetForLine(null);
		return new UnicodeTextLineInfo(
			start,
			_text.Length,
			endingAlignmentOffset,
			endingAlignmentOffset,
			0,
			0,
			_endingLineContentTop,
			_endingNewLineLineHeight ?? _defaultFontDetails.LineHeight,
			_endingLineBaselineOffset,
			IsLineRightToLeft(null),
			0,
			0,
			false,
			true);
	}

	/// <summary>
	/// The character hit at <paramref name="x"/> on a line: the cluster under it, with its length as the trailing length
	/// when <paramref name="x"/> is on the cluster's trailing half. Beside the line, the line's start or the end of its
	/// content, by the line's direction.
	/// </summary>
	internal (int index, int trailingLength) HitTestLine(int lineIndex, float x)
	{
		var info = GetLineInfo(lineIndex);
		if (info.IsEndingLine)
		{
			return (info.Start, 0);
		}

		var line = _lines[lineIndex];
		var contentEnd = line.end - info.NewlineLength;
		var beforeLine = x < info.AlignmentOffset;
		if (beforeLine || x >= info.AlignmentOffset + info.WidthIncludingTrailingWhitespace)
		{
			return beforeLine != info.RightToLeft ? (line.start, 0) : GetLineEndHit(line.start, contentEnd);
		}

		var prefixSummedWidths = _xyTable[lineIndex].prefixSummedWidths;
		var localX = x - info.AlignmentOffset;
		var clusterIndex = prefixSummedWidths.BinarySearch(
			(localX, null!),
			Comparer<(float sumUntilAfterCluster, LinkedListNode<Cluster> cluster)>.Create(
				static (a, b) => a.sumUntilAfterCluster.CompareTo(b.sumUntilAfterCluster)));
		clusterIndex = clusterIndex < 0 ? Math.Min(~clusterIndex, prefixSummedWidths.Count - 1) : Math.Min(clusterIndex + 1, prefixSummedWidths.Count - 1);

		var cluster = prefixSummedWidths[clusterIndex].cluster.Value;
		if (cluster.start >= contentEnd)
		{
			// The line break, which has no caret stop of its own.
			return GetLineEndHit(line.start, contentEnd);
		}

		var right = prefixSummedWidths[clusterIndex].sumUntilAfterCluster;
		var left = right - cluster.width;
		var onLeftHalf = localX - left < right - localX;
		return onLeftHalf != cluster.rtl ? (cluster.start, 0) : (cluster.start, cluster.end - cluster.start);
	}

	private (int index, int trailingLength) GetLineEndHit(int lineStart, int contentEnd)
	{
		if (contentEnd <= lineStart)
		{
			return (lineStart, 0);
		}

		var last = GetClusterAt(contentEnd - 1).Value;
		return (last.start, contentEnd - last.start);
	}

	/// <summary>
	/// The x of a caret before the character at <paramref name="index"/> on a line, on the side the text flows from. At
	/// or past the end of the line's content, the trailing edge of its last character.
	/// </summary>
	internal float GetCaretX(int lineIndex, int index)
	{
		var info = GetLineInfo(lineIndex);
		if (info.IsEndingLine)
		{
			return info.AlignmentOffset;
		}

		var line = _lines[lineIndex];
		var contentEnd = line.end - info.NewlineLength;
		if (contentEnd <= line.start)
		{
			return info.RightToLeft ? info.AlignmentOffset + info.WidthIncludingTrailingWhitespace : info.AlignmentOffset;
		}

		var atEnd = index >= contentEnd;
		var node = GetClusterAt(Math.Clamp(atEnd ? contentEnd - 1 : index, line.start, contentEnd - 1));
		var cluster = node.Value;
		var left = GetClusterLeft(lineIndex, cluster);
		var clusterLength = Math.Max(1, cluster.end - cluster.start);
		var fraction = atEnd ? 1 : (float)Math.Clamp(index - cluster.start, 0, clusterLength) / clusterLength;
		return cluster.rtl ? left + cluster.width * (1 - fraction) : left + cluster.width * fraction;
	}

	/// <summary>The cluster around <paramref name="index"/>: caret stops never fall inside it.</summary>
	internal (int start, int end) GetCaretStop(int index)
	{
		if (_text.Length == 0 || index >= _text.Length)
		{
			return (_text.Length, _text.Length);
		}

		var cluster = GetClusterAt(Math.Max(0, index)).Value;
		return (cluster.start, cluster.end);
	}

	/// <summary>
	/// The spans a range covers on a line, in visual order. Each cluster the range touches is covered whole, and adjacent
	/// clusters of one direction merge.
	/// </summary>
	internal List<UnicodeTextSpan> GetRangeBoundsInLine(int lineIndex, int start, int end)
	{
		var spans = new List<UnicodeTextSpan>();
		if (lineIndex >= _lines.Count || end <= start)
		{
			return spans;
		}

		var alignmentOffset = GetAlignmentOffsetForLine(_lines[lineIndex]);
		var x = alignmentOffset;
		foreach (var (sumUntilAfterCluster, node) in _xyTable[lineIndex].prefixSummedWidths)
		{
			var cluster = node.Value;
			var right = alignmentOffset + sumUntilAfterCluster;
			if (cluster.start < end && cluster.end > start)
			{
				if (spans.Count > 0 && spans[^1] is { } previous && previous.RightToLeft == cluster.rtl && previous.Right == x)
				{
					spans[^1] = previous with { Right = right };
				}
				else
				{
					spans.Add(new UnicodeTextSpan(x, right, cluster.rtl));
				}
			}
			x = right;
		}

		return spans;
	}

	/// <summary>The inline objects on a line, with their bounds.</summary>
	internal List<(int index, Rect bounds)> GetInlineObjectsInLine(int lineIndex)
	{
		var objects = new List<(int index, Rect bounds)>();
		if (_inlineObjects is null || lineIndex >= _lines.Count)
		{
			return objects;
		}

		var line = _lines[lineIndex];
		var top = GetLineContentTop(lineIndex);
		var alignmentOffset = GetAlignmentOffsetForLine(line);
		var x = alignmentOffset;
		foreach (var (sumUntilAfterCluster, node) in _xyTable[lineIndex].prefixSummedWidths)
		{
			if (GetInlineObject(node.Value, _inlineObjects) is { } inlineObject)
			{
				objects.Add((node.Value.start, new Rect(x, top + GetInlineObjectTop(inlineObject, line.lineHeight, line.baselineOffset), inlineObject.Width, inlineObject.Height)));
			}
			x = alignmentOffset + sumUntilAfterCluster;
		}

		return objects;
	}

	private float GetClusterLeft(int lineIndex, in Cluster cluster)
		=> GetAlignmentOffsetForLine(_lines[lineIndex])
			+ (cluster.indexInLine == 0 ? 0 : _xyTable[lineIndex].prefixSummedWidths[cluster.indexInLine - 1].sumUntilAfterCluster);
}
