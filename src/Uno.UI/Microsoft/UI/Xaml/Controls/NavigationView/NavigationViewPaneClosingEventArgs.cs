// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference NavigationViewPaneClosingEventArgs.cpp, commit 65718e2813

using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Provides data for the NavigationView.PaneClosing event.
/// </summary>
public partial class NavigationViewPaneClosingEventArgs
{
	private bool m_cancelled;
	private SplitViewPaneClosingEventArgs m_splitViewClosingArgs;

	internal NavigationViewPaneClosingEventArgs()
	{
	}

	/// <summary>
	/// Gets or sets a value that indicates whether
	/// the event should be canceled.
	/// </summary>
	public bool Cancel
	{
		get => m_cancelled;
		set
		{
			m_cancelled = value;

			var args = m_splitViewClosingArgs;
			if (args != null)
			{
				args.Cancel = value;
			}
		}
	}

	internal void SplitViewClosingArgs(SplitViewPaneClosingEventArgs value)
	{
		m_splitViewClosingArgs = value;
	}
}
