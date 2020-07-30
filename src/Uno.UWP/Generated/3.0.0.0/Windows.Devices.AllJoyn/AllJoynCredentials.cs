#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.AllJoyn
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AllJoynCredentials 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan Timeout
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan AllJoynCredentials.Timeout is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.AllJoyn.AllJoynCredentials", "TimeSpan AllJoynCredentials.Timeout");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Credentials.PasswordCredential PasswordCredential
		{
			get
			{
				throw new global::System.NotImplementedException("The member PasswordCredential AllJoynCredentials.PasswordCredential is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.AllJoyn.AllJoynCredentials", "PasswordCredential AllJoynCredentials.PasswordCredential");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Cryptography.Certificates.Certificate Certificate
		{
			get
			{
				throw new global::System.NotImplementedException("The member Certificate AllJoynCredentials.Certificate is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.AllJoyn.AllJoynCredentials", "Certificate AllJoynCredentials.Certificate");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.AllJoyn.AllJoynAuthenticationMechanism AuthenticationMechanism
		{
			get
			{
				throw new global::System.NotImplementedException("The member AllJoynAuthenticationMechanism AllJoynCredentials.AuthenticationMechanism is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.AllJoyn.AllJoynCredentials.AuthenticationMechanism.get
		// Forced skipping of method Windows.Devices.AllJoyn.AllJoynCredentials.Certificate.get
		// Forced skipping of method Windows.Devices.AllJoyn.AllJoynCredentials.Certificate.set
		// Forced skipping of method Windows.Devices.AllJoyn.AllJoynCredentials.PasswordCredential.get
		// Forced skipping of method Windows.Devices.AllJoyn.AllJoynCredentials.PasswordCredential.set
		// Forced skipping of method Windows.Devices.AllJoyn.AllJoynCredentials.Timeout.get
		// Forced skipping of method Windows.Devices.AllJoyn.AllJoynCredentials.Timeout.set
	}
}
