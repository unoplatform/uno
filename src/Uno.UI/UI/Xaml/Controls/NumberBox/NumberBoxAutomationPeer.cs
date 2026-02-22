// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference NumberBoxAutomationPeer.cpp, tag winui3/release/1.8.4

#nullable enable

using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes NumberBox types to Microsoft UI Automation.
/// </summary>
public partial class NumberBoxAutomationPeer : AutomationPeer, IRangeValueProvider
{
	private readonly NumberBox _owner;

	public NumberBoxAutomationPeer(NumberBox owner)
	{
		_owner = owner;
	}

	// IAutomationPeerOverrides
	protected override object? GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.RangeValue)
		{
			return this;
		}

		return base.GetPatternCore(patternInterface);
	}

	protected override string GetClassNameCore()
		=> nameof(NumberBox);

	protected override string GetNameCore()
	{
		var name = base.GetNameCore();

		if (string.IsNullOrEmpty(name))
		{
			if (_owner is NumberBox numberBox)
			{
				name = SharedHelpers.TryGetStringRepresentationFromObject(numberBox.Header);
			}
		}

		return name;
	}

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.Spinner;

	private NumberBox GetImpl() => _owner;

	// IRangeValueProvider
	public double Minimum => GetImpl().Minimum;

	public double Maximum => GetImpl().Maximum;

	public double Value => GetImpl().Value;

	public double SmallChange => GetImpl().SmallChange;

	public double LargeChange => GetImpl().LargeChange;

	public bool IsReadOnly => false;

	public void SetValue(double value)
	{
		GetImpl().Value = value;
	}

	internal void RaiseValueChangedEvent(double oldValue, double newValue)
	{
		base.RaisePropertyChangedEvent(
			RangeValuePatternIdentifiers.ValueProperty,
			PropertyValue.CreateDouble(oldValue),
			PropertyValue.CreateDouble(newValue));
	}
}
