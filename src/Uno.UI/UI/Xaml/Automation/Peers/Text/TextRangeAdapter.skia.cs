// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference textrangeadapter.cpp, TextRangeAdapter.h, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using System;
using System.Collections.Generic;
using Windows.Foundation;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Automation.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Text.Core;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Documents.BlockLayout;
using Microsoft.UI.Xaml.Documents.RichTextServices;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Automation.Peers.Text;

//---------------------------------------------------------------------------
//
//  TextRangeAdapter
//
//  Faithful port of the core CTextRangeAdapter (the ITextRangeProvider over a
//  start/end TextPointer pair + the owner's text view). WinUI splits this between
//  the core CTextRangeAdapter (ported here) and the projection
//  TextRangeAdapter_partial.cpp (WinRT marshalling). Uno folds both into this
//  single managed class implementing the public ITextRangeProvider surface.
//
//  Refcounting -> GC. CTextPointerWrapper -> Uno's TextPointer (which wraps an
//  immutable PlainTextPosition); Clone re-wraps the same plain position.
//
//---------------------------------------------------------------------------
internal sealed partial class TextRangeAdapter : ITextRangeProvider
{
	// The attributes compared when walking the format tree (CTextRangeAdapter AttributeIdInfo[]).
	private static readonly AutomationTextAttributesEnum[] AttributeIdInfo =
	{
		AutomationTextAttributesEnum.CapStyleAttribute,
		AutomationTextAttributesEnum.CultureAttribute,
		AutomationTextAttributesEnum.FontNameAttribute,
		AutomationTextAttributesEnum.FontSizeAttribute,
		AutomationTextAttributesEnum.FontWeightAttribute,
		AutomationTextAttributesEnum.ForegroundColorAttribute,
		AutomationTextAttributesEnum.HorizontalTextAlignmentAttribute,
		AutomationTextAttributesEnum.IndentationFirstLineAttribute,
		AutomationTextAttributesEnum.IsHiddenAttribute,
		AutomationTextAttributesEnum.IsItalicAttribute,
		AutomationTextAttributesEnum.IsReadOnlyAttribute,
		AutomationTextAttributesEnum.IsSubscriptAttribute,
		AutomationTextAttributesEnum.IsSuperscriptAttribute,
		AutomationTextAttributesEnum.MarginBottomAttribute,
		AutomationTextAttributesEnum.MarginLeadingAttribute,
		AutomationTextAttributesEnum.MarginTopAttribute,
		AutomationTextAttributesEnum.MarginTrailingAttribute,
	};

	// UIA sentinels for GetAttributeValue. WinUI returns dedicated UIAutomation singletons
	// (NotSupportedValue / MixedAttributeValue). Uno does not surface those yet, so we use shared
	// marker objects; clients comparing by reference still get a stable "not supported" / "mixed".
	// TODO Uno (UIA): replace with the platform NotSupported / MixedAttribute singletons.
	private static readonly object NotSupportedAttributeValue = new();
	private static readonly object MixedAttributeValue = new();

	// MUX text.cpp IsXamlNewline — CR/LF are treated as line/word breaks. CSelectionWordBreaker keeps this
	// private, so the same check is inlined here.
	private static bool IsXamlNewline(char character)
		=> character is (char)0x000A or (char)0x000D or (char)0x0085 or (char)0x2028;

	private readonly TextAdapter _pTextAdapter;
	private readonly FrameworkElement _pTextOwner;
	private readonly AutomationPeer _ownerPeer;
	private TextPointer _pStartTextPointer;
	private TextPointer _pEndTextPointer;

	private TextRangeAdapter(
		TextAdapter pTextAdapter,
		TextPointer pStartTextPointer,
		TextPointer pEndTextPointer,
		FrameworkElement pTextOwner,
		AutomationPeer ownerPeer)
	{
		_pTextAdapter = pTextAdapter;
		_pStartTextPointer = pStartTextPointer;
		_pEndTextPointer = pEndTextPointer;
		_pTextOwner = pTextOwner;
		_ownerPeer = ownerPeer;
	}

	// Plain-text creation path for RichTextBlock/RichTextBlockOverflow.
	internal static TextRangeAdapter? Create(
		TextAdapter pTextAdapter,
		TextPointer pStartTextPointer,
		TextPointer pEndTextPointer,
		FrameworkElement pTextOwner,
		AutomationPeer ownerPeer)
		=> new(pTextAdapter, pStartTextPointer, pEndTextPointer, pTextOwner, ownerPeer);

	// CTextPointerWrapper::Clone — TextPointer wraps an immutable PlainTextPosition, so cloning is
	// just re-wrapping the same plain position. Returns the original if re-wrapping fails (always valid here).
	internal static TextPointer ClonePointer(TextPointer pointer)
		=> TextPointer.CreateInstanceWithInternalPointer(pointer.GetPlainTextPosition()) ?? pointer;

	public ITextRangeProvider Clone()
	{
		var startPointer = ClonePointer(_pStartTextPointer);
		var endPointer = ClonePointer(_pEndTextPointer);
		return new TextRangeAdapter(_pTextAdapter, startPointer, endPointer, _pTextOwner, _ownerPeer);
	}

	public bool Compare(ITextRangeProvider textRangeProvider)
	{
		if (textRangeProvider is not TextRangeAdapter pTargetRange)
		{
			throw new ArgumentException(nameof(textRangeProvider));
		}

		if (!ReferenceEquals(_pTextOwner, pTargetRange._pTextOwner))
		{
			throw new ArgumentException(nameof(textRangeProvider));
		}

		var startOffset = _pStartTextPointer.Offset;
		var endOffset = _pEndTextPointer.Offset;
		var targetStartOffset = pTargetRange._pStartTextPointer.Offset;
		var targetEndOffset = pTargetRange._pEndTextPointer.Offset;

		return startOffset == targetStartOffset && endOffset == targetEndOffset;
	}

	public int CompareEndpoints(
		TextPatternRangeEndpoint endpoint,
		ITextRangeProvider textRangeProvider,
		TextPatternRangeEndpoint targetEndpoint)
	{
		if (textRangeProvider is not TextRangeAdapter pTargetTextRangeProvider)
		{
			throw new ArgumentException(nameof(textRangeProvider));
		}

		if (!ReferenceEquals(_pTextOwner, pTargetTextRangeProvider._pTextOwner))
		{
			throw new ArgumentException(nameof(textRangeProvider));
		}

		var pCallerEndPoint = endpoint == TextPatternRangeEndpoint.Start ? _pStartTextPointer : _pEndTextPointer;
		var pTargetEndPoint = targetEndpoint == TextPatternRangeEndpoint.Start
			? pTargetTextRangeProvider._pStartTextPointer
			: pTargetTextRangeProvider._pEndTextPointer;

		var callerEndPointPosition = pCallerEndPoint.Offset;
		var targetEndPointPosition = pTargetEndPoint.Offset;

		return callerEndPointPosition - targetEndPointPosition;
	}

	//------------------------------------------------------------------------
	//  GetEnclosingElement — returns the innermost element that encloses the text range.
	//------------------------------------------------------------------------
	public IRawElementProviderSimple GetEnclosingElement()
	{
		AutomationPeer? pAP = null;

		if (_pTextOwner is not null)
		{
			// If the range is in a link, that link should be the enclosing element.
			if (RangeIsInLink(out var link) && link is not null)
			{
				pAP = link.GetOrCreateAutomationPeer();
			}

			pAP ??= _ownerPeer;
		}

		return new IRawElementProviderSimple(pAP ?? _ownerPeer);
	}

	// CTextRangeAdapter::RangeIsInLink
	private bool RangeIsInLink(out Inline? link)
	{
		link = null;

		var startOffset = _pStartTextPointer.Offset;
		var endOffset = _pEndTextPointer.Offset;

		var pTextContainer = TextAdapter.GetTextContainer(_pTextOwner);
		if (pTextContainer is null)
		{
			return false;
		}

		// Don't let UIA get stuck right at the end of a line.
		pTextContainer.GetPositionCount(out var containerPositions);
		if ((uint)startOffset >= containerPositions || (uint)endOffset >= containerPositions)
		{
			return false;
		}

		// TODO Uno (UIA): Run.IsInsideHyperlink is not ported. Once a run-level "containing hyperlink"
		// lookup exists, resolve the start/end runs here and confirm they share the same Hyperlink.
		return false;
	}

	//--------------------------------------------------------------------------------------------------------------------------------------------
	// Returns the APs for child UIEs within this range.
	//--------------------------------------------------------------------------------------------------------------------------------------------
	public IRawElementProviderSimple[] GetChildren()
	{
		var pAPCollection = new List<AutomationPeer>();

		_pStartTextPointer.GetPlainTextPosition().GetOffset(out var startOffset);
		_pEndTextPointer.GetPlainTextPosition().GetOffset(out var endOffset);

		var pPageNode = TextAdapter.GetPageNode(_pTextOwner);
		if (pPageNode is not null)
		{
			var containerElements = pPageNode.GetElementsWithinRange(startOffset, endOffset);
			foreach (var container in containerElements)
			{
				UIElement? uiElement = null;
				try
				{
					uiElement = container.GetChild();
				}
				catch (NotSupportedException)
				{
					// TODO Uno (UIA): InlineUIContainer.GetChild is not implemented yet (throws). Skip the
					// container's child peer until the hosted child is exposed.
				}

				if (uiElement is not null)
				{
					var pAP = uiElement.OnCreateAutomationPeerInternal();
					if (pAP is not null)
					{
						pAPCollection.Add(pAP);
					}
				}
			}
		}

		GetLinkChildrenWithinRange(pAPCollection, startOffset, endOffset);

		var result = new IRawElementProviderSimple[pAPCollection.Count];
		for (int i = 0; i < pAPCollection.Count; i++)
		{
			result[i] = new IRawElementProviderSimple(pAPCollection[i]);
		}

		return result;
	}

	// CTextRangeAdapter::GetLinkChildrenWithinRange
	private void GetLinkChildrenWithinRange(List<AutomationPeer> pAPCollection, uint startOffset, uint endOffset)
	{
		// If the range is already the hyperlink, don't burrow further.
		var enclosing = GetEnclosingElement();
		if (enclosing.AutomationPeer is HyperlinkAutomationPeer)
		{
			return;
		}

		// TODO Uno (UIA): RichTextBlock/TextBlock GetFocusableChildren (the hyperlink collection) is not
		// ported. Once it is, walk the focusable children and add those whose link offsets fall within
		// [startOffset, endOffset) via IsLinkWithinRange.
	}

	// CTextRangeAdapter::IsLinkWithinRange
	private bool IsLinkWithinRange(Inline link, uint startOffset, uint endOffset)
	{
		if (link is not Hyperlink hyperlink)
		{
			// Could be an InlineUIContainer.
			return false;
		}

		var start = hyperlink.GetContentStart();
		var end = hyperlink.GetContentEnd();
		if (start is null || end is null)
		{
			return false;
		}

		var linkStartOffset = start.Offset;
		var linkEndOffset = end.Offset;

		// A text range child is "any object partially or fully contained by the range but does not contain it".
		bool isStartWithinRange = (uint)linkStartOffset >= startOffset && (uint)linkStartOffset < endOffset;
		bool isEndWithinRange = (uint)linkEndOffset > startOffset && (uint)linkEndOffset <= endOffset;

		return isStartWithinRange || isEndWithinRange;
	}

	public void ExpandToEnclosingUnit(TextUnit unit)
	{
		// First make it a degenerate range, then expand to the specified unit.
		Normalize(this);

		// Expanding Start to the unit boundary after normalizing moves end to the unit boundary, then we move
		// the start back by a unit. This avoids a separate "already at boundary" check.
		switch (unit)
		{
			case TextUnit.Character:
				ExpandToCharacter(TextPatternRangeEndpoint.End, this, 1, out _);
				break;
			case TextUnit.Format:
				ExpandToFormat(TextPatternRangeEndpoint.Start, this, 1, out var formatMove);
				if (formatMove > 0)
				{
					ExpandToFormat(TextPatternRangeEndpoint.Start, this, -1, out _);
				}
				break;
			case TextUnit.Word:
				ExpandToWord(TextPatternRangeEndpoint.Start, this, 1, out var wordMove);
				if (wordMove > 0)
				{
					ExpandToWord(TextPatternRangeEndpoint.Start, this, -1, out _);
				}
				break;
			case TextUnit.Line:
				IsAtEmptyLine(out var bIsEmptyLine); // Empty line is already an enclosing unit.
				if (!bIsEmptyLine)
				{
					ExpandToLine(TextPatternRangeEndpoint.Start, this, 1, out var lineMove, out _);
					if (lineMove > 0)
					{
						ExpandToLine(TextPatternRangeEndpoint.Start, this, -1, out _, out _);
					}
				}
				break;
			case TextUnit.Paragraph:
				ExpandToParagraph(TextPatternRangeEndpoint.Start, this, 1, out var paraMove, out var doExecuteForNextUnit);
				if (!doExecuteForNextUnit)
				{
					if (paraMove > 0)
					{
						ExpandToParagraph(TextPatternRangeEndpoint.Start, this, -1, out _, out _);
					}
					break;
				}
				// Falls through to Page when there is no block collection (matches C++ fallthrough).
				goto case TextUnit.Page;
			case TextUnit.Page:
				ExpandToPage(this, 1);
				goto case TextUnit.Document;
			case TextUnit.Document:
				ExpandToDocument(this, 1);
				break;
		}
	}

	public object GetAttributeValue(int attributeId)
	{
		var attributeID = (AutomationTextAttributesEnum)attributeId;
		object? retVal = null;

		_pStartTextPointer.GetPlainTextPosition().GetOffset(out var characterPosition);
		_pEndTextPointer.GetPlainTextPosition().GetOffset(out var rangeEndPosition);
		var pTextContainer = TextAdapter.GetTextContainer(_pTextOwner);
		if (pTextContainer is null)
		{
			return NotSupportedAttributeValue;
		}

		pTextContainer.GetPositionCount(out var positionCount);
		rangeEndPosition = rangeEndPosition < positionCount ? rangeEndPosition : positionCount;

		uint cCharacters = 0;
		object? value = null;
		uint elementEndPosition = 0;

		// Loop through the range to make sure the attribute value is the same across the whole range.
		// In case we find a difference, we always return MixedAttribute.
		do
		{
			TextElement? pContainingElement;
			TextNestingType textNestingType;
			do
			{
				// This eliminates the hidden runs and moves to the next chunk.
				characterPosition += cCharacters;
				if (characterPosition > rangeEndPosition || characterPosition >= positionCount)
				{
					if (characterPosition >= positionCount && value is null)
					{
						return NotSupportedAttributeValue;
					}

					return value ?? NotSupportedAttributeValue;
				}

				pTextContainer.GetRun(
					characterPosition,
					out _,
					out _,
					out textNestingType,
					out pContainingElement,
					out var characters,
					out cCharacters);

				if (characterPosition == rangeEndPosition)
				{
					break;
				}
			} while (pContainingElement is null && cCharacters > 0);

			if (pContainingElement is null && characterPosition == rangeEndPosition)
			{
				uint adjustedPosition = characterPosition;
				while (textNestingType == TextNestingType.OpenNesting)
				{
					adjustedPosition++;
					MUX_ASSERT(adjustedPosition < positionCount);
					pTextContainer.GetRun(adjustedPosition, out _, out _, out textNestingType, out pContainingElement, out _, out cCharacters);
				}
				while (textNestingType == TextNestingType.CloseNesting)
				{
					MUX_ASSERT(adjustedPosition > 0);
					--adjustedPosition;
					pTextContainer.GetRun(adjustedPosition, out _, out _, out textNestingType, out pContainingElement, out _, out cCharacters);
				}
			}

			if (pContainingElement is null)
			{
				break;
			}

			var pParagraph = GetParagraphFromTextElement(pContainingElement);
			value = GetAttributeValueFromTextElement(attributeID, pContainingElement, pParagraph);

			if (retVal is not null)
			{
				bool isValueSame = AttributeValueComparer(retVal, value);
				if (!isValueSame)
				{
					// Mixed values across the range. WinUI returns UIA's MixedAttributeValue sentinel.
					// TODO Uno (UIA): surface the real MixedAttribute sentinel; the shared sentinel below
					// is the closest "no single value" signal available today.
					return MixedAttributeValue;
				}
			}

			var elementEndPositionPoint = pContainingElement.GetContentEnd();
			if (elementEndPositionPoint is not null)
			{
				elementEndPositionPoint.GetPlainTextPosition().GetOffset(out elementEndPosition);
			}

			retVal = value;
		} while (rangeEndPosition > elementEndPosition);

		return retVal ?? NotSupportedAttributeValue;
	}

	public string GetText(int maxLength)
	{
		var pTextContainer = _pStartTextPointer.GetPlainTextPosition().GetTextContainer();
		if (pTextContainer is null)
		{
			return string.Empty;
		}

		var startOffset = _pStartTextPointer.Offset;
		var endOffset = _pEndTextPointer.Offset;

		var text = pTextContainer.GetText((uint)startOffset, (uint)endOffset, true /* insertNewlines */);

		if (maxLength >= 0 && text.Length > maxLength)
		{
			text = text.Substring(0, maxLength);
		}

		return text;
	}

	public void GetBoundingRectangles(out double[] returnValue)
	{
		var startOffset = _pStartTextPointer.Offset;
		var endOffset = _pEndTextPointer.Offset;
		var pTextView = TextAdapter.GetTextView(_pTextOwner);

		if (pTextView is null)
		{
			returnValue = Array.Empty<double>();
			return;
		}

		var rectangles = pTextView.TextRangeToTextBounds((uint)startOffset, (uint)endOffset);
		if (rectangles.Length == 0)
		{
			returnValue = Array.Empty<double>();
			return;
		}

		// Flatten the Rect list into a double array (UIA returns flat [X,Y,W,H,...]).
		var flat = new double[4 * rectangles.Length];
		for (int current = 0; current < rectangles.Length; current++)
		{
			// Transform element-relative bounds into root/world space for the UIA client.
			Rect rect = rectangles[current];
			try
			{
				var toRoot = _pTextOwner.TransformToVisual(null);
				rect = toRoot.TransformBounds(rect);
			}
			catch
			{
				// TODO Uno (UIA): keep element-relative bounds if the transform is unavailable.
			}

			flat[4 * current] = rect.X;
			flat[4 * current + 1] = rect.Y;
			flat[4 * current + 2] = rect.Width;
			flat[4 * current + 3] = rect.Height;
		}

		returnValue = flat;
	}

	//----------------------------------------------------------------------------------------------------------------------------------------------
	// For a non-degenerate text range, Move normalizes and moves the range. If the range cannot be moved as far as
	// requested but can move by a smaller number of units, it moves by the smaller number and returns that count.
	//----------------------------------------------------------------------------------------------------------------------------------------------
	public int Move(TextUnit unit, int count)
	{
		int moveCount = 0;

		// If we can't move to the next TextUnit (it doesn't exist) we discard the whole operation; Move is a
		// no-op for Page and Document in our design.
		if (count != 0 && unit != TextUnit.Page && unit != TextUnit.Document)
		{
			var documentRangeAdapter = _pTextAdapter.GetDocumentRange();
			var clonedRange = (TextRangeAdapter)Clone();
			if (documentRangeAdapter is null)
			{
				return 0;
			}

			ExpandToEnclosingUnit(unit);
			var pTextContainer = TextAdapter.GetTextContainer(_pTextOwner);
			if (pTextContainer is null)
			{
				return 0;
			}

			bool bIsEmptyLine = false;

			// We don't want to move backwards at all if we are at the start of the document.
			if (count < 0)
			{
				_pStartTextPointer.GetPlainTextPosition().GetOffset(out var endOffset);
				documentRangeAdapter._pStartTextPointer.GetPlainTextPosition().GetOffset(out var startOffset);
				MUX_ASSERT(startOffset <= endOffset);
				var text = pTextContainer.GetText(startOffset, endOffset, true);

				if (text.Length == 0)
				{
					moveCount = 0;
				}
				else
				{
					moveCount = MoveEndpointByUnitImpl(TextPatternRangeEndpoint.Start, unit, count, out _);
				}
			}
			else
			{
				moveCount = MoveEndpointByUnitImpl(TextPatternRangeEndpoint.Start, unit, count, out bIsEmptyLine);
			}

			// If we are in the last word/line/paragraph when moving forward, MoveEndpointByUnit will have moved
			// the start pointer to the end, which is not a valid move for Move; discard it.
			if (count > 0 && moveCount > 0)
			{
				_pStartTextPointer.GetPlainTextPosition().GetOffset(out var startOffset);
				documentRangeAdapter._pEndTextPointer.GetPlainTextPosition().GetOffset(out var endOffset);
				var text = pTextContainer.GetText(startOffset, endOffset, true);
				if (text.Length <= 2)
				{
					if (text.Length == 0 || IsXamlNewline(text[0]))
					{
						if (moveCount == 1)
						{
							if (unit != TextUnit.Line || !bIsEmptyLine)
							{
								moveCount = 0;
							}
						}
						else
						{
							if (unit == TextUnit.Line && bIsEmptyLine)
							{
								moveCount -= 1;
							}
							else
							{
								int adjustedMoveCount = MoveEndpointByUnitImpl(TextPatternRangeEndpoint.Start, unit, -1, out _);
								if (adjustedMoveCount != 0)
								{
									moveCount -= 1;
								}
							}
						}
					}
				}
			}

			if (moveCount != 0)
			{
				ExpandToEnclosingUnit(unit);
			}
			else
			{
				_pStartTextPointer = clonedRange._pStartTextPointer;
				_pEndTextPointer = clonedRange._pEndTextPointer;
			}
		}

		return moveCount;
	}

	public int MoveEndpointByUnit(TextPatternRangeEndpoint endpoint, TextUnit unit, int count)
		=> MoveEndpointByUnitImpl(endpoint, unit, count, out _);

	//------------------------------------------------------------------------------------------------------------------------------------------------
	// The endpoint is moved to the next available unit boundary. If it crosses the other endpoint, the other endpoint is
	// also moved, resulting in a degenerate range (start always <= end).
	//------------------------------------------------------------------------------------------------------------------------------------------------
	private int MoveEndpointByUnitImpl(TextPatternRangeEndpoint endPoint, TextUnit unit, int count, out bool pbIsEmptyLine)
	{
		uint moveCount = 0;
		bool doExecuteForNextUnit = false;
		pbIsEmptyLine = false;

		switch (unit)
		{
			case TextUnit.Character:
				ExpandToCharacter(endPoint, this, count, out moveCount);
				break;
			case TextUnit.Format:
				ExpandToFormat(endPoint, this, count, out moveCount);
				break;
			case TextUnit.Word:
				ExpandToWord(endPoint, this, count, out moveCount);
				break;
			case TextUnit.Line:
				ExpandToLine(endPoint, this, count, out moveCount, out pbIsEmptyLine);
				break;
			case TextUnit.Paragraph:
				ExpandToParagraph(endPoint, this, count, out moveCount, out doExecuteForNextUnit);
				if (!doExecuteForNextUnit)
				{
					break;
				}
				goto case TextUnit.Page;
			case TextUnit.Page:
			case TextUnit.Document:
				if (count != 0)
				{
					moveCount = 1;
					if (endPoint == TextPatternRangeEndpoint.Start)
					{
						if (count > 0)
						{
							ExpandToEnclosingUnit(unit);
							_pStartTextPointer = ClonePointer(_pEndTextPointer);
						}
						if (count < 0)
						{
							TextAdapter.GetContentEndPointers(_pTextOwner, out var contentStart, out _);
							if (contentStart is not null)
							{
								_pStartTextPointer = ClonePointer(contentStart);
							}
						}
					}
					else
					{
						if (count < 0)
						{
							_pEndTextPointer = ClonePointer(_pStartTextPointer);
							moveCount = 1;
						}
						if (count > 0)
						{
							TextAdapter.GetContentEndPointers(_pTextOwner, out _, out var contentEnd);
							if (contentEnd is not null)
							{
								_pEndTextPointer = ClonePointer(contentEnd);
							}
						}
					}
				}
				break;
		}

		return count < 0 ? 0 - (int)moveCount : (int)moveCount;
	}

	//----------------------------------------------------------------------------------------------------------------------------------------------
	// If the endpoint being moved crosses the other endpoint, that other endpoint is moved too, resulting in a
	// degenerate range and ensuring correct ordering of the endpoints (start <= end).
	//----------------------------------------------------------------------------------------------------------------------------------------------
	public void MoveEndpointByRange(
		TextPatternRangeEndpoint endpoint,
		ITextRangeProvider textRangeProvider,
		TextPatternRangeEndpoint targetEndpoint)
	{
		if (textRangeProvider is not TextRangeAdapter pTargetTextRangeProvider)
		{
			throw new ArgumentException(nameof(textRangeProvider));
		}

		if (!ReferenceEquals(_pTextOwner, pTargetTextRangeProvider._pTextOwner))
		{
			throw new ArgumentException(nameof(textRangeProvider));
		}

		var pTargetPointer = targetEndpoint == TextPatternRangeEndpoint.Start
			? pTargetTextRangeProvider._pStartTextPointer
			: pTargetTextRangeProvider._pEndTextPointer;

		if (endpoint == TextPatternRangeEndpoint.Start)
		{
			_pStartTextPointer = ClonePointer(pTargetPointer);

			if (_pStartTextPointer.Offset > _pEndTextPointer.Offset)
			{
				_pEndTextPointer = ClonePointer(_pStartTextPointer);
			}
		}
		else
		{
			_pEndTextPointer = ClonePointer(pTargetPointer);

			if (_pStartTextPointer.Offset > _pEndTextPointer.Offset)
			{
				_pStartTextPointer = ClonePointer(_pEndTextPointer);
			}
		}
	}

	public void Select()
	{
		var pTextSelectionManager = TextAdapter.GetSelectionManager(_pTextOwner);
		if (pTextSelectionManager is null)
		{
			return; // E_NOT_SUPPORTED
		}

		switch (_pTextOwner)
		{
			case RichTextBlock pRTbl:
				pRTbl.Select(_pStartTextPointer, _pEndTextPointer);
				break;
			case RichTextBlockOverflow pRTblo when pRTblo.GetMaster() is { } master:
				master.Select(_pStartTextPointer, _pEndTextPointer);
				break;
		}
	}

	public void AddToSelection()
	{
		// E_NOT_SUPPORTED
	}

	public void RemoveFromSelection()
	{
		// E_NOT_SUPPORTED
	}

	public void ScrollIntoView(bool alignToTop)
	{
		var startOffset = _pStartTextPointer.Offset;
		var endOffset = _pEndTextPointer.Offset;
		var pTextView = TextAdapter.GetTextView(_pTextOwner);

		if (pTextView is null)
		{
			return;
		}

		var rectangles = pTextView.TextRangeToTextBounds((uint)startOffset, (uint)endOffset);
		if (rectangles.Length > 0)
		{
			var finalRect = new Rect(
				rectangles[0].X,
				rectangles[0].Y,
				rectangles[0].Width,
				rectangles[0].Y + rectangles[^1].Y + rectangles[^1].Height);

			// TODO Uno (UIA): UIElement.BringIntoView(rect, forceIntoView, useAnimation) overload not available;
			// use the parameterless BringIntoView as a best-effort scroll.
			_pTextOwner.StartBringIntoView(new BringIntoViewOptions { TargetRect = finalRect });
		}
	}

	public ITextRangeProvider? FindAttribute(int attributeId, object value, bool backward)
	{
		// TODO Uno (UIA): FindAttribute is not implemented in the WinUI core CTextRangeAdapter either
		// (the projection returns null). Match that — no attribute search.
		return null;
	}

	public ITextRangeProvider? FindText(string text, bool backward, bool ignoreCase)
	{
		// TODO Uno (UIA): FindText is not implemented in the WinUI core CTextRangeAdapter (the projection
		// returns null). Match that.
		return null;
	}

	// ---- Static expansion helpers (CTextRangeAdapter::*) ---------------------------------------

	internal static void Normalize(TextRangeAdapter pTextRangeAdapter)
		=> pTextRangeAdapter._pEndTextPointer = ClonePointer(pTextRangeAdapter._pStartTextPointer);

	//  Validation that keeps the endpoint within DocumentRange and start <= end across all cases.
	internal static void ValidateAndAdjust(
		TextPatternRangeEndpoint endPoint,
		PlainTextPosition nextPosition,
		TextRangeAdapter pTextRangeAdapter,
		out bool pFoundEmptyAdjustedRange)
	{
		pFoundEmptyAdjustedRange = false;

		var documentRangeAdapter = pTextRangeAdapter._pTextAdapter.GetDocumentRange();
		if (documentRangeAdapter is null)
		{
			return;
		}

		documentRangeAdapter._pStartTextPointer.GetPlainTextPosition().GetOffset(out var contentStartOffset);
		documentRangeAdapter._pEndTextPointer.GetPlainTextPosition().GetOffset(out var contentEndOffset);

		uint oldOffset;
		uint newOffset;
		bool isEndPosition = true;

		if (endPoint == TextPatternRangeEndpoint.Start)
		{
			pTextRangeAdapter._pStartTextPointer.GetPlainTextPosition().GetOffset(out oldOffset);
			pTextRangeAdapter._pEndTextPointer.GetPlainTextPosition().GetOffset(out var endOffset);
			nextPosition.GetOffset(out var startOffset);

			// If Start is greater than End, End is brought to the same position as Start.
			if (startOffset > endOffset)
			{
				if (startOffset >= contentEndOffset)
				{
					pTextRangeAdapter._pStartTextPointer = ClonePointer(documentRangeAdapter._pEndTextPointer);
					newOffset = contentEndOffset;
				}
				else
				{
					pTextRangeAdapter._pStartTextPointer = TextPointer.CreateInstanceWithInternalPointer(nextPosition) ?? pTextRangeAdapter._pStartTextPointer;
					isEndPosition = false;
					newOffset = startOffset;
				}
				pTextRangeAdapter._pEndTextPointer = ClonePointer(pTextRangeAdapter._pStartTextPointer);
			}
			else
			{
				if (startOffset <= contentStartOffset)
				{
					pTextRangeAdapter._pStartTextPointer = ClonePointer(documentRangeAdapter._pStartTextPointer);
					newOffset = contentStartOffset;
				}
				else
				{
					pTextRangeAdapter._pStartTextPointer = TextPointer.CreateInstanceWithInternalPointer(nextPosition) ?? pTextRangeAdapter._pStartTextPointer;
					isEndPosition = false;
					newOffset = startOffset;
				}
			}
		}
		else
		{
			pTextRangeAdapter._pEndTextPointer.GetPlainTextPosition().GetOffset(out oldOffset);
			pTextRangeAdapter._pStartTextPointer.GetPlainTextPosition().GetOffset(out var startOffset);
			nextPosition.GetOffset(out var endOffset);

			// If End is less than Start, Start is brought to the same position as End.
			if (startOffset > endOffset)
			{
				if (endOffset <= contentStartOffset)
				{
					pTextRangeAdapter._pEndTextPointer = ClonePointer(documentRangeAdapter._pStartTextPointer);
					newOffset = contentStartOffset;
				}
				else
				{
					pTextRangeAdapter._pEndTextPointer = TextPointer.CreateInstanceWithInternalPointer(nextPosition) ?? pTextRangeAdapter._pEndTextPointer;
					isEndPosition = false;
					newOffset = endOffset;
				}
				pTextRangeAdapter._pStartTextPointer = ClonePointer(pTextRangeAdapter._pEndTextPointer);
			}
			else
			{
				if (endOffset >= contentEndOffset)
				{
					pTextRangeAdapter._pEndTextPointer = ClonePointer(documentRangeAdapter._pEndTextPointer);
					newOffset = contentEndOffset;
				}
				else
				{
					pTextRangeAdapter._pEndTextPointer = TextPointer.CreateInstanceWithInternalPointer(nextPosition) ?? pTextRangeAdapter._pEndTextPointer;
					isEndPosition = false;
					newOffset = endOffset;
				}
			}
		}

		if ((newOffset - oldOffset != 0) && !isEndPosition)
		{
			uint sOffset = newOffset > oldOffset ? oldOffset : newOffset;
			uint eOffset = newOffset + oldOffset - sOffset;

			var pTextContainer = TextAdapter.GetTextContainer(pTextRangeAdapter._pTextOwner);
			if (pTextContainer is not null)
			{
				var text = pTextContainer.GetText(sOffset, eOffset, true);
				if (text.Length == 0)
				{
					pFoundEmptyAdjustedRange = true;
				}
			}
		}
	}

	internal static void ExpandToCharacter(
		TextPatternRangeEndpoint endPoint,
		TextRangeAdapter pTextRangeAdapter,
		int count,
		out uint pnCount)
	{
		uint moveCount = 0;
		pnCount = 0;

		if (count != 0)
		{
			var plainTextPosition = endPoint == TextPatternRangeEndpoint.Start
				? pTextRangeAdapter._pStartTextPointer.GetPlainTextPosition()
				: pTextRangeAdapter._pEndTextPointer.GetPlainTextPosition();

			MoveByCharacter(count, out moveCount, ref plainTextPosition);
			ValidateAndAdjust(endPoint, plainTextPosition, pTextRangeAdapter, out var foundEmptyAdjustedRange);
			if (foundEmptyAdjustedRange)
			{
				ExpandToCharacter(endPoint, pTextRangeAdapter, count, out pnCount);
				return;
			}
		}

		pnCount = moveCount;
	}

	internal static void MoveByCharacter(int count, out uint movedCount, ref PlainTextPosition plainTextPosition)
	{
		var nextPosition = plainTextPosition;
		uint posCount = count >= 0 ? (uint)count : (uint)(0 - count);
		uint moveCount = 0;
		bool foundPos = true;

		for (uint i = 0; (i < posCount) && foundPos; i++)
		{
			uint startOffset;
			uint endOffset;

			if (count >= 0)
			{
				nextPosition.GetOffset(out startOffset);
				nextPosition.GetNextInsertionPosition(out foundPos, out nextPosition);
				nextPosition.GetOffset(out endOffset);
			}
			else
			{
				nextPosition.GetOffset(out endOffset);
				nextPosition.GetPreviousInsertionPosition(out foundPos, out nextPosition);
				nextPosition.GetOffset(out startOffset);
			}

			if (foundPos)
			{
				var pTextContainer = plainTextPosition.GetTextContainer();
				if (pTextContainer is not null)
				{
					var text = pTextContainer.GetText(startOffset, endOffset, true);
					// If it's an empty position, skip it.
					if (text.Length == 0)
					{
						// Skip this move but still move the same number of real units.
						posCount += 1;
					}
					else
					{
						moveCount++;
						// Only update the position if we actually moved over a non-empty position.
						plainTextPosition = nextPosition;
					}
				}
			}
		}

		movedCount = moveCount;
	}

	internal static void ExpandToFormat(
		TextPatternRangeEndpoint endPoint,
		TextRangeAdapter pTextRangeAdapter,
		int count,
		out uint pnCount)
	{
		pnCount = 0;
		if (count == 0)
		{
			return;
		}

		uint textPosition;
		if (endPoint == TextPatternRangeEndpoint.Start)
		{
			pTextRangeAdapter._pStartTextPointer.GetPlainTextPosition().GetOffset(out textPosition);
		}
		else
		{
			pTextRangeAdapter._pEndTextPointer.GetPlainTextPosition().GetOffset(out textPosition);
		}

		var pTextContainer = TextAdapter.GetTextContainer(pTextRangeAdapter._pTextOwner);
		if (pTextContainer is null)
		{
			return;
		}

		pTextContainer.GetPositionCount(out var positionCount);
		uint posCount = (uint)Math.Abs(count);
		uint cCharacters = 0;
		int direction = 1;
		TextElement? pContainingElement = null;
		ReadOnlyMemory<char> pCharacters;

		if (count > 0)
		{
			do
			{
				textPosition += cCharacters;
				if (textPosition >= positionCount)
				{
					return;
				}
				pTextContainer.GetRun(textPosition, out _, out _, out _, out pContainingElement, out pCharacters, out cCharacters);
			} while ((pCharacters.IsEmpty && cCharacters > 0) || (pContainingElement is LineBreak));
		}
		else
		{
			direction = -1;
			do
			{
				if (textPosition == 0 && cCharacters > 0)
				{
					return;
				}
				textPosition -= cCharacters;
				if (textPosition >= positionCount)
				{
					textPosition = positionCount - 1;
				}
				pTextContainer.GetRun(textPosition, out _, out _, out _, out pContainingElement, out pCharacters, out cCharacters);
			} while ((pCharacters.IsEmpty && cCharacters > 0) || (pContainingElement is LineBreak));
		}

		var pAfterBoundaryElement = pContainingElement;
		TextElement? pBeforeBoundaryElement = null;
		uint moveCount;
		for (moveCount = 0; moveCount < posCount && pAfterBoundaryElement is not null; moveCount++)
		{
			pContainingElement = pAfterBoundaryElement;
			pTextRangeAdapter.TraverseTextElementTreeForFormat(pContainingElement!, direction, out pAfterBoundaryElement, out pBeforeBoundaryElement);
		}

		TextPointer? textPointerWrapperFinal = null;
		if (direction > 0)
		{
			textPointerWrapperFinal = pBeforeBoundaryElement?.GetContentEnd();
		}
		else
		{
			if (pAfterBoundaryElement is not null)
			{
				textPointerWrapperFinal = pAfterBoundaryElement.GetContentEnd();
			}

			if (pAfterBoundaryElement is null || textPointerWrapperFinal is null)
			{
				var documentRangeAdapter = pTextRangeAdapter._pTextAdapter.GetDocumentRange();
				textPointerWrapperFinal = documentRangeAdapter?._pStartTextPointer;
			}
		}

		if (textPointerWrapperFinal is not null)
		{
			ValidateAndAdjust(endPoint, textPointerWrapperFinal.GetPlainTextPosition(), pTextRangeAdapter, out _);
		}

		pnCount = moveCount;
	}

	internal static void ExpandToWord(
		TextPatternRangeEndpoint endPoint,
		TextRangeAdapter pTextRangeAdapter,
		int count,
		out uint pnCount)
	{
		uint moveCount = 0;
		pnCount = 0;

		if (count != 0)
		{
			var plainTextPosition = endPoint == TextPatternRangeEndpoint.Start
				? pTextRangeAdapter._pStartTextPointer.GetPlainTextPosition()
				: pTextRangeAdapter._pEndTextPointer.GetPlainTextPosition();

			MoveByWord(count, out moveCount, ref plainTextPosition);
			ValidateAndAdjust(endPoint, plainTextPosition, pTextRangeAdapter, out var foundEmptyAdjustedRange);
			if (foundEmptyAdjustedRange)
			{
				ExpandToWord(endPoint, pTextRangeAdapter, count, out pnCount);
				return;
			}
		}

		pnCount = moveCount;
	}

	internal static void MoveByWord(int count, out uint movedCount, ref PlainTextPosition plainTextPosition)
	{
		// TODO Uno (UIA): word navigation. WinUI calls
		// CTextBoxHelpers::GetAdjacentWordNavigationBoundaryPosition(container, pos, FindBoundaryType, ...),
		// a wrapper that builds a text backend and calls CSelectionWordBreaker.GetAdjacentWordNavigationBoundary
		// (ported in SelectionWordBreaker.skia.cs). That container-level wrapper (the ISimpleTextBackend bridge)
		// is not ported yet, so word movement is a no-op here rather than guessing a boundary.
		movedCount = 0;
	}

	internal static void ExpandToLine(
		TextPatternRangeEndpoint endPoint,
		TextRangeAdapter pTextRangeAdapter,
		int count,
		out uint pnCount,
		out bool pbIsEmptyLine)
	{
		pnCount = 0;
		pbIsEmptyLine = false;

		if (count == 0)
		{
			return;
		}

		int offset;
		if (endPoint == TextPatternRangeEndpoint.Start)
		{
			offset = pTextRangeAdapter._pStartTextPointer.Offset;
		}
		else
		{
			offset = pTextRangeAdapter._pEndTextPointer.Offset;
		}

		var visibleRanges = pTextRangeAdapter._pTextAdapter.GetVisibleRangesInternal();
		int nLines = visibleRanges.Count;

		int startoffset = 0;
		int endoffset = 0;
		int lineIndex;
		for (lineIndex = 0; lineIndex < nLines; lineIndex++)
		{
			startoffset = visibleRanges[lineIndex]._pStartTextPointer.Offset;
			int deltaOffset = startoffset - endoffset;
			endoffset = visibleRanges[lineIndex]._pEndTextPointer.Offset;
			if (offset >= startoffset - deltaOffset && ((offset < endoffset) || (offset == startoffset)))
			{
				break;
			}
		}

		PlainTextPosition textPosition = default;
		if (count > 0)
		{
			if ((lineIndex >= nLines) || (lineIndex == nLines - 1 && startoffset == endoffset))
			{
				if (startoffset == endoffset)
				{
					pbIsEmptyLine = true;
				}
				pnCount = 0;
				return;
			}
			int newLineIndex = lineIndex + count;
			if (newLineIndex < nLines)
			{
				textPosition = visibleRanges[newLineIndex]._pStartTextPointer.GetPlainTextPosition();
				startoffset = visibleRanges[newLineIndex]._pStartTextPointer.Offset;
				endoffset = visibleRanges[newLineIndex]._pEndTextPointer.Offset;
			}
			else
			{
				textPosition = visibleRanges[nLines - 1]._pEndTextPointer.GetPlainTextPosition();
				startoffset = visibleRanges[nLines - 1]._pStartTextPointer.Offset;
				endoffset = visibleRanges[nLines - 1]._pEndTextPointer.Offset;
				newLineIndex = nLines;
			}
			if (startoffset == endoffset)
			{
				pbIsEmptyLine = true;
			}
			pnCount = (uint)Math.Max(0, newLineIndex - lineIndex);
		}
		else
		{
			int lineBreakerAdjuster = 0;
			if (offset <= startoffset || offset >= endoffset)
			{
				lineBreakerAdjuster = -1;
			}
			if (lineIndex + lineBreakerAdjuster == -1)
			{
				pnCount = 0;
				return;
			}
			int newLineIndex = Math.Min(nLines - 1, Math.Max(0, lineIndex + count + 1 + lineBreakerAdjuster));
			textPosition = visibleRanges[newLineIndex]._pStartTextPointer.GetPlainTextPosition();
			pnCount = (uint)Math.Abs(newLineIndex - lineIndex);
		}

		ValidateAndAdjust(endPoint, textPosition, pTextRangeAdapter, out _);
	}

	internal static void ExpandToParagraph(
		TextPatternRangeEndpoint endPoint,
		TextRangeAdapter pTextRangeAdapter,
		int count,
		out uint pnCount,
		out bool doExecuteForNextUnit)
	{
		pnCount = 0;
		doExecuteForNextUnit = false;

		if (count == 0)
		{
			return;
		}

		int offset;
		if (endPoint == TextPatternRangeEndpoint.Start)
		{
			offset = pTextRangeAdapter._pStartTextPointer.Offset;
		}
		else
		{
			offset = pTextRangeAdapter._pEndTextPointer.Offset;
		}

		var pBlockCollection = TextAdapter.GetBlockCollection(pTextRangeAdapter._pTextOwner);
		if (pBlockCollection is null)
		{
			doExecuteForNextUnit = true;
			return;
		}

		int startoffset = 0;
		int endoffset = 0;
		int numBlocks = pBlockCollection.Count;
		Paragraph? pParagraph = null;
		int paraIndex;
		for (paraIndex = 0; paraIndex < numBlocks; paraIndex++)
		{
			pParagraph = pBlockCollection[paraIndex] as Paragraph;
			if (pParagraph is not null)
			{
				var startPointerWrapper = pParagraph.GetContentStart();
				var endPointerWrapper = pParagraph.GetContentEnd();
				if (startPointerWrapper is null || endPointerWrapper is null)
				{
					continue;
				}
				startoffset = startPointerWrapper.Offset;
				int deltaOffset = startoffset - endoffset;
				endoffset = endPointerWrapper.Offset;
				if (offset >= startoffset - deltaOffset && offset < endoffset)
				{
					break;
				}
			}
		}

		PlainTextPosition textPosition = default;
		if (count >= 0)
		{
			if (paraIndex == numBlocks)
			{
				pnCount = 0;
				return;
			}
			int newParaIndex = Math.Min(numBlocks - 1, paraIndex + count - 1);
			if (newParaIndex >= 0)
			{
				pParagraph = pBlockCollection[newParaIndex] as Paragraph;
				if (pParagraph?.GetContentEnd() is { } endPointerWrapper)
				{
					textPosition = endPointerWrapper.GetPlainTextPosition();
				}
			}
			pnCount = (uint)(Math.Abs(newParaIndex - paraIndex) + 1);
		}
		else
		{
			int paraBreakerAdjuster = 0;
			if (offset <= startoffset || offset >= endoffset)
			{
				paraBreakerAdjuster = -1;
			}
			if (paraIndex + paraBreakerAdjuster == -1)
			{
				pnCount = 0;
				return;
			}
			int newParaIndex = Math.Max(0, paraIndex + count + 1 + paraBreakerAdjuster);
			if (newParaIndex < numBlocks)
			{
				pParagraph = pBlockCollection[newParaIndex] as Paragraph;
				if (pParagraph?.GetContentStart() is { } startPointerWrapper)
				{
					textPosition = startPointerWrapper.GetPlainTextPosition();
				}
			}
			pnCount = (uint)Math.Abs(newParaIndex - paraIndex);
		}

		ValidateAndAdjust(endPoint, textPosition, pTextRangeAdapter, out _);
	}

	internal static void ExpandToPage(TextRangeAdapter pTextRangeAdapter, int count)
		// In XAML TextOM the Document range is the same as the Page range.
		=> ExpandToDocument(pTextRangeAdapter, count);

	internal static void ExpandToDocument(TextRangeAdapter pTextRangeAdapter, int count)
	{
		var documentRangeAdapter = pTextRangeAdapter._pTextAdapter.GetDocumentRange();
		if (documentRangeAdapter is null)
		{
			return;
		}

		pTextRangeAdapter._pStartTextPointer = ClonePointer(documentRangeAdapter._pStartTextPointer);
		pTextRangeAdapter._pEndTextPointer = ClonePointer(documentRangeAdapter._pEndTextPointer);
	}

	// Traverses the n-ary TextElement tree forward/backward looking for a format boundary.
	// TODO Uno (UIA): faithful port of the stack-based tree walk requires CTextElementCollection
	// index/parent navigation (GetInlineCollection / GetItemImpl / IndexOf / InheritanceParent) that
	// is not surfaced on the Uno text tree yet. Until then this returns no boundary, so ExpandToFormat
	// collapses to the document range (the safe degenerate behavior).
	private void TraverseTextElementTreeForFormat(
		TextElement pTextElementStart,
		int direction,
		out TextElement? pAfterBoundaryElement,
		out TextElement? pBeforeBoundaryElement)
	{
		pAfterBoundaryElement = null;
		pBeforeBoundaryElement = pTextElementStart;
	}

	// CTextRangeAdapter::GetParagraphFromTextElement
	internal static Paragraph? GetParagraphFromTextElement(TextElement pElement)
	{
		object? pTempAsDO = pElement;
		while (pTempAsDO is not null
			&& pTempAsDO is not Paragraph
			&& pTempAsDO is not TextBlock)
		{
			pTempAsDO = pTempAsDO.GetParent();
		}

		return pTempAsDO as Paragraph;
	}

	// Retrieves the attribute value for a given TextElement. Properties live either on the TextElement,
	// its containing Paragraph, or the text control itself.
	private object? GetAttributeValueFromTextElement(
		AutomationTextAttributesEnum attributeID,
		TextElement? pContainingElement,
		Paragraph? pParagraph)
	{
		if (pContainingElement is null)
		{
			return null;
		}

		switch (attributeID)
		{
			case AutomationTextAttributesEnum.CapStyleAttribute:
				return Typography.GetCapitals(pContainingElement);
			case AutomationTextAttributesEnum.CultureAttribute:
				return pContainingElement.Language;
			case AutomationTextAttributesEnum.FontNameAttribute:
				return pContainingElement.FontFamily?.Source ?? string.Empty;
			case AutomationTextAttributesEnum.FontSizeAttribute:
				return pContainingElement.FontSize;
			case AutomationTextAttributesEnum.FontWeightAttribute:
				return pContainingElement.FontWeight.Weight;
			case AutomationTextAttributesEnum.ForegroundColorAttribute:
				return pContainingElement.Foreground;
			case AutomationTextAttributesEnum.HorizontalTextAlignmentAttribute:
				return _pTextOwner.HorizontalAlignment;
			case AutomationTextAttributesEnum.IndentationFirstLineAttribute:
				if (pParagraph is not null)
				{
					double textIndent = 0;
					if (_pTextOwner is RichTextBlock rtb)
					{
						textIndent = rtb.TextIndent;
					}
					return textIndent + pParagraph.TextIndent;
				}
				return 0d;
			case AutomationTextAttributesEnum.IsHiddenAttribute:
				return false;
			case AutomationTextAttributesEnum.IsItalicAttribute:
				return pContainingElement.FontStyle is global::Windows.UI.Text.FontStyle.Italic or global::Windows.UI.Text.FontStyle.Oblique;
			case AutomationTextAttributesEnum.IsReadOnlyAttribute:
				return true;
			case AutomationTextAttributesEnum.MarginBottomAttribute:
				return _pTextOwner.Margin.Bottom + (pParagraph?.Margin.Bottom ?? 0);
			case AutomationTextAttributesEnum.MarginLeadingAttribute:
				return _pTextOwner.Margin.Left + (pParagraph?.Margin.Left ?? 0);
			case AutomationTextAttributesEnum.MarginTopAttribute:
				return _pTextOwner.Margin.Top + (pParagraph?.Margin.Top ?? 0);
			case AutomationTextAttributesEnum.MarginTrailingAttribute:
				return _pTextOwner.Margin.Right + (pParagraph?.Margin.Right ?? 0);
			case AutomationTextAttributesEnum.IsSubscriptAttribute:
				return Typography.GetVariants(pContainingElement) == Microsoft.UI.Xaml.FontVariants.Subscript;
			case AutomationTextAttributesEnum.IsSuperscriptAttribute:
				return Typography.GetVariants(pContainingElement) == Microsoft.UI.Xaml.FontVariants.Superscript;
			default:
				// TODO Uno (UIA): UnderlineStyle/StrikethroughStyle/Underline/Strikethrough color attributes
				// require TextFormatting.TextDecorations access on the element. Not surfaced yet.
				return null;
		}
	}

	// TextAttribute-specific comparer.
	private static bool AttributeValueComparer(object? value1, object? value2)
	{
		if (value1 is Media.SolidColorBrush brush1 && value2 is Media.SolidColorBrush brush2)
		{
			return brush1.Color == brush2.Color;
		}

		return Equals(value1, value2);
	}

	// This method determines if the Start pointer of this range is at an empty/degenerate line.
	private void IsAtEmptyLine(out bool pbIsEmptyLine)
	{
		pbIsEmptyLine = false;

		int offset = _pStartTextPointer.Offset;
		var visibleRanges = _pTextAdapter.GetVisibleRangesInternal();
		int nLines = visibleRanges.Count;

		int startoffset = 0;
		int endoffset = 0;
		for (int lineIndex = 0; lineIndex < nLines; lineIndex++)
		{
			startoffset = visibleRanges[lineIndex]._pStartTextPointer.Offset;
			int deltaOffset = startoffset - endoffset;
			endoffset = visibleRanges[lineIndex]._pEndTextPointer.Offset;
			if (offset >= startoffset - deltaOffset && ((offset < endoffset) || (offset == startoffset)))
			{
				break;
			}
		}

		if (startoffset == endoffset)
		{
			pbIsEmptyLine = true;
		}
	}
}
