#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Phone.Management.Deployment
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public static partial class InstallationManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Phone.Management.Deployment.PackageInstallResult, uint> RemovePackageAsync( string packageFullName,  global::Windows.Management.Deployment.RemovalOptions removalOptions)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<PackageInstallResult, uint> InstallationManager.RemovePackageAsync(string packageFullName, RemovalOptions removalOptions) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperationWithProgress%3CPackageInstallResult%2C%20uint%3E%20InstallationManager.RemovePackageAsync%28string%20packageFullName%2C%20RemovalOptions%20removalOptions%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Phone.Management.Deployment.PackageInstallResult, uint> RegisterPackageAsync( global::System.Uri manifestUri,  global::System.Collections.Generic.IEnumerable<global::System.Uri> dependencyPackageUris,  global::Windows.Management.Deployment.DeploymentOptions deploymentOptions)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<PackageInstallResult, uint> InstallationManager.RegisterPackageAsync(Uri manifestUri, IEnumerable<Uri> dependencyPackageUris, DeploymentOptions deploymentOptions) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperationWithProgress%3CPackageInstallResult%2C%20uint%3E%20InstallationManager.RegisterPackageAsync%28Uri%20manifestUri%2C%20IEnumerable%3CUri%3E%20dependencyPackageUris%2C%20DeploymentOptions%20deploymentOptions%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::System.Collections.Generic.IEnumerable<global::Windows.ApplicationModel.Package> FindPackages( string packageName,  string packagePublisher)
		{
			throw new global::System.NotImplementedException("The member IEnumerable<Package> InstallationManager.FindPackages(string packageName, string packagePublisher) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IEnumerable%3CPackage%3E%20InstallationManager.FindPackages%28string%20packageName%2C%20string%20packagePublisher%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Phone.Management.Deployment.PackageInstallResult, uint> AddPackageAsync( string title,  global::System.Uri sourceLocation)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<PackageInstallResult, uint> InstallationManager.AddPackageAsync(string title, Uri sourceLocation) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperationWithProgress%3CPackageInstallResult%2C%20uint%3E%20InstallationManager.AddPackageAsync%28string%20title%2C%20Uri%20sourceLocation%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Phone.Management.Deployment.PackageInstallResult, uint> AddPackageAsync( string title,  global::System.Uri sourceLocation,  string instanceId,  string offerId,  global::System.Uri license)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<PackageInstallResult, uint> InstallationManager.AddPackageAsync(string title, Uri sourceLocation, string instanceId, string offerId, Uri license) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperationWithProgress%3CPackageInstallResult%2C%20uint%3E%20InstallationManager.AddPackageAsync%28string%20title%2C%20Uri%20sourceLocation%2C%20string%20instanceId%2C%20string%20offerId%2C%20Uri%20license%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::System.Collections.Generic.IEnumerable<global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Phone.Management.Deployment.PackageInstallResult, uint>> GetPendingPackageInstalls()
		{
			throw new global::System.NotImplementedException("The member IEnumerable<IAsyncOperationWithProgress<PackageInstallResult, uint>> InstallationManager.GetPendingPackageInstalls() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IEnumerable%3CIAsyncOperationWithProgress%3CPackageInstallResult%2C%20uint%3E%3E%20InstallationManager.GetPendingPackageInstalls%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::System.Collections.Generic.IEnumerable<global::Windows.ApplicationModel.Package> FindPackagesForCurrentPublisher()
		{
			throw new global::System.NotImplementedException("The member IEnumerable<Package> InstallationManager.FindPackagesForCurrentPublisher() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IEnumerable%3CPackage%3E%20InstallationManager.FindPackagesForCurrentPublisher%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::System.Collections.Generic.IEnumerable<global::Windows.ApplicationModel.Package> FindPackages()
		{
			throw new global::System.NotImplementedException("The member IEnumerable<Package> InstallationManager.FindPackages() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IEnumerable%3CPackage%3E%20InstallationManager.FindPackages%28%29");
		}
		#endif
	}
}
