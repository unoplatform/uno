#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Email
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class EmailMailboxAutoReply 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Response
		{
			get
			{
				throw new global::System.NotImplementedException("The member string EmailMailboxAutoReply.Response is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Email.EmailMailboxAutoReply", "string EmailMailboxAutoReply.Response");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsEnabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool EmailMailboxAutoReply.IsEnabled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Email.EmailMailboxAutoReply", "bool EmailMailboxAutoReply.IsEnabled");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Email.EmailMailboxAutoReply.IsEnabled.get
		// Forced skipping of method Windows.ApplicationModel.Email.EmailMailboxAutoReply.IsEnabled.set
		// Forced skipping of method Windows.ApplicationModel.Email.EmailMailboxAutoReply.Response.get
		// Forced skipping of method Windows.ApplicationModel.Email.EmailMailboxAutoReply.Response.set
	}
}
