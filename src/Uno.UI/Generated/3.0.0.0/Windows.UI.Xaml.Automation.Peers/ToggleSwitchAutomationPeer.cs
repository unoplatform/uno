#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Automation.Peers
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ToggleSwitchAutomationPeer : global::Windows.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer,global::Windows.UI.Xaml.Automation.Provider.IToggleProvider
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Automation.ToggleState ToggleState
		{
			get
			{
				throw new global::System.NotImplementedException("The member ToggleState ToggleSwitchAutomationPeer.ToggleState is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public ToggleSwitchAutomationPeer( global::Windows.UI.Xaml.Controls.ToggleSwitch owner) : base(owner)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Automation.Peers.ToggleSwitchAutomationPeer", "ToggleSwitchAutomationPeer.ToggleSwitchAutomationPeer(ToggleSwitch owner)");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Automation.Peers.ToggleSwitchAutomationPeer.ToggleSwitchAutomationPeer(Windows.UI.Xaml.Controls.ToggleSwitch)
		// Forced skipping of method Windows.UI.Xaml.Automation.Peers.ToggleSwitchAutomationPeer.ToggleState.get
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  void Toggle()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Automation.Peers.ToggleSwitchAutomationPeer", "void ToggleSwitchAutomationPeer.Toggle()");
		}
		#endif
		// Processing: Windows.UI.Xaml.Automation.Provider.IToggleProvider
	}
}
