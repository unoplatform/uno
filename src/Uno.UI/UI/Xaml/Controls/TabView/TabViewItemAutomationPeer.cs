// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference: TabViewItemAutomationPeer.cpp, commit 65718e2813

using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Helpers.WinUI;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes TabViewItem types to Microsoft UI Automation.
/// </summary>
public partial class TabViewItemAutomationPeer : ListViewItemAutomationPeer, ISelectionItemProvider
{
	/// <summary>
	/// Initializes an instance of TabViewItemAutomationPeer.
	/// </summary>
	/// <param name="owner">The TabViewItem control instance to create the peer for.</param>
	public TabViewItemAutomationPeer(TabViewItem owner) : base(owner)
	{
	}

	// IAutomationPeerOverrides
	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		// We subclass from ListViewItemAutomationPeer without using everything from it,
		// so we need to make sure we are returning TabViewItem's AutomationPeer for selection and drag, and not the parent class version
		return (patternInterface == PatternInterface.SelectionItem || patternInterface == PatternInterface.Drag) ? this : base.GetPatternCore(patternInterface);
	}

	protected override string GetClassNameCore() => nameof(TabViewItem);

	protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.TabItem;

	protected override string GetNameCore()
	{
		string returnHString = base.GetNameCore();

		// If a name hasn't been provided by AutomationProperties.Name in markup:
		if (string.IsNullOrEmpty(returnHString))
		{
			var tvi = Owner as TabViewItem;
			if (tvi != null)
			{
				returnHString = SharedHelpers.TryGetStringRepresentationFromObject(tvi.Header);
			}
		}

		return returnHString;
	}

	public bool IsSelected
	{
		get
		{
			var tvi = Owner as TabViewItem;
			if (tvi != null)
			{
				return tvi.IsSelected;
			}

			return false;
		}
	}

	public IRawElementProviderSimple SelectionContainer
	{
		get
		{
			var parent = GetParentTabView();
			if (parent != null)
			{
				var peer = FrameworkElementAutomationPeer.CreatePeerForElement(parent);
				if (peer != null)
				{
					return ProviderFromPeer(peer);
				}
			}

			return null;
		}
	}

	public void AddToSelection() => Select();

	public void RemoveFromSelection()
	{
		// Can't unselect in a TabView without knowing next selection
	}

	public void Select()
	{
		var owner = Owner as TabViewItem;
		if (owner != null)
		{
			owner.IsSelected = true;
		}
	}

	private TabView GetParentTabView()
	{
		TabView parentTabView = null;

		TabViewItem tabViewItem = Owner as TabViewItem;
		if (tabViewItem != null)
		{
			parentTabView = tabViewItem.GetParentTabView();
		}
		return parentTabView;
	}
}
