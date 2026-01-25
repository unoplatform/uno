// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference SemanticZoomAutomationPeer_Partial.cpp, tag winui3/release/1.8.4
namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes SemanticZoom types to Microsoft UI Automation.
/// </summary>
public partial class SemanticZoomAutomationPeer : FrameworkElementAutomationPeer, Provider.IToggleProvider
{
	public SemanticZoomAutomationPeer(Controls.SemanticZoom owner) : base(owner)
	{

	}

	protected override object GetPatternCore(PatternInterface patternInterface)
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

		(Owner as Controls.SemanticZoom).ToggleActiveView();
	}

	/// <summary>
	/// Gets a value that indicates whether the Toggle method can be called and result in a toggled view.
	/// </summary>
	public ToggleState ToggleState
	{
		get
		{
			if ((Owner as Controls.SemanticZoom).IsZoomedInViewActive)
			{
				return ToggleState.On;
			}
			else
			{
				return ToggleState.Off;
			}
		}
	}
}
