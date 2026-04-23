// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference GroupItemAutomationPeer_Partial.h, commit 5f9e85113

#nullable enable

using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

// Represents the GroupItemAutomationPeer
/// <summary>
/// Exposes <see cref="GroupItem"/> types to Microsoft UI Automation.
/// </summary>
public partial class GroupItemAutomationPeer : FrameworkElementAutomationPeer
{
	/// <summary>
	/// Initializes a new instance of the <see cref="GroupItemAutomationPeer"/> class.
	/// </summary>
	public GroupItemAutomationPeer(GroupItem owner)
		: base(owner)
	{
	}
}
