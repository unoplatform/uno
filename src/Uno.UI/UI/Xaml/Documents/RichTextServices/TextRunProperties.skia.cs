// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TextRunProperties.h, TextRunProperties.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using System;
using System.Globalization;
using Microsoft.UI.Xaml.Documents.TextFormatting;

namespace Microsoft.UI.Xaml.Documents.RichTextServices;

/// <summary>
/// TextRunProperties contains formatting properties for one run of text.
/// </summary>
internal sealed class TextRunProperties
{
	// CharacterSpacing values are in 1000ths of the em size.
	public const float CharacterSpacingScale = 1000.0f;

	// Font typeface representing font family, weight, style, stretch and language.
	private readonly FontDetails m_pFontTypeface;

	// Font rendering em size for run's character data.
	private readonly double m_fontSize;

	// The source of foreground brush.
	private readonly WeakReference<DependencyObject>? m_pForegroundBrushSource;

	private readonly bool m_hasUnderline;

	// Value indicating whether strikethrough is present.
	private readonly bool m_hasStrikethrough;

	// Extra space to display after each character or object.
	private readonly int m_characterSpacing;

	// TODO Uno: Silverlight inherited properties (TextOptions and Typography)
	// are not yet ported; EqualsForShaping omits the typography comparison until
	// InheritedProperties is brought over.

	// Culture info for this run.
	private readonly CultureInfo m_strCultureInfo;

	// CultureList info for this run.
	private readonly CultureInfo m_strCultureListInfo;

	// Initializes a new instance of the TextRunProperties class.
	public TextRunProperties(
		FontDetails pFontTypeface,
		double fontSize,
		bool hasUnderline,
		bool hasStrikethrough,
		int characterSpacing,
		WeakReference<DependencyObject>? pForegroundBrushSource,
		CultureInfo strCultureInfo,
		CultureInfo strCultureListInfo)
	{
		m_pFontTypeface = pFontTypeface;
		m_fontSize = fontSize;
		m_hasUnderline = hasUnderline;
		m_hasStrikethrough = hasStrikethrough;
		m_characterSpacing = characterSpacing;
		m_pForegroundBrushSource = pForegroundBrushSource;
		m_strCultureInfo = strCultureInfo;
		m_strCultureListInfo = strCultureListInfo;
	}

	// Gets font typeface.
	public FontDetails FontTypeface => m_pFontTypeface;

	// Gets font rendering size.
	public double FontSize => m_fontSize;

	// Gets the source of foreground brush.
	public WeakReference<DependencyObject>? ForegroundBrushSource => m_pForegroundBrushSource;

	public bool HasUnderline => m_hasUnderline;

	// Gets value indicating whether strikethrough is present.
	public bool HasStrikethrough => m_hasStrikethrough;

	// Extra space displayed after each character and object.
	public int CharacterSpacing => m_characterSpacing;

	// Gets culture info for this run.
	public CultureInfo CultureInfo => m_strCultureInfo;

	// Gets culture list info for this run.
	public CultureInfo CultureListInfo => m_strCultureListInfo;

	// Checks for equality with another TextRunProperties object, for purposes
	// of shaping.
	//
	// Full equality of text runs is not required to shape across multiple runs.
	// This method previously checked for full equality, but this meant that
	// it was not possible to have both correct shaping and variable sub-word
	// coloring (Windows Blue Bug# 263695). Now the checks include all
	// TextRunProperties except for the foreground brush and underlining.
	public bool EqualsForShaping(TextRunProperties? pProperties)
	{
		var equals = false;

		if (pProperties is not null)
		{
			if (m_pFontTypeface == pProperties.m_pFontTypeface
				&& m_fontSize == pProperties.m_fontSize
				&& m_characterSpacing == pProperties.m_characterSpacing)
			{
				equals = m_strCultureInfo.Equals(pProperties.m_strCultureInfo);
			}
		}

		return equals;
	}
}
