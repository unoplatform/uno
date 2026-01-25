// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ExpanderAutomationPeer.cpp, tag winui3/release/1.8.4

#nullable enable

using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Automation.Peers;

// WPF ExpanderAutomationPeer:
// https://github.com/dotnet/wpf/blob/main/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Automation/Peers/ExpanderAutomationPeer.cs

/// <summary>
/// Exposes Expander types to Microsoft UI Automation.
/// </summary>
public partial class ExpanderAutomationPeer : FrameworkElementAutomationPeer, IExpandCollapseProvider
{
	private const string c_ExpanderToggleButtonName = "ExpanderToggleButton";

	public ExpanderAutomationPeer(Expander owner) : base(owner)
	{
	}

	// IAutomationPeerOverrides

	protected override object? GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.ExpandCollapse)
		{
			return this;
		}

		return base.GetPatternCore(patternInterface);
	}

	protected override string GetClassNameCore()
	{
		// WPF uses "Expander" as its class name
		return nameof(Expander);
	}

	protected override string GetNameCore()
	{
		return base.GetNameCore();
	}

	protected override AutomationControlType GetAutomationControlTypeCore()
	{
		// Expander has "Button" as its default control type core
		return AutomationControlType.Button;
	}

	private AutomationPeer? GetExpanderToggleButtonPeer()
	{
		// We are not going to call the overriden one because that one doesn't have the toggle button.
		var childrenPeers = base.GetChildrenCore();

		if (childrenPeers == null)
		{
			return null;
		}

		foreach (var peer in childrenPeers)
		{
			if (peer.GetAutomationId() == c_ExpanderToggleButtonName)
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
	protected override AutomationPeer? GetPeerFromPointCore(Point point)
	{
		if (GetExpanderToggleButtonPeer() is { } toggleButtonPeer)
		{
			if (toggleButtonPeer is FrameworkElementAutomationPeer frameworkElementPeer &&
				frameworkElementPeer.Owner is ToggleButton toggleButton)
			{
				toggleButton.Focus(FocusState.Programmatic);
				return toggleButtonPeer;
			}
		}

		return base.GetPeerFromPointCore(point);
	}

	// We are going to take out the toggle button off the children, because we are setting
	// the toggle button's event source to this automation peer. This removes any cyclical
	// dependency.
	protected override IList<AutomationPeer>? GetChildrenCore()
	{
		var childrenPeers = base.GetChildrenCore();
		if (childrenPeers == null || childrenPeers.Count == 0)
		{
			return childrenPeers;
		}

		var peers = new List<AutomationPeer>(childrenPeers.Count - 1);
		foreach (var peer in childrenPeers)
		{
			if (peer.GetAutomationId() != c_ExpanderToggleButtonName)
			{
				peers.Add(peer);
			}
			else
			{
				// If it is ExpanderToggleButton, we want to exclude it but add its children into the peer
				var expanderToggleButtonChildrenPeers = peer.GetChildren();
				if (expanderToggleButtonChildrenPeers != null)
				{
					foreach (var expanderHeaderPeer in expanderToggleButtonChildrenPeers)
					{
						peers.Add(expanderHeaderPeer);
					}
				}
			}
		}

		return peers;
	}

	// IExpandCollapseProvider

	public ExpandCollapseState ExpandCollapseState
	{
		get
		{
			var state = ExpandCollapseState.Collapsed;
			if (Owner is Expander expander)
			{
				state = expander.IsExpanded
					? ExpandCollapseState.Expanded
					: ExpandCollapseState.Collapsed;
			}

			return state;
		}
	}

	public void Expand()
	{
		if (Owner is Expander expander)
		{
			expander.IsExpanded = true;
			RaiseExpandCollapseAutomationEvent(ExpandCollapseState.Expanded);
		}
	}

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
		if (AutomationPeer.ListenerExists(AutomationEvents.PropertyChanged))
		{
			var oldState = newState == ExpandCollapseState.Expanded
				? ExpandCollapseState.Collapsed
				: ExpandCollapseState.Expanded;

			// if box_value(oldState) doesn't work here, use ReferenceWithABIRuntimeClassName to make Narrator unbox it.
			RaisePropertyChangedEvent(
				ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
				oldState,
				newState);
		}
	}
}
