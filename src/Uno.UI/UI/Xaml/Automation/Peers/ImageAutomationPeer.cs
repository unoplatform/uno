namespace Microsoft.UI.Xaml.Automation.Peers
{
	public partial class ImageAutomationPeer : FrameworkElementAutomationPeer
	{
		public ImageAutomationPeer(Controls.Image owner) : base(owner)
		{
		}

		protected override string GetClassNameCore()
		{
			return "Image";
		}

		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			return AutomationControlType.Image;
		}
	}
}
