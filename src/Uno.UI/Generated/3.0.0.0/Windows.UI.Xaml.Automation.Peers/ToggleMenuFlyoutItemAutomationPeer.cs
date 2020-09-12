#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Automation.Peers
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ToggleMenuFlyoutItemAutomationPeer : global::Windows.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer,global::Windows.UI.Xaml.Automation.Provider.IToggleProvider
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Automation.ToggleState ToggleState
		{
			get
			{
				throw new global::System.NotImplementedException("The member ToggleState ToggleMenuFlyoutItemAutomationPeer.ToggleState is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public ToggleMenuFlyoutItemAutomationPeer( global::Windows.UI.Xaml.Controls.ToggleMenuFlyoutItem owner) : base(owner)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Automation.Peers.ToggleMenuFlyoutItemAutomationPeer", "ToggleMenuFlyoutItemAutomationPeer.ToggleMenuFlyoutItemAutomationPeer(ToggleMenuFlyoutItem owner)");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Automation.Peers.ToggleMenuFlyoutItemAutomationPeer.ToggleMenuFlyoutItemAutomationPeer(Windows.UI.Xaml.Controls.ToggleMenuFlyoutItem)
		// Forced skipping of method Windows.UI.Xaml.Automation.Peers.ToggleMenuFlyoutItemAutomationPeer.ToggleState.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Toggle()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Automation.Peers.ToggleMenuFlyoutItemAutomationPeer", "void ToggleMenuFlyoutItemAutomationPeer.Toggle()");
		}
		#endif
		// Processing: Windows.UI.Xaml.Automation.Provider.IToggleProvider
	}
}
