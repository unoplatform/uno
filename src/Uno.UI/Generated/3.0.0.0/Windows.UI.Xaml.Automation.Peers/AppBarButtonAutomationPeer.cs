#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Automation.Peers
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AppBarButtonAutomationPeer : global::Windows.UI.Xaml.Automation.Peers.ButtonAutomationPeer
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public AppBarButtonAutomationPeer( global::Windows.UI.Xaml.Controls.AppBarButton owner) : base(owner)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Automation.Peers.AppBarButtonAutomationPeer", "AppBarButtonAutomationPeer.AppBarButtonAutomationPeer(AppBarButton owner)");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Automation.Peers.AppBarButtonAutomationPeer.AppBarButtonAutomationPeer(Windows.UI.Xaml.Controls.AppBarButton)
	}
}
