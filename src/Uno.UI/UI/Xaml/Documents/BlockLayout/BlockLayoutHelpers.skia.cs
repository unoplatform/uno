// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference BlockLayoutHelpers.h, BlockLayoutHelpers.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable
#pragma warning disable CS8600, CS8602, CS8604, CS8618, CS0219, CS0414 // TODO Uno (Stage 5): WIP drafts not yet fully nullable-annotated

using System;
using System.Globalization;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents.RichTextServices;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Windows.UI.Text;
using Windows.Foundation;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Documents.BlockLayout;

internal static class BlockLayoutHelpers
{
	private const char UNICODE_ELLIPSIS = '…';

	//---------------------------------------------------------------------------
	//
	// BlockLayoutHelpers::GetTextFormatter
	//
	// Synopsis:
	//      Gets the TextFormatter instance from the layout engine owner.
	//
	//---------------------------------------------------------------------------
	public static void GetTextFormatter(
		DependencyObject pLayoutOwner,
		out TextFormatter ppTextFormatter)
	{
		MUX_ASSERT(pLayoutOwner != null);

		// NOTE (Uno): WinUI vended a per-control TextFormatter (RichTextBlock/TextBlock::GetTextFormatter,
		// LineServices-backed). On Uno the formatter wraps the Skia ParsedText engine and is a shared
		// stateless singleton, so the owner-type dispatch collapses to the single instance.
		if (pLayoutOwner is RichTextBlock || pLayoutOwner is RichTextBlockOverflow || pLayoutOwner is TextBlock)
		{
			ppTextFormatter = SkiaTextFormatter.Instance;
		}
		else
		{
			MUX_ASSERT(false);
			ppTextFormatter = SkiaTextFormatter.Instance;
		}
	}

	//---------------------------------------------------------------------------
	//
	// BlockLayoutHelpers::ReleaseTextFormatter
	//
	// Synopsis:
	//      Releases the TextFormatter instance.
	//
	//---------------------------------------------------------------------------
	public static void ReleaseTextFormatter(
		DependencyObject pLayoutOwner,
		TextFormatter pTextFormatter)
	{
		MUX_ASSERT(pLayoutOwner != null);

		// NOTE (Uno): ReleaseTextFormatter is a no-op — the Skia formatter is a shared singleton
		// with no per-acquisition state to return.
	}

	//---------------------------------------------------------------------------
	//
	// BlockLayoutHelpers::GetParagraphProperties
	//
	// Synopsis:
	//      Resolves paragraph properties from paragraph owner and control owner.
	//      Both are needed because certain properties e.g. TextWrapping
	//      are not available on CParagraph. Both owners will be the same for
	//      non-paragraph content e.g. TextBlock.
	//
	//---------------------------------------------------------------------------
	public static void GetParagraphProperties(
		DependencyObject? pParagraph,
		DependencyObject pLayoutOwner,
		out TextParagraphProperties ppTextParagraphProperties)
	{
		FontDetails? pFontTypeface = null;
		TextRunProperties pDefaultTextRunProperties;
		TextParagraphProperties pTextParagraphProperties;
		TextAlignment textAlignment;
		DependencyObject? pEffectiveParagraph = pParagraph;

		MUX_ASSERT(pLayoutOwner != null);

		if (pEffectiveParagraph == null)
		{
			// Use the layout owner as the effective paragraph.
			pEffectiveParagraph = pLayoutOwner;
		}

		// Since GetFormatting and GetInheritedAttachedProperties expect one "paragraph" element, use
		// the effective paragraph. GetTextAlignment expects both paragraph and layout owner and will handle the
		// NULL paragraph case, since it is public and called from other places in ParagraphNode. So use original values.
		// TODO Uno (integrate): font context -> FontDetailsCache. WinUI resolved the typeface via
		// CFontFamily::GetFontTypeface(GetFontContext(owner), CFontFaceCriteria(...)); on Uno the font comes from
		// FontDetailsCache.GetFont(fontFamilySource, fontSize, fontWeight, fontStretch, fontStyle).details. The
		// formatting (FontFamily/FontSize/FontWeight/FontStyle/FontStretch/CharacterSpacing/TextDecorations) is read
		// off the effective paragraph's inherited TextElement formatting; GetFormatting / GetInheritedAttachedProperties
		// are not yet ported, so resolve these from the effective paragraph's DPs.
		GetTextAlignment(pParagraph, pLayoutOwner, out textAlignment);

		// TODO Uno (integrate): inherited formatting resolution. The values below should come from the effective
		// paragraph's resolved (inherited) TextElement formatting, not directly from the layout owner. Until
		// GetFormatting is ported, read FontFamily/FontSize/FontWeight/FontStretch/FontStyle/CharacterSpacing/
		// TextDecorations off the effective paragraph (RichTextBlock/TextBlock/Block/TextElement) DPs.
		var fontFamilySource = GetFontFamilySource(pEffectiveParagraph);
		var fontSize = GetFontSize(pEffectiveParagraph);
		var fontWeight = GetFontWeight(pEffectiveParagraph);
		var fontStyle = GetFontStyle(pEffectiveParagraph);
		var fontStretch = GetFontStretch(pEffectiveParagraph);
		var textDecorations = GetTextDecorations(pEffectiveParagraph);
		var characterSpacing = GetCharacterSpacing(pEffectiveParagraph);

		// TODO Uno (integrate): font context -> FontDetailsCache.GetFont(...).details (see note above).
		pFontTypeface = FontDetailsCache.GetFont(fontFamilySource, (float)fontSize, fontWeight, fontStretch, fontStyle).details;

		pDefaultTextRunProperties = new TextRunProperties(
			pFontTypeface,
			// TODO Uno (integrate): CFormatting::GetScaledFontSize(GetContext()->GetFontScale()) — apply the
			// per-context font scale (TextScaleFactor). For now use the raw font size.
			fontSize,
			((uint)textDecorations & (uint)TextDecorations.Underline) > 0,
			((uint)textDecorations & (uint)TextDecorations.Strikethrough) > 0,
			characterSpacing,
			GetForegroundSource(pEffectiveParagraph),
			// TODO Uno (integrate): pFormatting->GetResolvedLanguageStringNoRef() /
			// GetResolvedLanguageListStringNoRef() — resolved Language / LanguageList strings mapped to CultureInfo,
			// and the trailing InheritedProperties argument (Silverlight inherited TextOptions/Typography) which the
			// Uno TextRunProperties ctor omits.
			CultureInfo.CurrentCulture,
			CultureInfo.CurrentCulture);

		pTextParagraphProperties = new TextParagraphProperties(
			GetFlowDirection(pLayoutOwner),
			pDefaultTextRunProperties,
			GetParagraphIndent(pParagraph, pLayoutOwner),
			GetTextWrapping(pLayoutOwner),
			GetTextLineBounds(pLayoutOwner),
			GetTextAlignment(pLayoutOwner));

		pTextParagraphProperties.SetFlags(TextParagraphProperties.Flags.Justify, textAlignment == TextAlignment.Justify);
		pTextParagraphProperties.SetFlags(TextParagraphProperties.Flags.TrimSideBearings, GetOpticalMarginAlignment(pLayoutOwner) == OpticalMarginAlignment.TrimSideBearings);
		pTextParagraphProperties.SetFlags(TextParagraphProperties.Flags.DetermineTextReadingOrderFromContent, GetTextReadingOrder(pLayoutOwner) == TextReadingOrder.DetectFromContent);
		pTextParagraphProperties.SetFlags(TextParagraphProperties.Flags.DetermineAlignmentFromContent, textAlignment == TextAlignment.DetectFromContent);

		ppTextParagraphProperties = pTextParagraphProperties;
	}

	//---------------------------------------------------------------------------
	//
	// BlockLayoutHelpers::CreateCollapsingSymbol
	//
	// Synopsis:
	//      Resolves collapsing properties from paragraph owner and control owner,
	//      and creates collapsing symbol.
	//
	//---------------------------------------------------------------------------
	public static void CreateCollapsingSymbol(
		DependencyObject? pParagraph,
		DependencyObject pLayoutOwner,
		out TextCollapsingSymbol ppCollapsingSymbol)
	{
		FontDetails? pFontTypeface;
		TextRunProperties pDefaultTextRunProperties;
		float collapsingCharWidth = 0.0f;
		DependencyObject? pEffectiveParagraph = pParagraph;

		MUX_ASSERT(pLayoutOwner != null);

		if (pEffectiveParagraph == null)
		{
			// Use the layout owner as the effective paragraph.
			pEffectiveParagraph = pLayoutOwner;
		}

		// TODO Uno (integrate): UNICODE_ELLIPSIS constant ('…'); shared with ParagraphTextSource's
		// UNICODE_* glyph constants (not yet centralized).
		const char pCollapsingChar = UNICODE_ELLIPSIS;

		// TODO Uno (integrate): font context -> FontDetailsCache. As in GetParagraphProperties, WinUI resolved the
		// typeface via CFontFamily::GetFontTypeface(GetFontContext(owner), CFontFaceCriteria(...)). On Uno read the
		// effective paragraph's inherited formatting and resolve the font via FontDetailsCache.GetFont(...).details.
		var fontFamilySource = GetFontFamilySource(pEffectiveParagraph);
		var fontSize = GetFontSize(pEffectiveParagraph);
		var fontWeight = GetFontWeight(pEffectiveParagraph);
		var fontStyle = GetFontStyle(pEffectiveParagraph);
		var fontStretch = GetFontStretch(pEffectiveParagraph);
		var textDecorations = GetTextDecorations(pEffectiveParagraph);
		var characterSpacing = GetCharacterSpacing(pEffectiveParagraph);

		pFontTypeface = FontDetailsCache.GetFont(fontFamilySource, (float)fontSize, fontWeight, fontStretch, fontStyle).details;

		pDefaultTextRunProperties = new TextRunProperties(
			pFontTypeface,
			// TODO Uno (integrate): GetScaledFontSize(GetContext()->GetFontScale()) — apply per-context font scale.
			fontSize,
			((uint)textDecorations & (uint)TextDecorations.Underline) > 0,
			((uint)textDecorations & (uint)TextDecorations.Strikethrough) > 0,
			characterSpacing,
			GetForegroundSource(pEffectiveParagraph),
			// TODO Uno (integrate): resolved Language / LanguageList strings -> CultureInfo (see GetParagraphProperties).
			CultureInfo.CurrentCulture,
			CultureInfo.CurrentCulture);

		// TODO Uno (integrate): CFontTypeface::MapCharacters / IFssFontFace — WinUI mapped the ellipsis to a concrete
		// font face (asserting a single mapped char), then measured its nominal advance via
		// ComputeCollapsingCharacterWidth(pFontFace, ...). On Uno the FontDetails already carries the resolved SKFont;
		// compute the ellipsis advance from it once the FontDetails-based glyph metrics bridge is wired up.
		ComputeCollapsingCharacterWidth(pFontTypeface, pCollapsingChar, pDefaultTextRunProperties, out collapsingCharWidth);

		var pCollapsingWidths = new float[] { collapsingCharWidth };

		// TODO Uno (integrate): TextCollapsingCharacters — the concrete TextCollapsingSymbol carrying the ellipsis
		// char(s), per-char widths, total width, flow direction, default run properties and the resolved font face.
		// Not yet ported; construct it from (pCollapsingChar, 1, pCollapsingWidths, collapsingCharWidth,
		// FlowDirection.LeftToRight, pDefaultTextRunProperties, pFontFace) when available.
		ppCollapsingSymbol = new TextCollapsingCharacters(
			pCollapsingChar,
			1,
			pCollapsingWidths,
			collapsingCharWidth,
			FlowDirection.LeftToRight,
			pDefaultTextRunProperties,
			pFontTypeface);
	}

	//---------------------------------------------------------------------------
	//
	// BlockLayoutHelpers::GetTextAlignment
	//
	// Synopsis:
	//      Reads TextAlignment value from paragraph/control.
	//      TextAlignment is not inherited, so if it's not set on the paragraph,
	//      we have to read from the control anyway.
	//
	//---------------------------------------------------------------------------
	public static void GetTextAlignment(
		DependencyObject? pParagraph,
		DependencyObject pLayoutOwner,
		out TextAlignment pTextAlignment)
	{
		TextAlignment textAlignment = TextAlignment.Left;
		bool paragraphValueSet = false;

		MUX_ASSERT(pLayoutOwner != null);

		// NULL paragraph may be passed in for TextBlock or empty RichTextBlock scenario. In that case look up this value from layout owner directly.
		if (null != pParagraph)
		{
			MUX_ASSERT(pParagraph is Paragraph);
			Paragraph pParagraphLocal = (Paragraph)pParagraph;
			// TODO Uno (integrate): HasLocalOrModifierValue(Block_TextAlignment) — C++ checks whether the property
			// has a local/modifier value before reading it. Uno approximates with ReadLocalValue != UnsetValue.
			if (pParagraphLocal.ReadLocalValue(Block.TextAlignmentProperty) != DependencyProperty.UnsetValue)
			{
				// Paragraph has a local value for this property, so use that.
				textAlignment = pParagraphLocal.TextAlignment;
				paragraphValueSet = true;
			}
		}

		if (!paragraphValueSet)
		{
			if (pLayoutOwner is RichTextBlock pRichTextBlock)
			{
				textAlignment = pRichTextBlock.TextAlignment;
			}
			else if (pLayoutOwner is TextBlock pTextBlock)
			{
				textAlignment = pTextBlock.TextAlignment;
			}
			else
			{
				MUX_ASSERT(false);
			}
		}

		pTextAlignment = textAlignment;
	}

	//---------------------------------------------------------------------------
	//
	// BlockLayoutHelpers::GetTextTrimming
	//
	// Synopsis:
	//      Reads TextTrimming value from control owner. This property is
	//      not exposed at paragraph level.
	//
	//---------------------------------------------------------------------------
	public static TextTrimming GetTextTrimming(
		DependencyObject pLayoutOwner)
	{
		TextTrimming textTrimming = TextTrimming.None;
		MUX_ASSERT(pLayoutOwner != null);

		if (pLayoutOwner is RichTextBlock pRichTextBlock)
		{
			textTrimming = pRichTextBlock.TextTrimming;
		}
		else if (pLayoutOwner is TextBlock pTextBlock)
		{
			textTrimming = pTextBlock.TextTrimming;
		}
		else
		{
			MUX_ASSERT(false);
		}

		return textTrimming;
	}

	//---------------------------------------------------------------------------
	//
	// BlockLayoutHelpers::ShowParagraphEllipsisOnPage
	//
	// Synopsis:
	//      Indicates whether paragraph ellipsis should be displayed on a
	//      particular page.
	//
	//  Notes:
	//      1. There are no checks for TextTrimming here. If TextTrimming
	//         is necessary for paragraph ellipsis to be shown callers should read
	//         the value separately.
	//      2. Check for ParagraphEllipsis is necessary because the page owner may
	//         want text to be trimmed if it overflows horizontally but not if it
	//         doesn't fit vertically - the page may handle this in other ways
	//         e.g. by breaking.
	//
	//  TODO: Remove this - this should be an option passed to
	//  Arrange.
	//
	//---------------------------------------------------------------------------
	public static bool ShowParagraphEllipsisOnPage(
		DependencyObject pPageOwner)
	{
		bool showParagraphEllipsis = false;
		MUX_ASSERT(pPageOwner != null);

		if (pPageOwner is RichTextBlock pRichTextBlock)
		{
			// RichTextBlock will only show paragraph ellipsis if it has no overflow
			// target, otherwise, overflow will go to the target.
			// TODO Uno (integrate): m_pOverflowTarget — RichTextBlock uses OverflowContentTarget DP.
			showParagraphEllipsis = (pRichTextBlock.OverflowContentTarget == null);
		}
		else if (pPageOwner is RichTextBlockOverflow pRichTextBlockOverflow)
		{
			// RichTextBlockOverflow will only show paragraph ellipsis if it has no overflow
			// target, otherwise, overflow will go to the target.
			// TODO Uno (integrate): m_pOverflowTarget — RichTextBlockOverflow uses OverflowContentTarget DP.
			showParagraphEllipsis = (pRichTextBlockOverflow.OverflowContentTarget == null);
		}
		else if (pPageOwner is TextBlock)
		{
			// TextBlock will always show paragraph ellipsis if trimming is on.
			showParagraphEllipsis = true;
		}
		else
		{
			MUX_ASSERT(false);
		}

		return showParagraphEllipsis;
	}

	//---------------------------------------------------------------------------
	//
	// BlockLayoutHelpers::GetPagePadding
	//
	// Synopsis:
	//      Gets the Padding value from the page owner.
	//
	//---------------------------------------------------------------------------
	public static void GetPagePadding(
		DependencyObject pPageOwner,
		out Thickness pPadding)
	{
		Thickness padding = new Thickness(0.0, 0.0, 0.0, 0.0);

		MUX_ASSERT(pPageOwner != null);

		if (pPageOwner is RichTextBlock pRichTextBlock)
		{
			padding = pRichTextBlock.Padding;
		}
		else if (pPageOwner is RichTextBlockOverflow pRichTextBlockOverflow)
		{
			padding = pRichTextBlockOverflow.Padding;
		}
		else if (pPageOwner is TextBlock pTextBlock)
		{
			padding = pTextBlock.Padding;
		}
		else
		{
			MUX_ASSERT(false);
		}

		pPadding = padding;
	}

	//---------------------------------------------------------------------------
	//
	// BlockLayoutHelpers::GetLayoutRoundingHeightAdjustment
	//
	// Synopsis:
	//      Gets the height adjustment value from the page owner.
	//
	//---------------------------------------------------------------------------
	public static void GetLayoutRoundingHeightAdjustment(
		DependencyObject pPageOwner,
		out float pLayoutRoundingHeightAdjustment)
	{
		float layoutRoundingHeightAdjustment = 0.0f;

		MUX_ASSERT(pPageOwner != null);

		// TODO Uno (integrate): m_layoutRoundingHeightAdjustment — RichTextBlock / RichTextBlockOverflow / TextBlock
		// store a layout-rounding height adjustment computed during measure. There is no Uno DP/field for this yet,
		// so all branches currently fall through to 0. Wire this up when the rounding adjustment is ported.
		if (pPageOwner is RichTextBlock || pPageOwner is RichTextBlockOverflow || pPageOwner is TextBlock)
		{
			layoutRoundingHeightAdjustment = 0.0f;
		}
		else
		{
			MUX_ASSERT(false);
		}

		pLayoutRoundingHeightAdjustment = layoutRoundingHeightAdjustment;
	}

	//---------------------------------------------------------------------------
	//
	// BlockLayoutHelpers::GetBlockMargin
	//
	// Synopsis:
	//      Gets the Margin value from the block element.
	//
	//---------------------------------------------------------------------------
	public static void GetBlockMargin(
		DependencyObject? pBlock,
		out Thickness pMargin)
	{
		Thickness margin = new Thickness(0.0, 0.0, 0.0, 0.0);

		// NULL pBlock may be passed in for TextBlock or empty RichTextBlock scenario.
		if (pBlock != null &&
			pBlock is Block pBlockLocal)
		{
			margin = pBlockLocal.Margin;
		}

		pMargin = margin;
	}

	//------------------------------------------------------------------------
	//
	// BlockLayoutHelpers::GetElementBaseline
	//
	//  Synopsis:
	//      Retrieves the baseline offset of a given UIElement.
	//
	//------------------------------------------------------------------------
	public static void GetElementBaseline(
		UIElement pElement,
		out float pBaseline)
	{
		MUX_ASSERT(pElement != null);

		if (pElement is RichTextBlock pRichTextBlock)
		{
			// TODO Uno (integrate): CRichTextBlock::GetBaselineOffset.
			pRichTextBlock.GetBaselineOffset(out pBaseline);
		}
		else if (pElement is RichTextBlockOverflow pRichTextBlockOverflow)
		{
			// TODO Uno (integrate): CRichTextBlockOverflow::GetBaselineOffset.
			pRichTextBlockOverflow.GetBaselineOffset(out pBaseline);
		}
		else if (pElement is TextBlock pTextBlock)
		{
			// TODO Uno (integrate): CTextBlock::GetBaselineOffset.
			pTextBlock.GetBaselineOffset(out pBaseline);
		}
		else
		{
			// TODO Uno (integrate): CUIElement::DesiredSize — fall back to the element's measured desired height.
			pBaseline = (float)pElement.DesiredSize.Height;
		}
	}

	//---------------------------------------------------------------------------
	//
	// BlockLayoutHelpers::GetLineStackingInfo
	//
	// Synopsis:
	//      Reads LineHeight/ LineStacking info value from paragraph/control.
	//      These properties are not inherited, so if not set on the paragraph,
	//      we have to read from the control anyway.
	//
	//---------------------------------------------------------------------------
	public static void GetLineStackingInfo(
		DependencyObject? pParagraph,
		DependencyObject pLayoutOwner,
		out LineStackingStrategy pLineStackingStrategy,
		out float pDefaultFontBaseline,
		out float pDefaultFontLineAdvance,
		out float pLineHeight)
	{
		bool paragraphLineHeightSet = false;
		bool paragraphLineStackingStrategySet = false;
		DependencyObject? pEffectiveParagraph = pParagraph;

		pLineStackingStrategy = LineStackingStrategy.MaxHeight;
		pDefaultFontBaseline = 0.0f;
		pDefaultFontLineAdvance = 0.0f;
		pLineHeight = 0.0f;

		MUX_ASSERT(pLayoutOwner != null);

		if (pEffectiveParagraph == null)
		{
			// Use the layout owner as the effective paragraph.
			pEffectiveParagraph = pLayoutOwner;
		}

		// Formatting can always be read from the paragraph, and font context from the control.
		{
			var fontFamilySource = GetFontFamilySource(pEffectiveParagraph);
			var fontSize = GetFontSize(pEffectiveParagraph);
			var fontWeight = GetFontWeight(pEffectiveParagraph);
			var fontStyle = GetFontStyle(pEffectiveParagraph);
			var fontStretch = GetFontStretch(pEffectiveParagraph);

			var details = FontDetailsCache.GetFont(fontFamilySource, (float)fontSize, fontWeight, fontStretch, fontStyle).details;

			// CFontFamily::GetTextLineBoundsMetrics — default baseline / line advance for the resolved
			// font, constrained by the owner's TextLineBounds.
			(pDefaultFontBaseline, pDefaultFontLineAdvance) = details.GetTextLineBoundsMetrics(GetTextLineBounds(pLayoutOwner));
		}

		// LineHeight and LineStackingStrategy must be read from the paragraph if set locally, the control otherwise.
		// NULL paragraph may be passed in for TextBlock or empty RichTextBlock scenario. In that case look up this value from layout owner directly.
		if (null != pParagraph)
		{
			MUX_ASSERT(pParagraph is Paragraph);
			Paragraph pParagraphLocal = (Paragraph)pParagraph;

			// TODO Uno (integrate): HasLocalOrModifierValue(Block_LineHeight) — approximated with ReadLocalValue.
			if (pParagraphLocal.ReadLocalValue(Block.LineHeightProperty) != DependencyProperty.UnsetValue)
			{
				// Paragraph has a local value for this property, so use that.
				pLineHeight = (float)pParagraphLocal.LineHeight;
				paragraphLineHeightSet = true;
			}

			// TODO Uno (integrate): HasLocalOrModifierValue(Block_LineStackingStrategy) — approximated with ReadLocalValue.
			if (pParagraphLocal.ReadLocalValue(Block.LineStackingStrategyProperty) != DependencyProperty.UnsetValue)
			{
				pLineStackingStrategy = pParagraphLocal.LineStackingStrategy;
				paragraphLineStackingStrategySet = true;
			}
		}

		// If values are not set on paragraph, read them from the control. CRichTextBlock is the only
		// control where this is currently possible.
		if (!(paragraphLineStackingStrategySet) ||
			!(paragraphLineHeightSet))
		{
			if (pLayoutOwner is RichTextBlock pRichTextBlock)
			{
				if (!paragraphLineStackingStrategySet)
				{
					pLineStackingStrategy = pRichTextBlock.LineStackingStrategy;
				}
				if (!paragraphLineHeightSet)
				{
					pLineHeight = (float)pRichTextBlock.LineHeight;
				}
			}
			else if (pLayoutOwner is TextBlock pTextBlock)
			{
				if (!paragraphLineStackingStrategySet)
				{
					pLineStackingStrategy = pTextBlock.LineStackingStrategy;
				}
				if (!paragraphLineHeightSet)
				{
					pLineHeight = (float)pTextBlock.LineHeight;
				}
			}
			else
			{
				MUX_ASSERT(false);
			}
		}
	}

	//---------------------------------------------------------------------------
	//
	// BlockLayoutHelpers::GetFontContext
	//
	// Synopsis:
	//      Rerieves font context value from control owner. Font context is
	//      not available at paragraph level.
	//
	//---------------------------------------------------------------------------
	// TODO Uno (integrate): font context -> FontDetailsCache. WinUI returned the owner's CFontContext (used to seed
	// font typeface resolution). On Uno font resolution flows through FontDetailsCache directly off the owner's
	// inherited formatting, so there is no separate CFontContext object. The shim returns the owner so callers
	// (e.g. ParagraphTextSource.GetFontContext) keep their structure; replace with the real bridge when defined.
	public static FontContext GetFontContext(
		DependencyObject pLayoutOwner)
	{
		FontContext? pFontContext = null;

		MUX_ASSERT(pLayoutOwner != null);

		if (pLayoutOwner is RichTextBlock pRichTextBlock)
		{
			// TODO Uno (integrate): CRichTextBlock::GetFontContext.
			pFontContext = new FontContext(); // TODO Uno (Stage 4): real font-context bridge
		}
		else if (pLayoutOwner is TextBlock pTextBlock)
		{
			// TODO Uno (integrate): CTextBlock::GetFontContext.
			pFontContext = new FontContext(); // TODO Uno (Stage 4): real font-context bridge
		}
		else
		{
			MUX_ASSERT(false);
		}

		return pFontContext!;
	}

	//---------------------------------------------------------------------------
	//
	// BlockLayoutHelpers::GetFormatting
	//
	// Synopsis:
	//      Reads formatting properties from paragraph owner. All properties in
	//      CFormatting are inherited by TextElement so they are always available
	//      at the paragraph level.
	//
	//---------------------------------------------------------------------------
	// TODO Uno (integrate): GetFormatting — WinUI returned the resolved (inherited) TextFormatting struct via
	// CDependencyObject::GetTextFormatting. The Uno TextFormatting equivalent (resolved font/decoration/language for a
	// TextElement) is not yet ported, so the property-resolution helpers below read the individual inherited DPs off
	// the effective paragraph instead. Restore the single GetFormatting call path once TextFormatting lands.
	private static object? GetFormatting(
		DependencyObject pParagraph)
	{
		MUX_ASSERT(pParagraph is Paragraph ||
			pParagraph is TextBlock ||
			pParagraph is RichTextBlock);

		// TODO Uno (integrate): CDependencyObject::GetTextFormatting(out ppFormatting).
		throw new NotImplementedException();
	}

	//---------------------------------------------------------------------------
	//
	// BlockLayoutHelpers::GetInheritedAttachedProperties
	//
	// Synopsis:
	//      Reads formatting properties from paragraph owner. All properties in
	//      CFormatting are inherited by TextElement so they are always available
	//      at the paragraph level.
	//
	//---------------------------------------------------------------------------
	// TODO Uno (integrate): GetInheritedAttachedProperties — WinUI returned the resolved InheritedProperties
	// (TextOptions / Typography). Not yet ported; the Uno TextRunProperties ctor omits the InheritedProperties
	// argument. Restore once InheritedProperties is brought over (see TextRunProperties.skia.cs note).
	private static InheritedProperties GetInheritedAttachedProperties(
		DependencyObject pParagraph)
	{
		MUX_ASSERT(pParagraph is Paragraph ||
			pParagraph is TextBlock ||
			pParagraph is RichTextBlock);

		// TODO Uno (integrate): CDependencyObject::GetInheritedProperties(out ppInheritedAttachedProperties).
		throw new NotImplementedException();
	}

	//---------------------------------------------------------------------------
	//
	// BlockLayoutHelpers::GetFlowDirection
	//
	//---------------------------------------------------------------------------
	public static FlowDirection GetFlowDirection(
		DependencyObject pLayoutOwner)
	{
		FlowDirection flowDirection = FlowDirection.LeftToRight;

		MUX_ASSERT(pLayoutOwner != null);

		// NOTE (Uno): WinUI read m_pTextFormatting->m_nFlowDirection after EnsureTextFormattingForRead. On Uno
		// FlowDirection is a FrameworkElement DP, so read it directly off the owning control.
		if (pLayoutOwner is RichTextBlock pRichTextBlock)
		{
			flowDirection = pRichTextBlock.FlowDirection;
		}
		else if (pLayoutOwner is TextBlock pTextBlock)
		{
			flowDirection = pTextBlock.FlowDirection;
		}
		else
		{
			MUX_ASSERT(false);
		}

		return flowDirection;
	}

	//---------------------------------------------------------------------------
	//
	// BlockLayoutHelpers::GetTextWrapping
	//
	//---------------------------------------------------------------------------
	private static TextWrapping GetTextWrapping(
		DependencyObject pLayoutOwner)
	{
		TextWrapping textWrapping = TextWrapping.NoWrap;

		MUX_ASSERT(pLayoutOwner != null);

		if (pLayoutOwner is RichTextBlock pRichTextBlock)
		{
			textWrapping = pRichTextBlock.TextWrapping;
		}
		else if (pLayoutOwner is TextBlock pTextBlock)
		{
			textWrapping = pTextBlock.TextWrapping;
		}
		else
		{
			MUX_ASSERT(false);
		}

		return textWrapping;
	}

	//---------------------------------------------------------------------------
	//
	// BlockLayoutHelpers::GetParagraphIndent
	//
	//---------------------------------------------------------------------------
	private static float GetParagraphIndent(
		DependencyObject? pParagraph,
		DependencyObject pLayoutOwner)
	{
		float indent = 0.0f;
		bool paragraphValueSet = false;

		MUX_ASSERT(pLayoutOwner != null);

		// NULL paragraph may be passed in for TextBlock or empty RichTextBlock scenario. In that case look up this value from layout owner directly.
		if (pParagraph != null)
		{
			MUX_ASSERT(pParagraph is Paragraph);
			Paragraph pParagraphLocal = (Paragraph)pParagraph;
			// TODO Uno (integrate): HasLocalOrModifierValue(Paragraph_TextIndent) — approximated with ReadLocalValue.
			if (pParagraphLocal.ReadLocalValue(Paragraph.TextIndentProperty) != DependencyProperty.UnsetValue)
			{
				// Paragraph has a local value for this property, so use that.
				indent = (float)pParagraphLocal.TextIndent;
				paragraphValueSet = true;
			}
		}

		if (!paragraphValueSet)
		{
			// TextBlock does not support TextiNDENT
			if (pLayoutOwner is RichTextBlock pRichTextBlock)
			{
				indent = (float)pRichTextBlock.TextIndent;
			}
		}

		return indent;
	}


	//---------------------------------------------------------------------------
	//
	// BlockLayoutHelpers::GetIsColorFontEnabled
	//
	//---------------------------------------------------------------------------
	public static bool GetIsColorFontEnabled(
		DependencyObject pLayoutOwner)
	{
		bool isColorFontEnabled = false;

		MUX_ASSERT(pLayoutOwner != null);

		// TODO Uno (integrate): IsColorFontEnabled — RichTextBlock/TextBlock expose IsColorFontEnabled in WinUI but
		// there is no Uno DP for it yet. All branches currently return the WinUI default (false). Wire to the DP
		// once IsColorFontEnabled is ported.
		if (pLayoutOwner is RichTextBlock || pLayoutOwner is TextBlock)
		{
			isColorFontEnabled = false;
		}
		else
		{
			MUX_ASSERT(false);
		}

		return isColorFontEnabled;
	}


	public static void ComputeCollapsingCharacterWidth(
		FontDetails pFontFace,
		char pCollapsingChar,
		TextRunProperties pTextRunProperties,
		out float pCollapsingCharWidth)
	{
		ushort uGlyphId = 0;
		float eAdvance = 0.0f;
		uint character = pCollapsingChar;

		// TODO Uno (integrate): IFssFontFace glyph metrics — WinUI determined the nominal advance width of the
		// collapsing character via GetGlyphIndices + GetDesignGlyphMetrics, then converted from design units to
		// 96ths of an inch at the run's font size:
		//   eAdvance = glyphMetrics.AdvanceWidth * runFontSize / fontMetrics.DesignUnitsPerEm.
		// On Uno read the advance from pFontFace (FontDetails/SKFont) for the codepoint. Placeholder leaves 0.
		if (pFontFace != null)
		{
			// Determine the nominal advance width of the collapsing character
			// Convert the advance width from size independent font units to
			// 96ths of an inch at the specified font size.
			eAdvance = 0.0f;
		}

		// All done. Return the advance width of the collapsing character.
		pCollapsingCharWidth = eAdvance;
	}

	//---------------------------------------------------------------------------
	//
	// BlockLayoutHelpers::GetOpticalMarginAlignment
	//
	// Synopsis:
	//      Reads OpticalMarginAlignment value from control owner.
	//
	//---------------------------------------------------------------------------
	public static OpticalMarginAlignment GetOpticalMarginAlignment(
		DependencyObject pLayoutOwner)
	{
		OpticalMarginAlignment opticalMarginAlignment = OpticalMarginAlignment.None;
		MUX_ASSERT(pLayoutOwner != null);

		// TODO Uno (integrate): OpticalMarginAlignment — RichTextBlock/TextBlock expose OpticalMarginAlignment in
		// WinUI but there is no Uno DP for it yet. All branches currently return the WinUI default (None). Wire to
		// the DP once OpticalMarginAlignment is ported.
		if (pLayoutOwner is RichTextBlock || pLayoutOwner is TextBlock)
		{
			opticalMarginAlignment = OpticalMarginAlignment.None;
		}
		else
		{
			MUX_ASSERT(false);
		}

		return opticalMarginAlignment;
	}

	//---------------------------------------------------------------------------
	//
	// BlockLayoutHelpers::GetTextLineBounds
	//
	//---------------------------------------------------------------------------
	private static TextLineBounds GetTextLineBounds(
		DependencyObject pLayoutOwner)
	{
		TextLineBounds textLineBounds = TextLineBounds.Full;

		MUX_ASSERT(pLayoutOwner != null);

		if (pLayoutOwner is RichTextBlock pRichTextBlock)
		{
			textLineBounds = pRichTextBlock.TextLineBounds;
		}
		else if (pLayoutOwner is TextBlock pTextBlock)
		{
			textLineBounds = pTextBlock.TextLineBounds;
		}
		else
		{
			MUX_ASSERT(false);
		}

		return textLineBounds;
	}

	//---------------------------------------------------------------------------
	//
	// BlockLayoutHelpers::GetTextReadingOrder
	//
	// Synopsis:
	//      Reads TextReadingOrder value from control owner.
	//
	//---------------------------------------------------------------------------
	public static TextReadingOrder GetTextReadingOrder(
		DependencyObject pLayoutOwner)
	{
		TextReadingOrder textReadingOrder = TextReadingOrder.Default;
		MUX_ASSERT(pLayoutOwner != null);

		// TODO Uno (integrate): TextReadingOrder — RichTextBlock/TextBlock expose TextReadingOrder in WinUI but there
		// is no Uno DP for it yet. All branches currently return the WinUI default (Default). Wire to the DP once
		// TextReadingOrder is ported.
		if (pLayoutOwner is RichTextBlock || pLayoutOwner is TextBlock)
		{
			textReadingOrder = TextReadingOrder.Default;
		}
		else
		{
			MUX_ASSERT(false);
		}

		return textReadingOrder;
	}

	private static TextAlignment GetTextAlignment(DependencyObject layoutOwner)
	{
		TextAlignment textAlignment = TextAlignment.Left;

		if (layoutOwner is RichTextBlock pRichTextBlock)
		{
			textAlignment = pRichTextBlock.TextAlignment;
		}
		else if (layoutOwner is TextBlock pTextBlock)
		{
			textAlignment = pTextBlock.TextAlignment;
		}
		else
		{
			MUX_ASSERT(false);
		}

		return textAlignment;
	}

	// NOTE (Uno): The nodes (BlockNode/ParagraphNode) call IsCloseReal for layout-bypass float comparisons.
	// WinUI defined this as IsCloseReal in xcpmath; ported here so BlockLayoutHelpers stays the single helper home.
	public static bool IsCloseReal(double a, double b) => Math.Abs(a - b) < 0.0001;

	#region Inherited-formatting resolution helpers (Uno)

	// TODO Uno (integrate): inherited TextFormatting resolution. These read the individual inherited DPs off the
	// "effective paragraph" element to stand in for the not-yet-ported GetFormatting(...) struct. They cover the
	// RichTextBlock / TextBlock / Block / TextElement cases that can act as the effective paragraph. Replace with a
	// single resolved TextFormatting read once that is brought over.

	private static string? GetFontFamilySource(DependencyObject element) => element switch
	{
		RichTextBlock rtb => rtb.FontFamily?.Source,
		TextBlock tb => tb.FontFamily?.Source,
		TextElement te => te.FontFamily?.Source,
		_ => null,
	};

	private static double GetFontSize(DependencyObject element) => element switch
	{
		RichTextBlock rtb => rtb.FontSize,
		TextBlock tb => tb.FontSize,
		TextElement te => te.FontSize,
		_ => 14.0,
	};

	private static FontWeight GetFontWeight(DependencyObject element) => element switch
	{
		RichTextBlock rtb => rtb.FontWeight,
		TextBlock tb => tb.FontWeight,
		TextElement te => te.FontWeight,
		_ => FontWeights.Normal,
	};

	private static FontStyle GetFontStyle(DependencyObject element) => element switch
	{
		RichTextBlock rtb => rtb.FontStyle,
		TextBlock tb => tb.FontStyle,
		TextElement te => te.FontStyle,
		_ => FontStyle.Normal,
	};

	private static FontStretch GetFontStretch(DependencyObject element) => element switch
	{
		RichTextBlock rtb => rtb.FontStretch,
		TextBlock tb => tb.FontStretch,
		TextElement te => te.FontStretch,
		_ => FontStretch.Normal,
	};

	private static int GetCharacterSpacing(DependencyObject element) => element switch
	{
		RichTextBlock rtb => rtb.CharacterSpacing,
		TextBlock tb => tb.CharacterSpacing,
		TextElement te => te.CharacterSpacing,
		_ => 0,
	};

	private static TextDecorations GetTextDecorations(DependencyObject element) => element switch
	{
		RichTextBlock rtb => rtb.TextDecorations,
		TextBlock tb => tb.TextDecorations,
		TextElement te => te.TextDecorations,
		_ => TextDecorations.None,
	};

	private static WeakReference<DependencyObject> GetForegroundSource(DependencyObject element)
		// WinUI used xref::get_weakref(pEffectiveParagraph) as the foreground brush source.
		=> new WeakReference<DependencyObject>(element);

	#endregion
}
