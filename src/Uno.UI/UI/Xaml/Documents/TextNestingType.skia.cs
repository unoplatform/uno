// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference EnumDefs.h (TextNestingType), tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

namespace Microsoft.UI.Xaml.Documents;

// Distinguishes runs corresponding to nesting level open/close from runs
// containing text or other content.
internal enum TextNestingType : byte
{
	OpenNesting,   // This run opens a new nesting level
	NestedContent, // This run contains content at the current nesting level
	CloseNesting,  // This run closes a nesting level
}
