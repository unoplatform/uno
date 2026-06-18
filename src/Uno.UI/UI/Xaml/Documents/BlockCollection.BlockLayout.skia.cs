// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Documents;

// WinUI ElementEdge (CTextPointerWrapper::ElementEdge) — identifies an edge of a
// TextElement when computing element offsets. Required by GetElementEdgeOffset below.
internal enum ElementEdge
{
	ContentStart,
	ContentEnd,
	ElementStart,
	ElementEnd,
}

partial class BlockCollection
{
	// Block list backing the collection. BlockCollection is an IList<Block>, so the
	// collection IS the ordered block list.
	internal IEnumerable<Block> GetCollection() => this;

	internal uint GetCount() => (uint)Count;

	internal Block GetItemWithAddRef(uint index) => this[(int)index];

	// TODO Uno (Stage 4): BlockCollection.GetElementEdgeOffset — maps a TextElement edge to a
	// character offset within the block collection (CBlockCollection::GetElementEdgeOffset).
	internal void GetElementEdgeOffset(
		InlineUIContainer pElement,
		ElementEdge edge,
		out uint pOffset,
		out bool pFound)
		=> throw new NotSupportedException("TODO Uno (Stage 4): BlockCollection.GetElementEdgeOffset");
}
