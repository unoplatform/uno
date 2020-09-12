#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.DialProtocol
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DialAppStateDetails 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string FullXml
		{
			get
			{
				throw new global::System.NotImplementedException("The member string DialAppStateDetails.FullXml is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.DialProtocol.DialAppState State
		{
			get
			{
				throw new global::System.NotImplementedException("The member DialAppState DialAppStateDetails.State is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.DialProtocol.DialAppStateDetails.State.get
		// Forced skipping of method Windows.Media.DialProtocol.DialAppStateDetails.FullXml.get
	}
}
