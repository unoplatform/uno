// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\FlyoutPresenterAutomationPeer_Partial.cpp, tag winui3/release/1.4.3, commit 685d2bf

using System;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes FlyoutPresenter types to Microsoft UI Automation.
/// </summary>
public partial class FlyoutPresenterAutomationPeer : FrameworkElementAutomationPeer
{
	public FlyoutPresenterAutomationPeer(FlyoutPresenter owner) : base(owner)
	{
	}

	internal static FlyoutPresenterAutomationPeer CreateInstanceWithOwner(FlyoutPresenter owner)
	{
		if (owner is null)
		{
			throw new ArgumentNullException(nameof(owner));
		}

		//TODO:MZ: Code greatly simplified here, ensure it is still correct.

		return new FlyoutPresenterAutomationPeer(owner);
	}

	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.Invoke)
		{
			return this;
		}
		else
		{
			return base.GetPatternCore(patternInterface);
		}
	}

	protected override string GetClassNameCore() => nameof(Flyout);

	protected override string GetAutomationIdCore()
	{
		string result = base.GetAutomationIdCore();
		if (string.IsNullOrEmpty(result))
		{
			var spFlyout = (FlyoutPresenter)Owner;
			result = spFlyout.GetOwnerName();
		}
		return result;
	}

	protected override AutomationControlType GetAutomationControlTypeCore() =>
		AutomationControlType.Pane;
}
