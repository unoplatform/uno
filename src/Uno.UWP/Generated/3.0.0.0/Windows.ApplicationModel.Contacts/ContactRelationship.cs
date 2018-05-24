#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Contacts
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ContactRelationship 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Other,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Spouse,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Partner,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Sibling,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Parent,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Child,
		#endif
	}
	#endif
}
