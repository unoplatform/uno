using System;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Helpers.WinUI;

namespace Microsoft.UI.Xaml.Automation.Peers;

public partial class ProgressRingAutomationPeer : FrameworkElementAutomationPeer, IRangeValueProvider
{
	public ProgressRingAutomationPeer(ProgressRing owner) : base(owner)
	{
		ArgumentNullException.ThrowIfNull(owner);
	}

	protected override object? GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.RangeValue)
		{
			return OwnerProgressRing.IsIndeterminate ? null : this;
		}

		return base.GetPatternCore(patternInterface);
	}

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.ProgressBar;

	protected override string GetClassNameCore()
		=> nameof(ProgressRing);

	protected override string GetNameCore()
	{
		var name = base.GetNameCore();
		var owner = OwnerProgressRing;

		if (owner.IsActive && owner.IsIndeterminate)
		{
			var localizedStatus = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_ProgressRingIndeterminateStatus);
			if (!string.IsNullOrEmpty(localizedStatus))
			{
				return string.IsNullOrEmpty(name)
					? localizedStatus
					: $"{localizedStatus} {name}";
			}
		}

		return name;
	}

	protected override string GetLocalizedControlTypeCore()
		=> ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_ProgressRingName);

	private ProgressRing OwnerProgressRing => (ProgressRing)Owner;

	public bool IsReadOnly => true;

	public double Minimum => OwnerProgressRing.Minimum;

	public double Maximum => OwnerProgressRing.Maximum;

	public double Value => OwnerProgressRing.Value;

	public double SmallChange => double.NaN;

	public double LargeChange => double.NaN;

	public void SetValue(double value) => OwnerProgressRing.Value = value;
}
