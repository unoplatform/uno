#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Vpn
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum VpnManagementErrorStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Ok,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Other,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InvalidXmlSyntax,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ProfileNameTooLong,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ProfileInvalidAppId,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AccessDenied,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CannotFindProfile,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AlreadyDisconnecting,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AlreadyConnected,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		GeneralAuthenticationFailure,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		EapFailure,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SmartCardFailure,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CertificateFailure,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ServerConfiguration,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NoConnection,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ServerConnection,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UserNamePassword,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DnsNotResolvable,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InvalidIP,
		#endif
	}
	#endif
}
