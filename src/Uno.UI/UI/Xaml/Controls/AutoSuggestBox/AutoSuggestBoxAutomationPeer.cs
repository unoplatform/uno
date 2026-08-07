// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference AutoSuggestBoxAutomationPeer_Partial.cpp, tag winui3/release/1.7.1

using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes AutoSuggestBox types to Microsoft UI Automation.
/// </summary>
public partial class AutoSuggestBoxAutomationPeer
	: FrameworkElementAutomationPeer, Provider.IInvokeProvider
{
	/// <summary>
	/// Initializes a new instance of the AutoSuggestBoxAutomationPeer class.
	/// </summary>
	/// <param name="owner">The AutoSuggestBox control that is associated with this AutoSuggestBoxAutomationPeer.</param>
	public AutoSuggestBoxAutomationPeer(AutoSuggestBox owner) : base(owner)
	{
	}

	/// <summary>
	/// Gets the name of the control class associated with the peer.
	/// </summary>
	/// <returns>The control class name.</returns>
	protected override string GetClassNameCore() => nameof(AutoSuggestBox);

	/// <summary>
	/// Gets the control type for the element that is associated with this AutoSuggestBoxAutomationPeer.
	/// </summary>
	/// <returns>A value of the enumeration.</returns>
	protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

	/// <summary>
	/// Gets the control pattern for the element that is associated with this AutoSuggestBoxAutomationPeer.
	/// </summary>
	/// <param name="patternInterface">The type of pattern to retrieve.</param>
	/// <returns>The requested pattern, or null if the pattern is not supported.</returns>
	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.Invoke)
		{
			return this;
		}

		return base.GetPatternCore(patternInterface);
	}

	/// <summary>
	/// Sends a request to submit the auto-suggest query to the AutoSuggestBox associated with the automation peer.
	/// </summary>
	public void Invoke()
		=> (Owner as AutoSuggestBox)?.ProgrammaticSubmitQuery();
}
