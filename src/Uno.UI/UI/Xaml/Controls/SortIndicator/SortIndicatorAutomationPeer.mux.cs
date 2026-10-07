// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\SortIndicator\SortIndicatorAutomationPeer.cpp, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using Microsoft.UI.Xaml.Automation.Peers;

namespace Microsoft.UI.Private.Controls;

partial class SortIndicatorAutomationPeer
{
	public SortIndicatorAutomationPeer(SortIndicator owner) : base(owner)
	{
	}

	// IAutomationPeerOverrides

	protected override string GetClassNameCore()
	{
		// Fixed string, not hstring_name_of<>: the internal namespace should not reach assistive tech.
		return "SortIndicator";
	}

	protected override AutomationControlType GetAutomationControlTypeCore()
	{
		// Decorative: the owning header announces sort state, so don't surface a separate control.
		return AutomationControlType.Image;
	}

	protected override bool IsControlElementCore() => false;

	protected override bool IsContentElementCore() => false;
}
