#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Automation.Peers
{
	#if false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class FrameworkElementAutomationPeer : global::Windows.UI.Xaml.Automation.Peers.AutomationPeer
	{
		// Skipping already declared property Owner
		// Skipping already declared method Windows.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.FrameworkElementAutomationPeer(Windows.UI.Xaml.FrameworkElement)
		// Forced skipping of method Windows.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.FrameworkElementAutomationPeer(Windows.UI.Xaml.FrameworkElement)
		// Forced skipping of method Windows.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.Owner.get
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.Automation.Peers.AutomationPeer FromElement( global::Windows.UI.Xaml.UIElement element)
		{
			throw new global::System.NotImplementedException("The member AutomationPeer FrameworkElementAutomationPeer.FromElement(UIElement element) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.Automation.Peers.AutomationPeer CreatePeerForElement( global::Windows.UI.Xaml.UIElement element)
		{
			throw new global::System.NotImplementedException("The member AutomationPeer FrameworkElementAutomationPeer.CreatePeerForElement(UIElement element) is not implemented in Uno.");
		}
		#endif
	}
}
