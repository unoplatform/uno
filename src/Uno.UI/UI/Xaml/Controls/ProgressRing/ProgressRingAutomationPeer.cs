// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ProgressRingAutomationPeer.cpp, tag winui3/release/1.8.4

#nullable enable

using System;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Helpers.WinUI;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes ProgressRing types to Microsoft UI Automation.
/// </summary>
public partial class ProgressRingAutomationPeer : FrameworkElementAutomationPeer, IRangeValueProvider
{
	public ProgressRingAutomationPeer(ProgressRing owner) : base(owner)
	{
	}

	protected override object? GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.RangeValue)
		{
			if (GetImpl().IsIndeterminate)
			{
				return null;
			}
			else
			{
				return this;
			}
		}

		return base.GetPatternCore(patternInterface);
	}

	protected override string GetClassNameCore()
		=> nameof(ProgressRing);

	protected override string GetNameCore()
	{
		// Check to see if the item has a defined AutomationProperties.Name
		var name = base.GetNameCore();

		if (Owner is ProgressRing progressRing)
		{
			if (progressRing.IsActive)
			{
				if (progressRing.IsIndeterminate)
				{
					return ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_ProgressRingIndeterminateStatus) + " " + name;
				}
				else
				{
					return name;
				}
			}
		}

		return name;
	}

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.ProgressBar;

	protected override string GetLocalizedControlTypeCore()
		=> ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_ProgressRingName);

	// IRangeValueProvider
	public bool IsReadOnly => true;

	public double Minimum => GetImpl().Minimum;

	public double Maximum => GetImpl().Maximum;

	public double Value => GetImpl().Value;

	public double SmallChange => double.NaN;

	public double LargeChange => double.NaN;

	public void SetValue(double value)
	{
		GetImpl().Value = value;
	}

	private ProgressRing GetImpl() => (ProgressRing)Owner;
}
