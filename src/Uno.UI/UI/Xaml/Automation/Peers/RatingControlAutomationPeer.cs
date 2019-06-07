using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Automation.Peers
{
	[global::Uno.NotImplemented]
	public partial class RatingControlAutomationPeer : FrameworkElementAutomationPeer
	{
		public RatingControlAutomationPeer(RatingControl owner) : base(owner)
		{
		}

		protected override string GetClassNameCore()
		{
			return "RatingControl";
		}

		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			return AutomationControlType.Slider;
		}
	}
}
