// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference ButtonBaseAutomationPeer_Partial.cpp, tag winui3/release/1.8.4

#nullable enable

using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;


namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes ButtonBase types to UI Automation.
/// </summary>
public partial class ButtonBaseAutomationPeer : FrameworkElementAutomationPeer
{
	protected ButtonBaseAutomationPeer(ButtonBase owner) : base(owner)
	{
	}

	protected ButtonBaseAutomationPeer(ButtonBaseAutomationPeer buttonBase) : base(buttonBase)
	{
	}

	protected override IList<AutomationPeer> GetChildrenCore()
	{
		var children = base.GetChildrenCore() ?? new List<AutomationPeer>();

		// When a button-attached flyout is open, the flyout presenter is hosted under
		// PopupRoot rather than as a visual descendant of the button. Surface the
		// flyout presenter peer as a logical child so UIA navigation flows
		// Button -> FlyoutPresenter -> inner controls, matching WinUI's tree.
		if (GetOpenFlyoutBase() is { } flyout
			&& flyout.GetPresenter() is { } presenter
			&& presenter.GetOrCreateAutomationPeer() is { } presenterPeer
			&& !children.Contains(presenterPeer))
		{
			children.Add(presenterPeer);
		}

		return children;
	}

	private FlyoutBase? GetOpenFlyoutBase()
	{
		FlyoutBase? flyout = Owner switch
		{
			Button button => button.Flyout,
			SplitButton splitButton => splitButton.Flyout,
			_ => null,
		};

		return flyout is { IsOpen: true } ? flyout : null;
	}

	// 	IFACEMETHODIMP ButtonBaseAutomationPeer::GetNameCore(_Out_ HSTRING* returnValue)
	// {
	//     HRESULT hr = S_OK;

	//     xaml::IUIElement* pOwner = NULL;
	//     IInspectable* pContent = NULL;

	//     IFC(ButtonBaseAutomationPeerGenerated::GetNameCore(returnValue));

	//     if (*returnValue == NULL)
	//     {
	//         IFC(get_Owner(&pOwner));
	//         IFCPTR(pOwner);
	//         IFC((static_cast<ButtonBase*>(pOwner))->get_Content(&pContent));
	//         IFC(IValueBoxer::UnboxValue(pContent, returnValue));
	//     }

	// Cleanup:
	//     ReleaseInterface(pOwner);
	//     ReleaseInterface(pContent);

	//     RRETURN(hr);
	// }



}
