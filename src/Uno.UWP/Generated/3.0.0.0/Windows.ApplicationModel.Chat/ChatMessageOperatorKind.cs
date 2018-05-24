#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Chat
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ChatMessageOperatorKind 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unspecified,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Sms,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Mms,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Rcs,
		#endif
	}
	#endif
}
