// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TextBoxAutomationPeer_Partial.cpp, tag winui3/release/1.4.2
using System.Collections.Generic;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes TextBox types to Microsoft UI Automation.
/// </summary>
public partial class TextBoxAutomationPeer : FrameworkElementAutomationPeer
{
	public TextBoxAutomationPeer(TextBox owner) : base(owner)
	{
	}

	protected override string GetClassNameCore() => nameof(TextBox);

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.Edit;

	protected override IEnumerable<AutomationPeer> GetDescribedByCore()
	{
		if (Owner is TextBox owner)
		{
			//UNO TODO Implement TextBoxPlaceholderTextHelper
			//IFC_RETURN(TextBoxPlaceholderTextHelper::SetupPlaceholderTextBlockDescribedBy(spOwner));

			return base.GetDescribedByCore();
		}

		return [];
	}
}
