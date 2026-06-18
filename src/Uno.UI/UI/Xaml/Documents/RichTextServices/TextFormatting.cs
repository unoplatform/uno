// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TextFormatting.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using System;
using Windows.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Documents.RichTextServices;

// A storage group which collects the resolved (inheritance-applied) text formatting
// values for a TextElement: font family/size/weight/style/stretch, foreground, text
// decorations, character spacing, flow direction and the resolved language strings.
//
// NOTE (Uno): WinUI maintained a generation-counter inheritance cache (m_cGenerationCounter /
// m_pCoreInheritedPropGenerationCounter, see IsOld()/SetIsUpToDate()) and a default-template copy
// path (Create/CreateCopy/CreateDefault). Uno already resolves text inheritance through
// FrameworkPropertyMetadataOptions.Inherits, so the snapshot is built on demand from the owning
// element's resolved dependency-property values and the generation-counter machinery is omitted.
internal sealed class TextFormatting
{
	// Resolved font family. Maps to WinUI m_pFontFamily.
	public FontFamily? FontFamily { get; set; }

	// Resolved foreground brush. Maps to WinUI m_pForeground.
	public Brush? Foreground { get; set; }

	// Resolved language string (as set). Maps to WinUI m_strLanguageString.
	public string LanguageString { get; set; } = string.Empty;

	// Resolved (NLS-normalized) language string. Maps to WinUI m_strResolvedLanguageString.
	public string ResolvedLanguageString { get; set; } = string.Empty;

	// Resolved font-fallback language list. Maps to WinUI m_strResolvedLanguageListString.
	public string ResolvedLanguageListString { get; set; } = string.Empty;

	// Resolved font size in pixels. Maps to WinUI m_eFontSize.
	public float FontSize { get; set; }

	// Extra spacing applied per character (1/1000 em). Maps to WinUI m_nCharacterSpacing.
	public int CharacterSpacing { get; set; }

	// Resolved font weight. Maps to WinUI m_nFontWeight.
	public FontWeight FontWeight { get; set; } = FontWeights.Normal;

	// Resolved font style. Maps to WinUI m_nFontStyle.
	public FontStyle FontStyle { get; set; } = FontStyle.Normal;

	// Resolved font stretch. Maps to WinUI m_nFontStretch.
	public FontStretch FontStretch { get; set; } = FontStretch.Normal;

	// Resolved text decorations. Maps to WinUI m_nTextDecorations.
	public TextDecorations TextDecorations { get; set; } = TextDecorations.None;

	// Resolved flow direction. Maps to WinUI m_nFlowDirection.
	public FlowDirection FlowDirection { get; set; } = FlowDirection.LeftToRight;

	// Whether the OS text scale factor applies to this element. Maps to WinUI m_isTextScaleFactorEnabled.
	public bool IsTextScaleFactorEnabled { get; set; } = true;

	//------------------------------------------------------------------------
	//
	//  Returns the font size scaled according to a given factor.
	//
	//------------------------------------------------------------------------
	public float GetScaledFontSize(float fontScale)
	{
		// If the text scale factor is disabled, or if the font size is 0 or less,
		// then we'll just return the stored font size.
		// The latter case will result in an expected runtime exception
		// that we want to keep in place.
		if (!IsTextScaleFactorEnabled || FontSize <= 0)
		{
			return FontSize;
		}

		// If the stored font size is less than 1, then the formula for scaling
		// will cause it to asymptotically shoot to infinity as it gets closer to 0,
		// so we'll cap the input font size to a minimum of 1.
		float inputFontSize = Math.Max(FontSize, 1.0f);

		// The formula that we get from design has it such that we scale
		// the output font size s_o relative to the input font size s_i,
		// making use of the font scale factor f, according to the formula
		//
		// s_o = s_i + max(-2.5 ln s_i + 15, 0) (f - 1)
		//
		return (float)(inputFontSize + Math.Max(-Math.Exp(1) * Math.Log(inputFontSize) + 18, 0.0) * (fontScale - 1));
	}

	// Resolved language string (no copy). Maps to WinUI GetResolvedLanguageStringNoRef.
	public string GetResolvedLanguageStringNoRef() => ResolvedLanguageString;

	// Resolved font-fallback language list (no copy). Maps to WinUI GetResolvedLanguageListStringNoRef.
	public string GetResolvedLanguageListStringNoRef() => ResolvedLanguageListString;
}
