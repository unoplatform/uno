#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.UserDataTasks
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class UserDataTaskQueryOptions 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.UserDataTasks.UserDataTaskQuerySortProperty SortProperty
		{
			get
			{
				throw new global::System.NotImplementedException("The member UserDataTaskQuerySortProperty UserDataTaskQueryOptions.SortProperty is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.UserDataTasks.UserDataTaskQueryOptions", "UserDataTaskQuerySortProperty UserDataTaskQueryOptions.SortProperty");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.UserDataTasks.UserDataTaskQueryKind Kind
		{
			get
			{
				throw new global::System.NotImplementedException("The member UserDataTaskQueryKind UserDataTaskQueryOptions.Kind is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.UserDataTasks.UserDataTaskQueryOptions", "UserDataTaskQueryKind UserDataTaskQueryOptions.Kind");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public UserDataTaskQueryOptions() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.UserDataTasks.UserDataTaskQueryOptions", "UserDataTaskQueryOptions.UserDataTaskQueryOptions()");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.UserDataTasks.UserDataTaskQueryOptions.UserDataTaskQueryOptions()
		// Forced skipping of method Windows.ApplicationModel.UserDataTasks.UserDataTaskQueryOptions.SortProperty.get
		// Forced skipping of method Windows.ApplicationModel.UserDataTasks.UserDataTaskQueryOptions.SortProperty.set
		// Forced skipping of method Windows.ApplicationModel.UserDataTasks.UserDataTaskQueryOptions.Kind.get
		// Forced skipping of method Windows.ApplicationModel.UserDataTasks.UserDataTaskQueryOptions.Kind.set
	}
}
