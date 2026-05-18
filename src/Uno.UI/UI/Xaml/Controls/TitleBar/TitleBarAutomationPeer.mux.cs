// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TitleBarAutomationPeer.cpp, commit 5f9e85113

using Microsoft.UI.Xaml.Automation.Peers;

namespace Microsoft.UI.Xaml.Controls;

partial class TitleBarAutomationPeer
{
	/// <summary>
	/// Initializes a new instance of the TitleBarAutomationPeer class.
	/// </summary>
	/// <param name="owner">The TitleBar associated with this automation peer.</param>
	public TitleBarAutomationPeer(TitleBar owner) : base(owner)
	{
	}

	// IAutomationPeerOverrides
	protected override AutomationControlType GetAutomationControlTypeCore()
	{
		return AutomationControlType.TitleBar;
	}

	protected override string GetClassNameCore()
	{
		return nameof(TitleBar);
	}

	protected override string GetNameCore()
	{
		string name = base.GetNameCore();

		if (string.IsNullOrEmpty(name))
		{
			if (Owner is TitleBar titleBar)
			{
				name = titleBar.Title;
			}
		}

		return name;
	}
}
