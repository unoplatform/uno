#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Chat
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public static partial class ChatCapabilitiesManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Chat.ChatCapabilities> GetCachedCapabilitiesAsync( string address,  string transportId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<ChatCapabilities> ChatCapabilitiesManager.GetCachedCapabilitiesAsync(string address, string transportId) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CChatCapabilities%3E%20ChatCapabilitiesManager.GetCachedCapabilitiesAsync%28string%20address%2C%20string%20transportId%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Chat.ChatCapabilities> GetCapabilitiesFromNetworkAsync( string address,  string transportId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<ChatCapabilities> ChatCapabilitiesManager.GetCapabilitiesFromNetworkAsync(string address, string transportId) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CChatCapabilities%3E%20ChatCapabilitiesManager.GetCapabilitiesFromNetworkAsync%28string%20address%2C%20string%20transportId%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Chat.ChatCapabilities> GetCachedCapabilitiesAsync( string address)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<ChatCapabilities> ChatCapabilitiesManager.GetCachedCapabilitiesAsync(string address) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CChatCapabilities%3E%20ChatCapabilitiesManager.GetCachedCapabilitiesAsync%28string%20address%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Chat.ChatCapabilities> GetCapabilitiesFromNetworkAsync( string address)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<ChatCapabilities> ChatCapabilitiesManager.GetCapabilitiesFromNetworkAsync(string address) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CChatCapabilities%3E%20ChatCapabilitiesManager.GetCapabilitiesFromNetworkAsync%28string%20address%29");
		}
		#endif
	}
}
