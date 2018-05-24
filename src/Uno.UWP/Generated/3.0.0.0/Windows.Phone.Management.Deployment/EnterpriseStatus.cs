#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Phone.Management.Deployment
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum EnterpriseStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Enrolled,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Disabled,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Revoked,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Expired,
		#endif
	}
	#endif
}
