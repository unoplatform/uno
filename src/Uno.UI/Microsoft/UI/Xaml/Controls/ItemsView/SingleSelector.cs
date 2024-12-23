// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference SingleSelector.cpp, tag winui3/release/1.5.0

#if !HAS_UNO_WINUI
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
#endif

namespace Windows.UI.Xaml.Controls;

internal partial class SingleSelector : SelectorBase
{
	public SingleSelector()
	{
		//ITEMSVIEW_TRACE_VERBOSE(null, TRACE_MSG_METH, METH_NAME, this);
	}

	//~SingleSelector()
	//{
	//	ITEMSVIEW_TRACE_VERBOSE(null, TRACE_MSG_METH, METH_NAME, this);
	//}

	public void FollowFocus(bool followFocus)
	{
		//ITEMSVIEW_TRACE_VERBOSE(null, TRACE_MSG_METH_INT, METH_NAME, this, followFocus);

		m_followFocus = followFocus;
	}

	public override void OnInteractedAction(IndexPath index, bool ctrl, bool shift)
	{
		//ITEMSVIEW_TRACE_VERBOSE(null, TRACE_MSG_METH_INT_INT, METH_NAME, this, ctrl, shift);

		var selectionModel = GetSelectionModel();
		selectionModel.SingleSelect = true;

		if (!ctrl)
		{
			selectionModel.SelectAt(index);
		}
		else if (!IsSelected(index))
		{
			selectionModel.SelectAt(index);
		}
		else
		{
			selectionModel.DeselectAt(index);
		}
	}

	public override void OnFocusedAction(IndexPath index, bool ctrl, bool shift)
	{
		//ITEMSVIEW_TRACE_VERBOSE(null, TRACE_MSG_METH_INT_INT, METH_NAME, this, ctrl, shift);

		var selectionModel = GetSelectionModel();
		selectionModel.SingleSelect = true;

		if (!ctrl && m_followFocus)
		{
			selectionModel.SelectAt(index);
		}
	}
}
