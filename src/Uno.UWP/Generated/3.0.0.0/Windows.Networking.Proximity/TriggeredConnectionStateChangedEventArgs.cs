#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Proximity
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class TriggeredConnectionStateChangedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint Id
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint TriggeredConnectionStateChangedEventArgs.Id is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Sockets.StreamSocket Socket
		{
			get
			{
				throw new global::System.NotImplementedException("The member StreamSocket TriggeredConnectionStateChangedEventArgs.Socket is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Proximity.TriggeredConnectState State
		{
			get
			{
				throw new global::System.NotImplementedException("The member TriggeredConnectState TriggeredConnectionStateChangedEventArgs.State is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.Proximity.TriggeredConnectionStateChangedEventArgs.State.get
		// Forced skipping of method Windows.Networking.Proximity.TriggeredConnectionStateChangedEventArgs.Id.get
		// Forced skipping of method Windows.Networking.Proximity.TriggeredConnectionStateChangedEventArgs.Socket.get
	}
}
