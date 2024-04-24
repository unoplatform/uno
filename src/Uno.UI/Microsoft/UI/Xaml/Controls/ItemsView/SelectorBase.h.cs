// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference SelectorBase.h, tag winui3/release/1.5.0

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
