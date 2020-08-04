#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Cryptography.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class EncryptedAndAuthenticatedData 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IBuffer AuthenticationTag
		{
			get
			{
				throw new global::System.NotImplementedException("The member IBuffer EncryptedAndAuthenticatedData.AuthenticationTag is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IBuffer EncryptedData
		{
			get
			{
				throw new global::System.NotImplementedException("The member IBuffer EncryptedAndAuthenticatedData.EncryptedData is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Security.Cryptography.Core.EncryptedAndAuthenticatedData.EncryptedData.get
		// Forced skipping of method Windows.Security.Cryptography.Core.EncryptedAndAuthenticatedData.AuthenticationTag.get
	}
}
