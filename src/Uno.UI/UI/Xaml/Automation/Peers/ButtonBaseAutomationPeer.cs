#pragma warning disable 108 // new keyword hiding
using System;

namespace Windows.UI.Xaml.Automation.Peers
{
	public  partial class ButtonBaseAutomationPeer : global::Windows.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer
	{
		public ButtonBaseAutomationPeer(IFrameworkElement element) : base(element)
		{
			throw new NotImplementedException();
		}
	}
}
