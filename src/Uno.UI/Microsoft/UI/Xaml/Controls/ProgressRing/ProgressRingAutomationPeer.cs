using Windows.UI.Xaml.Automation.Peers;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	public class ProgressRingAutomationPeer : AutomationPeer
	{
		private readonly ProgressRing _progressRing;

		public ProgressRingAutomationPeer(ProgressRing progressRing)
		{
			_progressRing = progressRing;
		}

		protected override string GetClassNameCore() => nameof(ProgressRing);

		protected override string GetNameCore()
		{
			var name = base.GetNameCore();

			if (_progressRing?.IsActive ?? false)
			{
				return "Busy" + name;
			}

			return name;
		}

		protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ProgressBar;
	}
}
