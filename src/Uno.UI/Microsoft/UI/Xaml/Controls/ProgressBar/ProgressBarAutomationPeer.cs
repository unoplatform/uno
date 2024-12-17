using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Automation.Provider;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	public class ProgressBarAutomationPeer : AutomationPeer, IRangeValueProvider
	{
		private readonly ProgressBar _owner;

		public ProgressBarAutomationPeer(ProgressBar owner)
		{
			_owner = owner;
		}

		protected override object GetPatternCore(PatternInterface patternInterface)
		{
			if (patternInterface == PatternInterface.RangeValue)
			{
				if (_owner.IsIndeterminate)
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

			var progressBar = _owner;

			if (progressBar.ShowError)
			{
				return "Error" + name;
			}
			else if (progressBar.ShowPaused)
			{
				return "Busy" + name;
			}
			else if (progressBar.IsIndeterminate)
			{
				return "Paused" + name;
			}
			return name;
		}

		protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ProgressBar;

		public bool IsReadOnly => true;
		public double Value => _owner.Value;
		public double SmallChange => double.NaN;
		public double LargeChange => double.NaN;
		public double Minimum => _owner.Minimum;
		public double Maximum => _owner.Maximum;
		public void SetValue(double value) => _owner.Value = value; // ???
	}
}
