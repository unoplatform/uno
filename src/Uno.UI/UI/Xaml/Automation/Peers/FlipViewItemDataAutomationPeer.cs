// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable
#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

public partial class FlipViewItemDataAutomationPeer : SelectorItemAutomationPeer, IScrollItemProvider
{
	public FlipViewItemDataAutomationPeer(object item, FlipViewAutomationPeer parent) : base(item, parent)
	{
	}

	protected override string GetClassNameCore() => nameof(FlipViewItem);

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.ListItem;

	public new void ScrollIntoView() => base.ScrollIntoView();
}
