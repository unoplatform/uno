#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Automation.Peers
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SliderAutomationPeer : global::Windows.UI.Xaml.Automation.Peers.RangeBaseAutomationPeer
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public SliderAutomationPeer( global::Windows.UI.Xaml.Controls.Slider owner) : base(owner)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Automation.Peers.SliderAutomationPeer", "SliderAutomationPeer.SliderAutomationPeer(Slider owner)");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Automation.Peers.SliderAutomationPeer.SliderAutomationPeer(Windows.UI.Xaml.Controls.Slider)
	}
}
