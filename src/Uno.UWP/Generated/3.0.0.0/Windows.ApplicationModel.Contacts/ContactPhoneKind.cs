#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Contacts
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ContactPhoneKind 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Home,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Mobile,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Work,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Other,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Pager,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BusinessFax,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HomeFax,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Company,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Assistant,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Radio,
		#endif
	}
	#endif
}
