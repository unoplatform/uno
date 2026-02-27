// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\MenuFlyoutItemAutomationPeer_Partial.cpp, tag winui3/release/1.5.4, commit 98a60c8

using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes MenuFlyoutItem types to Microsoft UI Automation.
/// </summary>
public partial class MenuFlyoutItemAutomationPeer : FrameworkElementAutomationPeer, IInvokeProvider
{
	/// <summary>
	/// Initializes a new instance of the MenuFlyoutItemAutomationPeer class.
	/// </summary>
	/// <param name="owner">The owner element to create for.</param>
	public MenuFlyoutItemAutomationPeer(MenuFlyoutItem owner) : base(owner)
	{
	}

	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.Invoke)
		{
			return this;
		}

		return base.GetPatternCore(patternInterface);
	}

	protected override string GetClassNameCore() => nameof(MenuFlyoutItem);

	protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.MenuItem;

	protected override string GetAcceleratorKeyCore()
	{
		var returnValue = base.GetAcceleratorKeyCore();
		if (string.IsNullOrEmpty(returnValue))
		{
			var owner = Owner as MenuFlyoutItem;
			if (owner != null)
			{
				var keyboardAcceleratorTextOverride = owner.KeyboardAcceleratorTextOverride;
				returnValue = GetTrimmedKeyboardAcceleratorTextOverride(keyboardAcceleratorTextOverride);
			}
		}
		return returnValue;
	}

	protected override int GetPositionInSetCore()
	{
		// First retrieve any valid value being directly set on the container, that value will get precedence.
		var returnValue = base.GetPositionInSetCore();

		// if it still is default value, calculate it ourselves.
		if (returnValue == -1)
		{
			returnValue = MenuFlyoutPresenter.GetPositionInSetHelper((MenuFlyoutItemBase)Owner);
		}

		return returnValue;
	}

	protected override int GetSizeOfSetCore()
	{
		// First retrieve any valid value being directly set on the container, that value will get precedence.
		var returnValue = base.GetSizeOfSetCore();

		// if it still is default value, calculate it ourselves.
		if (returnValue == -1)
		{
			returnValue = MenuFlyoutPresenter.GetSizeOfSetHelper((MenuFlyoutItemBase)Owner);
		}

		return returnValue;
	}

	/// <summary>
	/// Sends a request to invoke the menu flyout associated with the automation peer.
	/// </summary>
	public void Invoke()
	{
		if (!IsEnabled())
		{
			throw new ElementNotEnabledException();
		}

		((MenuFlyoutItem)Owner).Invoke();
	}
}
