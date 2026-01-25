// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable
#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using System;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

public partial class ListViewHeaderItemAutomationPeer : ListViewBaseHeaderItemAutomationPeer
{
	public ListViewHeaderItemAutomationPeer(ListViewHeaderItem owner) : base(owner)
	{
		ArgumentNullException.ThrowIfNull(owner);
	}

	protected override string GetClassNameCore() => nameof(ListViewHeaderItem);
}
