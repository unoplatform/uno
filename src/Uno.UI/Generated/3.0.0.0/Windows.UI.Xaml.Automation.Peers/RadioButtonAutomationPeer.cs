#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Automation.Peers
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public  partial class RadioButtonAutomationPeer : global::Windows.UI.Xaml.Automation.Peers.ToggleButtonAutomationPeer,global::Windows.UI.Xaml.Automation.Provider.ISelectionItemProvider
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  bool IsSelected
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool RadioButtonAutomationPeer.IsSelected is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Automation.Provider.IRawElementProviderSimple SelectionContainer
		{
			get
			{
				throw new global::System.NotImplementedException("The member IRawElementProviderSimple RadioButtonAutomationPeer.SelectionContainer is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public RadioButtonAutomationPeer( global::Windows.UI.Xaml.Controls.RadioButton owner) : base(owner)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Automation.Peers.RadioButtonAutomationPeer", "RadioButtonAutomationPeer.RadioButtonAutomationPeer(RadioButton owner)");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Automation.Peers.RadioButtonAutomationPeer.RadioButtonAutomationPeer(Windows.UI.Xaml.Controls.RadioButton)
		// Forced skipping of method Windows.UI.Xaml.Automation.Peers.RadioButtonAutomationPeer.IsSelected.get
		// Forced skipping of method Windows.UI.Xaml.Automation.Peers.RadioButtonAutomationPeer.SelectionContainer.get
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  void AddToSelection()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Automation.Peers.RadioButtonAutomationPeer", "void RadioButtonAutomationPeer.AddToSelection()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  void RemoveFromSelection()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Automation.Peers.RadioButtonAutomationPeer", "void RadioButtonAutomationPeer.RemoveFromSelection()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  void Select()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Automation.Peers.RadioButtonAutomationPeer", "void RadioButtonAutomationPeer.Select()");
		}
		#endif
		// Processing: Windows.UI.Xaml.Automation.Provider.ISelectionItemProvider
	}
}
