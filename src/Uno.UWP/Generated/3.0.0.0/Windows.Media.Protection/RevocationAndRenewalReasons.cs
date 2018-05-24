#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Protection
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum RevocationAndRenewalReasons 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UserModeComponentLoad,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		KernelModeComponentLoad,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AppComponent,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		GlobalRevocationListLoadFailed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InvalidGlobalRevocationListSignature,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		GlobalRevocationListAbsent,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ComponentRevoked,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InvalidComponentCertificateExtendedKeyUse,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ComponentCertificateRevoked,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InvalidComponentCertificateRoot,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ComponentHighSecurityCertificateRevoked,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ComponentLowSecurityCertificateRevoked,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BootDriverVerificationFailed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ComponentSignedWithTestCertificate,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		EncryptionFailure,
		#endif
	}
	#endif
}
