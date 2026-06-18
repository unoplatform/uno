// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

namespace Microsoft.UI.Xaml.Documents;

partial class Paragraph
{
	// CParagraph::GetInlineCollection — the paragraph's inline content collection.
	internal InlineCollection GetInlineCollection() => Inlines;

	// GetPositionCount / GetRun / GetContainingElement / GetElementEdgeOffset live in
	// Paragraph.TextContainer.skia.cs (the run-model partial).
}
