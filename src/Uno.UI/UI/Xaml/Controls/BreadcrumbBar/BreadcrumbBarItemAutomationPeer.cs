// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference BreadcrumbBarItemAutomationPeer.cpp, tag winui3/release/1.8.4

#nullable enable
#pragma warning disable CS8603 // Possible null reference return

using Microsoft.UI.Xaml.Controls;
using Uno.UI.Helpers.WinUI;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes BreadcrumbBar types to Microsoft UI Automation.
/// </summary>
public partial class BreadcrumbBarItemAutomationPeer : FrameworkElementAutomationPeer, IInvokeProvider
{
	/// <summary>
	/// Initializes a new instance of the BreadcrumbBarItemAutomationPeer class.
	/// </summary>
	/// <param name="owner"></param>
	public BreadcrumbBarItemAutomationPeer(BreadcrumbBarItem owner) : base(owner)
	{
	}

	// IAutomationPeerOverrides
	protected override string GetLocalizedControlTypeCore() =>
		ResourceAccessor.GetLocalizedStringResource(
			ResourceAccessor.SR_BreadcrumbBarItemLocalizedControlType);

	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.Invoke)
		{
			return this;
		}

		return base.GetPatternCore(patternInterface);
	}

	protected override string GetClassNameCore() => nameof(BreadcrumbBarItem);

	protected override AutomationControlType GetAutomationControlTypeCore() =>
		AutomationControlType.Button;

	private BreadcrumbBarItem? GetImpl()
	{
		BreadcrumbBarItem? impl = null;

		if (Owner is BreadcrumbBarItem breadcrumbItem)
		{
			impl = breadcrumbItem;
		}

		return impl;
	}

	/// <summary>
	/// Sends a request to invoke the item associated with the automation peer.
	/// </summary>
	public void Invoke()
	{
		if (GetImpl() is { } breadcrumbItem)
		{
			breadcrumbItem.OnClickEvent(null, null);
		}
	}
}
