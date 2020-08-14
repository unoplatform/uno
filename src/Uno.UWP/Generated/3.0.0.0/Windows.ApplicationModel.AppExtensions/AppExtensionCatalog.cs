#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.AppExtensions
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AppExtensionCatalog 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.ApplicationModel.AppExtensions.AppExtension>> FindAllAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<AppExtension>> AppExtensionCatalog.FindAllAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> RequestRemovePackageAsync( string packageFullName)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> AppExtensionCatalog.RequestRemovePackageAsync(string packageFullName) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.AppExtensions.AppExtensionCatalog.PackageInstalled.add
		// Forced skipping of method Windows.ApplicationModel.AppExtensions.AppExtensionCatalog.PackageInstalled.remove
		// Forced skipping of method Windows.ApplicationModel.AppExtensions.AppExtensionCatalog.PackageUpdating.add
		// Forced skipping of method Windows.ApplicationModel.AppExtensions.AppExtensionCatalog.PackageUpdating.remove
		// Forced skipping of method Windows.ApplicationModel.AppExtensions.AppExtensionCatalog.PackageUpdated.add
		// Forced skipping of method Windows.ApplicationModel.AppExtensions.AppExtensionCatalog.PackageUpdated.remove
		// Forced skipping of method Windows.ApplicationModel.AppExtensions.AppExtensionCatalog.PackageUninstalling.add
		// Forced skipping of method Windows.ApplicationModel.AppExtensions.AppExtensionCatalog.PackageUninstalling.remove
		// Forced skipping of method Windows.ApplicationModel.AppExtensions.AppExtensionCatalog.PackageStatusChanged.add
		// Forced skipping of method Windows.ApplicationModel.AppExtensions.AppExtensionCatalog.PackageStatusChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.ApplicationModel.AppExtensions.AppExtensionCatalog Open( string appExtensionName)
		{
			throw new global::System.NotImplementedException("The member AppExtensionCatalog AppExtensionCatalog.Open(string appExtensionName) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.ApplicationModel.AppExtensions.AppExtensionCatalog, global::Windows.ApplicationModel.AppExtensions.AppExtensionPackageInstalledEventArgs> PackageInstalled
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.AppExtensions.AppExtensionCatalog", "event TypedEventHandler<AppExtensionCatalog, AppExtensionPackageInstalledEventArgs> AppExtensionCatalog.PackageInstalled");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.AppExtensions.AppExtensionCatalog", "event TypedEventHandler<AppExtensionCatalog, AppExtensionPackageInstalledEventArgs> AppExtensionCatalog.PackageInstalled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.ApplicationModel.AppExtensions.AppExtensionCatalog, global::Windows.ApplicationModel.AppExtensions.AppExtensionPackageStatusChangedEventArgs> PackageStatusChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.AppExtensions.AppExtensionCatalog", "event TypedEventHandler<AppExtensionCatalog, AppExtensionPackageStatusChangedEventArgs> AppExtensionCatalog.PackageStatusChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.AppExtensions.AppExtensionCatalog", "event TypedEventHandler<AppExtensionCatalog, AppExtensionPackageStatusChangedEventArgs> AppExtensionCatalog.PackageStatusChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.ApplicationModel.AppExtensions.AppExtensionCatalog, global::Windows.ApplicationModel.AppExtensions.AppExtensionPackageUninstallingEventArgs> PackageUninstalling
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.AppExtensions.AppExtensionCatalog", "event TypedEventHandler<AppExtensionCatalog, AppExtensionPackageUninstallingEventArgs> AppExtensionCatalog.PackageUninstalling");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.AppExtensions.AppExtensionCatalog", "event TypedEventHandler<AppExtensionCatalog, AppExtensionPackageUninstallingEventArgs> AppExtensionCatalog.PackageUninstalling");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.ApplicationModel.AppExtensions.AppExtensionCatalog, global::Windows.ApplicationModel.AppExtensions.AppExtensionPackageUpdatedEventArgs> PackageUpdated
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.AppExtensions.AppExtensionCatalog", "event TypedEventHandler<AppExtensionCatalog, AppExtensionPackageUpdatedEventArgs> AppExtensionCatalog.PackageUpdated");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.AppExtensions.AppExtensionCatalog", "event TypedEventHandler<AppExtensionCatalog, AppExtensionPackageUpdatedEventArgs> AppExtensionCatalog.PackageUpdated");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.ApplicationModel.AppExtensions.AppExtensionCatalog, global::Windows.ApplicationModel.AppExtensions.AppExtensionPackageUpdatingEventArgs> PackageUpdating
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.AppExtensions.AppExtensionCatalog", "event TypedEventHandler<AppExtensionCatalog, AppExtensionPackageUpdatingEventArgs> AppExtensionCatalog.PackageUpdating");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.AppExtensions.AppExtensionCatalog", "event TypedEventHandler<AppExtensionCatalog, AppExtensionPackageUpdatingEventArgs> AppExtensionCatalog.PackageUpdating");
			}
		}
		#endif
	}
}
