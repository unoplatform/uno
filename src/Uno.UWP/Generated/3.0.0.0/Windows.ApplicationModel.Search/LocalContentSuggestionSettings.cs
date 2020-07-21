#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Search
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class LocalContentSuggestionSettings 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Enabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool LocalContentSuggestionSettings.Enabled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Search.LocalContentSuggestionSettings", "bool LocalContentSuggestionSettings.Enabled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string AqsFilter
		{
			get
			{
				throw new global::System.NotImplementedException("The member string LocalContentSuggestionSettings.AqsFilter is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Search.LocalContentSuggestionSettings", "string LocalContentSuggestionSettings.AqsFilter");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<global::Windows.Storage.StorageFolder> Locations
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<StorageFolder> LocalContentSuggestionSettings.Locations is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<string> PropertiesToMatch
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<string> LocalContentSuggestionSettings.PropertiesToMatch is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public LocalContentSuggestionSettings() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Search.LocalContentSuggestionSettings", "LocalContentSuggestionSettings.LocalContentSuggestionSettings()");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Search.LocalContentSuggestionSettings.LocalContentSuggestionSettings()
		// Forced skipping of method Windows.ApplicationModel.Search.LocalContentSuggestionSettings.Enabled.set
		// Forced skipping of method Windows.ApplicationModel.Search.LocalContentSuggestionSettings.Enabled.get
		// Forced skipping of method Windows.ApplicationModel.Search.LocalContentSuggestionSettings.Locations.get
		// Forced skipping of method Windows.ApplicationModel.Search.LocalContentSuggestionSettings.AqsFilter.set
		// Forced skipping of method Windows.ApplicationModel.Search.LocalContentSuggestionSettings.AqsFilter.get
		// Forced skipping of method Windows.ApplicationModel.Search.LocalContentSuggestionSettings.PropertiesToMatch.get
	}
}
