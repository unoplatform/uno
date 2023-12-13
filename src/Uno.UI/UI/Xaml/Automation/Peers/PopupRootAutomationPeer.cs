// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\PopupRootAutomationPeer_Partial.cpp, tag winui3/release/1.4.3, commit 685d2bf

#nullable enable

using System;
using System.Collections.Generic;
using DirectUI;
using Windows.UI.Xaml.Automation.Provider;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Automation.Peers;

internal class PopupRootAutomationPeer : FrameworkElementAutomationPeer, IInvokeProvider
{
	// Initializes a new instance of the PopupRootAutomationPeer class.
	public PopupRootAutomationPeer(PopupRoot owner) : base(owner)
	{
	}

	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		bool isEnabled = IsEnabled();
		if (!isEnabled)
		{
			throw new InvalidOperationException("Element not enabled");
		}

		var owner = Owner;
		var isLightDismiss = ((PopupRoot)owner).IsTopmostPopupInLightDismissChain();

		if (isLightDismiss && patternInterface == PatternInterface.Invoke)
		{
			return this;
		}
		else
		{
			return base.GetPatternCore(patternInterface);
		}
	}

	protected override string GetClassNameCore() => nameof(PopupRoot);

	protected override string GetNameCore()
	{
		var owner = Owner;
		bool lightDismiss = ((PopupRoot)owner).IsTopmostPopupInLightDismissChain();

		if (lightDismiss)
		{
			return DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString(UIA_LIGHTDISMISS_NAME);
		}
		else
		{
			// TODO:MZ: What to return here?
			return "";
		}
	}

	protected override string GetAutomationIdCore() => "Light Dismiss";

	protected override AutomationControlType GetAutomationControlTypeCore()
	{
		var owner = Owner;
		bool lightDismiss = ((PopupRoot)owner).IsTopmostPopupInLightDismissChain();

		if (lightDismiss)
		{
			return AutomationControlType.Button;
		}
		else
		{
			// TODO:MZ: What to return here?
			return default;
		}
	}

	protected override bool IsControlElementCore() => true;

	protected override bool IsContentElementCore() => true;

	protected override IList<AutomationPeer> GetChildrenCore()
	{
		// TODO:MZ: What to return here.
		return Array.Empty<AutomationPeer>();
	}

	public void Invoke()
	{
		var isEnabled = IsEnabled();
		if (!isEnabled)
		{
			throw new InvalidOperationException("Element not enabled");
		}

		var owner = Owner;
		owner.CloseTopmostPopup();
	}

	private IEnumerable<AutomationPeer> GetFlowsFromCoreImpl()
	{
		ctl::ComPtr<TrackerCollection<xaml_automation_peers::AutomationPeer*>> spPeers;
		ctl::ComPtr<xaml_automation_peers::IAutomationPeer> spAP;

		IFC_RETURN(ctl::make(&spPeers));
		IFC_RETURN(GetLightDismissingPopupAP(&spAP));
		IFC_RETURN(spPeers->Append(spAP.Get()));

		*returnValue = spPeers.Detach();
	}

	private IEnumerable<AutomationPeer> GetFlowsToCoreImpl()
	{
		ctl::ComPtr<TrackerCollection<xaml_automation_peers::AutomationPeer*>> spPeers;
		ctl::ComPtr<xaml_automation_peers::IAutomationPeer> spAP;

		IFC_RETURN(ctl::make(&spPeers));
		IFC_RETURN(GetLightDismissingPopupAP(&spAP));
		IFC_RETURN(spPeers->Append(spAP.Get()));

		*returnValue = spPeers.Detach();
	}

	private AutomationPeer? GetLightDismissingPopupAP()
	{
		var isEnabled = IsEnabled();
		if (!isEnabled)
		{
			throw new InvalidOperationException("Element not enabled");
		}

		var owner = Owner;
		var lightDismissingPopupDO = ((PopupRoot)owner).GetTopmostPopupInLightDismissChain();
		//if (lightDismissingPopupDO is not null)
		//{
		//	IFC(DXamlCore::GetCurrent()->GetPeer(pLightDismissingPopupDO, &spLightDismissingPopup));
		//}

		if (lightDismissingPopupDO is Popup popup)
		{
			return popup.GetOrCreateAutomationPeer();
		}

		return null;
	}
}
