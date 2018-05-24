#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Contacts
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ContactCardTabKind 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Default,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Email,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Messaging,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Phone,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Video,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		OrganizationalHierarchy,
		#endif
	}
	#endif
}
