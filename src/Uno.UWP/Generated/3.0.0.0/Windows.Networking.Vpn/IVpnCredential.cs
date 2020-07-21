#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Vpn
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IVpnCredential 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string AdditionalPin
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Security.Cryptography.Certificates.Certificate CertificateCredential
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Security.Credentials.PasswordCredential OldPasswordCredential
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Security.Credentials.PasswordCredential PasskeyCredential
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.Networking.Vpn.IVpnCredential.PasskeyCredential.get
		// Forced skipping of method Windows.Networking.Vpn.IVpnCredential.CertificateCredential.get
		// Forced skipping of method Windows.Networking.Vpn.IVpnCredential.AdditionalPin.get
		// Forced skipping of method Windows.Networking.Vpn.IVpnCredential.OldPasswordCredential.get
	}
}
