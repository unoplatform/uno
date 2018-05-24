#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Contacts
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ContactFieldType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Email,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PhoneNumber,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Location,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InstantMessage,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Custom,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ConnectedServiceAccount,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ImportantDate,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Address,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SignificantOther,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Notes,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Website,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		JobInfo,
		#endif
	}
	#endif
}
