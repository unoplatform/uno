#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Connectivity
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class NetworkSecuritySettings 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Connectivity.NetworkAuthenticationType NetworkAuthenticationType
		{
			get
			{
				throw new global::System.NotImplementedException("The member NetworkAuthenticationType NetworkSecuritySettings.NetworkAuthenticationType is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Connectivity.NetworkEncryptionType NetworkEncryptionType
		{
			get
			{
				throw new global::System.NotImplementedException("The member NetworkEncryptionType NetworkSecuritySettings.NetworkEncryptionType is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.Connectivity.NetworkSecuritySettings.NetworkAuthenticationType.get
		// Forced skipping of method Windows.Networking.Connectivity.NetworkSecuritySettings.NetworkEncryptionType.get
	}
}
