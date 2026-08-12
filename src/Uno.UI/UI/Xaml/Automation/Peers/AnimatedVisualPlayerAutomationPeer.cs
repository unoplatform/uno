// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference AnimatedVisualPlayerAutomationPeer.h/.cpp, commit 3cae15f0

#nullable enable

using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes AnimatedVisualPlayer types to Microsoft UI Automation.
/// </summary>
public partial class AnimatedVisualPlayerAutomationPeer : FrameworkElementAutomationPeer
{
	public AnimatedVisualPlayerAutomationPeer(AnimatedVisualPlayer owner) : base(owner)
	{
	}

	protected override string GetClassNameCore()
		=> nameof(AnimatedVisualPlayer);

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.Image;
}
