// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference SemanticZoomAutomationPeer_Partial.cpp, tag winui3/release/1.8.4, commit dc46907e92

#nullable enable

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes SemanticZoom types to Microsoft UI Automation.
/// </summary>
public partial class SemanticZoomAutomationPeer : FrameworkElementAutomationPeer, Provider.IToggleProvider
{
	// Initializes a new instance of the SemanticZoomAutomationPeer class.
	public SemanticZoomAutomationPeer(Controls.SemanticZoom owner) : base(owner)
	{

	}

	// Deconstructor

	protected override object? GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.Toggle)
		{
			return this;
		}
		else
		{
			return base.GetPatternCore(patternInterface);
		}
	}

	protected override string GetClassNameCore() => nameof(Controls.SemanticZoom);

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.SemanticZoom;

	/// <summary>
	/// Cycles through the toggle states of a control.
	/// </summary>
	public void Toggle()
	{
		if (!IsEnabled())
		{
			throw new ElementNotEnabledException();
		}

#if __SKIA__
		((Controls.SemanticZoom)Owner).AutomationSemanticZoomOnToggle();
#else
		((Controls.SemanticZoom)Owner).ToggleActiveView();
#endif
	}

	/// <summary>
	/// Gets a value that indicates whether the Toggle method can be called and result in a toggled view.
	/// </summary>
	public ToggleState ToggleState
	{
		get
		{
			if (((Controls.SemanticZoom)Owner).IsZoomedInViewActive)
			{
				return ToggleState.On;
			}
			else
			{
				return ToggleState.Off;
			}
		}
	}

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
}
