#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.UserDataAccounts
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum UserDataAccountContentKinds 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Email,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Contact,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Appointment,
		#endif
	}
	#endif
}
