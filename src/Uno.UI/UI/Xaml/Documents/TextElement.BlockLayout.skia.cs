// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

using System;

namespace Microsoft.UI.Xaml.Documents;

// TODO Uno (Stage 4): minimal KnownPropertyIndex subset needed by the block-layout run model.
// The full WinUI KnownPropertyIndex enum is generated from the metadata table; this placeholder
// carries only the member(s) referenced by the ported BlockLayout code until that enum is ported.
internal enum KnownPropertyIndex
{
	Run_FlowDirection,
}

partial class TextElement
{
	// TODO Uno (Stage 4): TextElement.EnsureTextFormattingForRead — realizes the element's resolved
	// text formatting before it is read (CTextElement::EnsureTextFormattingForRead).
	internal void EnsureTextFormattingForRead()
		=> throw new NotSupportedException("TODO Uno (Stage 4): TextElement.EnsureTextFormattingForRead");

	// TODO Uno (Stage 4): TextElement.IsPropertyDefaultByIndex — whether a property is still at its
	// default (unset) value, addressed by KnownPropertyIndex (CDependencyObject::IsPropertyDefaultByIndex).
	internal bool IsPropertyDefaultByIndex(KnownPropertyIndex propertyIndex)
		=> throw new NotSupportedException("TODO Uno (Stage 4): TextElement.IsPropertyDefaultByIndex");
}
