// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference SelectorBase.cpp, tag winui3/release/1.5.0

using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls;

internal partial class SelectorBase
{
	public SelectorBase()
	{
		//ITEMSVIEW_TRACE_VERBOSE(null, TRACE_MSG_METH, METH_NAME, this);
	}

	//~SelectorBase()
	//{
	//	ITEMSVIEW_TRACE_VERBOSE(null, TRACE_MSG_METH, METH_NAME, this);
	//}

	public void SetSelectionModel(SelectionModel selectionModel)
	{
		//ITEMSVIEW_TRACE_VERBOSE(null, TRACE_MSG_METH_PTR, METH_NAME, this, selectionModel);
		m_selectionModel = selectionModel;
	}

	public void DeselectWithAnchorPreservation(int index)
	{
		MUX_ASSERT(index != -1);

		if (m_selectionModel != null)
		{
			var anchorIndexPath = m_selectionModel.AnchorIndex;

			MUX_ASSERT(anchorIndexPath == null || anchorIndexPath.GetSize() == 1);

			int anchorIndex = anchorIndexPath == null ? -1 : anchorIndexPath.GetAt(0);

			m_selectionModel.Deselect(index);

			if (anchorIndex != -1)
			{
				m_selectionModel.SetAnchorIndex(anchorIndex);
			}
		}
	}

	protected bool IsSelected(IndexPath index)
	{
		bool isSelected = false;

		if (m_selectionModel != null)
		{
			bool? isSelectedNullable = m_selectionModel.IsSelectedAt(index);

			if (isSelectedNullable != null)
			{
				isSelected = isSelectedNullable.Value;
			}
		}

		return isSelected;
	}

	protected virtual bool CanSelect(IndexPath index)
	{
		return m_selectionModel != null;
	}

	public virtual void SelectAll()
	{
		//ITEMSVIEW_TRACE_VERBOSE(null, TRACE_MSG_METH, METH_NAME, this);

		if (m_selectionModel != null)
		{
			m_selectionModel.SelectAll();
		}
	}

	public virtual void Clear()
	{
		//ITEMSVIEW_TRACE_VERBOSE(null, TRACE_MSG_METH, METH_NAME, this);

		if (m_selectionModel != null)
		{
			m_selectionModel.ClearSelection();
		}
	}
}
