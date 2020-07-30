#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Cryptography
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CryptographicBuffer 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool Compare( global::Windows.Storage.Streams.IBuffer object1,  global::Windows.Storage.Streams.IBuffer object2)
		{
			throw new global::System.NotImplementedException("The member bool CryptographicBuffer.Compare(IBuffer object1, IBuffer object2) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Storage.Streams.IBuffer GenerateRandom( uint length)
		{
			throw new global::System.NotImplementedException("The member IBuffer CryptographicBuffer.GenerateRandom(uint length) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static uint GenerateRandomNumber()
		{
			throw new global::System.NotImplementedException("The member uint CryptographicBuffer.GenerateRandomNumber() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Storage.Streams.IBuffer CreateFromByteArray( byte[] value)
		{
			throw new global::System.NotImplementedException("The member IBuffer CryptographicBuffer.CreateFromByteArray(byte[] value) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void CopyToByteArray( global::Windows.Storage.Streams.IBuffer buffer, out byte[] value)
		{
			throw new global::System.NotImplementedException("The member void CryptographicBuffer.CopyToByteArray(IBuffer buffer, out byte[] value) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Storage.Streams.IBuffer DecodeFromHexString( string value)
		{
			throw new global::System.NotImplementedException("The member IBuffer CryptographicBuffer.DecodeFromHexString(string value) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static string EncodeToHexString( global::Windows.Storage.Streams.IBuffer buffer)
		{
			throw new global::System.NotImplementedException("The member string CryptographicBuffer.EncodeToHexString(IBuffer buffer) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Storage.Streams.IBuffer DecodeFromBase64String( string value)
		{
			throw new global::System.NotImplementedException("The member IBuffer CryptographicBuffer.DecodeFromBase64String(string value) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static string EncodeToBase64String( global::Windows.Storage.Streams.IBuffer buffer)
		{
			throw new global::System.NotImplementedException("The member string CryptographicBuffer.EncodeToBase64String(IBuffer buffer) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Storage.Streams.IBuffer ConvertStringToBinary( string value,  global::Windows.Security.Cryptography.BinaryStringEncoding encoding)
		{
			throw new global::System.NotImplementedException("The member IBuffer CryptographicBuffer.ConvertStringToBinary(string value, BinaryStringEncoding encoding) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static string ConvertBinaryToString( global::Windows.Security.Cryptography.BinaryStringEncoding encoding,  global::Windows.Storage.Streams.IBuffer buffer)
		{
			throw new global::System.NotImplementedException("The member string CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding encoding, IBuffer buffer) is not implemented in Uno.");
		}
		#endif
	}
}
