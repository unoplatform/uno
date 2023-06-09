using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#nullable enable

namespace Windows.UI.Xaml.Documents.TextFormatting
{
	/// <summary>
	/// Represents the set of segment spans that make up a rendered text line.
	/// </summary>
	internal class RenderLine
	{
		private readonly List<RenderSegmentSpan> _segmentSpans;
		private List<RenderSegmentSpan>? _renderOrderedSegmentSpans;

		/// <summary>
		/// Gets the segment spans in the order they appear within the text string.
		/// </summary>
		public IReadOnlyList<RenderSegmentSpan> SegmentSpans => _segmentSpans;

		/// <summary>
		/// Gets the segment spans in the order they should be rendered (from left to right).
		/// </summary>
		public IReadOnlyList<RenderSegmentSpan> RenderOrderedSegmentSpans => _renderOrderedSegmentSpans ??= GetRenderOrderedSegmentSpans();

		public float Width { get; }

		public float WidthWithoutTrailingSpaces { get; }

		public float Height { get; }

		public float BaselineOffsetY { get; }

		public bool Wraps { get; }

		public RenderLine(List<RenderSegmentSpan> spans, LineStackingStrategy lineStackingStrategy, float lineHeight, bool firstLine, bool wraps)
		{
			_segmentSpans = new(spans);

			Width = 0;

			int lastIndex = _segmentSpans.Count - 1;

			for (int i = 0; i < lastIndex; i++)
			{
				Width += _segmentSpans[i].Width;
			}

			WidthWithoutTrailingSpaces = Width;

			WidthWithoutTrailingSpaces += _segmentSpans[lastIndex].WidthWithoutTrailingSpaces;
			Width += _segmentSpans[lastIndex].Width;

			var maxStackHeight = float.MinValue;
			var maxAboveBaselineHeight = float.MinValue;
			var maxBelowBaselineHeight = float.MinValue;

			for (var i = 0; i < _segmentSpans.Count; i++)
			{
				var inline = _segmentSpans[i].Segment.Inline;

				maxStackHeight = Math.Max(maxStackHeight, inline.LineHeight);
				maxAboveBaselineHeight = Math.Max(maxAboveBaselineHeight, inline.AboveBaselineHeight);
				maxBelowBaselineHeight = Math.Max(maxBelowBaselineHeight, inline.BelowBaselineHeight);
			}

			switch (lineStackingStrategy)
			{
				case LineStackingStrategy.MaxHeight:
					if (lineHeight == 0)
					{
						Height = maxStackHeight;
						BaselineOffsetY = -maxBelowBaselineHeight;
					}
					else
					{
						if (lineHeight < maxStackHeight)
						{
							Height = maxStackHeight;
							BaselineOffsetY = -maxBelowBaselineHeight;

						}
						else
						{
							Height = lineHeight;
							BaselineOffsetY = GetBaselineOffsetY(lineHeight, maxStackHeight, maxBelowBaselineHeight);
						}
					}

					break;

				case LineStackingStrategy.BlockLineHeight:
					Height = lineHeight;
					BaselineOffsetY = GetBaselineOffsetY(lineHeight, maxStackHeight, maxBelowBaselineHeight);
					break;

				default: // LineStackingStrategy.BaselineToBaseline:
					if (firstLine)
					{
						Height = maxAboveBaselineHeight;
						BaselineOffsetY = 0;
					}
					else
					{
						Height = lineHeight;
						BaselineOffsetY = GetBaselineOffsetY(lineHeight, maxStackHeight, maxBelowBaselineHeight);
					}

					break;
			}

			Wraps = wraps;
		}

		internal (float LineOffset, float JustifySpaceOffset) GetOffsets(float availableWidth, TextAlignment alignment)
		{
			switch (alignment)
			{
				case TextAlignment.Left:
					return (GetLeftCharacterSpacingOffset(), 0);

				case TextAlignment.Right:
					return (GetRightCharacterSpacingOffset() + availableWidth - Width, 0);

				case TextAlignment.Center:
					float charSpacingOffset = (GetLeftCharacterSpacingOffset() + GetRightCharacterSpacingOffset());
					return ((charSpacingOffset + availableWidth - WidthWithoutTrailingSpaces) / 2, 0);

				default: // TextAlignment.Justify
					float justifySpaceOffset = 0;

					if (Wraps && availableWidth > WidthWithoutTrailingSpaces)
					{
						justifySpaceOffset = (availableWidth - WidthWithoutTrailingSpaces) / CountJustifySpaces();
					}

					return (GetLeftCharacterSpacingOffset(), justifySpaceOffset);
			}

			int CountJustifySpaces()
			{
				int justifySpaceCount = 0;

				for (int i = 0; i < _segmentSpans.Count - 1; i++)
				{
					justifySpaceCount += _segmentSpans[i].TrailingSpaces;
				}

				return justifySpaceCount;
			}

			// These methods compensate for trailing or leading character spacing on lines so the text aligns with the edge of the layout:

			float GetLeftCharacterSpacingOffset()
			{
				for (int i = 0; i < _segmentSpans.Count; i++)
				{
					var span = _segmentSpans[i];

					if (span.Width > 0)
					{
						return span.Segment.Direction == FlowDirection.RightToLeft ? -span.CharacterSpacing : 0;
					}
				}

				return 0;
			}

			float GetRightCharacterSpacingOffset()
			{
				for (int i = _segmentSpans.Count - 1; i >= 0; i--)
				{
					var span = _segmentSpans[i];

					if (span.Width > 0)
					{
						return span.Segment.Direction == FlowDirection.LeftToRight ? span.CharacterSpacing : 0;
					}
				}

				return 0;
			}
		}

		private List<RenderSegmentSpan> GetRenderOrderedSegmentSpans()
		{
			List<RenderSegmentSpan>? orderedSpans = null;
			int i = 0;

			while (i < _segmentSpans.Count - 1)
			{
				var span = _segmentSpans[i];

				if (span.Segment.Direction == FlowDirection.RightToLeft)
				{
					int rtlEndIndex = i + 1; // Exclusive end index of the RTL span run

					// Check if next span is also RTL before allocating/copying an ordered list,
					// since reversing the order of a single RTL span run is a no-op.

					if (_segmentSpans[rtlEndIndex].Segment.Direction == FlowDirection.RightToLeft)
					{
						orderedSpans ??= new(_segmentSpans);

						while (++rtlEndIndex < _segmentSpans.Count && _segmentSpans[rtlEndIndex].Segment.Direction == FlowDirection.RightToLeft) { }

						orderedSpans.Reverse(i, rtlEndIndex - i);
					}

					i = rtlEndIndex + 1; // Can skip next element since we know it is LTR
				}
				else
				{
					i++;
				}
			}

			return orderedSpans ?? _segmentSpans;
		}

		/// <summary>
		/// Gets the offset of the baseline for this render line based on a custom line height. Scales the default baseline offset
		/// by the ratio of the default line height to the custom line height.
		/// </summary>
		private float GetBaselineOffsetY(float lineHeight, float maxStackHeight, float maxBelowBaselineHeight)
		{
			if (maxStackHeight == 0)
			{
				return 0;
			}

			var defaultBaselineOffsetY = -maxBelowBaselineHeight;
			return defaultBaselineOffsetY * lineHeight / maxStackHeight;
		}
	}
}
