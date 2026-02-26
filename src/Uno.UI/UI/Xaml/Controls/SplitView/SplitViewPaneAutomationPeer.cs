// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference SplitViewPaneAutomationPeer_Partial.cpp/.h, commit 69097129a

#nullable enable

using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Exposes SplitView pane types to Microsoft UI Automation.
/// Implements the Window pattern when the pane is light-dismissible (modal window).
/// </summary>
internal partial class SplitViewPaneAutomationPeer : FrameworkElementAutomationPeer, IWindowProvider
{
	public SplitViewPaneAutomationPeer(FrameworkElement owner) : base(owner)
	{
	}

	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.Window && IsWindowContextEnabled())
		{
			return this;
		}

		return base.GetPatternCore(patternInterface)!;
	}

	protected override string GetClassNameCore() => "SplitViewPane";

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.Window;

	public bool IsModal => true;

	public bool IsTopmost => true;

	public bool Maximizable => false;

	public bool Minimizable => false;

	public WindowInteractionState InteractionState => WindowInteractionState.Running;

	public WindowVisualState VisualState => WindowVisualState.Normal;

	public void Close()
	{
		// No-op per WinUI implementation.
	}

	public void SetVisualState(WindowVisualState state)
	{
		// No-op per WinUI implementation.
	}

	public bool WaitForInputIdle(int milliseconds) => false;

	private bool IsWindowContextEnabled()
	{
		if (Owner is FrameworkElement ownerFE &&
			ownerFE.TemplatedParent is SplitView splitView)
		{
			return splitView.CanLightDismiss();
		}

		return false;
	}
}
