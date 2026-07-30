// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference MediaPlayerElementAutomationPeer_Partial.cpp, tag winui3/release/1.8.4

using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes <see cref="MediaPlayerElement"/> to Microsoft UI Automation.
/// </summary>
public partial class MediaPlayerElementAutomationPeer : FrameworkElementAutomationPeer
{
	public MediaPlayerElementAutomationPeer(MediaPlayerElement owner) : base(owner)
	{
	}

	protected override string GetClassNameCore() => nameof(MediaPlayerElementAutomationPeer);

	protected override string GetLocalizedControlTypeCore()
		=> DirectUI.DXamlCore.GetCurrentNoCreate()?.GetLocalizedResourceString("UIA_AP_MEDIAPLAYERELEMENT") ?? base.GetLocalizedControlTypeCore();
}
