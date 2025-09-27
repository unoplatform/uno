// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ExtendedSelector.cpp, tag winui3/release/1.5.0

#if !HAS_UNO_WINUI
using Microsoft.UI.Xaml.Controls;
#endif

namespace Microsoft.UI.Xaml.Controls;

internal class ExtendedSelector : SelectorBase
{
	// ExtendedSelector::ExtendedSelector()
	// {
	// 	ITEMSVIEW_TRACE_VERBOSE(nullptr, TRACE_MSG_METH, METH_NAME, this);
	// }

	// ExtendedSelector::~ExtendedSelector()
	// {
	// 	ITEMSVIEW_TRACE_VERBOSE(nullptr, TRACE_MSG_METH, METH_NAME, this);
	// }

	public override void OnInteractedAction(IndexPath index, bool ctrl, bool shift)
	{
		// ITEMSVIEW_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_INT_INT, METH_NAME, this, ctrl, shift);

		var selectionModel = GetSelectionModel();
		if (shift)
		{
			var anchorIndex = selectionModel.AnchorIndex;
			if (anchorIndex is not null)
			{
				selectionModel.ClearSelection();
				selectionModel.AnchorIndex = anchorIndex;
				selectionModel.SelectRangeFromAnchorTo(index);
			}
		}
		else if (ctrl)
		{
			if (IsSelected(index))
			{
				selectionModel.DeselectAt(index);
			}
			else
			{
				selectionModel.SelectAt(index);
			}
		}
		else
		{
			// Only clear selection if interacting with a different item.
			if (!IsSelected(index))
			{
				selectionModel.ClearSelection();
				selectionModel.SelectAt(index);
			}
		}
	}

	public override void OnFocusedAction(IndexPath index, bool ctrl, bool shift)
	{
		// ITEMSVIEW_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_INT_INT, METH_NAME, this, ctrl, shift);

		var selectionModel = GetSelectionModel();

		if (shift && ctrl)
		{
			if (selectionModel.AnchorIndex is not null)
			{
				selectionModel.SelectRangeFromAnchorTo(index);
			}
		}
		else if (shift)
		{
			var anchorIndex = selectionModel.AnchorIndex;
			if (anchorIndex is not null)
			{
				selectionModel.ClearSelection();
				selectionModel.AnchorIndex = anchorIndex;
				selectionModel.SelectRangeFromAnchorTo(index);
			}
		}
		else if (!ctrl)
		{
			selectionModel.ClearSelection();
			selectionModel.SelectAt(index);
		}
	}
}
