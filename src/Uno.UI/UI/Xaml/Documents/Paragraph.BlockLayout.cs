// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference BlockTextElement.h (CParagraph::GetInlineCollection), tag winui3/release/2.4.0, commit e8442d07a

#nullable enable

namespace Microsoft.UI.Xaml.Documents;

partial class Paragraph
{
	// CParagraph::GetInlineCollection — the paragraph's inline content collection.
	internal InlineCollection GetInlineCollection() => Inlines;

	// GetPositionCount / GetRun / GetContainingElement / GetElementEdgeOffset live in
	// Paragraph.TextContainer.cs (the run-model partial).
}
