#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Phone.PersonalInformation.Provisioning
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public static partial class MessagePartnerProvisioningManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction ImportSmsToSystemAsync( bool incoming,  bool read,  string body,  string sender,  global::System.Collections.Generic.IReadOnlyList<string> recipients,  global::System.DateTimeOffset deliveryTime)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction MessagePartnerProvisioningManager.ImportSmsToSystemAsync(bool incoming, bool read, string body, string sender, IReadOnlyList<string> recipients, DateTimeOffset deliveryTime) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncAction%20MessagePartnerProvisioningManager.ImportSmsToSystemAsync%28bool%20incoming%2C%20bool%20read%2C%20string%20body%2C%20string%20sender%2C%20IReadOnlyList%3Cstring%3E%20recipients%2C%20DateTimeOffset%20deliveryTime%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction ImportMmsToSystemAsync( bool incoming,  bool read,  string subject,  string sender,  global::System.Collections.Generic.IReadOnlyList<string> recipients,  global::System.DateTimeOffset deliveryTime,  global::System.Collections.Generic.IReadOnlyList<global::System.Collections.Generic.IReadOnlyDictionary<string, object>> attachments)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction MessagePartnerProvisioningManager.ImportMmsToSystemAsync(bool incoming, bool read, string subject, string sender, IReadOnlyList<string> recipients, DateTimeOffset deliveryTime, IReadOnlyList<IReadOnlyDictionary<string, object>> attachments) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncAction%20MessagePartnerProvisioningManager.ImportMmsToSystemAsync%28bool%20incoming%2C%20bool%20read%2C%20string%20subject%2C%20string%20sender%2C%20IReadOnlyList%3Cstring%3E%20recipients%2C%20DateTimeOffset%20deliveryTime%2C%20IReadOnlyList%3CIReadOnlyDictionary%3Cstring%2C%20object%3E%3E%20attachments%29");
		}
		#endif
	}
}
