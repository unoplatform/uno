// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ListViewBase_Partial_Interaction.cpp, tag winui3/release/1.8.4, commit dc46907e92

#nullable enable

using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Controls;

partial class ListViewBase
{
	partial void TryHandleSemanticZoomItemClick(
		int clickedIndex,
		SelectorItem clickedContainer,
		object clickedItem,
		FocusState focusState,
		ref bool handled)
	{
		if (SemanticZoomOwner is not { CanChangeViews: true } owner ||
			IsZoomedInView ||
			clickedContainer is null)
		{
			return;
		}

		clickedContainer.Focus(focusState);
		FocusedIndexContainerItem = (clickedIndex, clickedContainer, clickedItem);
		owner.ToggleActiveViewWithFocusState(focusState);
		handled = true;
	}
}
