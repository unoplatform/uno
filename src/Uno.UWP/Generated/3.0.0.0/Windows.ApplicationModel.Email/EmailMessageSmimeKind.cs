#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Email
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum EmailMessageSmimeKind 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ClearSigned,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		OpaqueSigned,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Encrypted,
		#endif
	}
	#endif
}
