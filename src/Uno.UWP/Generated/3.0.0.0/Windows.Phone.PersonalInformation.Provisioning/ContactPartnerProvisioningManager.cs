#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Phone.PersonalInformation.Provisioning
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ContactPartnerProvisioningManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction AssociateNetworkAccountAsync( global::Windows.Phone.PersonalInformation.ContactStore store,  string networkName,  string networkAccountId)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction ContactPartnerProvisioningManager.AssociateNetworkAccountAsync(ContactStore store, string networkName, string networkAccountId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction ImportVcardToSystemAsync( global::Windows.Storage.Streams.IInputStream stream)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction ContactPartnerProvisioningManager.ImportVcardToSystemAsync(IInputStream stream) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction AssociateSocialNetworkAccountAsync( global::Windows.Phone.PersonalInformation.ContactStore store,  string networkName,  string networkAccountId)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction ContactPartnerProvisioningManager.AssociateSocialNetworkAccountAsync(ContactStore store, string networkName, string networkAccountId) is not implemented in Uno.");
		}
		#endif
	}
}
