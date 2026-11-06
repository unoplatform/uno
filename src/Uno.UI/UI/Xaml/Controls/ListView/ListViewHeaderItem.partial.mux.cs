// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ListViewHeaderItem_Partial.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d

#nullable enable

using Microsoft.UI.Xaml.Automation.Peers;

namespace Microsoft.UI.Xaml.Controls;

partial class ListViewHeaderItem
{
	// Create ListViewHeaderItemAutomationPeer to represent the ListViewHeaderItem.
	protected override AutomationPeer OnCreateAutomationPeer() => new ListViewHeaderItemAutomationPeer(this);
}
