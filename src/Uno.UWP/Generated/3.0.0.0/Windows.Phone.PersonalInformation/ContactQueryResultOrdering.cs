#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Phone.PersonalInformation
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ContactQueryResultOrdering 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SystemDefault,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		GivenNameFamilyName,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		FamilyNameGivenName,
		#endif
	}
	#endif
}
