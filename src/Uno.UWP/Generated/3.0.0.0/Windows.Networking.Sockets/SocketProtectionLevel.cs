#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Sockets
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum SocketProtectionLevel 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PlainSocket,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Ssl,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SslAllowNullEncryption,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BluetoothEncryptionAllowNullAuthentication,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BluetoothEncryptionWithAuthentication,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Ssl3AllowWeakEncryption,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Tls10,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Tls11,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Tls12,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unspecified,
		#endif
	}
	#endif
}
