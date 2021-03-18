#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Phone.PersonalInformation.Provisioning
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MessagePartnerProvisioningManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction ImportSmsToSystemAsync( bool incoming,  bool read,  string body,  string sender,  global::System.Collections.Generic.IReadOnlyList<string> recipients,  global::System.DateTimeOffset deliveryTime)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction MessagePartnerProvisioningManager.ImportSmsToSystemAsync(bool incoming, bool read, string body, string sender, IReadOnlyList<string> recipients, DateTimeOffset deliveryTime) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction ImportMmsToSystemAsync( bool incoming,  bool read,  string subject,  string sender,  global::System.Collections.Generic.IReadOnlyList<string> recipients,  global::System.DateTimeOffset deliveryTime,  global::System.Collections.Generic.IReadOnlyList<global::System.Collections.Generic.IReadOnlyDictionary<string, object>> attachments)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction MessagePartnerProvisioningManager.ImportMmsToSystemAsync(bool incoming, bool read, string subject, string sender, IReadOnlyList<string> recipients, DateTimeOffset deliveryTime, IReadOnlyList<IReadOnlyDictionary<string, object>> attachments) is not implemented in Uno.");
		}
		#endif
	}
}
