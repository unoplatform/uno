// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\SelectorBar\SelectorBarItemAutomationPeer.cpp, tag winui3/release/1.8.4

#nullable enable

using Microsoft.UI.Xaml.Controls;
using Uno.UI.Helpers.WinUI;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes SelectorBarItem types to Microsoft UI Automation.
/// </summary>
public partial class SelectorBarItemAutomationPeer : ItemContainerAutomationPeer
{
	/// <summary>
	/// Initializes a new instance of the SelectorBarItemAutomationPeer class.
	/// </summary>
	/// <param name="owner"></param>
	public SelectorBarItemAutomationPeer(SelectorBarItem owner) : base(owner)
	{
	}

	protected override string GetNameCore()
	{
		// First choice given to the AutomationProperties.Name.
		string returnString = AutomationProperties.GetName(Owner);

		if (string.IsNullOrEmpty(returnString))
		{
			// Second choice given to the SelectorBarItem.Text property.
			if (Owner is SelectorBarItem selectorBarItem)
			{
				returnString = selectorBarItem.Text;
			}
		}

		if (string.IsNullOrEmpty(returnString))
		{
			// Third choice given to the ItemContainer.Child property.
			if (Owner is ItemContainer itemContainer)
			{
				returnString = SharedHelpers.TryGetStringRepresentationFromObject(itemContainer.Child);
			}
		}

		if (string.IsNullOrEmpty(returnString))
		{
			// Fourth choice given to the localized control name.
			returnString = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_SelectorBarItemDefaultControlName);
		}

		return returnString;
	}

	protected override string GetLocalizedControlTypeCore() =>
		ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_SelectorBarItemDefaultControlName);

	protected override string GetClassNameCore() => nameof(SelectorBarItem);
}
