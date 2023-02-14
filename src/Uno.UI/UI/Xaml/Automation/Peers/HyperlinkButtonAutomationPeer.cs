#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers
{
	public partial class HyperlinkButtonAutomationPeer : ButtonBaseAutomationPeer, Provider.IInvokeProvider
	{
		public HyperlinkButtonAutomationPeer(HyperlinkButton owner) : base(owner)
		{
		}

		protected override string GetClassNameCore()
		{
			return "Hyperlink";
		}

		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			return AutomationControlType.Hyperlink;
		}

		public void Invoke()
		{
			if (IsEnabled())
			{
				(Owner as HyperlinkButton).AutomationPeerClick();
			}
		}
	}
}
