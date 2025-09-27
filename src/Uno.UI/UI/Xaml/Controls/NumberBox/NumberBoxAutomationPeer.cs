// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference src\controls\dev\NumberBox\NumberBoxAutomationPeer.cpp, tag winui3/release/1.7.1, commit 5f27a786ac9

using Uno.UI.Helpers.WinUI;
using Windows.Foundation.Metadata;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

namespace Microsoft.UI.Xaml.Controls;

public class NumberBoxAutomationPeer : AutomationPeer, IRangeValueProvider
{
	private NumberBox _owner;

	internal NumberBoxAutomationPeer(NumberBox owner)
	{
		_owner = owner;
	}

	// IAutomationPeerOverrides
	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.RangeValue)
		{
			return this;
		}

		return base.GetPatternCore(patternInterface);
	}

	protected override string GetClassNameCore() => nameof(NumberBox);

	protected override string GetNameCore()
	{
		var name = base.GetNameCore();

		if (string.IsNullOrEmpty(name))
		{
			name = SharedHelpers.TryGetStringRepresentationFromObject(_owner.Header);
		}

		return name;
	}

	protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Spinner;

	private NumberBox GetImpl() => _owner;

	// IRangeValueProvider
	public double Minimum => GetImpl().Minimum;

	public double Maximum => GetImpl().Maximum;

	public double Value => GetImpl().Value;

	public double SmallChange => GetImpl().SmallChange;

	public double LargeChange => GetImpl().LargeChange;

	public bool IsReadOnly => false;

	public void SetValue(double value) => GetImpl().Value = value;

	internal void RaiseValueChangedEvent(double oldValue, double newValue)
	{
		if (ApiInformation.IsPropertyPresent("Microsoft.UI.Xaml.Automation.RangeValuePatternIdentifiers, Uno.UI", nameof(RangeValuePatternIdentifiers.ValueProperty)))
		{
			RaisePropertyChangedEvent(RangeValuePatternIdentifiers.ValueProperty,
						   oldValue,
						   newValue);
		}
	}
}
