#pragma warning disable 108 // new keyword hiding
using System;
using Uno;

namespace Windows.UI.Xaml.Automation.Peers
{
	public  partial class FrameworkElementAutomationPeer : global::Windows.UI.Xaml.Automation.Peers.AutomationPeer
	{
		[NotImplemented]
		public FrameworkElementAutomationPeer(IFrameworkElement element)
		{
			throw new NotImplementedException();
		}

		[NotImplemented]
		public FrameworkElementAutomationPeer(object element)
		{
			throw new NotImplementedException();
		}

		[NotImplemented]
		public FrameworkElementAutomationPeer()
		{
			throw new NotImplementedException();
		}

		[NotImplemented]
		public static global::Windows.UI.Xaml.Automation.Peers.AutomationPeer FromElement(global::Windows.UI.Xaml.UIElement element)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer", " FrameworkElementAutomationPeer.FromElement(UIElement element)");
			return null;
		}
	}
}
