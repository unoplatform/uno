#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Automation.Peers
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class ToggleButtonAutomationPeer : global::Windows.UI.Xaml.Automation.Peers.ButtonBaseAutomationPeer,global::Windows.UI.Xaml.Automation.Provider.IToggleProvider
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Automation.ToggleState ToggleState
		{
			get
			{
				throw new global::System.NotImplementedException("The member ToggleState ToggleButtonAutomationPeer.ToggleState is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public ToggleButtonAutomationPeer( global::Windows.UI.Xaml.Controls.Primitives.ToggleButton owner) : base(owner)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Automation.Peers.ToggleButtonAutomationPeer", "ToggleButtonAutomationPeer.ToggleButtonAutomationPeer(ToggleButton owner)");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Automation.Peers.ToggleButtonAutomationPeer.ToggleButtonAutomationPeer(Windows.UI.Xaml.Controls.Primitives.ToggleButton)
		// Forced skipping of method Windows.UI.Xaml.Automation.Peers.ToggleButtonAutomationPeer.ToggleState.get
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  void Toggle()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Automation.Peers.ToggleButtonAutomationPeer", "void ToggleButtonAutomationPeer.Toggle()");
		}
		#endif
		// Processing: Windows.UI.Xaml.Automation.Provider.IToggleProvider
	}
}
