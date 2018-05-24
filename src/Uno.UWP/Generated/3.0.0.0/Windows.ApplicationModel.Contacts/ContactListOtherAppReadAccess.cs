#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Contacts
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ContactListOtherAppReadAccess 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SystemOnly,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Limited,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Full,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
	}
	#endif
}
