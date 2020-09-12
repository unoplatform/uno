#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Vpn
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class VpnCredential : global::Windows.Networking.Vpn.IVpnCredential
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string AdditionalPin
		{
			get
			{
				throw new global::System.NotImplementedException("The member string VpnCredential.AdditionalPin is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Cryptography.Certificates.Certificate CertificateCredential
		{
			get
			{
				throw new global::System.NotImplementedException("The member Certificate VpnCredential.CertificateCredential is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Credentials.PasswordCredential OldPasswordCredential
		{
			get
			{
				throw new global::System.NotImplementedException("The member PasswordCredential VpnCredential.OldPasswordCredential is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Credentials.PasswordCredential PasskeyCredential
		{
			get
			{
				throw new global::System.NotImplementedException("The member PasswordCredential VpnCredential.PasskeyCredential is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.Vpn.VpnCredential.PasskeyCredential.get
		// Forced skipping of method Windows.Networking.Vpn.VpnCredential.CertificateCredential.get
		// Forced skipping of method Windows.Networking.Vpn.VpnCredential.AdditionalPin.get
		// Forced skipping of method Windows.Networking.Vpn.VpnCredential.OldPasswordCredential.get
		// Processing: Windows.Networking.Vpn.IVpnCredential
	}
}
