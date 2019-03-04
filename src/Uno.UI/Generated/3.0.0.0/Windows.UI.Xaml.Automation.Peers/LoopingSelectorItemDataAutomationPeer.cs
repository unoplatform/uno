#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Automation.Peers
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class LoopingSelectorItemDataAutomationPeer : global::Windows.UI.Xaml.Automation.Peers.AutomationPeer,global::Windows.UI.Xaml.Automation.Provider.IVirtualizedItemProvider
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void Realize()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Automation.Peers.LoopingSelectorItemDataAutomationPeer", "void LoopingSelectorItemDataAutomationPeer.Realize()");
		}
		#endif
		// Processing: Windows.UI.Xaml.Automation.Provider.IVirtualizedItemProvider
	}
}
