// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference SplitViewLightDismissAutomationPeer_Partial.cpp/.h, commit 69097129a

#nullable enable

using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Exposes SplitView light dismiss layer to Microsoft UI Automation.
/// Implements the Invoke pattern to trigger light dismiss.
/// </summary>
internal partial class SplitViewLightDismissAutomationPeer : FrameworkElementAutomationPeer, IInvokeProvider
{
	public SplitViewLightDismissAutomationPeer(FrameworkElement owner) : base(owner)
	{
	}

	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.Invoke && IsLightDismissEnabled())
		{
			return this;
		}

		return base.GetPatternCore(patternInterface)!;
	}

	protected override string GetClassNameCore() => "SplitViewLightDismiss";

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.Button;

	protected override string GetNameCore()
	{
		// TODO Uno: Return localized "Light Dismiss" string via ResourceAccessor
		return "Light Dismiss";
	}

	protected override string GetAutomationIdCore() => "LightDismiss";

	public void Invoke()
	{
		if (Owner is FrameworkElement ownerFE &&
			ownerFE.TemplatedParent is SplitView splitView &&
			splitView.CanLightDismiss())
		{
			splitView.TryCloseLightDismissiblePane();
		}
	}

	private bool IsLightDismissEnabled()
	{
		if (Owner is FrameworkElement ownerFE &&
			ownerFE.TemplatedParent is SplitView splitView)
		{
			return splitView.CanLightDismiss();
		}

		return false;
	}
}
