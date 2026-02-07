// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable
#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using System;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Automation.Peers;

public partial class FlyoutPresenterAutomationPeer : FrameworkElementAutomationPeer, IInvokeProvider
{
	public FlyoutPresenterAutomationPeer(FlyoutPresenter owner) : base(owner)
	{
		ArgumentNullException.ThrowIfNull(owner);
	}

	protected override object GetPatternCore(PatternInterface patternInterface)
		=> patternInterface == PatternInterface.Invoke ? this : base.GetPatternCore(patternInterface);

	protected override string GetClassNameCore() => "Flyout";

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.Pane;

	protected override string GetAutomationIdCore()
	{
		var id = base.GetAutomationIdCore();
		if (!string.IsNullOrEmpty(id))
		{
			return id;
		}

		return FlyoutPresenterOwner?.Name ?? string.Empty;
	}

	public void Invoke()
	{
		TryGetOwningFlyout()?.Hide();
	}

	private FlyoutPresenter? FlyoutPresenterOwner => Owner as FlyoutPresenter;

	private FlyoutBase? TryGetOwningFlyout()
	{
		if (FlyoutPresenterOwner?.TemplatedParent is FlyoutBase flyout)
		{
			return flyout;
		}

		return null;
	}
}
