#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Miracast
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MiracastReceiverConnectionCreatedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Miracast.MiracastReceiverConnection Connection
		{
			get
			{
				throw new global::System.NotImplementedException("The member MiracastReceiverConnection MiracastReceiverConnectionCreatedEventArgs.Connection is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=MiracastReceiverConnection%20MiracastReceiverConnectionCreatedEventArgs.Connection");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Pin
		{
			get
			{
				throw new global::System.NotImplementedException("The member string MiracastReceiverConnectionCreatedEventArgs.Pin is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20MiracastReceiverConnectionCreatedEventArgs.Pin");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Miracast.MiracastReceiverConnectionCreatedEventArgs.Connection.get
		// Forced skipping of method Windows.Media.Miracast.MiracastReceiverConnectionCreatedEventArgs.Pin.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Deferral GetDeferral()
		{
			throw new global::System.NotImplementedException("The member Deferral MiracastReceiverConnectionCreatedEventArgs.GetDeferral() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Deferral%20MiracastReceiverConnectionCreatedEventArgs.GetDeferral%28%29");
		}
		#endif
	}
}
