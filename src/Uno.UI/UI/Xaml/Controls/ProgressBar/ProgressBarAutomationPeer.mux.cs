// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ProgressBarAutomationPeer.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Helpers.WinUI;

namespace Microsoft.UI.Xaml.Automation.Peers;

partial class ProgressBarAutomationPeer
{
	/// <summary>
	/// Initializes a new instance of the ProgressBarAutomationPeer class.
	/// </summary>
	/// <param name="owner">The ProgressBar control instance to create the peer for.</param>
	public ProgressBarAutomationPeer(ProgressBar owner) : base(owner)
	{
	}

	// IAutomationPeerOverrides
	protected override object? GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.RangeValue)
		{
			if (Owner is ProgressBar progressBar)
			{
				if (progressBar.IsIndeterminate)
				{
					return null;
				}
			}

			return this;
		}

		return base.GetPatternCore(patternInterface);
	}

	protected override string GetClassNameCore() => typeof(ProgressBar).FullName!;

	protected override string GetNameCore()
	{
		//Check to see if the item has a defined AutomationProperties.Name
		var name = base.GetNameCore();

		if (Owner is ProgressBar progressBar)
		{
			if (progressBar.ShowError)
			{
				return ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_ProgressBarErrorStatus) + name;
			}
			else if (progressBar.ShowPaused)
			{
				return ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_ProgressBarPausedStatus) + name;
			}
			else if (progressBar.IsIndeterminate)
			{
				return ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_ProgressBarIndeterminateStatus) + name;
			}
		}
		return name;
	}

	protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ProgressBar;

	private ProgressBar? GetImpl() => Owner as ProgressBar;

	// IRangeValueProvider
	double IRangeValueProvider.Value => GetImpl()!.Value;

	double IRangeValueProvider.SmallChange => double.NaN;

	double IRangeValueProvider.LargeChange => double.NaN;

	double IRangeValueProvider.Minimum => GetImpl()!.Minimum;

	double IRangeValueProvider.Maximum => GetImpl()!.Maximum;

	void IRangeValueProvider.SetValue(double value) => GetImpl()!.Value = value;
}
