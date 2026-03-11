// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable
using Microsoft.UI.Xaml.Automation.Provider;

namespace Microsoft.UI.Xaml.Automation.Peers;

public partial class ComboBoxItemDataAutomationPeer : SelectorItemAutomationPeer, IScrollItemProvider
{
	public ComboBoxItemDataAutomationPeer(object item, ComboBoxAutomationPeer parent) : base(item, parent)
	{
	}

	protected override string GetClassNameCore() => nameof(Controls.ComboBoxItem);

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.ListItem;

	public new void ScrollIntoView() => base.ScrollIntoView();

	public new void Select()
	{
		base.Select();
		CollapseParentComboBox();
	}

	public new void AddToSelection()
	{
		base.AddToSelection();
		CollapseParentComboBox();
	}

	public new void RemoveFromSelection()
	{
		// ComboBox does not allow deselecting the current item through automation.
	}

	private void CollapseParentComboBox()
	{
		if (ItemsControlAutomationPeer is IExpandCollapseProvider provider)
		{
			provider.Collapse();
		}
	}
}
