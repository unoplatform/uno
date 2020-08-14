#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Email
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class EmailQueryTextSearch 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Text
		{
			get
			{
				throw new global::System.NotImplementedException("The member string EmailQueryTextSearch.Text is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Email.EmailQueryTextSearch", "string EmailQueryTextSearch.Text");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Email.EmailQuerySearchScope SearchScope
		{
			get
			{
				throw new global::System.NotImplementedException("The member EmailQuerySearchScope EmailQueryTextSearch.SearchScope is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Email.EmailQueryTextSearch", "EmailQuerySearchScope EmailQueryTextSearch.SearchScope");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Email.EmailQuerySearchFields Fields
		{
			get
			{
				throw new global::System.NotImplementedException("The member EmailQuerySearchFields EmailQueryTextSearch.Fields is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Email.EmailQueryTextSearch", "EmailQuerySearchFields EmailQueryTextSearch.Fields");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Email.EmailQueryTextSearch.Fields.get
		// Forced skipping of method Windows.ApplicationModel.Email.EmailQueryTextSearch.Fields.set
		// Forced skipping of method Windows.ApplicationModel.Email.EmailQueryTextSearch.SearchScope.get
		// Forced skipping of method Windows.ApplicationModel.Email.EmailQueryTextSearch.SearchScope.set
		// Forced skipping of method Windows.ApplicationModel.Email.EmailQueryTextSearch.Text.get
		// Forced skipping of method Windows.ApplicationModel.Email.EmailQueryTextSearch.Text.set
	}
}
