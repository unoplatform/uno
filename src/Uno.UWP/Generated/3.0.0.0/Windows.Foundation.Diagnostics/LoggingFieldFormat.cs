#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Foundation.Diagnostics
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum LoggingFieldFormat 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Default,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Hidden,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		String,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Boolean,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Hexadecimal,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ProcessId,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ThreadId,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Port,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Ipv4Address,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Ipv6Address,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SocketAddress,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Xml,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Json,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Win32Error,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NTStatus,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HResult,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		FileTime,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Signed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unsigned,
		#endif
	}
	#endif
}
