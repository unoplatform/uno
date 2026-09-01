// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TextPointerWrapper.h (CTextPointerWrapper::ElementEdge), tag winui3/release/2.4.0, commit e8442d07a

#nullable enable

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

	// GetPositionCount / GetRun / GetContainingElement / GetElementEdgeOffset / CacheLengths
	// live in BlockCollection.TextContainer.cs (the run-model partial).
}
