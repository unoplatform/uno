#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Contacts
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ContactMatchReasonKind 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Name,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		EmailAddress,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PhoneNumber,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		JobInfo,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		YomiName,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Other,
		#endif
	}
	#endif
}
