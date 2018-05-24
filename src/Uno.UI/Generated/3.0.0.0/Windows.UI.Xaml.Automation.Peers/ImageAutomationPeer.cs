#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Automation.Peers
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ImageAutomationPeer : global::Windows.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public ImageAutomationPeer( global::Windows.UI.Xaml.Controls.Image owner) : base(owner)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Automation.Peers.ImageAutomationPeer", "ImageAutomationPeer.ImageAutomationPeer(Image owner)");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Automation.Peers.ImageAutomationPeer.ImageAutomationPeer(Windows.UI.Xaml.Controls.Image)
	}
}
