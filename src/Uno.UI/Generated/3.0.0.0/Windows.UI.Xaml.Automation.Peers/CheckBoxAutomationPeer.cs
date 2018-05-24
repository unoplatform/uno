#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Automation.Peers
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CheckBoxAutomationPeer : global::Windows.UI.Xaml.Automation.Peers.ToggleButtonAutomationPeer
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public CheckBoxAutomationPeer( global::Windows.UI.Xaml.Controls.CheckBox owner) : base(owner)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Automation.Peers.CheckBoxAutomationPeer", "CheckBoxAutomationPeer.CheckBoxAutomationPeer(CheckBox owner)");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Automation.Peers.CheckBoxAutomationPeer.CheckBoxAutomationPeer(Windows.UI.Xaml.Controls.CheckBox)
	}
}
