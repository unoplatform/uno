#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface ICoreAcceleratorKeys 
	{
		// Forced skipping of method Windows.UI.Core.ICoreAcceleratorKeys.AcceleratorKeyActivated.add
		// Forced skipping of method Windows.UI.Core.ICoreAcceleratorKeys.AcceleratorKeyActivated.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		 event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Core.CoreDispatcher, global::Windows.UI.Core.AcceleratorKeyEventArgs> AcceleratorKeyActivated;
		#endif
	}
}
