// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ExpanderAutomationPeer.cpp, tag winui3/release/1.4.2

using System.Collections.Generic;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Automation.Provider;
using Windows.UI.Xaml.Controls.Primitives;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

/// <summary>
/// Exposes Expander types to Microsoft UI Automation.
/// </summary>
public class ExpanderAutomationPeer : FrameworkElementAutomationPeer, IExpandCollapseProvider
{
	private const string s_ExpanderToggleButtonName = "ExpanderToggleButton";

	/// <summary>
	/// Initializes a new instance of the ExpanderAutomationPeer class.
	/// </summary>
	/// <param name="owner"></param>
	public ExpanderAutomationPeer(Expander owner) : base(owner)
	{
	}

	// IAutomationPeerOverrides

	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.ExpandCollapse)
		{
			return this;
		}

		return base.GetPatternCore(patternInterface);
	}

	protected override string GetClassNameCore() => nameof(Expander);

	protected override string GetNameCore() => base.GetNameCore();

	protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

	private AutomationPeer GetExpanderToggleButtonPeer()
	{
		// We are not going to call the overriden one because that one doesn't have the toggle button.
		var childrenPeers = base.GetChildrenCore();
		// 	auto childrenPeers = GetInner().as<winrt::IAutomationPeerOverrides>().GetChildrenCore();

		foreach (var peer in childrenPeers)
		{
			if (peer.GetAutomationId() == s_ExpanderToggleButtonName)
			{
				return peer;
			}
		}

		return null;
	}

	protected override bool HasKeyboardFocusCore()
	{
		if (GetExpanderToggleButtonPeer() is { } toggleButtonPeer)
		{
			// Since the EventsSource of the toggle button
			// is the same as the expander's, we need to
			// redirect the focus of the expander and base it on the toggle button's.
			return toggleButtonPeer.HasKeyboardFocus();
		}

		// If the toggle button doesn't have the current focus, then
		// the expander's not focused.
		return false;
	}

	protected override bool IsKeyboardFocusableCore()
	{
		if (GetExpanderToggleButtonPeer() is { } toggleButtonPeer)
		{
			return toggleButtonPeer.IsKeyboardFocusable();
		}

		return false;
	}

	// This function gets called when there's narrator and the user is trying to touch the expander
	// If this happens, we will return the toggle button's peer and focus it programmatically,
	// to synchronize this touch focus with the keyboard one.
	protected override AutomationPeer GetPeerFromPointCore(Point point)
	{
		var childrenPeers = base.GetChildrenCore();

		if (GetExpanderToggleButtonPeer() is { } toggleButtonPeer)
		{
			var frameworkElementPeer = toggleButtonPeer as FrameworkElementAutomationPeer;
			var toggleButton = frameworkElementPeer?.Owner as ToggleButton;
			toggleButton?.Focus(FocusState.Programmatic);
			return toggleButtonPeer;
		}

		return GetPeerFromPointCore(point);
	}

	// We are going to take out the toggle button off the children, because we are setting
	// the toggle button's event source to this automation peer. This removes any cyclical
	// dependency.
	protected override IList<AutomationPeer> GetChildrenCore()
	{
		var childrenPeers = base.GetChildrenCore();
		var peers = new List<AutomationPeer>(childrenPeers.Count - 1);
		foreach (var peer in childrenPeers)
		{
			if (peer.GetAutomationId() != s_ExpanderToggleButtonName)
			{
				peers.Add(peer);
			}
			else
			{
				// If it is ExpanderToggleButton, we want to exclude it but add its children into the peer 
				var expanderToggleButtonChildrenPeers = peer.GetChildren();
				foreach (var expanderHeaderPeer in expanderToggleButtonChildrenPeers)
				{
					peers.Add(expanderHeaderPeer);
				}
			}
		}

		return peers;
	}

	// IExpandCollapseProvider

	//Uno Doc:
	/// <summary>
	/// Gets the value of the Expander.IsExpanded property and returns
	/// whether the Expander is currently expanded or collapsed.
	/// </summary>
	public ExpandCollapseState ExpandCollapseState
	{
		get
		{
			var state = ExpandCollapseState.Collapsed;

			if (Owner is Expander expander)
			{
				state = expander.IsExpanded ?
					ExpandCollapseState.Expanded :
					ExpandCollapseState.Collapsed;
			}

			return state;
		}
	}

	/// <summary>
	/// Displays the content area of the control.
	/// </summary>
	public void Expand()
	{
		if (Owner is Expander expander)
		{
			expander.IsExpanded = true;
			RaiseExpandCollapseAutomationEvent(ExpandCollapseState.Expanded);
		}
	}

	/// <summary>
	/// Hides the content area of the control.
	/// </summary>
	public void Collapse()
	{
		if (Owner is Expander expander)
		{
			expander.IsExpanded = false;
			RaiseExpandCollapseAutomationEvent(ExpandCollapseState.Collapsed);
		}
	}

	internal void RaiseExpandCollapseAutomationEvent(ExpandCollapseState newState)
	{
		// Uno Doc: AutomationEvents not currently implemented so added an API check
		// if (winrt::AutomationPeer::ListenerExists(winrt::AutomationEvents::PropertyChanged))
		if (ApiInformation.IsEnumNamedValuePresent(
			"Windows.UI.Xaml.Automation.Peers.AutomationEvents",
			nameof(AutomationEvents.PropertyChanged)) && ListenerExists(AutomationEvents.PropertyChanged))
		{
			ExpandCollapseState oldState = (newState == ExpandCollapseState.Expanded) ?
				ExpandCollapseState.Collapsed :
				ExpandCollapseState.Expanded;

			// if box_value(oldState) doesn't work here, use ReferenceWithABIRuntimeClassName to make Narrator unbox it.
			RaisePropertyChangedEvent(ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
				oldState,
				newState);
		}
	}
}
