#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.NetworkOperators
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class HotspotAuthenticationContext 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Uri AuthenticationUrl
		{
			get
			{
				throw new global::System.NotImplementedException("The member Uri HotspotAuthenticationContext.AuthenticationUrl is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Connectivity.NetworkAdapter NetworkAdapter
		{
			get
			{
				throw new global::System.NotImplementedException("The member NetworkAdapter HotspotAuthenticationContext.NetworkAdapter is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Uri RedirectMessageUrl
		{
			get
			{
				throw new global::System.NotImplementedException("The member Uri HotspotAuthenticationContext.RedirectMessageUrl is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Data.Xml.Dom.XmlDocument RedirectMessageXml
		{
			get
			{
				throw new global::System.NotImplementedException("The member XmlDocument HotspotAuthenticationContext.RedirectMessageXml is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  byte[] WirelessNetworkId
		{
			get
			{
				throw new global::System.NotImplementedException("The member byte[] HotspotAuthenticationContext.WirelessNetworkId is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.NetworkOperators.HotspotAuthenticationContext.WirelessNetworkId.get
		// Forced skipping of method Windows.Networking.NetworkOperators.HotspotAuthenticationContext.NetworkAdapter.get
		// Forced skipping of method Windows.Networking.NetworkOperators.HotspotAuthenticationContext.RedirectMessageUrl.get
		// Forced skipping of method Windows.Networking.NetworkOperators.HotspotAuthenticationContext.RedirectMessageXml.get
		// Forced skipping of method Windows.Networking.NetworkOperators.HotspotAuthenticationContext.AuthenticationUrl.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void IssueCredentials( string userName,  string password,  string extraParameters,  bool markAsManualConnectOnFailure)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.NetworkOperators.HotspotAuthenticationContext", "void HotspotAuthenticationContext.IssueCredentials(string userName, string password, string extraParameters, bool markAsManualConnectOnFailure)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void AbortAuthentication( bool markAsManual)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.NetworkOperators.HotspotAuthenticationContext", "void HotspotAuthenticationContext.AbortAuthentication(bool markAsManual)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SkipAuthentication()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.NetworkOperators.HotspotAuthenticationContext", "void HotspotAuthenticationContext.SkipAuthentication()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void TriggerAttentionRequired( string packageRelativeApplicationId,  string applicationParameters)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.NetworkOperators.HotspotAuthenticationContext", "void HotspotAuthenticationContext.TriggerAttentionRequired(string packageRelativeApplicationId, string applicationParameters)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Networking.NetworkOperators.HotspotCredentialsAuthenticationResult> IssueCredentialsAsync( string userName,  string password,  string extraParameters,  bool markAsManualConnectOnFailure)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<HotspotCredentialsAuthenticationResult> HotspotAuthenticationContext.IssueCredentialsAsync(string userName, string password, string extraParameters, bool markAsManualConnectOnFailure) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool TryGetAuthenticationContext( string evenToken, out global::Windows.Networking.NetworkOperators.HotspotAuthenticationContext context)
		{
			throw new global::System.NotImplementedException("The member bool HotspotAuthenticationContext.TryGetAuthenticationContext(string evenToken, out HotspotAuthenticationContext context) is not implemented in Uno.");
		}
		#endif
	}
}
