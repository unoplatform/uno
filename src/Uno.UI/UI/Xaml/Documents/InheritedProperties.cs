// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference InheritedProperties.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Documents;

//------------------------------------------------------------------------
//
//  InheritedProperties
//
//  A storage group which collects inherited properties such as the OpenType
//  typographic feature settings.
//
//  NOTE (Uno): WinUI also carried a generation-counter inheritance cache
//  (m_cGenerationCounter / m_pCoreInheritedPropGenerationCounter, IsOld()/SetIsUpToDate())
//  and a per-DependencyObject property-flag store. Uno resolves attached-property
//  inheritance through the property system, so the snapshot is built on demand from the
//  owning element's resolved Typography attached properties and that machinery is omitted.
//  WinUI's TextOptions (TextHintingMode/TextRenderingMode/TextFormattingMode) is a pure
//  D2D/DWrite rendering-hint group whose enums do not exist on Uno, so it is not ported.
//
//------------------------------------------------------------------------
internal sealed class InheritedProperties
{
	// The constituent properties are public so that the snapshot builder can
	// set them when reading the resolved attached-property values.
	public TypographyProperties Typography = new();  // OpenType features

	//------------------------------------------------------------------------
	//
	//  Typography attached properties
	//
	//------------------------------------------------------------------------
	public struct TypographyProperties
	{
		public int AnnotationAlternates;
		public FontEastAsianLanguage EastAsianLanguage;
		public FontEastAsianWidths EastAsianWidths;
		public int StandardSwashes;
		public int ContextualSwashes;
		public int StylisticAlternates;
		public FontCapitals Capitals;
		public FontFraction Fraction;
		public FontNumeralStyle NumeralStyle;
		public FontNumeralAlignment NumeralAlignment;
		public FontVariants Variants;
		public bool EastAsianExpertForms;
		public bool StandardLigatures;
		public bool ContextualLigatures;
		public bool DiscretionaryLigatures;
		public bool HistoricalLigatures;
		public bool ContextualAlternates;
		public bool StylisticSet1;
		public bool StylisticSet2;
		public bool StylisticSet3;
		public bool StylisticSet4;
		public bool StylisticSet5;
		public bool StylisticSet6;
		public bool StylisticSet7;
		public bool StylisticSet8;
		public bool StylisticSet9;
		public bool StylisticSet10;
		public bool StylisticSet11;
		public bool StylisticSet12;
		public bool StylisticSet13;
		public bool StylisticSet14;
		public bool StylisticSet15;
		public bool StylisticSet16;
		public bool StylisticSet17;
		public bool StylisticSet18;
		public bool StylisticSet19;
		public bool StylisticSet20;
		public bool CapitalSpacing;
		public bool Kerning;
		public bool CaseSensitiveForms;
		public bool HistoricalForms;
		public bool SlashedZero;
		public bool MathematicalGreek;

		// Default Typography matches the property-system defaults; an all-default
		// (default-constructed) struct represents "no typographic features set".
		public readonly bool IsTypographyDefault()
		{
			return AnnotationAlternates == 0
				&& EastAsianLanguage == FontEastAsianLanguage.Normal
				&& EastAsianWidths == FontEastAsianWidths.Normal
				&& StandardSwashes == 0
				&& ContextualSwashes == 0
				&& StylisticAlternates == 0
				&& Capitals == FontCapitals.Normal
				&& Fraction == FontFraction.Normal
				&& NumeralStyle == FontNumeralStyle.Normal
				&& NumeralAlignment == FontNumeralAlignment.Normal
				&& Variants == FontVariants.Normal
				&& !EastAsianExpertForms
				&& StandardLigatures
				&& ContextualLigatures
				&& !DiscretionaryLigatures
				&& !HistoricalLigatures
				&& ContextualAlternates
				&& !StylisticSet1 && !StylisticSet2 && !StylisticSet3 && !StylisticSet4 && !StylisticSet5
				&& !StylisticSet6 && !StylisticSet7 && !StylisticSet8 && !StylisticSet9 && !StylisticSet10
				&& !StylisticSet11 && !StylisticSet12 && !StylisticSet13 && !StylisticSet14 && !StylisticSet15
				&& !StylisticSet16 && !StylisticSet17 && !StylisticSet18 && !StylisticSet19 && !StylisticSet20
				&& !CapitalSpacing
				&& Kerning
				&& !CaseSensitiveForms
				&& !HistoricalForms
				&& !SlashedZero
				&& !MathematicalGreek;
		}

		public readonly bool IsTypographySame(TypographyProperties other) => Equals(other);
	}
}
