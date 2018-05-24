#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Automation.Peers
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public  partial class TextBlockAutomationPeer : global::Windows.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public TextBlockAutomationPeer( global::Windows.UI.Xaml.Controls.TextBlock owner) : base(owner)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Automation.Peers.TextBlockAutomationPeer", "TextBlockAutomationPeer.TextBlockAutomationPeer(TextBlock owner)");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Automation.Peers.TextBlockAutomationPeer.TextBlockAutomationPeer(Windows.UI.Xaml.Controls.TextBlock)
	}
}
