// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RepeatButtonAutomationPeer_Partial.cpp, tag winui3/release/1.8.4

using System;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Automation.Peers;

public partial class RepeatButtonAutomationPeer : ButtonBaseAutomationPeer, IInvokeProvider
{
	public RepeatButtonAutomationPeer(RepeatButton owner) : base(owner)
	{
	}

	protected override object GetPatternCore(PatternInterface patternInterface) =>
		patternInterface == PatternInterface.Invoke ?
			this : base.GetPatternCore(patternInterface);

	protected override string GetClassNameCore() => nameof(RepeatButton);

	protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;

	public void Invoke()
	{
		bool bIsEnabled = IsEnabled();

		if (!bIsEnabled)
		{
			throw new InvalidOperationException("Button not enabled");
		}

		var owner = (ButtonBase)Owner;
		owner.ProgrammaticClick();
	}
}
