// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference MultipleSelector.cpp, tag winui3/release/1.5.0

namespace Microsoft.UI.Xaml.Controls;

internal partial class MultipleSelector : SelectorBase
{
	public MultipleSelector()
	{
		//ITEMSVIEW_TRACE_VERBOSE(null, TRACE_MSG_METH, METH_NAME, this);
	}

	//~MultipleSelector()
	//{
	//	ITEMSVIEW_TRACE_VERBOSE(null, TRACE_MSG_METH, METH_NAME, this);
	//}

	public override void OnInteractedAction(IndexPath index, bool ctrl, bool shift)
	{
		//ITEMSVIEW_TRACE_VERBOSE(null, TRACE_MSG_METH_INT_INT, METH_NAME, this, ctrl, shift);

		var selectionModel = GetSelectionModel();
		if (shift)
		{
			var anchorIndex = selectionModel.AnchorIndex;
			if (anchorIndex is not null)
			{
				bool? isAnchorSelectedNullable = selectionModel.IsSelectedAt(anchorIndex);
				bool isAnchorSelected = false;
				if (isAnchorSelectedNullable != null)
				{
					isAnchorSelected = isAnchorSelectedNullable.Value;
				}

				bool? isIndexSelectedNullable = selectionModel.IsSelectedAt(index);
				bool isIndexSelected = false;
				if (isIndexSelectedNullable != null)
				{
					isIndexSelected = isIndexSelectedNullable.Value;
				}

				if (isAnchorSelected != isIndexSelected)
				{
					if (isAnchorSelected)
					{
						selectionModel.SelectRangeFromAnchorTo(index);
					}
					else
					{
						selectionModel.DeselectRangeFromAnchorTo(index);
					}
				}
			}
		}
		else if (IsSelected(index))
		{
			selectionModel.DeselectAt(index);
		}
		else
		{
			selectionModel.SelectAt(index);
		}
	}

	public override void OnFocusedAction(IndexPath index, bool ctrl, bool shift)
	{
		//ITEMSVIEW_TRACE_VERBOSE(null, TRACE_MSG_METH_INT_INT, METH_NAME, this, ctrl, shift);

		if (shift)
		{
			var selectionModel = GetSelectionModel();
			var anchorIndex = selectionModel.AnchorIndex;
			if (anchorIndex is not null)
			{
				bool? isAnchorSelectedNullable = selectionModel.IsSelectedAt(anchorIndex);
				bool isAnchorSelected = false;
				if (isAnchorSelectedNullable != null)
				{
					isAnchorSelected = isAnchorSelectedNullable.Value;
				}

				if (isAnchorSelected)
				{
					selectionModel.SelectRangeFromAnchorTo(index);
				}
				else
				{
					selectionModel.DeselectRangeFromAnchorTo(index);
				}
			}
		}
	}
}
