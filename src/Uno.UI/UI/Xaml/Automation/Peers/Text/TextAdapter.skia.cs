// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference textadapter.cpp, TextAdapter.h, tag winui3/release/1.8.2, commit 4a1c6184c

// TODO Uno: unify the DirectUI minimal TextAdapter onto this faithful port (cross-control, separate effort).

#nullable enable

using System;
using System.Collections.Generic;
using Windows.Foundation;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Text.Core;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Documents.BlockLayout;
using Microsoft.UI.Xaml.Documents.RichTextServices;

namespace Microsoft.UI.Xaml.Automation.Peers.Text;

//---------------------------------------------------------------------------
//
//  TextAdapter
//
//  Faithful port of the core CTextAdapter (the ITextProvider implementation for
//  RichTextBlock / RichTextBlockOverflow). WinUI splits this between the core
//  CTextAdapter (the logic ported here) and the projection TextAdapter_partial.cpp
//  (the WinRT marshalling). Uno folds both into this single managed class that
//  implements the public ITextProvider surface directly.
//
//  m_pTextOwner is the owning RichTextBlock / RichTextBlockOverflow. The static
//  helpers type-switch on it exactly as the C++ helpers switch on KnownTypeIndex.
//
//---------------------------------------------------------------------------
internal sealed partial class TextAdapter : ITextProvider
{
	private const double HorizontalAdjuster = 0.75;

	private readonly FrameworkElement _pTextOwner;
	private readonly AutomationPeer _ownerPeer;

	internal TextAdapter(FrameworkElement owner, AutomationPeer ownerPeer)
	{
		_pTextOwner = owner;
		_ownerPeer = ownerPeer;
	}

	internal FrameworkElement Owner => _pTextOwner;

	//---------------------------------------------------------------------------------
	// Gets a text range that encloses the main text of a document.
	//---------------------------------------------------------------------------------
	public ITextRangeProvider DocumentRange => GetDocumentRange()!;

	internal TextRangeAdapter? GetDocumentRange()
	{
		GetContentEndPointers(_pTextOwner, out var startTextPointer, out var endTextPointer);
		if (startTextPointer is not null && endTextPointer is not null)
		{
			return TextRangeAdapter.Create(this, startTextPointer, endTextPointer, _pTextOwner, _ownerPeer);
		}

		return null;
	}

	//-------------------------------------------------------------
	// We only support single Selection here.
	//-------------------------------------------------------------
	public SupportedTextSelection SupportedTextSelection
		=> GetSelectionManager(_pTextOwner) is not null
			? SupportedTextSelection.Single
			: SupportedTextSelection.None;

	//--------------------------------------------------------------------------------------------------------------------------------------------
	// Returns an array of Text ranges of selections. In this case there's only single selection so it returns one range of selected text.
	//--------------------------------------------------------------------------------------------------------------------------------------------
	public ITextRangeProvider[] GetSelection()
	{
		GetSelectionEndPointers(_pTextOwner, out var selectionStartTextPointer, out var selectionEndTextPointer);

		if (selectionStartTextPointer is not null && selectionEndTextPointer is not null)
		{
			var startOffset = selectionStartTextPointer.Offset;
			var endOffset = selectionEndTextPointer.Offset;
			if (endOffset < startOffset)
			{
				(selectionStartTextPointer, selectionEndTextPointer) = (selectionEndTextPointer, selectionStartTextPointer);
			}

			var selectedRange = TextRangeAdapter.Create(this, selectionStartTextPointer, selectionEndTextPointer, _pTextOwner, _ownerPeer);
			if (selectedRange is not null)
			{
				return new ITextRangeProvider[] { selectedRange };
			}
		}

		return Array.Empty<ITextRangeProvider>();
	}

	//--------------------------------------------------------------------------------------------------------------------------------------------
	// Retrieves an array of disjoint text ranges from a text container. Each text range begins with the first partially visible line and ends
	// with the last partially visible line.
	//--------------------------------------------------------------------------------------------------------------------------------------------
	public ITextRangeProvider[] GetVisibleRanges()
		=> GetVisibleRangesInternal().ToArray();

	// Typed accessor used by TextRangeAdapter's line navigation (it reads each range's start/end pointers).
	internal List<TextRangeAdapter> GetVisibleRangesInternal()
	{
		var result = new List<TextRangeAdapter>();

		GetContentEndPointers(_pTextOwner, out var startPointer, out var endPointer);
		if (startPointer is null || endPointer is null)
		{
			return result;
		}

		var startOffset = startPointer.Offset;
		var endOffset = endPointer.Offset;

		var pTextView = GetTextView(_pTextOwner);
		if (pTextView is null)
		{
			return result;
		}

		var rectangles = pTextView.TextRangeToTextBounds((uint)startOffset, (uint)endOffset);
		var hitTestRects = rectangles.Length;

		var container = GetTextContainer(_pTextOwner);

		// Create TextRanges for all visible bounding Rectangles.
		for (int current = 0; current < hitTestRects; current++)
		{
			// We exclude the empty rects, otherwise TextRangeAdapter.ExpandToLine will fail to move forward.
			if (rectangles[current].Width == 0 || rectangles[current].Height == 0)
			{
				continue;
			}

			PlainTextPosition plainTextPosition;
			uint textPosition;
			TextGravity gravity;
			Point pixelCoordinate;
			TextPointer? rangeStart;
			TextPointer? rangeEnd;

			// If Width is less than 1, having a degenerate range to represent the line is the best option,
			// especially since this deals well with LineBreaks.
			if (rectangles[current].Width < 1)
			{
				pixelCoordinate = new Point(
					rectangles[current].X,
					// Sample the middle of the vertical line bound to get the character index.
					rectangles[current].Y + rectangles[current].Height / 2);
				textPosition = pTextView.PixelPositionToTextPosition(pixelCoordinate, true, out gravity);
				startOffset = (int)textPosition;
				plainTextPosition = new PlainTextPosition(container!, textPosition, gravity);
				rangeStart = TextPointer.CreateInstanceWithInternalPointer(plainTextPosition);
				rangeEnd = TextPointer.CreateInstanceWithInternalPointer(plainTextPosition);
			}
			else
			{
				// calculate start pointer for the range
				pixelCoordinate = new Point(
					rectangles[current].X + HorizontalAdjuster,
					rectangles[current].Y + rectangles[current].Height / 2);
				textPosition = pTextView.PixelPositionToTextPosition(pixelCoordinate, true, out gravity);
				startOffset = (int)textPosition;
				plainTextPosition = new PlainTextPosition(container!, textPosition, gravity);
				rangeStart = TextPointer.CreateInstanceWithInternalPointer(plainTextPosition);

				// calculate end pointer for the range
				pixelCoordinate = new Point(
					rectangles[current].X + rectangles[current].Width - HorizontalAdjuster,
					rectangles[current].Y + rectangles[current].Height / 2);
				textPosition = pTextView.PixelPositionToTextPosition(pixelCoordinate, true, out gravity);
				endOffset = (int)textPosition;
				plainTextPosition = new PlainTextPosition(container!, textPosition, gravity);
				rangeEnd = TextPointer.CreateInstanceWithInternalPointer(plainTextPosition);
			}

			// In case the rectangle corresponds to BiDi, endOffset corresponding to point(top, Right) will be less than
			// startOffset corresponding to point (top, left) and needs to be adjusted accordingly.
			if (endOffset < startOffset)
			{
				(rangeStart, rangeEnd) = (rangeEnd, rangeStart);
			}

			if (rangeStart is not null && rangeEnd is not null)
			{
				var textRangeAdapter = TextRangeAdapter.Create(this, rangeStart, rangeEnd, _pTextOwner, _ownerPeer);
				if (textRangeAdapter is not null)
				{
					result.Add(textRangeAdapter);
				}
			}
		}

		return result;
	}

	//------------------------------------------------------------------------------------------------------------------------------
	// Verify the child is actually a child of the Text Owner for the TextAdapter, then find the containing range for the child.
	//------------------------------------------------------------------------------------------------------------------------------
	public ITextRangeProvider RangeFromChild(IRawElementProviderSimple childElement)
	{
		var pAP = childElement?.AutomationPeer;
		if (pAP is null || _pTextOwner is null)
		{
			return DocumentRange;
		}

		foreach (var child in GetOwnerPeerChildren())
		{
			if (ReferenceEquals(pAP, child))
			{
				var iuc = GetParentInlineUIContainer(pAP);
				if (iuc is not null)
				{
					// Using PixelPositionToTextPosition (which is used by RangeFromPoint) on an InlineUIContainer
					// is unreliable, so have the InlineUIContainer find its own position within its parent instead.
					return RangeFromInlineUIContainer(iuc) ?? DocumentRange;
				}
				else if (pAP is HyperlinkAutomationPeer)
				{
					// ClickablePoints for links should be at the start of the link.
					// We can't rely on just the clickable point — we also need the AP, because if the link is in an
					// overflow, RangeFromPoint will assume the point is relative to the master block, not the overflow.
					var clickablePointAP = pAP.GetClickablePoint();
					return RangeFromLink(pAP, clickablePointAP) ?? DocumentRange;
				}
				else
				{
					// Calculate the clickable point on the element with respect to the owner, then do RangeFromPoint.
					var clickablePointAP = pAP.GetClickablePoint();
					return RangeFromPoint(clickablePointAP);
				}
			}
		}

		return DocumentRange;
	}

	// Walks up from the peer's owning DependencyObject to its parent InlineUIContainer, if any.
	private static InlineUIContainer? GetParentInlineUIContainer(AutomationPeer pAP)
	{
		// WinUI walks from pAP->GetDONoRef() up the logical/visual parent chain looking for an
		// InlineUIContainer. The hosted child's peer is a FrameworkElementAutomationPeer over the child
		// UIElement, so we walk the child's parent chain.
		object? obj = (pAP as FrameworkElementAutomationPeer)?.Owner;
		while (obj is not null && obj is not InlineUIContainer)
		{
			obj = obj.GetParent();
		}

		return obj as InlineUIContainer;
	}

	private TextRangeAdapter? RangeFromLink(AutomationPeer pAP, Point point)
	{
		var pTextContainer = GetTextContainer(_pTextOwner);

		TextPointer? start = null;
		TextPointer? end = null;

		// Hyperlink is a TextElement (not a UIElement), so its peer is a HyperlinkAutomationPeer that
		// holds the owning Hyperlink directly (FrameworkElementAutomationPeer.Owner would be a UIElement).
		if (pAP is HyperlinkAutomationPeer hyperlinkPeer && hyperlinkPeer.Owner is { } hyperlink)
		{
			start = hyperlink.GetContentStart();
			end = hyperlink.GetContentEnd();
		}

		// If either of these are null, an automation peer for the link would never have been created to get here.
		if (start is null || end is null)
		{
			return null;
		}

		var startEdge = start.Offset;
		var endEdge = end.Offset;
		uint linkLength = (uint)(endEdge - startEdge);

		// Get the visual parent's view in order to get the PixelPosition from the text view. If we don't use the
		// visual parent, links in RichTextBlockOverflows won't get the correct text position from the pixel.
		// There are situations where text has been truncated such that there may be runs/hyperlinks outside the
		// available space which are not laid out as part of the text view, so there won't be a parent found; in that
		// case we return null which causes accessibility to ignore it.
		var pContentStartVisualParent = start.VisualParent;
		if (pContentStartVisualParent is null)
		{
			return null;
		}

		var pTextView = GetTextView(pContentStartVisualParent);
		if (pTextView is null)
		{
			return null;
		}

		// Transform the screen point into element-relative coordinates and hit-test it.
		var pixelCoordinate = ReverseTransformFromRoot(_pTextOwner, point);
		_ = pTextView.PixelPositionToTextPosition(pixelCoordinate, true, out var gravity);

		if (pTextContainer is null)
		{
			return null;
		}

		// Pixel position is the start of the range.
		var startPlain = new PlainTextPosition(pTextContainer, (uint)startEdge, gravity);
		var startPointer = TextPointer.CreateInstanceWithInternalPointer(startPlain);
		// Add the length to the first character to get the ending text position.
		var endPlain = new PlainTextPosition(pTextContainer, (uint)startEdge + linkLength, gravity);
		var endPointer = TextPointer.CreateInstanceWithInternalPointer(endPlain);

		if (startPointer is null || endPointer is null)
		{
			return null;
		}

		return TextRangeAdapter.Create(this, startPointer, endPointer, _pTextOwner, _ownerPeer);
	}

	//------------------------------------------------------------------------------------------------------------------
	// Retrieves a text range from the vicinity of a screen coordinate. It returns a degenerate TextRange.
	//------------------------------------------------------------------------------------------------------------------
	public ITextRangeProvider RangeFromPoint(Point screenLocation)
	{
		var pTextView = GetTextView(_pTextOwner);

		if (pTextView is not null)
		{
			// Get the text position from the given pixel position.
			var pixelCoordinate = ReverseTransformFromRoot(_pTextOwner, screenLocation);
			var textPosition = pTextView.PixelPositionToTextPosition(pixelCoordinate, true, out var gravity);
			var container = GetTextContainer(_pTextOwner);
			if (container is not null)
			{
				var plainTextPosition = new PlainTextPosition(container, textPosition, gravity);
				var startPointer = TextPointer.CreateInstanceWithInternalPointer(plainTextPosition);
				var endPointer = TextPointer.CreateInstanceWithInternalPointer(plainTextPosition);
				if (startPointer is not null && endPointer is not null)
				{
					var textRangeAdapter = TextRangeAdapter.Create(this, startPointer, endPointer, _pTextOwner, _ownerPeer);
					if (textRangeAdapter is not null)
					{
						return textRangeAdapter;
					}
				}
			}
		}

		return DocumentRange;
	}

	private TextRangeAdapter? RangeFromInlineUIContainer(InlineUIContainer iuc)
	{
		var pPageNode = GetPageNode(_pTextOwner);
		var container = GetTextContainer(_pTextOwner);
		if (pPageNode is null || container is null)
		{
			// TODO Uno (UIA): PageNode.FindInlineUIContainerOffset not yet exposed for the master RichTextBlock.
			return null;
		}

		uint textPosition = pPageNode.FindInlineUIContainerOffset(iuc);
		var plainTextPosition = new PlainTextPosition(container, textPosition, TextGravity.LineForwardCharacterBackward);

		var startPointer = TextPointer.CreateInstanceWithInternalPointer(plainTextPosition);
		var endPointer = TextPointer.CreateInstanceWithInternalPointer(plainTextPosition);
		if (startPointer is null || endPointer is null)
		{
			return null;
		}

		return TextRangeAdapter.Create(this, startPointer, endPointer, _pTextOwner, _ownerPeer);
	}

	// ---- Static helpers (CTextAdapter::Get* — type-switch on the owner) ------------------------

	internal static void GetContentEndPointers(
		FrameworkElement? pObject,
		out TextPointer? startTextPointer,
		out TextPointer? endTextPointer)
	{
		startTextPointer = null;
		endTextPointer = null;

		switch (pObject)
		{
			case RichTextBlock pRTbl:
				startTextPointer = pRTbl.ContentStart;
				endTextPointer = pRTbl.ContentEnd;
				break;
			case RichTextBlockOverflow pRTblo:
				// TODO Uno (UIA): RichTextBlockOverflow.ContentStart/ContentEnd (text-pointer slice of the master).
				startTextPointer = GetOverflowContentStart(pRTblo);
				endTextPointer = GetOverflowContentEnd(pRTblo);
				break;
				// TODO Uno (UIA): TextBlock content pointers (served today by the DirectUI.TextAdapter path).
		}
	}

	internal static void GetSelectionEndPointers(
		FrameworkElement? pObject,
		out TextPointer? selectionStartTextPointer,
		out TextPointer? selectionEndTextPointer)
	{
		selectionStartTextPointer = null;
		selectionEndTextPointer = null;

		switch (pObject)
		{
			case RichTextBlock pRTbl:
				selectionStartTextPointer = pRTbl.SelectionStart;
				selectionEndTextPointer = pRTbl.SelectionEnd;
				break;
			case RichTextBlockOverflow pRTblo:
				if (pRTblo.GetMaster() is { } master)
				{
					selectionStartTextPointer = master.SelectionStart;
					selectionEndTextPointer = master.SelectionEnd;
				}
				break;
		}

		GetContentEndPointers(pObject, out var contentStartTextPointer, out var contentEndTextPointer);

		// Selection extends between RichTextBlock and RichTextBlockOverflow, but on the TextPattern side they
		// are treated as individual TextAdapters, so cut off within the current DocumentRange.
		if (selectionStartTextPointer is not null && selectionEndTextPointer is not null
			&& contentStartTextPointer is not null && contentEndTextPointer is not null)
		{
			var selectionStartOffset = selectionStartTextPointer.Offset;
			var selectionEndOffset = selectionEndTextPointer.Offset;
			var contentStartOffset = contentStartTextPointer.Offset;
			var contentEndOffset = contentEndTextPointer.Offset;

			if (selectionStartOffset <= contentStartOffset)
			{
				selectionStartTextPointer = TextRangeAdapter.ClonePointer(contentStartTextPointer);
			}

			if (selectionEndOffset <= selectionStartOffset)
			{
				selectionEndTextPointer = TextRangeAdapter.ClonePointer(selectionStartTextPointer);
			}

			if (selectionStartOffset > contentEndOffset)
			{
				selectionStartTextPointer = TextRangeAdapter.ClonePointer(contentEndTextPointer);
			}

			if (selectionEndOffset > contentEndOffset)
			{
				selectionEndTextPointer = TextRangeAdapter.ClonePointer(contentEndTextPointer);
			}
		}
	}

	// Helper to get the ITextView specific to the different control. For a RTB or RTBOverflow, returns the
	// TextView for just this element (no knowledge of linked layout) — the single-element view.
	internal static ITextView? GetTextView(FrameworkElement? pObject) => pObject switch
	{
		RichTextBlock pRTbl => pRTbl.GetSingleElementTextView(),
		RichTextBlockOverflow pRTblo => pRTblo.GetSingleElementTextView(),
		_ => null,
	};

	// Helper to get the TextContainer specific to the different control.
	internal static ITextContainer? GetTextContainer(FrameworkElement? pObject) => pObject switch
	{
		RichTextBlock pRTbl => pRTbl.Blocks.GetTextContainer(),
		RichTextBlockOverflow pRTblo => pRTblo.GetMaster()?.Blocks.GetTextContainer(),
		_ => null,
	};

	internal static BlockCollection? GetBlockCollection(FrameworkElement? pObject) => pObject switch
	{
		RichTextBlock pRTbl => pRTbl.Blocks,
		RichTextBlockOverflow pRTblo => pRTblo.GetMaster()?.Blocks,
		_ => null,
	};

	internal static PageNode? GetPageNode(FrameworkElement? pObject) => pObject switch
	{
		// TODO Uno (UIA): RichTextBlock.GetPageNode() not exposed on the master yet.
		RichTextBlockOverflow pRTblo => pRTblo.GetPageNode(),
		_ => null,
	};

	// Helper to get the TextSelectionManager specific to the different control.
	internal static TextSelectionManager? GetSelectionManager(FrameworkElement? pObject) => pObject switch
	{
		RichTextBlock pRTbl => pRTbl.GetSelectionManager(),
		RichTextBlockOverflow pRTblo => pRTblo.GetMaster()?.GetSelectionManager(),
		_ => null,
	};

	// ---- Uno bridge helpers --------------------------------------------------------------------

	// WinUI reaches the owner's AP children via m_pTextOwner->GetAPChildren. On Uno we read the owner peer's
	// children (the same recursive peer set produced by GetChildrenCore).
	private IList<AutomationPeer> GetOwnerPeerChildren()
		=> _ownerPeer.GetChildren() ?? (IList<AutomationPeer>)Array.Empty<AutomationPeer>();

	// WinUI: TransformToRoot + ReverseTransform to map a root/screen point into element-relative pixels.
	private static Point ReverseTransformFromRoot(FrameworkElement element, Point rootPoint)
	{
		try
		{
			var fromRoot = element.TransformToVisual(null);
			var inverse = fromRoot.Inverse;
			if (inverse is not null)
			{
				return inverse.TransformPoint(rootPoint);
			}
		}
		catch
		{
			// TODO Uno (UIA): fall back to the untransformed point if the transform is unavailable.
		}

		return rootPoint;
	}

	// TODO Uno (UIA): RichTextBlockOverflow content-pointer slice of the master container.
	// WinUI returns CRichTextBlockOverflow::GetContentStart()/GetContentEnd() (the overflow's
	// own page slice). Until those are ported, return null so the overflow adapter degrades to
	// "no document range" rather than mis-reporting the whole master.
	private static TextPointer? GetOverflowContentStart(RichTextBlockOverflow overflow) => null;

	private static TextPointer? GetOverflowContentEnd(RichTextBlockOverflow overflow) => null;
}
