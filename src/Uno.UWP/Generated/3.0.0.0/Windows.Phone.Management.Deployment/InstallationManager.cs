#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Phone.Management.Deployment
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class InstallationManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Phone.Management.Deployment.PackageInstallResult, uint> RemovePackageAsync( string packageFullName,  global::Windows.Management.Deployment.RemovalOptions removalOptions)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<PackageInstallResult, uint> InstallationManager.RemovePackageAsync(string packageFullName, RemovalOptions removalOptions) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Phone.Management.Deployment.PackageInstallResult, uint> RegisterPackageAsync( global::System.Uri manifestUri,  global::System.Collections.Generic.IEnumerable<global::System.Uri> dependencyPackageUris,  global::Windows.Management.Deployment.DeploymentOptions deploymentOptions)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<PackageInstallResult, uint> InstallationManager.RegisterPackageAsync(Uri manifestUri, IEnumerable<Uri> dependencyPackageUris, DeploymentOptions deploymentOptions) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::System.Collections.Generic.IEnumerable<global::Windows.ApplicationModel.Package> FindPackages( string packageName,  string packagePublisher)
		{
			throw new global::System.NotImplementedException("The member IEnumerable<Package> InstallationManager.FindPackages(string packageName, string packagePublisher) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Phone.Management.Deployment.PackageInstallResult, uint> AddPackageAsync( string title,  global::System.Uri sourceLocation)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<PackageInstallResult, uint> InstallationManager.AddPackageAsync(string title, Uri sourceLocation) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Phone.Management.Deployment.PackageInstallResult, uint> AddPackageAsync( string title,  global::System.Uri sourceLocation,  string instanceId,  string offerId,  global::System.Uri license)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<PackageInstallResult, uint> InstallationManager.AddPackageAsync(string title, Uri sourceLocation, string instanceId, string offerId, Uri license) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::System.Collections.Generic.IEnumerable<global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Phone.Management.Deployment.PackageInstallResult, uint>> GetPendingPackageInstalls()
		{
			throw new global::System.NotImplementedException("The member IEnumerable<IAsyncOperationWithProgress<PackageInstallResult, uint>> InstallationManager.GetPendingPackageInstalls() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::System.Collections.Generic.IEnumerable<global::Windows.ApplicationModel.Package> FindPackagesForCurrentPublisher()
		{
			throw new global::System.NotImplementedException("The member IEnumerable<Package> InstallationManager.FindPackagesForCurrentPublisher() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::System.Collections.Generic.IEnumerable<global::Windows.ApplicationModel.Package> FindPackages()
		{
			throw new global::System.NotImplementedException("The member IEnumerable<Package> InstallationManager.FindPackages() is not implemented in Uno.");
		}
		#endif
	}
}
