#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.NetworkOperators
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ProvisioningAgent 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public ProvisioningAgent() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.NetworkOperators.ProvisioningAgent", "ProvisioningAgent.ProvisioningAgent()");
		}
		#endif
		// Forced skipping of method Windows.Networking.NetworkOperators.ProvisioningAgent.ProvisioningAgent()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Networking.NetworkOperators.ProvisionFromXmlDocumentResults> ProvisionFromXmlDocumentAsync( string provisioningXmlDocument)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<ProvisionFromXmlDocumentResults> ProvisioningAgent.ProvisionFromXmlDocumentAsync(string provisioningXmlDocument) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CProvisionFromXmlDocumentResults%3E%20ProvisioningAgent.ProvisionFromXmlDocumentAsync%28string%20provisioningXmlDocument%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.NetworkOperators.ProvisionedProfile GetProvisionedProfile( global::Windows.Networking.NetworkOperators.ProfileMediaType mediaType,  string profileName)
		{
			throw new global::System.NotImplementedException("The member ProvisionedProfile ProvisioningAgent.GetProvisionedProfile(ProfileMediaType mediaType, string profileName) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=ProvisionedProfile%20ProvisioningAgent.GetProvisionedProfile%28ProfileMediaType%20mediaType%2C%20string%20profileName%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Networking.NetworkOperators.ProvisioningAgent CreateFromNetworkAccountId( string networkAccountId)
		{
			throw new global::System.NotImplementedException("The member ProvisioningAgent ProvisioningAgent.CreateFromNetworkAccountId(string networkAccountId) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=ProvisioningAgent%20ProvisioningAgent.CreateFromNetworkAccountId%28string%20networkAccountId%29");
		}
		#endif
	}
}
