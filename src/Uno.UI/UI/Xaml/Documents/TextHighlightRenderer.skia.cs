// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TextHighlightRenderer.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using System.Collections.Generic;
using Uno.UI;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls.Text.Core;
using Microsoft.UI.Xaml.Media;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Documents;

internal static class TextHighlightRenderer
{
	// HWRenderCollection handles rendering the background implicitly.  It could almost
	// do the same for foreground except that there are two very similar but different methods for adding the
	// foreground highlight rects to either D2DTextDrawingContext or DrawingContext.  A callback is provided
	// instead to give the caller the opportunity to draw the foreground.
	// This offers the best of some bad choices.  It could handle itself internally but it would have to overload
	// and provide a lot of different functions.  It could attempt to do background rendering as well, but that has
	// two different methods.
	public delegate void ForegroundRenderingCallback(
		SolidColorBrush? foregroundBrush,
		uint highlightRectCount,
		Rect[] highlightRects);

	private delegate void IterateHighlighterCallback(
		SolidColorBrush? foregroundBrush,
		SolidColorBrush? backgroundBrush,
		uint highlightRectCount,
		Rect[] highlightRects);

	private static void GetDefaultHighlighterBrushes(
		out SolidColorBrush? foregroundBrush,
		out SolidColorBrush? backgroundBrush)
	{
		// C++ resolves these via CCoreServices::LookupThemeResource and SimulateFreeze()s them.
		// On Uno the theme-resource lookup flows through ResourceResolver.
		foregroundBrush = ResourceResolver.ResolveResourceStatic<SolidColorBrush>("TextControlHighlighterForeground");
		MUX_ASSERT(foregroundBrush is not null);

		backgroundBrush = ResourceResolver.ResolveResourceStatic<SolidColorBrush>("TextControlHighlighterBackground");
		MUX_ASSERT(backgroundBrush is not null);
	}

	private static void IterateMergedHighlighters(
		TextHighlighterCollection? textHighlighters,
		List<HighlightRegion> textSelections,
		ITextView textView,
		IterateHighlighterCallback callback)
	{
		var textLength = (int)textView.GetContentLength();

		// Merge all the highlighters so that there is no overlap in ranges and
		// earlier items in the collection get overwritten by later ones.
		TextHighlightMerge merge = new();

		if (textHighlighters is not null)
		{
			GetDefaultHighlighterBrushes(
				out var defaultForegroundBrush,
				out var defaultBackgroundBrush);

			foreach (var textHighlighter in textHighlighters.GetCollection())
			{
				MUX_ASSERT(textHighlighter is not null);

				SolidColorBrush? highlightForegroundBrush;
				if (textHighlighter!.Foreground is not null)
				{
					highlightForegroundBrush = textHighlighter.Foreground as SolidColorBrush;
				}
				else
				{
					highlightForegroundBrush = defaultForegroundBrush;
				}

				SolidColorBrush? highlightBackgroundBrush;
				if (textHighlighter.Background is not null)
				{
					highlightBackgroundBrush = textHighlighter.Background as SolidColorBrush;
				}
				else
				{
					highlightBackgroundBrush = defaultBackgroundBrush;
				}

				MUX_ASSERT(textHighlighter.Ranges is not null);
				foreach (var textRange in textHighlighter.Ranges!)
				{
					// Resolve the highlight rects

					// Adjust for content offsets in each inline and paragraph. This is also accounted for in textLength.
					int startOffset = textView.GetAdjustedPosition(textRange.StartIndex);
					int endOffset = textView.GetAdjustedPosition(textRange.StartIndex + textRange.Length) - 1;
					MUX_ASSERT((endOffset - startOffset) < textLength);

					// Only highlight valid ranges
					if ((startOffset >= 0) &&
						(endOffset >= 0) &&
						(startOffset <= endOffset))
					{
						merge.AddRegion(
							new HighlightRegion(
								startOffset,
								endOffset,
								highlightForegroundBrush,
								highlightBackgroundBrush));
					}
				}
			}
		}

		if (textSelections.Count != 0)
		{
			foreach (HighlightRegion selection in textSelections)
			{
				if (selection.StartIndex >= 0 &&
					selection.EndIndex >= 0 &&
					selection.StartIndex <= selection.EndIndex)
				{
					merge.AddRegion(
						new HighlightRegion(
							selection.StartIndex,
							selection.EndIndex,
							selection.ForegroundBrush,
							selection.BackgroundBrush));
				}
			}
		}

		// Iterate over the merged collection
		// Note that the merge algorithm works with inclusive ranges [Start,End]
		// Whereas TextRangeToTextBounds works on [Start,End), so 1 is incremented
		// to the endIndex to compensate.  Additionally, endOffset was appropriately adjusted
		// based on the length of the range when the regions were added to the merge algorithm.
		foreach (var highlightRegion in merge.Regions)
		{
			// Get the highlight rects
			Rect[] rectangles = textView.TextRangeToTextBounds(
				(uint)highlightRegion.StartIndex,
				(uint)(highlightRegion.EndIndex + 1));

			callback(
				highlightRegion.ForegroundBrush,
				highlightRegion.BackgroundBrush,
				(uint)rectangles.Length,
				rectangles);
		}
	}

	public static void HWRenderCollection(
		TextHighlighterCollection? textHighlighters,
		List<HighlightRegion> textSelections,
		ITextView? textView,
		ForegroundRenderingCallback foregroundRenderingCallback)
	{
		if (((textHighlighters is not null && textHighlighters.GetCount() > 0) ||
			(textSelections.Count != 0)) &&
			(textView is not null))
		{
			IterateMergedHighlighters(
				textHighlighters,
				textSelections,
				textView,
				(foregroundBrush,
				 backgroundBrush,
				 highlightRectCount,
				 highlightRects) =>
			{
				HWHighlightRect(
					backgroundBrush,
					highlightRectCount,
					highlightRects);

				foregroundRenderingCallback(
					foregroundBrush,
					highlightRectCount,
					highlightRects);
			});
		}
	}

	// TODO: Change TextSelectionManager to use HWHighlightRect
	public static void HWHighlightRect(
		SolidColorBrush? highlightBrush,
		uint numHighlightRects,
		Rect[] highlightRects)
	{
		// TODO Uno (Stage 9): the actual rect drawing in WinUI is D2D
		// (CTextBoxHelpers::SnapRectToPixel + IContentRenderer::RenderSolidColorRectangle).
		// On Uno, highlight drawing flows through ParsedText.Draw(highlighters) / RichTextVisual,
		// so the faithful rect/merge computation above is the portable value and the final draw
		// is routed there rather than reimplemented here.
		_ = highlightBrush;
		_ = numHighlightRects;
		_ = highlightRects;
	}
}
