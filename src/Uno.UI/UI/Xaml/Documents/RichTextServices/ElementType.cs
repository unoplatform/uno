// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ElementType.h, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

namespace Microsoft.UI.Xaml.Documents.RichTextServices;

/// <summary>
/// Identifies the role of a TextElementBackingStore element.
/// </summary>
internal enum ElementType
{
	// Structural element that holds Inline elements.
	Paragraph = 0,

	// Formatting element that may hold other Inline elements.
	Inline = 1,

	// An explicit line break within a Paragraph.
	LineBreak = 2,

	// An embedded object (UIElement).
	Object = 3,
}
