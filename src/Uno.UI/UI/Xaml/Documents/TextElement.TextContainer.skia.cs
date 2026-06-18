// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TextElement.cpp / TextFormatting.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using System.Globalization;
using static Microsoft.UI.Xaml.Controls._Tracing;
using TypographyAttachedProperties = Microsoft.UI.Xaml.Documents.Typography;

namespace Microsoft.UI.Xaml.Documents;

partial class TextElement
{
	//------------------------------------------------------------------------
	//
	//  CTextElement::GetPositionCount
	//
	//  Base class always returns 2 for start/end edges. Elements with different
	//  counts/content override.
	//
	//------------------------------------------------------------------------
	internal virtual void GetPositionCount(out uint pcPositions) => pcPositions = 2;

	//------------------------------------------------------------------------
	//
	//  CTextElement::GetContainingElement
	//
	//  Retrieves the containing nested text element from a character position.
	//
	//------------------------------------------------------------------------
	internal virtual void GetContainingElement(
		uint characterPosition,
		out TextElement? ppContainingElement)
	{
		GetPositionCount(out var cPositions);
		MUX_ASSERT(characterPosition < cPositions);

		// 0 is viewed as the element start and is not technically contained in the element.
		// E.g. an empty TextElement has a position count of 2: 0<Element>1</Element>2
		// 0 is before the element start, 2 is after the end, and only 1 is contained. So offsets
		// > 0 and < position count are contained by the element.
		if (characterPosition > 0)
		{
			ppContainingElement = this;
		}
		else
		{
			ppContainingElement = null;
		}
	}

	// Inherited property support (CTextElement::m_pTextFormatting). The resolved text-formatting
	// snapshot for this element, realized on demand by EnsureTextFormattingForRead.
	internal RichTextServices.TextFormatting? m_pTextFormatting;

	//------------------------------------------------------------------------
	//
	//  CTextElement::GetOffsetForEdge
	//
	//  Gets the offset corresponding to an element edge.
	//
	//------------------------------------------------------------------------
	internal void GetOffsetForEdge(ElementEdge edge, out uint pOffset)
	{
		GetPositionCount(out var positionCount);
		MUX_ASSERT(positionCount >= 2);

		uint offset = 0;

		switch (edge)
		{
			case ElementEdge.ContentStart:
				offset = 1;
				break;
			case ElementEdge.ElementStart:
				offset = 0;
				break;
			case ElementEdge.ContentEnd:
				offset = positionCount - 1;
				break;
			case ElementEdge.ElementEnd:
				offset = positionCount;
				break;
			default:
				MUX_ASSERT(false);
				break;
		}

		pOffset = offset;
	}

	//------------------------------------------------------------------------
	//
	//  CTextElement::EnsureTextFormattingForRead
	//
	//  Realizes the element's resolved text formatting before it is read.
	//
	//  NOTE (Uno): WinUI consulted a generation-counter cache and pulled inherited values
	//  from the parent chain. Uno resolves text inheritance through the property system, so
	//  this rebuilds the snapshot from the element's current resolved dependency-property values
	//  (no generation-counter cache, so it never goes stale against the property system).
	//
	//------------------------------------------------------------------------
	internal void EnsureTextFormattingForRead()
	{
		m_pTextFormatting = BuildTextFormatting();
	}

	//------------------------------------------------------------------------
	//
	//  CDependencyObject::GetTextFormatting
	//
	//  Returns the resolved (inheritance-applied) text formatting snapshot.
	//
	//------------------------------------------------------------------------
	internal void GetTextFormatting(out RichTextServices.TextFormatting? ppTextFormatting)
	{
		EnsureTextFormattingForRead();
		ppTextFormatting = m_pTextFormatting;
	}

	//------------------------------------------------------------------------
	//
	//  CDependencyObject::GetInheritedProperties
	//
	//  Returns the resolved inherited (Typography) attached-property snapshot.
	//
	//------------------------------------------------------------------------
	internal void GetInheritedProperties(out InheritedProperties? ppInheritedProperties)
	{
		ppInheritedProperties = BuildInheritedProperties();
	}

	//------------------------------------------------------------------------
	//
	//  CDependencyObject::IsPropertyDefaultByIndex
	//
	//  Whether a property is still at its default (unset) value, addressed by index.
	//  Only Run_FlowDirection is consulted by the run model (directional-control detection).
	//
	//------------------------------------------------------------------------
	internal bool IsPropertyDefaultByIndex(KnownPropertyIndex propertyIndex)
	{
		if (propertyIndex == KnownPropertyIndex.Run_FlowDirection && this is Run run)
		{
			return ReadLocalValue(Run.FlowDirectionProperty) == DependencyProperty.UnsetValue;
		}

		return true;
	}

	private RichTextServices.TextFormatting BuildTextFormatting()
	{
		var resolvedLanguage = CultureInfo.CurrentCulture.Name;

		return new RichTextServices.TextFormatting
		{
			FontFamily = FontFamily,
			Foreground = Foreground,
			FontSize = (float)FontSize,
			CharacterSpacing = CharacterSpacing,
			FontWeight = FontWeight,
			FontStyle = FontStyle,
			FontStretch = FontStretch,
			TextDecorations = TextDecorations,
			FlowDirection = GetFlowDirection(),
			IsTextScaleFactorEnabled = IsTextScaleFactorEnabled,
			LanguageString = resolvedLanguage,
			ResolvedLanguageString = resolvedLanguage,
			ResolvedLanguageListString = resolvedLanguage,
		};
	}

	// Resolves the element's flow direction. FlowDirection lives on Run (and on the
	// containing FrameworkElement) in Uno; non-Run text elements inherit it.
	private FlowDirection GetFlowDirection()
		=> this is Run run ? run.FlowDirection : FlowDirection.LeftToRight;

	private InheritedProperties BuildInheritedProperties()
	{
		var inherited = new InheritedProperties();
		inherited.Typography = new InheritedProperties.TypographyProperties
		{
			AnnotationAlternates = TypographyAttachedProperties.GetAnnotationAlternates(this),
			EastAsianExpertForms = TypographyAttachedProperties.GetEastAsianExpertForms(this),
			EastAsianLanguage = TypographyAttachedProperties.GetEastAsianLanguage(this),
			EastAsianWidths = TypographyAttachedProperties.GetEastAsianWidths(this),
			StandardLigatures = TypographyAttachedProperties.GetStandardLigatures(this),
			ContextualLigatures = TypographyAttachedProperties.GetContextualLigatures(this),
			DiscretionaryLigatures = TypographyAttachedProperties.GetDiscretionaryLigatures(this),
			HistoricalLigatures = TypographyAttachedProperties.GetHistoricalLigatures(this),
			StandardSwashes = TypographyAttachedProperties.GetStandardSwashes(this),
			ContextualSwashes = TypographyAttachedProperties.GetContextualSwashes(this),
			ContextualAlternates = TypographyAttachedProperties.GetContextualAlternates(this),
			StylisticAlternates = TypographyAttachedProperties.GetStylisticAlternates(this),
			StylisticSet1 = TypographyAttachedProperties.GetStylisticSet1(this),
			StylisticSet2 = TypographyAttachedProperties.GetStylisticSet2(this),
			StylisticSet3 = TypographyAttachedProperties.GetStylisticSet3(this),
			StylisticSet4 = TypographyAttachedProperties.GetStylisticSet4(this),
			StylisticSet5 = TypographyAttachedProperties.GetStylisticSet5(this),
			StylisticSet6 = TypographyAttachedProperties.GetStylisticSet6(this),
			StylisticSet7 = TypographyAttachedProperties.GetStylisticSet7(this),
			StylisticSet8 = TypographyAttachedProperties.GetStylisticSet8(this),
			StylisticSet9 = TypographyAttachedProperties.GetStylisticSet9(this),
			StylisticSet10 = TypographyAttachedProperties.GetStylisticSet10(this),
			StylisticSet11 = TypographyAttachedProperties.GetStylisticSet11(this),
			StylisticSet12 = TypographyAttachedProperties.GetStylisticSet12(this),
			StylisticSet13 = TypographyAttachedProperties.GetStylisticSet13(this),
			StylisticSet14 = TypographyAttachedProperties.GetStylisticSet14(this),
			StylisticSet15 = TypographyAttachedProperties.GetStylisticSet15(this),
			StylisticSet16 = TypographyAttachedProperties.GetStylisticSet16(this),
			StylisticSet17 = TypographyAttachedProperties.GetStylisticSet17(this),
			StylisticSet18 = TypographyAttachedProperties.GetStylisticSet18(this),
			StylisticSet19 = TypographyAttachedProperties.GetStylisticSet19(this),
			StylisticSet20 = TypographyAttachedProperties.GetStylisticSet20(this),
			Capitals = TypographyAttachedProperties.GetCapitals(this),
			CapitalSpacing = TypographyAttachedProperties.GetCapitalSpacing(this),
			Kerning = TypographyAttachedProperties.GetKerning(this),
			CaseSensitiveForms = TypographyAttachedProperties.GetCaseSensitiveForms(this),
			HistoricalForms = TypographyAttachedProperties.GetHistoricalForms(this),
			Fraction = TypographyAttachedProperties.GetFraction(this),
			NumeralStyle = TypographyAttachedProperties.GetNumeralStyle(this),
			NumeralAlignment = TypographyAttachedProperties.GetNumeralAlignment(this),
			SlashedZero = TypographyAttachedProperties.GetSlashedZero(this),
			MathematicalGreek = TypographyAttachedProperties.GetMathematicalGreek(this),
			Variants = TypographyAttachedProperties.GetVariants(this),
		};

		return inherited;
	}
}
