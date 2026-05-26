using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Uno.UI.Helpers.WinUI;

namespace Microsoft.UI.Xaml.Automation.Peers;

public partial class ProgressBarAutomationPeer : RangeBaseAutomationPeer, IRangeValueProvider
{
	public ProgressBarAutomationPeer(ProgressBar owner) : base(owner)
	{
	}

	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.RangeValue)
		{
			if (Owner is ProgressBar progressBar && progressBar.IsIndeterminate)
			{
				return null;
			}

			return this;
		}

		return base.GetPatternCore(patternInterface);
	}

	protected override string GetClassNameCore() => nameof(ProgressBar);

	protected override string GetNameCore()
	{
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

	private ProgressBar GetImpl() => (ProgressBar)Owner;

	bool IRangeValueProvider.IsReadOnly => true;

	double IRangeValueProvider.Value => GetImpl().Value;

	double IRangeValueProvider.SmallChange => double.NaN;

	double IRangeValueProvider.LargeChange => double.NaN;

	double IRangeValueProvider.Minimum => GetImpl().Minimum;

	double IRangeValueProvider.Maximum => GetImpl().Maximum;

	void IRangeValueProvider.SetValue(double value) => throw new ElementNotEnabledException();
}
