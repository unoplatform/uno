// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference LandmarkTargetAutomationPeer_Partial.cpp

#nullable enable

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Automation peer used to force a <see cref="FrameworkElement"/> that sets
/// <see cref="AutomationProperties.LandmarkTypeProperty"/> or
/// <see cref="AutomationProperties.LocalizedLandmarkTypeProperty"/> (but isn't otherwise a
/// control and wouldn't otherwise be in the UIA tree) into the tree, so the landmark reaches
/// UIA clients. Mirrors WinUI's LandmarkTargetAutomationPeer, which is an internal implementation
/// peer (not part of the public API surface) reporting <see cref="AutomationControlType.Group"/>.
/// </summary>
internal partial class LandmarkTargetAutomationPeer : FrameworkElementAutomationPeer
{
	public LandmarkTargetAutomationPeer(FrameworkElement owner) : base(owner)
	{
	}

	protected override string GetClassNameCore() => "LandmarkTarget";

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.Group;
}
