#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls.Text.Core;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Documents.BlockLayout;
using Microsoft.UI.Xaml.Documents.RichTextServices;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Microsoft.UI.Xaml.Media;
using Uno.Foundation.Logging;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Paints the lines a RichTextBlock's or RichTextBlockOverflow's page node recorded when it was drawn.
/// </summary>
/// <remarks>
/// The page lays out in the element's flow space, which the element's composition flip mirrors for RightToLeft, as in
/// WinUI. The engine draws physically, so the painter cancels that mirror and puts each line where its flow box lands.
/// </remarks>
internal static class RichTextPagePainter
{
	// Painting runs on every invalidation: the line buffer is reused rather than allocated per paint (UI thread only).
	private static readonly List<UnicodeText.LineDraw> _lineDraws = new();
	private static readonly TextHighlighter[] _noHighlighters = [];
	private static bool _mirrorMismatchLogged;

#if !DEBUG
	// What each element's mirror was last checked against: in Release the ancestor walk runs only when that changes.
	private static readonly ConditionalWeakTable<FrameworkElement, StrongBox<(bool Mirrored, Size Size, Vector3 Offset)>> _mirrorChecks = new();
#endif

	public static void Paint(FrameworkElement owner, PageNode? page, RichTextBlock master, in Visual.PaintingSession session)
	{
		if (page is null || page.IsMeasureDirty() || page.IsArrangeDirty())
		{
			return;
		}

		// The layout's direction: the master's, also for an overflow, which the layout engine shares.
		var mirror = TextFlowMirror.ForLayout(owner, BlockLayoutHelpers.GetFlowDirection(master));
		CheckCompositionMirrors(owner, mirror);

		var drawingSession = session.Session;
		mirror.PushCounterMirror(drawingSession);
		try
		{
			var lineDraws = _lineDraws;
			var paintHighlights = master.HasParagraphHighlights;
			var blockIndex = (int)page.GetFirstChildIndex();
			var paragraphStart = paintHighlights ? GetParagraphStart(master, blockIndex) : 0;

			// The children's offsets in the page, as BlockNode.TransformOffsetToRoot gives them, summed as they come
			// rather than asked per line, which walks the children before each one.
			var pageOffset = page.GetContentRenderingOffset();
			var childTop = 0.0;

			for (var child = page.GetFirstChild(); child is not null; child = child.GetNext(), blockIndex++)
			{
				var paragraph = child is ParagraphNode paragraphNode ? paragraphNode.GetParagraph() : null;
				var paragraphLength = paintHighlights && paragraph is not null ? RichTextBlock.GetParagraphTextLength(paragraph) : 0;
				var contentOffset = child.GetContentRenderingOffset();
				var nodeOrigin = new Point(pageOffset.X + contentOffset.X, pageOffset.Y + childTop + contentOffset.Y);
				Debug.Assert(Math.Abs(nodeOrigin.X - child.TransformOffsetToRoot(default).X) + Math.Abs(nodeOrigin.Y - child.TransformOffsetToRoot(default).Y) < 0.01, "A page's children are stacked in order.");
				childTop += child.GetRenderSize().Height;

				if (child is ParagraphNode node
					&& node.GetDrawingContext() is ParagraphDrawingContext drawingContext
					&& drawingContext.GetTextDrawingContext().Lines is { Count: > 0 } records)
				{
					var highlighters = paintHighlights
						? master.GetParagraphHighlighters(paragraphStart, paragraphLength, records[0].Line.Layout.Base)
						: _noHighlighters;

					// One call per layout: a collapsed line comes from a trimmed twin of the paragraph's layout.
					for (var first = 0; first < records.Count;)
					{
						var layout = records[first].Line.Layout;
						if (!layout.UpdateForegrounds(master.ForegroundVersion))
						{
							// Its text changed before the layout pass that remakes it: lay it out again.
							master.InvalidateContent();
						}
						lineDraws.Clear();
						var next = first;
						for (; next < records.Count && ReferenceEquals(records[next].Line.Layout, layout); next++)
						{
							lineDraws.Add(GetLineDraw(nodeOrigin, records[next], mirror));
						}

						layout.Text.DrawLines(owner, session, lineDraws, caret: null, highlighters, compositionRange: null, suppressBackplate: false, foregroundOverride: null);
						first = next;
					}
				}

				paragraphStart += paragraphLength + RichTextBlockView.InterParagraphSeparatorLength;
			}
		}
		finally
		{
			_lineDraws.Clear();
			drawingSession.Restore();
		}
	}

	// The line's flow box [x, x + width] in the page lands at its mirror; the engine draws the line's ink from x0.
	private static UnicodeText.LineDraw GetLineDraw(Point nodeOrigin, in RecordedTextDrawingContext.LineRecord record, TextFlowMirror mirror)
	{
		var info = record.Line.Info;
		var origin = new Point(nodeOrigin.X + record.Origin.X, nodeOrigin.Y + record.Origin.Y);
		var bandTop = nodeOrigin.Y + record.BandTop;
		var (left, _) = mirror.MapSpan(origin.X, info.Width);

		return new UnicodeText.LineDraw(
			record.Line.EngineLine,
			new Vector2((float)(left - info.X0), (float)(origin.Y - info.Top)),
			(float)bandTop,
			(float)record.BandHeight);
	}

	// The flat index of a block's first character: each paragraph before it, then its "\r\n".
	private static int GetParagraphStart(RichTextBlock master, int blockIndex)
	{
		var start = 0;
		var blocks = master.Blocks;
		for (var i = 0; i < blockIndex && i < blocks.Count; i++)
		{
			if (blocks[i] is Paragraph paragraph)
			{
				start += RichTextBlock.GetParagraphTextLength(paragraph);
			}

			start += RichTextBlockView.InterParagraphSeparatorLength;
		}

		return start;
	}

	// The element's local space must be mirrored exactly when its layout's direction is RightToLeft: that is what the
	// counter-mirror cancels, and what hit-tests and hosted children rely on. Otherwise the glyphs draw mirrored.
	private static void CheckCompositionMirrors(FrameworkElement owner, TextFlowMirror mirror)
	{
#if SUPPORTS_RTL
		if (!owner.IsInLiveTree)
		{
			return;
		}
#if !DEBUG
		// The direction or the arrangement changes the flip; a paint that changes neither can't break it.
		var state = (mirror.IsMirrored, owner.RenderSize, owner.ActualOffset);
		if (_mirrorMismatchLogged)
		{
			return;
		}

		var last = _mirrorChecks.GetOrCreateValue(owner);
		if (last.Value == state)
		{
			return;
		}

		last.Value = state;
#endif

		var mirrored = false;
		for (var element = (UIElement?)owner; element is not null; element = VisualTreeHelper.GetParent(element) as UIElement)
		{
			mirrored ^= element.GetFlowDirectionTransform().M11 < 0;
		}

		if (mirrored != mirror.IsMirrored)
		{
			Debug.Assert(false, $"{owner.GetType().Name}'s local space should be mirrored exactly when its layout is RightToLeft.");
			if (!_mirrorMismatchLogged && typeof(RichTextPagePainter).Log().IsEnabled(LogLevel.Warning))
			{
				typeof(RichTextPagePainter).Log().Warn(
					$"{owner.GetType().Name}'s local space is {(mirrored ? "" : "not ")}mirrored while its layout is {(mirror.IsMirrored ? "RightToLeft" : "LeftToRight")}: its text is drawn mirrored.");
			}

			_mirrorMismatchLogged = true;
		}
#endif
	}
}
