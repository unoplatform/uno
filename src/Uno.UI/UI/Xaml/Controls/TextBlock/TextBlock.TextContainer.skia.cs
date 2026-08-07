// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TextBlock.cpp / TextFormatting.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using System.Globalization;
using Microsoft.UI.Xaml.Documents;

namespace Microsoft.UI.Xaml.Controls;

partial class TextBlock
{
	// CDependencyObject::GetTextFormatting — the resolved text formatting snapshot for the TextBlock.
	// InlineCollection.GetRun reads this for the past-end (EOP) run, since the InlineCollection does
	// not know its owning control's formatting. Built from the TextBlock's own resolved (inherited) DPs.
	internal void GetTextFormatting(out global::Microsoft.UI.Xaml.Documents.RichTextServices.TextFormatting? ppTextFormatting)
	{
		var resolvedLanguage = CultureInfo.CurrentCulture.Name;

		ppTextFormatting = new global::Microsoft.UI.Xaml.Documents.RichTextServices.TextFormatting
		{
			FontFamily = FontFamily,
			Foreground = Foreground,
			FontSize = (float)FontSize,
			CharacterSpacing = CharacterSpacing,
			FontWeight = FontWeight,
			FontStyle = FontStyle,
			FontStretch = FontStretch,
			TextDecorations = TextDecorations,
			FlowDirection = FlowDirection,
			IsTextScaleFactorEnabled = IsTextScaleFactorEnabled,
			LanguageString = resolvedLanguage,
			ResolvedLanguageString = resolvedLanguage,
			ResolvedLanguageListString = resolvedLanguage,
		};
	}

	// CDependencyObject::GetInheritedProperties — the resolved inherited (Typography) snapshot.
	internal void GetInheritedProperties(out InheritedProperties? ppInheritedProperties)
		=> ppInheritedProperties = new InheritedProperties();
}
