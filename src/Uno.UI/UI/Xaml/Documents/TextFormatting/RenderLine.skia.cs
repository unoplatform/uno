using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Windows.UI.Xaml.Documents.TextFormatting
{
	/// <summary>
	/// Represents the set of segment spans that make up a rendered text line.
	/// </summary>
	internal readonly struct RenderLine
	{
		private readonly List<RenderSegmentSpan> _segmentSpans;

		public IReadOnlyList<RenderSegmentSpan> SegmentSpans => _segmentSpans;

		public float Width { get; }

		public float WidthWithoutTrailingSpaces { get; }

		public RenderLine(IEnumerable<RenderSegmentSpan> spans)
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
		}

		public double GetMaxStackHeight() => _segmentSpans.Max(s => s.Segment.Inline.LineHeight);

		public double GetMaxAboveBaselineHeight() => _segmentSpans.Max(s => s.Segment.Inline.AboveBaselineHeight);

		public double GetMaxBelowBaselineHeight() => _segmentSpans.Max(s => s.Segment.Inline.BelowBaselineHeight);
	}
}
