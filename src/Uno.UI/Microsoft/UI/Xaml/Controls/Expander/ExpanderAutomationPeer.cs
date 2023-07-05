#nullable disable

using System.Collections.Generic;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Automation.Provider;
using Windows.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Exposes Expander types to Microsoft UI Automation.
/// </summary>
public class ExpanderAutomationPeer : FrameworkElementAutomationPeer, IExpandCollapseProvider
{
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

	protected override bool HasKeyboardFocusCore()
	{
		// We are not going to call the overriden one because that one doesn't have the toggle button.
		var childrenPeers = base.GetChildrenCore();

		foreach (var peer in childrenPeers)
		{
			if (peer.GetAutomationId() == "ExpanderToggleButton")
			{
				// Since the EventsSource of the toggle button
				// is the same as the expander's, we need to
				// redirect the focus of the expander and base it on the toggle button's.
				return peer.HasKeyboardFocus();
			}
		}

		// If the toggle button doesn't have the current focus, then
		// the expander's not focused.
		return false;
	}

	// This function gets called when there's narrator and the user is trying to touch the expander
	// If this happens, we will return the toggle button's peer and focus it programmatically,
	// to synchronize this touch focus with the keyboard one.
	protected override AutomationPeer GetPeerFromPointCore(Point point)
	{
		var childrenPeers = base.GetChildrenCore();

		foreach (var peer in childrenPeers)
		{
			if (peer.GetAutomationId() == "ExpanderToggleButton")
			{
				var frameworkElementPeer = peer as FrameworkElementAutomationPeer;
				var toggleButton = frameworkElementPeer.Owner as ToggleButton;
				toggleButton?.Focus(FocusState.Programmatic);
				return peer;
			}
		}

		return base.GetPeerFromPointCore(point);
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
			if (peer.GetAutomationId() != "ExpanderToggleButton")
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
		if (ApiInformation.IsEnumNamedValuePresent(
			"Windows.UI.Xaml.Automation.Peers.AutomationEvents",
			nameof(AutomationEvents.PropertyChanged)))
		{
			if (ListenerExists(AutomationEvents.PropertyChanged))
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
}
