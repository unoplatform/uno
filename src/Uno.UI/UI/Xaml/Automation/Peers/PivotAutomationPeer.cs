// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml/phone/lib/PivotAutomationPeer_Partial.cpp, tag winui3/release/1.8.4

using System;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes <see cref="Pivot"/> to Microsoft UI Automation.
/// </summary>
public partial class PivotAutomationPeer : ItemsControlAutomationPeer, ISelectionProvider, IScrollProvider
{
	public PivotAutomationPeer(Pivot owner) : base(owner)
	{
	}

	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.Selection ||
			patternInterface == PatternInterface.Scroll)
		{
			return this;
		}

		return base.GetPatternCore(patternInterface);
	}

	protected override string GetClassNameCore() => nameof(Pivot);

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.Tab;

	protected override string GetNameCore()
	{
		var name = base.GetNameCore();
		if (!string.IsNullOrEmpty(name))
		{
			return name;
		}

		// Fall back to the pivot title (matches WinUI: GetPlainText(Title)).
		if (Owner is Pivot pivot && pivot.Title is { } title)
		{
			return title.ToString() ?? string.Empty;
		}

		return string.Empty;
	}

	// ISelectionProvider

	public bool CanSelectMultiple => false;

	public bool IsSelectionRequired => true;

	public IRawElementProviderSimple[] GetSelection()
	{
		if (Owner is not Pivot pivot || pivot.SelectedItem is null)
		{
			return Array.Empty<IRawElementProviderSimple>();
		}

		var itemPeer = CreateItemAutomationPeer(pivot.SelectedItem);
		if (itemPeer is null)
		{
			return Array.Empty<IRawElementProviderSimple>();
		}

		var provider = ProviderFromPeer(itemPeer);
		return provider is null
			? Array.Empty<IRawElementProviderSimple>()
			: new[] { provider };
	}

	// IScrollProvider — Uno's Pivot does not surface a programmatic scroll API yet.
	// Report the pivot as non-scrollable so UIA clients can still query the pattern
	// without failing. Matches WinUI's behavior for a non-carousel pivot.

	public bool HorizontallyScrollable => false;

	public bool VerticallyScrollable => false;

	public double HorizontalScrollPercent => ScrollPatternIdentifiers.NoScroll;

	public double VerticalScrollPercent => ScrollPatternIdentifiers.NoScroll;

	public double HorizontalViewSize => 100.0;

	public double VerticalViewSize => 100.0;

	public void Scroll(ScrollAmount horizontalAmount, ScrollAmount verticalAmount)
	{
		// No-op: programmatic horizontal scrolling not exposed by Uno's Pivot.
	}

	public void SetScrollPercent(double horizontalPercent, double verticalPercent)
	{
		// No-op: programmatic horizontal scrolling not exposed by Uno's Pivot.
	}
}
