// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

namespace Microsoft.UI.Xaml.Documents;

partial class BlockCollection
{
	// Uno glue (Stage 9b): the TextSelectionManager is created against an ITextContainer.
	// BlockCollection already implements RichTextServices.ITextContainer (see the run-model
	// partials), so the container is the collection itself.
	internal RichTextServices.ITextContainer GetTextContainer() => this;
}
