// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

namespace Microsoft.UI.Xaml.Documents;

// TODO Uno (Stage 4): minimal KnownPropertyIndex subset needed by the block-layout run model.
// The full WinUI KnownPropertyIndex enum is generated from the metadata table; this placeholder
// carries only the member(s) referenced by the ported BlockLayout code until that enum is ported.
internal enum KnownPropertyIndex
{
	Run_FlowDirection,
}

// TextElement.EnsureTextFormattingForRead, IsPropertyDefaultByIndex, GetTextFormatting and
// GetInheritedProperties live in TextElement.TextContainer.skia.cs (the run-model partial).
