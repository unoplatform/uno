// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\MenuFlyoutPresenterAutomationPeer_Partial.cpp, tag winui3/release/1.5.4, commit 98a60c8

using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes MenuFlyoutPresenter types to Microsoft UI Automation.
/// </summary>
public partial class MenuFlyoutPresenterAutomationPeer : ItemsControlAutomationPeer
{
	/// <summary>
	/// Initializes a new instance of the MenuFlyoutPresenterAutomationPeer class.
	/// </summary>
	/// <param name="owner">The owner element to create for.</param>
	public MenuFlyoutPresenterAutomationPeer(MenuFlyoutPresenter owner) : base(owner)
	{
	}

	protected override string GetAutomationIdCore()
	{
		var result = base.GetAutomationIdCore();
		if (string.IsNullOrEmpty(result))
		{
			result = ((MenuFlyoutPresenter)Owner).GetOwnerName();
		}
		return result;
	}

	protected override string GetClassNameCore() => nameof(MenuFlyoutPresenter);

	protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Menu;

	protected override ItemAutomationPeer OnCreateItemAutomationPeer(object item)
	{
		// Not providing automation peer for separator element
		if (item is MenuFlyoutSeparator)
		{
			return null;
		}

		return new ItemAutomationPeer(item, this); // TODO:MZ: Currently throws
	}
}
