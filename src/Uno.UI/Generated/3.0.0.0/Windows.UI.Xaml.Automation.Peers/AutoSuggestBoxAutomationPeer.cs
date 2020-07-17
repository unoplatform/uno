#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Automation.Peers
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AutoSuggestBoxAutomationPeer : global::Windows.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer,global::Windows.UI.Xaml.Automation.Provider.IInvokeProvider
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public AutoSuggestBoxAutomationPeer( global::Windows.UI.Xaml.Controls.AutoSuggestBox owner) : base(owner)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Automation.Peers.AutoSuggestBoxAutomationPeer", "AutoSuggestBoxAutomationPeer.AutoSuggestBoxAutomationPeer(AutoSuggestBox owner)");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Automation.Peers.AutoSuggestBoxAutomationPeer.AutoSuggestBoxAutomationPeer(Windows.UI.Xaml.Controls.AutoSuggestBox)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Invoke()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Automation.Peers.AutoSuggestBoxAutomationPeer", "void AutoSuggestBoxAutomationPeer.Invoke()");
		}
		#endif
		// Processing: Windows.UI.Xaml.Automation.Provider.IInvokeProvider
	}
}
