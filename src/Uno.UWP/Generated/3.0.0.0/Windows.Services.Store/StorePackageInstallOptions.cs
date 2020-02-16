#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Services.Store
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class StorePackageInstallOptions 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool AllowForcedAppRestart
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool StorePackageInstallOptions.AllowForcedAppRestart is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Services.Store.StorePackageInstallOptions", "bool StorePackageInstallOptions.AllowForcedAppRestart");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public StorePackageInstallOptions() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Services.Store.StorePackageInstallOptions", "StorePackageInstallOptions.StorePackageInstallOptions()");
		}
		#endif
		// Forced skipping of method Windows.Services.Store.StorePackageInstallOptions.StorePackageInstallOptions()
		// Forced skipping of method Windows.Services.Store.StorePackageInstallOptions.AllowForcedAppRestart.get
		// Forced skipping of method Windows.Services.Store.StorePackageInstallOptions.AllowForcedAppRestart.set
	}
}
