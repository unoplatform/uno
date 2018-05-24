#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Automation.Peers
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ButtonAutomationPeer : global::Windows.UI.Xaml.Automation.Peers.ButtonBaseAutomationPeer,global::Windows.UI.Xaml.Automation.Provider.IInvokeProvider
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public ButtonAutomationPeer( global::Windows.UI.Xaml.Controls.Button owner) : base(owner)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Automation.Peers.ButtonAutomationPeer", "ButtonAutomationPeer.ButtonAutomationPeer(Button owner)");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Automation.Peers.ButtonAutomationPeer.ButtonAutomationPeer(Windows.UI.Xaml.Controls.Button)
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  void Invoke()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Automation.Peers.ButtonAutomationPeer", "void ButtonAutomationPeer.Invoke()");
		}
		#endif
		// Processing: Windows.UI.Xaml.Automation.Provider.IInvokeProvider
	}
}
