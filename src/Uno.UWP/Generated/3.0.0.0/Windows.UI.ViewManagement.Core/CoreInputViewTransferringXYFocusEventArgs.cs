#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.ViewManagement.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CoreInputViewTransferringXYFocusEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool TransferHandled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool CoreInputViewTransferringXYFocusEventArgs.TransferHandled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.Core.CoreInputViewTransferringXYFocusEventArgs", "bool CoreInputViewTransferringXYFocusEventArgs.TransferHandled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool KeepPrimaryViewVisible
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool CoreInputViewTransferringXYFocusEventArgs.KeepPrimaryViewVisible is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.Core.CoreInputViewTransferringXYFocusEventArgs", "bool CoreInputViewTransferringXYFocusEventArgs.KeepPrimaryViewVisible");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.ViewManagement.Core.CoreInputViewXYFocusTransferDirection Direction
		{
			get
			{
				throw new global::System.NotImplementedException("The member CoreInputViewXYFocusTransferDirection CoreInputViewTransferringXYFocusEventArgs.Direction is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Rect Origin
		{
			get
			{
				throw new global::System.NotImplementedException("The member Rect CoreInputViewTransferringXYFocusEventArgs.Origin is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.ViewManagement.Core.CoreInputViewTransferringXYFocusEventArgs.Origin.get
		// Forced skipping of method Windows.UI.ViewManagement.Core.CoreInputViewTransferringXYFocusEventArgs.Direction.get
		// Forced skipping of method Windows.UI.ViewManagement.Core.CoreInputViewTransferringXYFocusEventArgs.TransferHandled.set
		// Forced skipping of method Windows.UI.ViewManagement.Core.CoreInputViewTransferringXYFocusEventArgs.TransferHandled.get
		// Forced skipping of method Windows.UI.ViewManagement.Core.CoreInputViewTransferringXYFocusEventArgs.KeepPrimaryViewVisible.set
		// Forced skipping of method Windows.UI.ViewManagement.Core.CoreInputViewTransferringXYFocusEventArgs.KeepPrimaryViewVisible.get
	}
}
