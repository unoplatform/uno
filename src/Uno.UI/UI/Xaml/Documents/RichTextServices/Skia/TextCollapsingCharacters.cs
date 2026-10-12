// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TextCollapsingCharacters.h, TextCollapsingCharacters.cpp, tag winui3/release/2.4.0, commit e8442d07a

#nullable enable

using System;
using Windows.Foundation;
using Microsoft.UI.Xaml.Documents.TextFormatting;

namespace Microsoft.UI.Xaml.Documents.RichTextServices;

/// <summary>
/// The collapsing symbol (ellipsis) shown when a TextLine is collapsed by text trimming.
/// </summary>
internal sealed class TextCollapsingCharacters : TextCollapsingSymbol
{
	private readonly float[] _characterWidths;

	public TextCollapsingCharacters(
		char collapsingChar,
		int characterCount,
		float[] characterWidths,
		float width,
		FlowDirection flowDirection,
		TextRunProperties textRunProperties,
		FontDetails fontDetails)
	{
		CollapsingChar = collapsingChar;
		CharacterCount = characterCount;
		_characterWidths = characterWidths;
		Width = width;
		FlowDirection = flowDirection;
		TextRunProperties = textRunProperties;
		FontDetails = fontDetails;
	}

	public char CollapsingChar { get; }

	public int CharacterCount { get; }

	public override double Width { get; }

	public FlowDirection FlowDirection { get; }

	public TextRunProperties TextRunProperties { get; }

	// The resolved font the symbol is shaped and drawn with.
	public FontDetails FontDetails { get; }

	public float GetCharacterWidth(int index) => _characterWidths[index];

	// The symbol is emitted onto the collapsed RenderLine and painted by ParsedText.Draw along with
	// the rest of the paragraph, so there is no separate drawing-context recording pass.
	public override void Draw(TextDrawingContext drawingContext, Point origin, double viewportWidth, FlowDirection flowDirection)
		=> throw new NotSupportedException(
			"The collapsing symbol is drawn as part of the collapsed line by ParsedText.Draw.");
}
