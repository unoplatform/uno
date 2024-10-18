// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference GridViewHeaderItemAutomationPeer_Partial.cpp, tag winui3/release/1.4.2

using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes GridViewHeaderItem types to Microsoft UI Automation.
/// </summary>
public partial class GridViewHeaderItemAutomationPeer : ListViewBaseHeaderItemAutomationPeer
{
	public GridViewHeaderItemAutomationPeer(GridViewHeaderItem owner) : base(owner)
	{

	}

	protected override string GetClassNameCore() => nameof(GridViewHeaderItem);
}
