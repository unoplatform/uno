// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference LiveReorderHelper.h, microsoft-ui-xaml main branch (dxaml/xcp/components/livereorderhelper, not in tag winui3/release/2.5.1), commit bac7a9c33ff


#nullable enable

using System.Collections.Generic;
using Windows.Foundation;

namespace DirectUI.Components.LiveReorderHelper;

internal struct MovedItem
{
	internal int sourceIndex;
	internal int destinationIndex;
	internal Rect sourceRect;
	internal Rect destinationRect;

	internal MovedItem(int sourceIdx, int destinationIdx, Rect sourceRt, Rect destinationRt)
	{
		sourceIndex = sourceIdx;
		destinationIndex = destinationIdx;
		sourceRect = sourceRt;
		destinationRect = destinationRt;
	}
}

internal partial class MovedItems
{
	// used to access the private moved indexes tuple
	public List<MovedItem>.Enumerator GetEnumerator() => m_movedItems.GetEnumerator();

	internal uint Size => (uint)m_movedItems.Count;

	internal void Clear() => m_movedItems.Clear();

	// stores the indexes of the items that have been moved
	// and their original layout information
	private readonly List<MovedItem> m_movedItems = new();
}

