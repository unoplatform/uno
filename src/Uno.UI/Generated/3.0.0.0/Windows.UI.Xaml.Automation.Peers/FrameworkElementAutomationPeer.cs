#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Automation.Peers
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class FrameworkElementAutomationPeer : global::Windows.UI.Xaml.Automation.Peers.AutomationPeer
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.UIElement Owner
		{
			get
			{
				throw new global::System.NotImplementedException("The member UIElement FrameworkElementAutomationPeer.Owner is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public FrameworkElementAutomationPeer( global::Windows.UI.Xaml.FrameworkElement owner) : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer", "FrameworkElementAutomationPeer.FrameworkElementAutomationPeer(FrameworkElement owner)");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.FrameworkElementAutomationPeer(Windows.UI.Xaml.FrameworkElement)
		// Forced skipping of method Windows.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.Owner.get
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.Automation.Peers.AutomationPeer FromElement( global::Windows.UI.Xaml.UIElement element)
		{
			throw new global::System.NotImplementedException("The member AutomationPeer FrameworkElementAutomationPeer.FromElement(UIElement element) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.Automation.Peers.AutomationPeer CreatePeerForElement( global::Windows.UI.Xaml.UIElement element)
		{
			throw new global::System.NotImplementedException("The member AutomationPeer FrameworkElementAutomationPeer.CreatePeerForElement(UIElement element) is not implemented in Uno.");
		}
		#endif
	}
}
