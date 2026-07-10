// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ParagraphTextSource.h, ParagraphTextSource.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable
#pragma warning disable CS8600, CS8602, CS8604, CS8618, CS0219, CS0414 // TODO Uno (Stage 5): WIP drafts not yet fully nullable-annotated

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents.RichTextServices;
using Windows.UI.Text;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Documents.BlockLayout;

//---------------------------------------------------------------------------
//
//  ParagraphTextSource
//
//  TextSource for Paragraph element. Can also be used for TextBlock since
//  both Paragraph and TextBlock are based on InlineCollection content model.
//
//---------------------------------------------------------------------------
// NOTE (Uno): ParagraphTextSource must implement BOTH the WinUI TextSource contract
// AND the Uno-specific ISkiaParagraphSource bridge that feeds the Skia ParsedText
// engine (SkiaTextFormatter reads GetLeafInlines/DefaultLineHeight/LineHeight/
// LineStackingStrategy off this same object).
internal sealed class ParagraphTextSource : TextSource, ISkiaParagraphSource
{
	private const char UNICODE_CARRIAGE_RETURN = '\u000D';
	private const char UNICODE_LINE_FEED = '\u000A';
	private const char UNICODE_CHARACTER_TABULATION = '\u0009';
	private const char UNICODE_PARAGRAPH_SEPARATOR = '\u2029';
	private static bool IS_TRAILING_SURROGATE(char c) => char.IsLowSurrogate(c);

	private readonly Paragraph? m_pParagraph;
	private readonly FrameworkElement m_pContentOwner;
	private ParagraphNode? m_pParagraphNode;

	// Creates an instance of this ParagraphTextSource. If the owning control is
	// a TextBlock, pContent may be null since the TextBlock acts as a paragraph in this case.
	public ParagraphTextSource(
		Paragraph? pParagraph,
		FrameworkElement pContentOwner)
	{
		m_pParagraph = pParagraph;
		m_pContentOwner = pContentOwner;
		m_pParagraphNode = null;
	}

	//---------------------------------------------------------------------------
	//
	//  Member:
	//      ParagraphTextSource::GetTextRun
	//
	//  Synopsis:
	//      Fetches one run of text.
	//
	//---------------------------------------------------------------------------
	public override TextRun GetTextRun(uint characterIndex)
		// TODO Uno (Stage 4): WinUI backing-store run model. The Skia render path lays out
		// the whole paragraph from ISkiaParagraphSource.GetLeafInlines, so GetTextRun is not
		// exercised yet; the faithful port lands with the element model (ITextContainer/GetRun).
		=> throw new NotSupportedException("ParagraphTextSource.GetTextRun is not yet ported; the Skia path uses ISkiaParagraphSource.");

	//---------------------------------------------------------------------------
	//
	//  Member:
	//      ParagraphTextSource::GetEmbeddedElementHost
	//
	//  Synopsis:
	//      Get the embedded element host for TextSource in the current
	//      formatting context.
	//
	//---------------------------------------------------------------------------
	public override IEmbeddedElementHost? GetEmbeddedElementHost() => m_pParagraphNode!.GetEmbeddedElementHost();

	//---------------------------------------------------------------------------
	//
	//  Member:
	//      ParagraphTextSource::SetParagraphNode
	//
	//  Synopsis:
	//      Sets the embedded element host for TextSource in the current
	//      formatting context.
	//
	//---------------------------------------------------------------------------
	public void SetParagraphNode(ParagraphNode pNode) => m_pParagraphNode = pNode;

	// Checks if a position is within a surrogate pair or CRLF sequence.
	public static bool IsInSurrogateCRLF(
		InlineCollection inlines,
		uint characterIndex)
	{
		uint length = 0;
		ReadOnlyMemory<char> pCharacters = default;
		uint runLength = 0;
		bool isInSurrogateCRLF = false;

		// TODO Uno (integrate): CInlineCollection::GetPositionCount / GetRun.
		inlines.GetPositionCount(out length);
		MUX_ASSERT(characterIndex <= length);

		inlines.GetRun(characterIndex, out _, out _, out _, out _, out pCharacters, out runLength);
		if (!pCharacters.IsEmpty &&
			runLength > 0)
		{
			if (IS_TRAILING_SURROGATE(pCharacters.Span[0]))
			{
				isInSurrogateCRLF = true;
			}
			else if (pCharacters.Span[0] == UNICODE_LINE_FEED && characterIndex > 0)
			{
				// There's a LF here.  Go back one character and look for CR.
				ReadOnlyMemory<char> pLookForCR = default;
				uint cLookForCR = 0;

				inlines.GetRun(characterIndex - 1, out _, out _, out _, out _, out pLookForCR, out cLookForCR);
				if (cLookForCR > 0
					&& !pLookForCR.IsEmpty
					&& pLookForCR.Span[0] == UNICODE_CARRIAGE_RETURN)
				{
					// We are in the middle of a CR+LF sequence.
					isInSurrogateCRLF = true;
				}
			}
		}
		return isInSurrogateCRLF;
	}

	//---------------------------------------------------------------------------
	//
	//  Member:
	//      ParagraphTextSource::IsInSurrogateCRLF
	//
	//  Synopsis:
	//      Checks if a position is within a surrogate pair or CRLF sequence.
	//
	//---------------------------------------------------------------------------
	public bool IsInSurrogateCRLF(uint characterIndex)
	{
		InlineCollection? pInlines = null;

		// m_pParagraph can be NULL for a content-free RichTextBlock or for a TextBlock.
		// For the TextBlock case, use the TextBlock's InlineCollection. For empty RichTextBlock, inlines should be considered NULL for this logic.
		if (m_pParagraph != null)
		{
			pInlines = m_pParagraph.Inlines;
		}
		else if (m_pContentOwner is TextBlock pTextBlock)
		{
			pInlines = pTextBlock.Inlines;
		}

		if (pInlines != null)
		{
			return IsInSurrogateCRLF(pInlines, characterIndex);
		}
		return false;
	}

	//---------------------------------------------------------------------------
	//
	//  Member:
	//      ParagraphTextSource::SplitAtFormatControlCharacter
	//
	//  Synopsis:
	//      Checks for formatting control characters requiring special runs:
	//      CR/LF/Tab and splits the content, creating a run for the control
	//      character.
	//
	//---------------------------------------------------------------------------
	// (removed) run-model helper — Stage 4


	// (removed) run-model helper — Stage 4


	//---------------------------------------------------------------------------
	//
	//  Member:
	//      ParagraphTextSource::GetDirectionalControl
	//
	//  Synopsis:
	//      Gets DirectionalControl change caused by a TextElement if it has a flow direction different than
	//      its parent TextElement.
	//
	//---------------------------------------------------------------------------
	// (removed) run-model helper — Stage 4


	//---------------------------------------------------------------------------
	//
	//  Member:
	//      ParagraphTextSource::GetFontContext
	//
	//---------------------------------------------------------------------------
	// TODO Uno (integrate): BlockLayoutHelpers::GetFontContext — returns the CFontContext for the owner.
	// (removed) GetFontContext — run-model helper, Stage 4

	#region ISkiaParagraphSource (Uno bridge to the Skia ParsedText engine)

	// TODO Uno (integrate): ISkiaParagraphSource feeds the Skia text formatter. These members
	// project the same paragraph/inline content GetTextRun walks above into the whole-paragraph
	// input ParsedText expects (leaf inlines + line metrics). Wire them to the leaf inline list of
	// m_pParagraph (or the TextBlock inlines) and to BlockLayoutHelpers line-stacking info.

	Inline[] ISkiaParagraphSource.GetLeafInlines()
		=> m_pParagraph?.Inlines.TraversedTree.leafTree ?? Array.Empty<Inline>();

	// Mirrors the InlineUIContainer branch of ParagraphTextSource::GetTextRun: an open-nesting
	// InlineUIContainer yields a PageHostedObjectRun, whose Format measures the child against the
	// embedded element host and caches its size and baseline on the container.
	IReadOnlyDictionary<InlineUIContainer, (ObjectRun Run, ObjectRunMetrics Metrics)>? ISkiaParagraphSource.FormatInlineObjects(float paragraphWidth)
	{
		Dictionary<InlineUIContainer, (ObjectRun Run, ObjectRunMetrics Metrics)>? inlineObjects = null;
		uint characterIndex = 0;

		foreach (var inline in ((ISkiaParagraphSource)this).GetLeafInlines())
		{
			if (inline is InlineUIContainer pUIContainer)
			{
				TextRunProperties pTextProperties = new(
					pUIContainer.FontInfo,
					pUIContainer.FontSize,
					(pUIContainer.TextDecorations & TextDecorations.Underline) != 0,
					(pUIContainer.TextDecorations & TextDecorations.Strikethrough) != 0,
					pUIContainer.CharacterSpacing,
					new WeakReference<DependencyObject>(pUIContainer),
					CultureInfo.CurrentCulture,
					CultureInfo.CurrentCulture);

				PageHostedObjectRun pTextRun = new(pUIContainer, characterIndex, pTextProperties);
				pTextRun.Format(this, paragraphWidth, default, out var metrics);

				inlineObjects ??= new();
				inlineObjects[pUIContainer] = (pTextRun, metrics);

				// InlineUIContainer only has 2 positions - Open/Close.
				characterIndex += 2;
			}
			else
			{
				characterIndex += (uint)inline.GetText().Length;
			}
		}

		return inlineObjects;
	}

	float ISkiaParagraphSource.DefaultLineHeight
		=> (float)((m_pContentOwner as Microsoft.UI.Xaml.Controls.RichTextBlock)?.FontSize ?? 14.0);

	float ISkiaParagraphSource.LineHeight
	{
		get
		{
			if (m_pParagraph is { } paragraph && paragraph.IsDependencyPropertySet(Block.LineHeightProperty))
			{
				return (float)paragraph.LineHeight;
			}
			return (float)((m_pContentOwner as Microsoft.UI.Xaml.Controls.RichTextBlock)?.LineHeight ?? 0.0);
		}
	}

	LineStackingStrategy ISkiaParagraphSource.LineStackingStrategy
	{
		get
		{
			if (m_pParagraph is { } paragraph && paragraph.IsDependencyPropertySet(Block.LineStackingStrategyProperty))
			{
				return paragraph.LineStackingStrategy;
			}
			return (m_pContentOwner as Microsoft.UI.Xaml.Controls.RichTextBlock)?.LineStackingStrategy ?? LineStackingStrategy.MaxHeight;
		}
	}

	#endregion
}
