#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.NetworkOperators
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class UssdSession 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Networking.NetworkOperators.UssdReply> SendMessageAndGetReplyAsync( global::Windows.Networking.NetworkOperators.UssdMessage message)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<UssdReply> UssdSession.SendMessageAndGetReplyAsync(UssdMessage message) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CUssdReply%3E%20UssdSession.SendMessageAndGetReplyAsync%28UssdMessage%20message%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Close()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.NetworkOperators.UssdSession", "void UssdSession.Close()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Networking.NetworkOperators.UssdSession CreateFromNetworkAccountId( string networkAccountId)
		{
			throw new global::System.NotImplementedException("The member UssdSession UssdSession.CreateFromNetworkAccountId(string networkAccountId) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=UssdSession%20UssdSession.CreateFromNetworkAccountId%28string%20networkAccountId%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Networking.NetworkOperators.UssdSession CreateFromNetworkInterfaceId( string networkInterfaceId)
		{
			throw new global::System.NotImplementedException("The member UssdSession UssdSession.CreateFromNetworkInterfaceId(string networkInterfaceId) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=UssdSession%20UssdSession.CreateFromNetworkInterfaceId%28string%20networkInterfaceId%29");
		}
		#endif
	}
}
