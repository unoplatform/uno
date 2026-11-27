#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Documents.RichTextServices;

/// <summary>
/// One line of a <see cref="UnicodeText"/> layout, as a WinUI <see cref="TextLine"/>.
/// </summary>
/// <remarks>
/// <para>
/// The engine lays out in physical space; a <see cref="TextLine"/> reports distances from the start of the line in the
/// paragraph's flow direction, which is mirrored for RightToLeft, as WinUI's line services do. Within the line, a point
/// at distance u from the line's flow start is at <c>x0 + MapX(u)</c> in the engine, x0 being the left of its ink and
/// the mirror spanning the line's width without trailing whitespace. That flip already follows the reading order, so
/// every line reports <see cref="TextLine.AlignmentFollowsReadingOrder"/>, and ParagraphNode's own placement is WinUI's.
/// </para>
/// <para>
/// Positions are node indices: an object takes none (see <see cref="UnicodeParagraphLayout"/>).
/// </para>
/// </remarks>
internal sealed class UnicodeTextLine : TextLine
{
	private readonly UnicodeParagraphLayout _layout;
	private readonly int _engineLine;
	private readonly UnicodeTextLineInfo _info;
	private readonly TextSource _textSource;
	private readonly uint _nodeStart;
	private readonly bool _collapsed;

	// Made by UnicodeParagraphLayout.GetLine, which keeps the lines with their layout.
	internal UnicodeTextLine(TextSource textSource, UnicodeParagraphLayout layout, int engineLine, UnicodeTextLine? collapsedFrom)
	{
		_textSource = textSource;
		_layout = layout;
		_engineLine = engineLine;
		_info = layout.Text.GetLineInfo(engineLine);
		_collapsed = collapsedFrom is not null;

		m_width = _info.Width;
		m_widthIncludingTrailingWhitespace = _info.WidthIncludingTrailingWhitespace;
		m_height = _info.Height;
		m_textHeight = _info.Height;
		m_baseline = _info.Baseline;
		m_textBaseline = _info.Baseline;
		m_start = 0;
		m_alignmentFollowsReadingOrder = true;

		if (collapsedFrom is not null)
		{
			// The ellipsis may run to the text's end, so the positions are the line's own.
			_nodeStart = collapsedFrom._nodeStart;
			m_length = collapsedFrom.m_length;
			m_newlineLength = collapsedFrom.m_newlineLength;
			m_trailingWhitespaceLength = 0;
			m_pTextLineBreak = collapsedFrom.m_pTextLineBreak;
			return;
		}

		var lineStart = layout.ToNode(_info.Start);
		var length = layout.ToNode(_info.End) - lineStart;
		if (engineLine == 0 && layout.NodeTextIsEmpty)
		{
			// WinUI's paragraph terminator: a paragraph without text still formats a line of one position, or
			// PageNode drops it and every paragraph after it.
			length = Math.Max(length, 1);
		}

		_nodeStart = (uint)lineStart;
		m_length = (uint)length;
		m_newlineLength = (uint)_info.NewlineLength;
		m_trailingWhitespaceLength = (uint)_info.TrailingWhitespaceLength;

		if (engineLine + 1 < layout.Text.VisualLineCount)
		{
			m_pTextLineBreak = new UnicodeTextLineBreak(textSource, layout, layout.LayoutKey.WrappingWidth, engineLine + 1, layout.Text.GetLineInfo(engineLine + 1).Start);
		}
	}

	/// <summary>The layout this line is drawn from: a twin of the paragraph's layout when collapsed.</summary>
	internal UnicodeParagraphLayout Layout => _layout;

	internal int EngineLine => _engineLine;

	internal UnicodeTextLineInfo Info => _info;

	// The paragraph (on one page) this line was formatted for.
	internal TextSource TextSource => _textSource;

	private FlowDirection Direction => _layout.LayoutKey.FlowDirection;

	// Between the engine's ink frame (x - x0) and the line's flow distance; it is its own inverse.
	private TextFlowMirror LineMirror => new(_info.Width, Direction == FlowDirection.RightToLeft);

	private int NodeEnd => (int)(_nodeStart + m_length);

	// Adds the containers of the objects on this line.
	internal void CollectInlineObjects(HashSet<InlineUIContainer> containers)
	{
		if (!_layout.HasObjects)
		{
			return;
		}

		foreach (var (index, _) in _layout.Text.GetInlineObjectsInLine(_engineLine))
		{
			if (_layout.GetContainer(index) is { } container)
			{
				containers.Add(container);
			}
		}
	}

	// Places the embedded objects of the line; bounds is the line box in the paragraph's flow space.
	public override void Arrange(Rect bounds)
	{
		if (!_layout.HasObjects)
		{
			return;
		}

		foreach (var (index, rect) in _layout.Text.GetInlineObjectsInLine(_engineLine))
		{
			if (_layout.GetObjectRun(index) is { } objectRun)
			{
				var (left, _) = LineMirror.MapSpan(rect.X - _info.X0, rect.Width);
				objectRun.Arrange(new Point(bounds.X + left, bounds.Y + rect.Y - _info.Top));
			}
		}
	}

	// The line is drawn by the engine when the page paints: record where ParagraphNode put it.
	public override void Draw(TextDrawingContext drawingContext, Point origin, double viewportWidth)
		=> ((RecordedTextDrawingContext)drawingContext).Record(this, origin);

	public override TextLine Collapse(double collapsingWidth, TextTrimming collapsingStyle, TextCollapsingSymbol? collapsingSymbol)
	{
		if (_collapsed || collapsingStyle is not (TextTrimming.CharacterEllipsis or TextTrimming.WordEllipsis) || _info.IsEndingLine)
		{
			return this;
		}

		// A line that fits is collapsed for the paragraph ellipsis: the text after it isn't shown.
		var forced = Width <= collapsingWidth;
		var twin = _layout.GetTrimmed(_engineLine, collapsingWidth, collapsingStyle, forced);
		if (_engineLine >= twin.Text.VisualLineCount || !twin.Text.GetLineInfo(_engineLine).HasEllipsis)
		{
			// Nothing to trim: a single cluster is never collapsed.
			return this;
		}

		return twin.GetLine(_textSource, _engineLine, collapsedFrom: this);
	}

	public override bool HasCollapsed => _collapsed;

	// Clusters can span several code units; caret navigation reads them from the engine.
	public override bool HasMultiCharacterClusters => true;

	public override CharacterHit GetCharacterHitFromDistance(double distance)
	{
		if (_info.IsEndingLine)
		{
			return new CharacterHit((int)_nodeStart, 0);
		}

		var x = _info.X0 + LineMirror.MapX(distance);
		var (index, trailingLength) = _layout.Text.HitTestLine(_engineLine, (float)x);

		// The caret position maps as one value, so a hit on an object's far half lands after the object, not on the
		// character after it.
		var caret = Math.Clamp(_layout.ToNode(index + trailingLength), (int)_nodeStart, NodeEnd);
		var first = Math.Clamp(_layout.ToNode(index), (int)_nodeStart, caret);
		return trailingLength == 0 || caret == first
			? new CharacterHit(caret, 0)
			: new CharacterHit(first, caret - first);
	}

	public override double GetDistanceFromCharacterHit(CharacterHit characterHit)
	{
		// A leading edge belongs to the character at the position, after any object there; a trailing edge to the one
		// before it.
		var position = characterHit.FirstCharacterIndex + characterHit.TrailingLength;
		var engineIndex = characterHit.TrailingLength == 0 ? _layout.ToEngineEnd(position) : _layout.ToEngineStart(position);
		var caretX = _layout.Text.GetCaretX(_engineLine, engineIndex);
		return LineMirror.MapX(caretX - _info.X0);
	}

	// Returns the leading edge of the nearest preceding cluster, or characterHit when there is none.
	public override CharacterHit GetPreviousCaretCharacterHit(CharacterHit characterHit)
	{
		var position = characterHit.FirstCharacterIndex;
		var stop = _layout.ToNode(_layout.Text.GetCaretStop(_layout.ToEngineEnd(position)).start);

		// From a leading edge at a caret stop, move to the previous cluster with a position of its own.
		if (characterHit.TrailingLength == 0 && stop == position)
		{
			if (stop <= _nodeStart)
			{
				return characterHit;
			}

			// Objects have no position of their own, so step over them.
			var engineIndex = _layout.ToEngineStart(position);
			do
			{
				engineIndex = _layout.Text.GetCaretStop(engineIndex - 1).start;
				stop = _layout.ToNode(engineIndex);
			}
			while (stop == position && engineIndex > 0);
		}

		return new CharacterHit(Math.Max(stop, (int)_nodeStart), 0);
	}

	// Returns the trailing edge of the nearest following cluster, or characterHit when there is none.
	public override CharacterHit GetNextCaretCharacterHit(CharacterHit characterHit)
	{
		var (start, end) = GetNodeCaretStop(characterHit.FirstCharacterIndex);

		// From the trailing edge of a caret stop, move to the trailing edge of the next one.
		if (characterHit.TrailingLength != 0 && characterHit.FirstCharacterIndex + characterHit.TrailingLength == end)
		{
			(start, end) = GetNodeCaretStop(end);
		}

		if (start >= NodeEnd || end <= start)
		{
			return characterHit;
		}

		return new CharacterHit(start, end - start);
	}

	// The cluster at a node position, skipping objects, which have no position.
	private (int start, int end) GetNodeCaretStop(int position)
	{
		var (start, end) = _layout.Text.GetCaretStop(_layout.ToEngineEnd(position));
		return (_layout.ToNode(start), _layout.ToNode(end));
	}

	public override TextBounds[] GetTextBounds(int firstCharacterIndex, int textLength)
	{
		var direction = Direction;
		var start = Math.Clamp(firstCharacterIndex, (int)_nodeStart, NodeEnd);
		var end = Math.Clamp(firstCharacterIndex + textLength, start, NodeEnd);

		if (end <= start)
		{
			return Array.Empty<TextBounds>();
		}

		// The range covers characters only: an object at either edge is left out, one inside is covered.
		var spans = _info.IsEndingLine
			? new List<UnicodeTextSpan>()
			: _layout.Text.GetRangeBoundsInLine(_engineLine, _layout.ToEngineEnd(start), _layout.ToEngineStart(end));

		if (spans.Count == 0)
		{
			// A position without ink (a paragraph without text, a line break): an empty box at its caret.
			var caret = _info.IsEndingLine ? 0 : LineMirror.MapX(_layout.Text.GetCaretX(_engineLine, _layout.ToEngineEnd(start)) - _info.X0);
			return new[] { new TextBounds(new Rect(caret, 0, 0, m_height), direction) };
		}

		var bounds = new TextBounds[spans.Count];
		for (var i = 0; i < spans.Count; i++)
		{
			var span = spans[i];
			var (left, width) = LineMirror.MapSpan(span.Left - _info.X0, span.Right - span.Left);
			bounds[i] = new TextBounds(new Rect(left, 0, width, m_height), span.RightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight);
		}

		return bounds;
	}

	public override FlowDirection GetDetectedParagraphDirection()
		=> _info.RightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

	public override FlowDirection GetParagraphDirection() => Direction;
}
