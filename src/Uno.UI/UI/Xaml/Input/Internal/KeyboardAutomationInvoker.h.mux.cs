// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\components\input\inc\KeyboardAutomationInvoker.h, tag winui3/release/1.4.3, commit 685d2bf

using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Automation.Provider;
using Uno.Foundation.Logging;
using static DirectUI.WinError;

namespace Windows.UI.Xaml.Input;

internal static class KeyboardAutomationInvoker
{
	// TODO Uno: This is a adjustment as GetPatternInterface is not needed in Uno currently.
	internal static object GetPatternInterface(this object pattern) => pattern;

	// This function is used as the default way to "invoke" a piece of UI through AccessKeys or Keyboard Accelerators.
	// It is not meant to be exhaustive, simply the most valuable defaults for those features.  The order of attempts
	// here is publicly documented.
	internal static bool InvokeAutomationAction(DependencyObject pDO)
	{
		// This implementation is adapted from the VuiButtonBehavior.cpp from the xbox shell for voice UI automation
		// Implementation differs from the XBOX in that we do not move focus

		// GetAutomationPeer does not automatically create a peer if it does not exist.
		if (pDO is not FrameworkElement frameworkElement) // TODO Uno: In WinUI there is no such check, but not important currently.
		{
			return false;
		}

		AutomationPeer ownerAutomationPeer = frameworkElement.GetAutomationPeer();
		if (ownerAutomationPeer is null)
		{
			// let's try creating an automation peer
			ownerAutomationPeer = frameworkElement.OnCreateAutomationPeerInternal();
			// Want to print a message when there is no automation peer for the owner and the item is not a scope (and can be navigated into).
			if (ownerAutomationPeer is null && !AccessKeys.IsAccessKeyScope(pDO))
			{
				if (typeof(KeyboardAutomationInvoker).Log().IsEnabled(LogLevel.Debug))
				{
					typeof(KeyboardAutomationInvoker).Log().LogDebug("An automation peer for this component could not be found or created, meaning we are unable to attempt an invoke on this element. Consider implementing desired behavior in the event handler for AccessKeyInvoked or KeyboardAccelerator.Invoked.");
				}
				return false;
			}
		}

		// For the next methods, the logic is to first get the IUIAProvider for a specific automation pattern.
		// Then, if the IUIAProvider is not null, get the automation pattern interface for the provider. 
		// The automation pattern interface iteself exposes the available automation methods.
		var uIAInvokeProvider = ownerAutomationPeer.GetPattern(PatternInterface.Invoke);
		if (uIAInvokeProvider is not null) //For e.g. Button
		{
			var invokeProvider = (IInvokeProvider)(uIAInvokeProvider.GetPatternInterface());
			if (invokeProvider is not null)
			{
				try
				{
					invokeProvider.Invoke();
					return true;
				}
				catch
				{
					return false;
				}
			}
		}

		var uIAToggleProvider = ownerAutomationPeer.GetPattern(PatternInterface.Toggle);
		if (uIAToggleProvider is not null)  //E.g. CheckBox, AppBar
		{
			var toggleProvider = (IToggleProvider)(uIAToggleProvider.GetPatternInterface());
			if (toggleProvider is not null)
			{
				try
				{
					toggleProvider.Toggle();
					return true;
				}
				catch
				{
					return false;
				}
			}
		}

		var uIASelectionItemProvider = ownerAutomationPeer.GetPattern(PatternInterface.SelectionItem);
		if (uIASelectionItemProvider is not null) //For e.g. RadioButton
		{
			var selectionItemProvider = (ISelectionItemProvider)(uIASelectionItemProvider.GetPatternInterface());
			if (selectionItemProvider is not null)
			{
				return SUCCEEDED(() => selectionItemProvider.Select());
			}
		}

		var uIAExpandCollapseProvider = ownerAutomationPeer.GetPattern(PatternInterface.ExpandCollapse);
		if (uIAExpandCollapseProvider is not null) //For e.g. Combobox
		{
			var expandCollapseProvider = (IExpandCollapseProvider)(uIAExpandCollapseProvider.GetPatternInterface());
			if (expandCollapseProvider is not null)
			{
				ExpandCollapseState state = expandCollapseProvider.ExpandCollapseState;

				if (state == ExpandCollapseState.Expanded)
				{
					return SUCCEEDED(() => expandCollapseProvider.Collapse());
				}
				else
				{
					return SUCCEEDED(() => expandCollapseProvider.Expand());
				}
			}
		}

		// At this point we diverge from the XBox implementation. XBOX shell includes support for list view multi-selection 
		// as well as combo box dismiss when a combobox item was selected
		// For now this will wait for PM guidance as far as how this looks from an access keys standpoint.
		return false;
	}
}
