#pragma warning disable 108 // new keyword hiding
using System;

namespace Windows.UI.Xaml.Automation.Peers
{
	public  partial class ToggleButtonAutomationPeer : global::Windows.UI.Xaml.Automation.Peers.ButtonBaseAutomationPeer,global::Windows.UI.Xaml.Automation.Provider.IToggleProvider
	{
		[Uno.NotImplemented]
		public ToggleButtonAutomationPeer(IFrameworkElement element) : base(element)
		{
			throw new NotImplementedException();
		}

		[Uno.NotImplemented]
		public ToggleButtonAutomationPeer(object element) : base(null)
		{
			throw new NotImplementedException();
		}
	}
}
