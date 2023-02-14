#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Microsoft.UI.Xaml.Automation.Peers
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class RepeatButtonAutomationPeer : global::Microsoft.UI.Xaml.Automation.Peers.ButtonBaseAutomationPeer,global::Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public RepeatButtonAutomationPeer( global::Microsoft.UI.Xaml.Controls.Primitives.RepeatButton owner) : base(owner)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Automation.Peers.RepeatButtonAutomationPeer", "RepeatButtonAutomationPeer.RepeatButtonAutomationPeer(RepeatButton owner)");
		}
		#endif
		// Forced skipping of method Microsoft.UI.Xaml.Automation.Peers.RepeatButtonAutomationPeer.RepeatButtonAutomationPeer(Microsoft.UI.Xaml.Controls.Primitives.RepeatButton)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Invoke()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Automation.Peers.RepeatButtonAutomationPeer", "void RepeatButtonAutomationPeer.Invoke()");
		}
		#endif
		// Processing: Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider
	}
}
