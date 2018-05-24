#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Text
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum LinkType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Undefined,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NotALink,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ClientLink,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		FriendlyLinkName,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		FriendlyLinkAddress,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AutoLink,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AutoLinkEmail,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AutoLinkPhone,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AutoLinkPath,
		#endif
	}
	#endif
}
