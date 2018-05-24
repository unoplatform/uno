#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Vpn
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum VpnCredentialType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UsernamePassword,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UsernameOtpPin,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UsernamePasswordAndPin,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UsernamePasswordChange,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SmartCard,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ProtectedCertificate,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UnProtectedCertificate,
		#endif
	}
	#endif
}
