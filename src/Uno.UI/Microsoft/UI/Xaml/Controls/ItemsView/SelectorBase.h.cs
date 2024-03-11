// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if !HAS_UNO_WINUI
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
#endif

namespace Microsoft.UI.Xaml.Controls;

internal partial class SelectorBase
{
	public virtual void OnInteractedAction(IndexPath index, bool ctrl, bool shift) { }
	public virtual void OnFocusedAction(IndexPath index, bool ctrl, bool shift) { }

	protected SelectionModel GetSelectionModel()
	{
		return m_selectionModel;
	}

	private SelectionModel m_selectionModel;
}
