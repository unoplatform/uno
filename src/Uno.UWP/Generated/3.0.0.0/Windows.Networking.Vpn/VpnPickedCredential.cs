#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Vpn
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class VpnPickedCredential 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string AdditionalPin
		{
			get
			{
				throw new global::System.NotImplementedException("The member string VpnPickedCredential.AdditionalPin is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Credentials.PasswordCredential OldPasswordCredential
		{
			get
			{
				throw new global::System.NotImplementedException("The member PasswordCredential VpnPickedCredential.OldPasswordCredential is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Credentials.PasswordCredential PasskeyCredential
		{
			get
			{
				throw new global::System.NotImplementedException("The member PasswordCredential VpnPickedCredential.PasskeyCredential is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.Vpn.VpnPickedCredential.PasskeyCredential.get
		// Forced skipping of method Windows.Networking.Vpn.VpnPickedCredential.AdditionalPin.get
		// Forced skipping of method Windows.Networking.Vpn.VpnPickedCredential.OldPasswordCredential.get
	}
}
