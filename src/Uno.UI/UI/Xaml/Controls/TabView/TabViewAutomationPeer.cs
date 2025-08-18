// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference: TabViewAutomationPeer.cpp, commit 65718e2813

using System;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes TabView types to Microsoft UI Automation.
/// </summary>
public partial class TabViewAutomationPeer : FrameworkElementAutomationPeer, ISelectionProvider
{
	/// <summary>
	/// Initializes a new instance of the TabViewAutomationPeer class.
	/// </summary>
	/// <param name="owner">The TabView control instance to create the peer for.</param>
	public TabViewAutomationPeer(TabView owner) : base(owner)
	{
	}

	// IAutomationPeerOverrides
	protected override object GetPatternCore(PatternInterface patternInterface) =>
		patternInterface == PatternInterface.Selection ? this : base.GetPatternCore(patternInterface);

	protected override string GetClassNameCore() => nameof(TabView);

	protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Tab;

	bool ISelectionProvider.CanSelectMultiple => false;

	bool ISelectionProvider.IsSelectionRequired => true;

	IRawElementProviderSimple[] ISelectionProvider.GetSelection()
	{
		var tabView = Owner as TabView;
		if (tabView != null)
		{
			var tabViewItem = tabView.ContainerFromIndex(tabView.SelectedIndex) as TabViewItem;
			if (tabViewItem != null)
			{
				var peer = FrameworkElementAutomationPeer.CreatePeerForElement(tabViewItem);
				if (peer != null)
				{
					return new[] { ProviderFromPeer(peer) };
				}
			}
		}
		return Array.Empty<IRawElementProviderSimple>();
	}
}
