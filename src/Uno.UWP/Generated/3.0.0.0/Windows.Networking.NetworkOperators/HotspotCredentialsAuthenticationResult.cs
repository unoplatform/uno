#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.NetworkOperators
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class HotspotCredentialsAuthenticationResult 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Data.Xml.Dom.XmlDocument AuthenticationReplyXml
		{
			get
			{
				throw new global::System.NotImplementedException("The member XmlDocument HotspotCredentialsAuthenticationResult.AuthenticationReplyXml is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool HasNetworkErrorOccurred
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool HotspotCredentialsAuthenticationResult.HasNetworkErrorOccurred is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Uri LogoffUrl
		{
			get
			{
				throw new global::System.NotImplementedException("The member Uri HotspotCredentialsAuthenticationResult.LogoffUrl is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.NetworkOperators.HotspotAuthenticationResponseCode ResponseCode
		{
			get
			{
				throw new global::System.NotImplementedException("The member HotspotAuthenticationResponseCode HotspotCredentialsAuthenticationResult.ResponseCode is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.NetworkOperators.HotspotCredentialsAuthenticationResult.HasNetworkErrorOccurred.get
		// Forced skipping of method Windows.Networking.NetworkOperators.HotspotCredentialsAuthenticationResult.ResponseCode.get
		// Forced skipping of method Windows.Networking.NetworkOperators.HotspotCredentialsAuthenticationResult.LogoffUrl.get
		// Forced skipping of method Windows.Networking.NetworkOperators.HotspotCredentialsAuthenticationResult.AuthenticationReplyXml.get
	}
}
