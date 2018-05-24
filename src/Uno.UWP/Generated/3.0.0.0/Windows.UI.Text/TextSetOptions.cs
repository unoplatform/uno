#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Text
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum TextSetOptions 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UnicodeBidi,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unlink,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unhide,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CheckTextLimit,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		FormatRtf,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ApplyRtfDocumentDefaults,
		#endif
	}
	#endif
}
