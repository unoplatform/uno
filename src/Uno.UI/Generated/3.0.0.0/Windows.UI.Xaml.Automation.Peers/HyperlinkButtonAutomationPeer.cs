#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Automation.Peers
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public  partial class HyperlinkButtonAutomationPeer : global::Windows.UI.Xaml.Automation.Peers.ButtonBaseAutomationPeer,global::Windows.UI.Xaml.Automation.Provider.IInvokeProvider
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public HyperlinkButtonAutomationPeer( global::Windows.UI.Xaml.Controls.HyperlinkButton owner) : base(owner)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Automation.Peers.HyperlinkButtonAutomationPeer", "HyperlinkButtonAutomationPeer.HyperlinkButtonAutomationPeer(HyperlinkButton owner)");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Automation.Peers.HyperlinkButtonAutomationPeer.HyperlinkButtonAutomationPeer(Windows.UI.Xaml.Controls.HyperlinkButton)
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  void Invoke()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Automation.Peers.HyperlinkButtonAutomationPeer", "void HyperlinkButtonAutomationPeer.Invoke()");
		}
		#endif
		// Processing: Windows.UI.Xaml.Automation.Provider.IInvokeProvider
	}
}
