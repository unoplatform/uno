#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Miracast
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MiracastReceiverDisconnectedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Miracast.MiracastReceiverConnection Connection
		{
			get
			{
				throw new global::System.NotImplementedException("The member MiracastReceiverConnection MiracastReceiverDisconnectedEventArgs.Connection is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Miracast.MiracastReceiverDisconnectedEventArgs.Connection.get
	}
}
