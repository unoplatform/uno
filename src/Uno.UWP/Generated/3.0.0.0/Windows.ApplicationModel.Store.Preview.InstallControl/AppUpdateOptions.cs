#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Store.Preview.InstallControl
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AppUpdateOptions 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string CatalogId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string AppUpdateOptions.CatalogId is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Store.Preview.InstallControl.AppUpdateOptions", "string AppUpdateOptions.CatalogId");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool AllowForcedAppRestart
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool AppUpdateOptions.AllowForcedAppRestart is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Store.Preview.InstallControl.AppUpdateOptions", "bool AppUpdateOptions.AllowForcedAppRestart");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool AutomaticallyDownloadAndInstallUpdateIfFound
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool AppUpdateOptions.AutomaticallyDownloadAndInstallUpdateIfFound is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Store.Preview.InstallControl.AppUpdateOptions", "bool AppUpdateOptions.AutomaticallyDownloadAndInstallUpdateIfFound");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public AppUpdateOptions() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Store.Preview.InstallControl.AppUpdateOptions", "AppUpdateOptions.AppUpdateOptions()");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Store.Preview.InstallControl.AppUpdateOptions.AppUpdateOptions()
		// Forced skipping of method Windows.ApplicationModel.Store.Preview.InstallControl.AppUpdateOptions.CatalogId.get
		// Forced skipping of method Windows.ApplicationModel.Store.Preview.InstallControl.AppUpdateOptions.CatalogId.set
		// Forced skipping of method Windows.ApplicationModel.Store.Preview.InstallControl.AppUpdateOptions.AllowForcedAppRestart.get
		// Forced skipping of method Windows.ApplicationModel.Store.Preview.InstallControl.AppUpdateOptions.AllowForcedAppRestart.set
		// Forced skipping of method Windows.ApplicationModel.Store.Preview.InstallControl.AppUpdateOptions.AutomaticallyDownloadAndInstallUpdateIfFound.get
		// Forced skipping of method Windows.ApplicationModel.Store.Preview.InstallControl.AppUpdateOptions.AutomaticallyDownloadAndInstallUpdateIfFound.set
	}
}
