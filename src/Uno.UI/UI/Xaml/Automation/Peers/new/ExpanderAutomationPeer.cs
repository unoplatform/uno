using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Microsoft.UI.Xaml.Automation.Peers;

public partial class ExpanderAutomationPeer : FrameworkElementAutomationPeer, IExpandCollapseProvider
{
	private const string ExpanderToggleButtonName = "ExpanderToggleButton";

	public ExpanderAutomationPeer(Expander owner) : base(owner)
	{
		ArgumentNullException.ThrowIfNull(owner);
	}

	protected override object GetPatternCore(PatternInterface patternInterface)
		=> patternInterface == PatternInterface.ExpandCollapse ? this : base.GetPatternCore(patternInterface);

	protected override string GetClassNameCore() => nameof(Expander);

	protected override string GetNameCore() => base.GetNameCore();

	protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

	protected override bool HasKeyboardFocusCore()
		=> GetExpanderToggleButtonPeer()?.HasKeyboardFocus() ?? false;

	protected override bool IsKeyboardFocusableCore()
		=> GetExpanderToggleButtonPeer()?.IsKeyboardFocusable() ?? false;

	protected override AutomationPeer? GetPeerFromPointCore(Point point)
	{
		if (GetExpanderToggleButtonPeer() is { } toggleButtonPeer &&
			toggleButtonPeer is FrameworkElementAutomationPeer frameworkElementPeer &&
			frameworkElementPeer.Owner is ToggleButton toggleButton)
		{
			toggleButton.Focus(FocusState.Programmatic);
			return toggleButtonPeer;
		}

		return base.GetPeerFromPointCore(point);
	}

	protected override IList<AutomationPeer>? GetChildrenCore()
	{
		var children = base.GetChildrenCore();
		if (children == null || children.Count == 0)
		{
			return children;
		}

		var peers = new List<AutomationPeer>(children.Count);
		foreach (var peer in children)
		{
			if (peer.GetAutomationId() == ExpanderToggleButtonName)
			{
				var toggleChildren = peer.GetChildren();
				if (toggleChildren != null)
				{
					peers.AddRange(toggleChildren);
				}
				continue;
			}

			peers.Add(peer);
		}

		return peers;
	}

	private AutomationPeer? GetExpanderToggleButtonPeer()
	{
		var childrenPeers = base.GetChildrenCore();
		if (childrenPeers == null)
		{
			return null;
		}

		foreach (var peer in childrenPeers)
		{
			if (peer.GetAutomationId() == ExpanderToggleButtonName)
			{
				return peer;
			}
		}

		return null;
	}

	public ExpandCollapseState ExpandCollapseState
	{
		get
		{
			if (Owner is Expander expander)
			{
				return expander.IsExpanded
					? ExpandCollapseState.Expanded
					: ExpandCollapseState.Collapsed;
			}

			return ExpandCollapseState.Collapsed;
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
		if (ApiInformation.IsEnumNamedValuePresent(
			"Microsoft.UI.Xaml.Automation.Peers.AutomationEvents, Uno.UI",
			nameof(AutomationEvents.PropertyChanged)) &&
			ListenerExists(AutomationEvents.PropertyChanged))
		{
			var oldState = newState == ExpandCollapseState.Expanded
				? ExpandCollapseState.Collapsed
				: ExpandCollapseState.Expanded;

			RaisePropertyChangedEvent(
				ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
				oldState,
				newState);
		}
	}
}
