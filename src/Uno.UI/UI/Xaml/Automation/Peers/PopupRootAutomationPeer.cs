// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference PopupRootAutomationPeer_Partial.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

using System.Collections.Generic;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Automation.Peers;

internal partial class PopupRootAutomationPeer : FrameworkElementAutomationPeer, IInvokeProvider
{
	public PopupRootAutomationPeer(PopupRoot owner) : base(owner)
	{
	}

	private PopupRoot PopupRootOwner => (PopupRoot)Owner;

	protected override object? GetPatternCore(PatternInterface patternInterface)
	{
		if (!IsEnabled())
		{
			throw new ElementNotEnabledException();
		}

		if (PopupRootOwner.IsTopmostPopupInLightDismissChain() && patternInterface == PatternInterface.Invoke)
		{
			return this;
		}

		return base.GetPatternCore(patternInterface);
	}

	protected override string GetClassNameCore() => "PopupRoot";

	protected override string? GetNameCore()
		=> PopupRootOwner.IsTopmostPopupInLightDismissChain()
			? DirectUI.DXamlCore.GetCurrentNoCreate()?.GetLocalizedResourceString("UIA_LIGHTDISMISS_NAME")
			: null;

	protected override string GetAutomationIdCore() => "Light Dismiss";

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.Button;

	protected override bool IsControlElementCore() => true;

	protected override bool IsContentElementCore() => true;

	protected override IList<AutomationPeer>? GetChildrenCore() => null;

	public void Invoke()
	{
		if (!IsEnabled())
		{
			throw new ElementNotEnabledException();
		}

		PopupRootOwner.CloseTopmostPopup();
	}

	protected override IEnumerable<AutomationPeer>? GetFlowsFromCore() => GetLightDismissingPopupAPAsList();

	protected override IEnumerable<AutomationPeer>? GetFlowsToCore() => GetLightDismissingPopupAPAsList();

	private IEnumerable<AutomationPeer>? GetLightDismissingPopupAPAsList()
		=> GetLightDismissingPopupAP() is { } peer ? new[] { peer } : null;

	private AutomationPeer? GetLightDismissingPopupAP()
	{
		if (!IsEnabled())
		{
			throw new ElementNotEnabledException();
		}

		return PopupRootOwner.GetTopmostPopupInLightDismissChain()?.GetOrCreateAutomationPeer();
	}
}
