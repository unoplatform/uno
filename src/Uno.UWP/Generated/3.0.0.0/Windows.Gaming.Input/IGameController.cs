#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Gaming.Input
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IGameController 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Gaming.Input.Headset Headset
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool IsWireless
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.System.User User
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.Gaming.Input.IGameController.HeadsetConnected.add
		// Forced skipping of method Windows.Gaming.Input.IGameController.HeadsetConnected.remove
		// Forced skipping of method Windows.Gaming.Input.IGameController.HeadsetDisconnected.add
		// Forced skipping of method Windows.Gaming.Input.IGameController.HeadsetDisconnected.remove
		// Forced skipping of method Windows.Gaming.Input.IGameController.UserChanged.add
		// Forced skipping of method Windows.Gaming.Input.IGameController.UserChanged.remove
		// Forced skipping of method Windows.Gaming.Input.IGameController.Headset.get
		// Forced skipping of method Windows.Gaming.Input.IGameController.IsWireless.get
		// Forced skipping of method Windows.Gaming.Input.IGameController.User.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		 event global::Windows.Foundation.TypedEventHandler<global::Windows.Gaming.Input.IGameController, global::Windows.Gaming.Input.Headset> HeadsetConnected;
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		 event global::Windows.Foundation.TypedEventHandler<global::Windows.Gaming.Input.IGameController, global::Windows.Gaming.Input.Headset> HeadsetDisconnected;
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		 event global::Windows.Foundation.TypedEventHandler<global::Windows.Gaming.Input.IGameController, global::Windows.System.UserChangedEventArgs> UserChanged;
		#endif
	}
}
