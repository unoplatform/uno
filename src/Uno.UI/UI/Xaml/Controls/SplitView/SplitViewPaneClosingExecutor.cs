// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference SplitView.cpp, tag winui3/release/1.4.2

namespace Microsoft.UI.Xaml.Controls;

internal class SplitViewPaneClosingExecutor
{
	SplitView m_spSplitView;
	SplitViewPaneClosingEventArgs m_spPaneClosingEventArgs;

	public SplitViewPaneClosingExecutor(SplitView pSplitView, SplitViewPaneClosingEventArgs pPaneClosingEventArgs)
	{
		m_spSplitView = pSplitView;
		m_spPaneClosingEventArgs = pPaneClosingEventArgs;
	}

	public void Execute()
	{
		if (m_spPaneClosingEventArgs.Cancel)
		{
			m_spSplitView.OnCancelClosing();
			return;
		}

		m_spSplitView.IsPaneOpen = false;
	}
}
