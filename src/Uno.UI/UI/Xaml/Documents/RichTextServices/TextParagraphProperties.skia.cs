// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TextParagraphProperties.h, TextParagraphProperties.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using System;

namespace Microsoft.UI.Xaml.Documents.RichTextServices;

/// <summary>
/// Provides a set of properties, such as alignment, or indentation, that can be
/// applied to a paragraph.
/// </summary>
internal sealed class TextParagraphProperties
{
	[Flags]
	public enum Flags
	{
		None = 0,
		Justify = 0x00000001,
		TrimSideBearings = 0x00000002, // Flag for enabling OpticalMarginAlignment
		DetermineTextReadingOrderFromContent = 0x00000004,
		DetermineAlignmentFromContent = 0x00000008,
	}

	// Paragraph's default run properties.
	private readonly TextRunProperties m_pDefaultTextRunProperties;

	// Paragraph's flow direction.
	private readonly FlowDirection m_flowDirection;

	// Properties controlling line indent.
	private readonly double m_firstLineIndent;

	// Paragraph's textwrapping setting.
	private readonly TextWrapping m_textWrapping;

	// Storage for flags.
	private Flags m_flags;

	// Paragraph's TextLineBounds setting.
	private readonly TextLineBounds m_textLineBounds;

	private readonly TextAlignment m_textAlignment;

	// Constructor.
	public TextParagraphProperties(
		FlowDirection flowDirection,
		TextRunProperties pDefaultTextRunProperties,
		double firstLineIndent,
		TextWrapping textWrapping,
		TextLineBounds textLineBounds,
		TextAlignment textAlignment)
	{
		m_flowDirection = flowDirection;
		m_pDefaultTextRunProperties = pDefaultTextRunProperties;
		m_firstLineIndent = firstLineIndent;
		m_textWrapping = textWrapping;
		m_textLineBounds = textLineBounds;
		m_textAlignment = textAlignment;
		m_flags = Flags.None;
	}

	// Gets flow direction.
	public FlowDirection FlowDirection => m_flowDirection;

	// Gets default incremental tab value.
	//
	// Default incremental tab is set to the font size for default text run
	// properties.
	// TODO Uno: Reconcile against the Skia engine's Segment tab width (hard-coded
	// to 48) so tab stops match between the two layout paths.
	public double DefaultIncrementalTab => 4 * m_pDefaultTextRunProperties.FontSize;

	// Gets paragraph's default run properties.
	public TextRunProperties DefaultTextRunProperties => m_pDefaultTextRunProperties;

	// Properties controlling line indent.
	public double FirstLineIndent => m_firstLineIndent;

	// Gets paragraph's TextWrapping setting.
	public TextWrapping TextWrapping => m_textWrapping;

	// Gets value for specified flags.
	public bool GetFlags(Flags flags) => (m_flags & flags) == flags;

	// Gets TextLineBounds property value.
	public TextLineBounds TextLineBounds => m_textLineBounds;

	public TextAlignment TextAlignment => m_textAlignment;

	// Sets flags to specified value.
	public void SetFlags(Flags flags, bool value)
	{
		if (value)
		{
			m_flags |= flags;
		}
		else
		{
			m_flags &= ~flags;
		}
	}
}
