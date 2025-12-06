using System;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation.Metadata;

namespace Microsoft.UI.Xaml.Automation.Peers;

public partial class NumberBoxAutomationPeer : AutomationPeer, IRangeValueProvider
{
	private readonly NumberBox _owner;

	public NumberBoxAutomationPeer(NumberBox owner)
	{
		_owner = owner ?? throw new ArgumentNullException(nameof(owner));
	}

	protected override object? GetPatternCore(PatternInterface patternInterface)
		=> patternInterface == PatternInterface.RangeValue ? this : base.GetPatternCore(patternInterface);

	protected override string GetClassNameCore() => nameof(NumberBox);

	protected override string GetNameCore()
	{
		var name = base.GetNameCore();
		return string.IsNullOrEmpty(name)
			? SharedHelpers.TryGetStringRepresentationFromObject(_owner.Header)
			: name;
	}

	protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Spinner;

	private NumberBox OwnerNumberBox => _owner;

	public double Minimum => OwnerNumberBox.Minimum;

	public double Maximum => OwnerNumberBox.Maximum;

	public double Value => OwnerNumberBox.Value;

	public double SmallChange => OwnerNumberBox.SmallChange;

	public double LargeChange => OwnerNumberBox.LargeChange;

	public bool IsReadOnly => false;

	public void SetValue(double value) => OwnerNumberBox.Value = value;

	internal void RaiseValueChangedEvent(double oldValue, double newValue)
	{
		if (ApiInformation.IsPropertyPresent(
			"Microsoft.UI.Xaml.Automation.RangeValuePatternIdentifiers, Uno.UI",
			nameof(RangeValuePatternIdentifiers.ValueProperty)))
		{
			RaisePropertyChangedEvent(
				RangeValuePatternIdentifiers.ValueProperty,
				oldValue,
				newValue);
		}
	}
}
