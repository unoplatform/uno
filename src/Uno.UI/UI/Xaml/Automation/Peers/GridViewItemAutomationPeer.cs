// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference GridViewItemAutomationPeer_Partial.cpp, tag winui3/release/1.8.4

using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes the data items in the collection of Items in GridView types to UI Automation.
/// </summary>
public partial class GridViewItemAutomationPeer : FrameworkElementAutomationPeer
{
	public GridViewItemAutomationPeer(GridViewItem owner) : base(owner)
	{

	}

	protected override string GetClassNameCore() => nameof(GridViewItem);
}
