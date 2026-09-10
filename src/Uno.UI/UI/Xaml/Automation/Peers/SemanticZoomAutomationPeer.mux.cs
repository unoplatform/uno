// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference SemanticZoomAutomationPeer_Partial.cpp, commit 3c9c168844

#nullable enable

using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Automation.Peers;

public partial class SemanticZoomAutomationPeer
{
	// Initializes a new instance of the SemanticZoomAutomationPeer class.
	public SemanticZoomAutomationPeer(Controls.SemanticZoom owner) : base(owner)
	{
	}

	protected override object? GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.Toggle)
		{
			return this;
		}

		return base.GetPatternCore(patternInterface);
	}

	protected override string GetClassNameCore() => nameof(Controls.SemanticZoom);

	protected override AutomationControlType GetAutomationControlTypeCore() =>
		AutomationControlType.SemanticZoom;

	/// <summary>
	/// Cycles through the toggle states of a control.
	/// </summary>
	public void Toggle()
	{
		if (!IsEnabled())
		{
			throw new ElementNotEnabledException();
		}

		((Controls.SemanticZoom)Owner).AutomationSemanticZoomOnToggle();
	}

	/// <summary>
	/// Gets a value that indicates whether the Toggle method can be called and result in a toggled view.
	/// </summary>
	public ToggleState ToggleState =>
		((Controls.SemanticZoom)Owner).IsZoomedInViewActive
			? ToggleState.On
			: ToggleState.Off;

	internal void RaiseToggleStatePropertyChangedEvent(bool newValue)
	{
		var oldState = ToggleState.On;
		var newState = ToggleState.On;

		if (newValue)
		{
			oldState = ToggleState.Off;
		}
		else
		{
			newState = ToggleState.Off;
		}

		RaisePropertyChangedEvent(TogglePatternIdentifiers.ToggleStateProperty, oldState, newState);
	}

	protected override IList<AutomationPeer> GetChildrenCore()
	{
		if ((Owner as Controls.SemanticZoom)?.AutomationGetActivePresenter() is { } activePresenter)
		{
			return GetAutomationPeersForChildrenOfElement(activePresenter);
		}

		return new List<AutomationPeer>();
	}

	internal IList<AutomationPeer> GetAutomationPeerChildren(UIElement presenter) =>
		GetAutomationPeersForChildrenOfElement(presenter);
}
