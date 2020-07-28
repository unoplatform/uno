#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Sockets
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IWebSocketControl2 : global::Windows.Networking.Sockets.IWebSocketControl
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.Collections.Generic.IList<global::Windows.Security.Cryptography.Certificates.ChainValidationResult> IgnorableServerCertificateErrors
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.Networking.Sockets.IWebSocketControl2.IgnorableServerCertificateErrors.get
	}
}
