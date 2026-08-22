// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TextRunType.h, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

namespace Microsoft.UI.Xaml.Documents.RichTextServices;

/// <summary>
/// TextRunType enumerates possible types of data contained in a TextRun.
/// </summary>
internal enum TextRunType
{
	// Unicode text.
	Text,

	// Hidden run - element start or end, or other hidden data.
	Hidden,

	// End of line.
	EndOfLine,

	// End of paragraph.
	EndOfParagraph,

	// Object - UIElement embedded in text.
	Object,

	// Directional control - explicit change in flow direction.
	DirectionalControl,
}
