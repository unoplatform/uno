#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Automation.Peers
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CaptureElementAutomationPeer : global::Windows.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public CaptureElementAutomationPeer( global::Windows.UI.Xaml.Controls.CaptureElement owner) : base(owner)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Automation.Peers.CaptureElementAutomationPeer", "CaptureElementAutomationPeer.CaptureElementAutomationPeer(CaptureElement owner)");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Automation.Peers.CaptureElementAutomationPeer.CaptureElementAutomationPeer(Windows.UI.Xaml.Controls.CaptureElement)
	}
}
