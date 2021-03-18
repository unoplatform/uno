#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Email
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class EmailQueryOptions 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Email.EmailQuerySortProperty SortProperty
		{
			get
			{
				throw new global::System.NotImplementedException("The member EmailQuerySortProperty EmailQueryOptions.SortProperty is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Email.EmailQueryOptions", "EmailQuerySortProperty EmailQueryOptions.SortProperty");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Email.EmailQuerySortDirection SortDirection
		{
			get
			{
				throw new global::System.NotImplementedException("The member EmailQuerySortDirection EmailQueryOptions.SortDirection is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Email.EmailQueryOptions", "EmailQuerySortDirection EmailQueryOptions.SortDirection");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Email.EmailQueryKind Kind
		{
			get
			{
				throw new global::System.NotImplementedException("The member EmailQueryKind EmailQueryOptions.Kind is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Email.EmailQueryOptions", "EmailQueryKind EmailQueryOptions.Kind");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<string> FolderIds
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<string> EmailQueryOptions.FolderIds is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Email.EmailQueryTextSearch TextSearch
		{
			get
			{
				throw new global::System.NotImplementedException("The member EmailQueryTextSearch EmailQueryOptions.TextSearch is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public EmailQueryOptions( string text) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Email.EmailQueryOptions", "EmailQueryOptions.EmailQueryOptions(string text)");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Email.EmailQueryOptions.EmailQueryOptions(string)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public EmailQueryOptions( string text,  global::Windows.ApplicationModel.Email.EmailQuerySearchFields fields) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Email.EmailQueryOptions", "EmailQueryOptions.EmailQueryOptions(string text, EmailQuerySearchFields fields)");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Email.EmailQueryOptions.EmailQueryOptions(string, Windows.ApplicationModel.Email.EmailQuerySearchFields)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public EmailQueryOptions() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Email.EmailQueryOptions", "EmailQueryOptions.EmailQueryOptions()");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Email.EmailQueryOptions.EmailQueryOptions()
		// Forced skipping of method Windows.ApplicationModel.Email.EmailQueryOptions.TextSearch.get
		// Forced skipping of method Windows.ApplicationModel.Email.EmailQueryOptions.SortDirection.get
		// Forced skipping of method Windows.ApplicationModel.Email.EmailQueryOptions.SortDirection.set
		// Forced skipping of method Windows.ApplicationModel.Email.EmailQueryOptions.SortProperty.get
		// Forced skipping of method Windows.ApplicationModel.Email.EmailQueryOptions.SortProperty.set
		// Forced skipping of method Windows.ApplicationModel.Email.EmailQueryOptions.Kind.get
		// Forced skipping of method Windows.ApplicationModel.Email.EmailQueryOptions.Kind.set
		// Forced skipping of method Windows.ApplicationModel.Email.EmailQueryOptions.FolderIds.get
	}
}
