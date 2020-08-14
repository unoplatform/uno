#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Web.UI.Interop
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WebViewControlAcceleratorKeyPressedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Handled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool WebViewControlAcceleratorKeyPressedEventArgs.Handled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Web.UI.Interop.WebViewControlAcceleratorKeyPressedEventArgs", "bool WebViewControlAcceleratorKeyPressedEventArgs.Handled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Core.CoreAcceleratorKeyEventType EventType
		{
			get
			{
				throw new global::System.NotImplementedException("The member CoreAcceleratorKeyEventType WebViewControlAcceleratorKeyPressedEventArgs.EventType is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Core.CorePhysicalKeyStatus KeyStatus
		{
			get
			{
				throw new global::System.NotImplementedException("The member CorePhysicalKeyStatus WebViewControlAcceleratorKeyPressedEventArgs.KeyStatus is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Web.UI.Interop.WebViewControlAcceleratorKeyRoutingStage RoutingStage
		{
			get
			{
				throw new global::System.NotImplementedException("The member WebViewControlAcceleratorKeyRoutingStage WebViewControlAcceleratorKeyPressedEventArgs.RoutingStage is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.VirtualKey VirtualKey
		{
			get
			{
				throw new global::System.NotImplementedException("The member VirtualKey WebViewControlAcceleratorKeyPressedEventArgs.VirtualKey is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Web.UI.Interop.WebViewControlAcceleratorKeyPressedEventArgs.EventType.get
		// Forced skipping of method Windows.Web.UI.Interop.WebViewControlAcceleratorKeyPressedEventArgs.VirtualKey.get
		// Forced skipping of method Windows.Web.UI.Interop.WebViewControlAcceleratorKeyPressedEventArgs.KeyStatus.get
		// Forced skipping of method Windows.Web.UI.Interop.WebViewControlAcceleratorKeyPressedEventArgs.RoutingStage.get
		// Forced skipping of method Windows.Web.UI.Interop.WebViewControlAcceleratorKeyPressedEventArgs.Handled.get
		// Forced skipping of method Windows.Web.UI.Interop.WebViewControlAcceleratorKeyPressedEventArgs.Handled.set
	}
}
