// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml/phone/lib/PivotItemAutomationPeer_Partial.cpp, tag winui3/release/1.8.4

using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes <see cref="PivotItem"/> instances to Microsoft UI Automation.
/// </summary>
public partial class PivotItemAutomationPeer : FrameworkElementAutomationPeer
{
	public PivotItemAutomationPeer(PivotItem owner) : base(owner)
	{
	}

	protected override string GetClassNameCore() => nameof(PivotItem);

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.TabItem;

	protected override string GetNameCore()
	{
		var name = base.GetNameCore();
		if (!string.IsNullOrEmpty(name))
		{
			return name;
		}

		// Fall back to the pivot item header (matches WinUI: GetPlainText(Header)).
		if (Owner is PivotItem pivotItem && pivotItem.Header is { } header)
		{
			return header.ToString() ?? string.Empty;
		}

		return string.Empty;
	}
}
