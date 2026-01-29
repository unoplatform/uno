// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ListBoxItemDataAutomationPeer_Partial.cpp, tag winui3/release/1.8.4
namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes ListBox items to Microsoft UI Automation, using a data representation of the item 
/// so that the peer supports scrolling to that item with data awareness.
/// </summary>
public partial class ListBoxItemDataAutomationPeer : SelectorItemAutomationPeer, Provider.IScrollItemProvider
{
	public ListBoxItemDataAutomationPeer(object item, ListBoxAutomationPeer parent) : base(item, parent)
	{

	}

	protected override string GetClassNameCore() => nameof(Controls.ListBoxItem);

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.ListItem;

	public new void ScrollIntoView() => base.ScrollIntoView();
}
