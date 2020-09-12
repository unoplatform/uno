#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PackageCatalog 
	{
		// Forced skipping of method Windows.ApplicationModel.PackageCatalog.PackageStaging.add
		// Forced skipping of method Windows.ApplicationModel.PackageCatalog.PackageStaging.remove
		// Forced skipping of method Windows.ApplicationModel.PackageCatalog.PackageInstalling.add
		// Forced skipping of method Windows.ApplicationModel.PackageCatalog.PackageInstalling.remove
		// Forced skipping of method Windows.ApplicationModel.PackageCatalog.PackageUpdating.add
		// Forced skipping of method Windows.ApplicationModel.PackageCatalog.PackageUpdating.remove
		// Forced skipping of method Windows.ApplicationModel.PackageCatalog.PackageUninstalling.add
		// Forced skipping of method Windows.ApplicationModel.PackageCatalog.PackageUninstalling.remove
		// Forced skipping of method Windows.ApplicationModel.PackageCatalog.PackageStatusChanged.add
		// Forced skipping of method Windows.ApplicationModel.PackageCatalog.PackageStatusChanged.remove
		// Forced skipping of method Windows.ApplicationModel.PackageCatalog.PackageContentGroupStaging.add
		// Forced skipping of method Windows.ApplicationModel.PackageCatalog.PackageContentGroupStaging.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.PackageCatalogAddOptionalPackageResult> AddOptionalPackageAsync( string optionalPackageFamilyName)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<PackageCatalogAddOptionalPackageResult> PackageCatalog.AddOptionalPackageAsync(string optionalPackageFamilyName) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.PackageCatalogRemoveOptionalPackagesResult> RemoveOptionalPackagesAsync( global::System.Collections.Generic.IEnumerable<string> optionalPackageFamilyNames)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<PackageCatalogRemoveOptionalPackagesResult> PackageCatalog.RemoveOptionalPackagesAsync(IEnumerable<string> optionalPackageFamilyNames) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.ApplicationModel.PackageCatalogAddResourcePackageResult, global::Windows.ApplicationModel.PackageInstallProgress> AddResourcePackageAsync( string resourcePackageFamilyName,  string resourceID,  global::Windows.ApplicationModel.AddResourcePackageOptions options)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<PackageCatalogAddResourcePackageResult, PackageInstallProgress> PackageCatalog.AddResourcePackageAsync(string resourcePackageFamilyName, string resourceID, AddResourcePackageOptions options) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.PackageCatalogRemoveResourcePackagesResult> RemoveResourcePackagesAsync( global::System.Collections.Generic.IEnumerable<global::Windows.ApplicationModel.Package> resourcePackages)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<PackageCatalogRemoveResourcePackagesResult> PackageCatalog.RemoveResourcePackagesAsync(IEnumerable<Package> resourcePackages) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.ApplicationModel.PackageCatalog OpenForCurrentPackage()
		{
			throw new global::System.NotImplementedException("The member PackageCatalog PackageCatalog.OpenForCurrentPackage() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.ApplicationModel.PackageCatalog OpenForCurrentUser()
		{
			throw new global::System.NotImplementedException("The member PackageCatalog PackageCatalog.OpenForCurrentUser() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.ApplicationModel.PackageCatalog, global::Windows.ApplicationModel.PackageInstallingEventArgs> PackageInstalling
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.PackageCatalog", "event TypedEventHandler<PackageCatalog, PackageInstallingEventArgs> PackageCatalog.PackageInstalling");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.PackageCatalog", "event TypedEventHandler<PackageCatalog, PackageInstallingEventArgs> PackageCatalog.PackageInstalling");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.ApplicationModel.PackageCatalog, global::Windows.ApplicationModel.PackageStagingEventArgs> PackageStaging
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.PackageCatalog", "event TypedEventHandler<PackageCatalog, PackageStagingEventArgs> PackageCatalog.PackageStaging");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.PackageCatalog", "event TypedEventHandler<PackageCatalog, PackageStagingEventArgs> PackageCatalog.PackageStaging");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.ApplicationModel.PackageCatalog, global::Windows.ApplicationModel.PackageStatusChangedEventArgs> PackageStatusChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.PackageCatalog", "event TypedEventHandler<PackageCatalog, PackageStatusChangedEventArgs> PackageCatalog.PackageStatusChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.PackageCatalog", "event TypedEventHandler<PackageCatalog, PackageStatusChangedEventArgs> PackageCatalog.PackageStatusChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.ApplicationModel.PackageCatalog, global::Windows.ApplicationModel.PackageUninstallingEventArgs> PackageUninstalling
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.PackageCatalog", "event TypedEventHandler<PackageCatalog, PackageUninstallingEventArgs> PackageCatalog.PackageUninstalling");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.PackageCatalog", "event TypedEventHandler<PackageCatalog, PackageUninstallingEventArgs> PackageCatalog.PackageUninstalling");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.ApplicationModel.PackageCatalog, global::Windows.ApplicationModel.PackageUpdatingEventArgs> PackageUpdating
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.PackageCatalog", "event TypedEventHandler<PackageCatalog, PackageUpdatingEventArgs> PackageCatalog.PackageUpdating");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.PackageCatalog", "event TypedEventHandler<PackageCatalog, PackageUpdatingEventArgs> PackageCatalog.PackageUpdating");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.ApplicationModel.PackageCatalog, global::Windows.ApplicationModel.PackageContentGroupStagingEventArgs> PackageContentGroupStaging
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.PackageCatalog", "event TypedEventHandler<PackageCatalog, PackageContentGroupStagingEventArgs> PackageCatalog.PackageContentGroupStaging");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.PackageCatalog", "event TypedEventHandler<PackageCatalog, PackageContentGroupStagingEventArgs> PackageCatalog.PackageContentGroupStaging");
			}
		}
		#endif
	}
}
