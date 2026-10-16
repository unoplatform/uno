// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference GridViewItem_Partial.cpp, tag winui3/release/2.5.1

#nullable enable

namespace Microsoft.UI.Xaml.Controls;

partial class GridViewItem
{
	// OnCreateAutomationPeer and PrepareState (TemplateSettings creation) live in GridViewItem.cs.

	// Sets the value to display as the dragged items count.
	internal override void SetDragItemsCountDisplay(
		uint dragItemsCount)
	{
		base.SetDragItemsCountDisplay(dragItemsCount);
		TemplateSettings.DragItemsCount = (int)dragItemsCount;
	}
}
