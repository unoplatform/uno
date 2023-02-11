#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Cryptography.Certificates
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class UserCertificateStore 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Name
		{
			get
			{
				throw new global::System.NotImplementedException("The member string UserCertificateStore.Name is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20UserCertificateStore.Name");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> RequestAddAsync( global::Windows.Security.Cryptography.Certificates.Certificate certificate)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> UserCertificateStore.RequestAddAsync(Certificate certificate) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3Cbool%3E%20UserCertificateStore.RequestAddAsync%28Certificate%20certificate%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> RequestDeleteAsync( global::Windows.Security.Cryptography.Certificates.Certificate certificate)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> UserCertificateStore.RequestDeleteAsync(Certificate certificate) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3Cbool%3E%20UserCertificateStore.RequestDeleteAsync%28Certificate%20certificate%29");
		}
		#endif
		// Forced skipping of method Windows.Security.Cryptography.Certificates.UserCertificateStore.Name.get
	}
}
