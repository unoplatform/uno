#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Cryptography.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CryptographicKey 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint KeySize
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint CryptographicKey.KeySize is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=uint%20CryptographicKey.KeySize");
			}
		}
		#endif
		// Forced skipping of method Windows.Security.Cryptography.Core.CryptographicKey.KeySize.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IBuffer Export()
		{
			throw new global::System.NotImplementedException("The member IBuffer CryptographicKey.Export() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IBuffer%20CryptographicKey.Export%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IBuffer Export( global::Windows.Security.Cryptography.Core.CryptographicPrivateKeyBlobType BlobType)
		{
			throw new global::System.NotImplementedException("The member IBuffer CryptographicKey.Export(CryptographicPrivateKeyBlobType BlobType) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IBuffer%20CryptographicKey.Export%28CryptographicPrivateKeyBlobType%20BlobType%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IBuffer ExportPublicKey()
		{
			throw new global::System.NotImplementedException("The member IBuffer CryptographicKey.ExportPublicKey() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IBuffer%20CryptographicKey.ExportPublicKey%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IBuffer ExportPublicKey( global::Windows.Security.Cryptography.Core.CryptographicPublicKeyBlobType BlobType)
		{
			throw new global::System.NotImplementedException("The member IBuffer CryptographicKey.ExportPublicKey(CryptographicPublicKeyBlobType BlobType) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IBuffer%20CryptographicKey.ExportPublicKey%28CryptographicPublicKeyBlobType%20BlobType%29");
		}
		#endif
	}
}
