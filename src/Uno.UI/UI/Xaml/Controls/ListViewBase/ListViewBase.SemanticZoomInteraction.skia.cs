// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ListViewBase_Partial_Interaction.cpp, tag winui3/release/1.8.4, commit dc46907e92

#nullable enable

using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.System;

namespace Microsoft.UI.Xaml.Controls;

partial class ListViewBase
{
	internal bool OnGroupHeaderKeyDown(object? item, VirtualKey originalKey, VirtualKey key)
	{
		// Ignore already handled events
		if (IsInDragDrop())
		{
			// During an exclusive interaction (drag/drop), disable all keyboard interaction.
			// Handle the event, since other controls don't have the necessary context around drag/drop interactions.
			return true;
		}

		return key switch
		{
			// Enter, Space (gamepad 'A' maps to space)
			VirtualKey.Enter or VirtualKey.Space => ToggleSemanticZoomActiveView(item),
			_ => false
		};
	}

	internal bool OnGroupHeaderKeyUp(object? item, VirtualKey originalKey, VirtualKey key)
	{
		// Ignore already handled events
		if (IsInDragDrop())
		{
			// During an exclusive interaction (drag/drop), disable all interaction.
			// Handle the event, since other controls don't have the necessary context around drag/drop interactions.
			return true;
		}

		return key switch
		{
			// Enter, Space (gamepad 'A' maps to space)
			VirtualKey.Enter or VirtualKey.Space => ToggleSemanticZoomActiveView(item),
			_ => false
		};
	}

	internal bool OnHeaderItemTap(object? item) => ToggleSemanticZoomActiveView(item);

	internal bool ToggleSemanticZoomActiveView(object? item)
	{
		var handled = false;

		// Get the SemanticZoom owner
		if (SemanticZoomOwner is { } semanticZoom)
		{
			// If we cannot change views, doing a view change will
			// result in an IFCEXPECT - When doing keydown or Tap we
			// do not want to crash the app, so we just don't change views
			// in that case.
			if (semanticZoom.CanChangeViews && IsZoomedInView)
			{
				m_tpSZRequestingItem = item;

				// Call ToggleActiveViewFromHeaderItem
				handled = semanticZoom.ToggleActiveViewFromHeaderItem();
			}
		}

		return handled;
	}

	partial void TryHandleSemanticZoomItemClick(
		int clickedIndex,
		SelectorItem clickedContainer,
		object clickedItem,
		FocusState focusState,
		ref bool handled)
	{
		if (SemanticZoomOwner is not { } owner ||
			IsZoomedInView ||
			clickedContainer is null)
		{
			return;
		}

		clickedContainer.Focus(focusState);
		FocusedIndexContainerItem = (clickedIndex, clickedContainer, clickedItem);
		SetFocusedIndex(clickedIndex);
		owner.ToggleActiveViewWithFocusState(focusState);
		handled = true;
	}
}
