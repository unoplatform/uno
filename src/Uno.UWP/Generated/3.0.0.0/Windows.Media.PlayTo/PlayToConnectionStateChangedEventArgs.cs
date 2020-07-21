#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.PlayTo
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PlayToConnectionStateChangedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.PlayTo.PlayToConnectionState CurrentState
		{
			get
			{
				throw new global::System.NotImplementedException("The member PlayToConnectionState PlayToConnectionStateChangedEventArgs.CurrentState is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.PlayTo.PlayToConnectionState PreviousState
		{
			get
			{
				throw new global::System.NotImplementedException("The member PlayToConnectionState PlayToConnectionStateChangedEventArgs.PreviousState is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.PlayTo.PlayToConnectionStateChangedEventArgs.PreviousState.get
		// Forced skipping of method Windows.Media.PlayTo.PlayToConnectionStateChangedEventArgs.CurrentState.get
	}
}
