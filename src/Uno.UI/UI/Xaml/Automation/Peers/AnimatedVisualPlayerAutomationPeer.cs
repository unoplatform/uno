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
	/// <summary>
	/// Initializes a new instance of the AnimatedVisualPlayerAutomationPeer class.
	/// </summary>
	/// <param name="owner">The AnimatedVisualPlayer control instance to create the peer for.</param>
	public AnimatedVisualPlayerAutomationPeer(AnimatedVisualPlayer owner) : base(owner)
	{
	}

	protected override string GetClassNameCore()
		=> nameof(AnimatedVisualPlayer);

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.Image;
}
