// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference PopupRoot_Partial.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

using Microsoft.UI.Xaml.Automation.Peers;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class PopupRoot
{
	// Create PopupRootAutomationPeer to represent the PopupRoot.
	protected override AutomationPeer? OnCreateAutomationPeer()
		=> IsTopmostPopupInLightDismissChain() ? new PopupRootAutomationPeer(this) : null;

	// CoreImports::PopupRoot_CloseTopmostPopup
	internal void CloseTopmostPopup() => CloseTopmostPopup(FocusState.Programmatic, PopupFilter.LightDismissOnly);
}
