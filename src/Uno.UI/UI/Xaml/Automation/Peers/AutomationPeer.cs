#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Automation.Peers
{
	public  partial class AutomationPeer : global::Windows.UI.Xaml.DependencyObject
	{
		[global::Uno.NotImplemented]
		public static bool ListenerExists( global::Windows.UI.Xaml.Automation.Peers.AutomationEvents eventId)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Automation.Peers.AutomationPeer", "bool AutomationPeer.ListenerExists");

			return false;
		}
	}
}
