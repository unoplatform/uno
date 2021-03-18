#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Management.Deployment
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PackageManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Management.Deployment.PackageManagerDebugSettings DebugSettings
		{
			get
			{
				throw new global::System.NotImplementedException("The member PackageManagerDebugSettings PackageManager.DebugSettings is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public PackageManager() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Management.Deployment.PackageManager", "PackageManager.PackageManager()");
		}
		#endif
		// Forced skipping of method Windows.Management.Deployment.PackageManager.PackageManager()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Management.Deployment.DeploymentResult, global::Windows.Management.Deployment.DeploymentProgress> AddPackageAsync( global::System.Uri packageUri,  global::System.Collections.Generic.IEnumerable<global::System.Uri> dependencyPackageUris,  global::Windows.Management.Deployment.DeploymentOptions deploymentOptions)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> PackageManager.AddPackageAsync(Uri packageUri, IEnumerable<Uri> dependencyPackageUris, DeploymentOptions deploymentOptions) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Management.Deployment.DeploymentResult, global::Windows.Management.Deployment.DeploymentProgress> UpdatePackageAsync( global::System.Uri packageUri,  global::System.Collections.Generic.IEnumerable<global::System.Uri> dependencyPackageUris,  global::Windows.Management.Deployment.DeploymentOptions deploymentOptions)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> PackageManager.UpdatePackageAsync(Uri packageUri, IEnumerable<Uri> dependencyPackageUris, DeploymentOptions deploymentOptions) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Management.Deployment.DeploymentResult, global::Windows.Management.Deployment.DeploymentProgress> RemovePackageAsync( string packageFullName)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> PackageManager.RemovePackageAsync(string packageFullName) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Management.Deployment.DeploymentResult, global::Windows.Management.Deployment.DeploymentProgress> StagePackageAsync( global::System.Uri packageUri,  global::System.Collections.Generic.IEnumerable<global::System.Uri> dependencyPackageUris)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> PackageManager.StagePackageAsync(Uri packageUri, IEnumerable<Uri> dependencyPackageUris) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Management.Deployment.DeploymentResult, global::Windows.Management.Deployment.DeploymentProgress> RegisterPackageAsync( global::System.Uri manifestUri,  global::System.Collections.Generic.IEnumerable<global::System.Uri> dependencyPackageUris,  global::Windows.Management.Deployment.DeploymentOptions deploymentOptions)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> PackageManager.RegisterPackageAsync(Uri manifestUri, IEnumerable<Uri> dependencyPackageUris, DeploymentOptions deploymentOptions) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IEnumerable<global::Windows.ApplicationModel.Package> FindPackages()
		{
			throw new global::System.NotImplementedException("The member IEnumerable<Package> PackageManager.FindPackages() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IEnumerable<global::Windows.ApplicationModel.Package> FindPackagesForUser( string userSecurityId)
		{
			throw new global::System.NotImplementedException("The member IEnumerable<Package> PackageManager.FindPackagesForUser(string userSecurityId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IEnumerable<global::Windows.ApplicationModel.Package> FindPackages( string packageName,  string packagePublisher)
		{
			throw new global::System.NotImplementedException("The member IEnumerable<Package> PackageManager.FindPackages(string packageName, string packagePublisher) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IEnumerable<global::Windows.ApplicationModel.Package> FindPackagesForUser( string userSecurityId,  string packageName,  string packagePublisher)
		{
			throw new global::System.NotImplementedException("The member IEnumerable<Package> PackageManager.FindPackagesForUser(string userSecurityId, string packageName, string packagePublisher) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IEnumerable<global::Windows.Management.Deployment.PackageUserInformation> FindUsers( string packageFullName)
		{
			throw new global::System.NotImplementedException("The member IEnumerable<PackageUserInformation> PackageManager.FindUsers(string packageFullName) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetPackageState( string packageFullName,  global::Windows.Management.Deployment.PackageState packageState)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Management.Deployment.PackageManager", "void PackageManager.SetPackageState(string packageFullName, PackageState packageState)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Package FindPackage( string packageFullName)
		{
			throw new global::System.NotImplementedException("The member Package PackageManager.FindPackage(string packageFullName) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Management.Deployment.DeploymentResult, global::Windows.Management.Deployment.DeploymentProgress> CleanupPackageForUserAsync( string packageName,  string userSecurityId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> PackageManager.CleanupPackageForUserAsync(string packageName, string userSecurityId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IEnumerable<global::Windows.ApplicationModel.Package> FindPackages( string packageFamilyName)
		{
			throw new global::System.NotImplementedException("The member IEnumerable<Package> PackageManager.FindPackages(string packageFamilyName) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IEnumerable<global::Windows.ApplicationModel.Package> FindPackagesForUser( string userSecurityId,  string packageFamilyName)
		{
			throw new global::System.NotImplementedException("The member IEnumerable<Package> PackageManager.FindPackagesForUser(string userSecurityId, string packageFamilyName) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Package FindPackageForUser( string userSecurityId,  string packageFullName)
		{
			throw new global::System.NotImplementedException("The member Package PackageManager.FindPackageForUser(string userSecurityId, string packageFullName) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Management.Deployment.DeploymentResult, global::Windows.Management.Deployment.DeploymentProgress> RemovePackageAsync( string packageFullName,  global::Windows.Management.Deployment.RemovalOptions removalOptions)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> PackageManager.RemovePackageAsync(string packageFullName, RemovalOptions removalOptions) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Management.Deployment.DeploymentResult, global::Windows.Management.Deployment.DeploymentProgress> StagePackageAsync( global::System.Uri packageUri,  global::System.Collections.Generic.IEnumerable<global::System.Uri> dependencyPackageUris,  global::Windows.Management.Deployment.DeploymentOptions deploymentOptions)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> PackageManager.StagePackageAsync(Uri packageUri, IEnumerable<Uri> dependencyPackageUris, DeploymentOptions deploymentOptions) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Management.Deployment.DeploymentResult, global::Windows.Management.Deployment.DeploymentProgress> RegisterPackageByFullNameAsync( string mainPackageFullName,  global::System.Collections.Generic.IEnumerable<string> dependencyPackageFullNames,  global::Windows.Management.Deployment.DeploymentOptions deploymentOptions)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> PackageManager.RegisterPackageByFullNameAsync(string mainPackageFullName, IEnumerable<string> dependencyPackageFullNames, DeploymentOptions deploymentOptions) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IEnumerable<global::Windows.ApplicationModel.Package> FindPackagesWithPackageTypes( global::Windows.Management.Deployment.PackageTypes packageTypes)
		{
			throw new global::System.NotImplementedException("The member IEnumerable<Package> PackageManager.FindPackagesWithPackageTypes(PackageTypes packageTypes) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IEnumerable<global::Windows.ApplicationModel.Package> FindPackagesForUserWithPackageTypes( string userSecurityId,  global::Windows.Management.Deployment.PackageTypes packageTypes)
		{
			throw new global::System.NotImplementedException("The member IEnumerable<Package> PackageManager.FindPackagesForUserWithPackageTypes(string userSecurityId, PackageTypes packageTypes) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IEnumerable<global::Windows.ApplicationModel.Package> FindPackagesWithPackageTypes( string packageName,  string packagePublisher,  global::Windows.Management.Deployment.PackageTypes packageTypes)
		{
			throw new global::System.NotImplementedException("The member IEnumerable<Package> PackageManager.FindPackagesWithPackageTypes(string packageName, string packagePublisher, PackageTypes packageTypes) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IEnumerable<global::Windows.ApplicationModel.Package> FindPackagesForUserWithPackageTypes( string userSecurityId,  string packageName,  string packagePublisher,  global::Windows.Management.Deployment.PackageTypes packageTypes)
		{
			throw new global::System.NotImplementedException("The member IEnumerable<Package> PackageManager.FindPackagesForUserWithPackageTypes(string userSecurityId, string packageName, string packagePublisher, PackageTypes packageTypes) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IEnumerable<global::Windows.ApplicationModel.Package> FindPackagesWithPackageTypes( string packageFamilyName,  global::Windows.Management.Deployment.PackageTypes packageTypes)
		{
			throw new global::System.NotImplementedException("The member IEnumerable<Package> PackageManager.FindPackagesWithPackageTypes(string packageFamilyName, PackageTypes packageTypes) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IEnumerable<global::Windows.ApplicationModel.Package> FindPackagesForUserWithPackageTypes( string userSecurityId,  string packageFamilyName,  global::Windows.Management.Deployment.PackageTypes packageTypes)
		{
			throw new global::System.NotImplementedException("The member IEnumerable<Package> PackageManager.FindPackagesForUserWithPackageTypes(string userSecurityId, string packageFamilyName, PackageTypes packageTypes) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Management.Deployment.DeploymentResult, global::Windows.Management.Deployment.DeploymentProgress> StageUserDataAsync( string packageFullName)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> PackageManager.StageUserDataAsync(string packageFullName) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Management.Deployment.PackageVolume> AddPackageVolumeAsync( string packageStorePath)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<PackageVolume> PackageManager.AddPackageVolumeAsync(string packageStorePath) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Management.Deployment.DeploymentResult, global::Windows.Management.Deployment.DeploymentProgress> AddPackageAsync( global::System.Uri packageUri,  global::System.Collections.Generic.IEnumerable<global::System.Uri> dependencyPackageUris,  global::Windows.Management.Deployment.DeploymentOptions deploymentOptions,  global::Windows.Management.Deployment.PackageVolume targetVolume)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> PackageManager.AddPackageAsync(Uri packageUri, IEnumerable<Uri> dependencyPackageUris, DeploymentOptions deploymentOptions, PackageVolume targetVolume) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void ClearPackageStatus( string packageFullName,  global::Windows.Management.Deployment.PackageStatus status)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Management.Deployment.PackageManager", "void PackageManager.ClearPackageStatus(string packageFullName, PackageStatus status)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Management.Deployment.DeploymentResult, global::Windows.Management.Deployment.DeploymentProgress> RegisterPackageAsync( global::System.Uri manifestUri,  global::System.Collections.Generic.IEnumerable<global::System.Uri> dependencyPackageUris,  global::Windows.Management.Deployment.DeploymentOptions deploymentOptions,  global::Windows.Management.Deployment.PackageVolume appDataVolume)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> PackageManager.RegisterPackageAsync(Uri manifestUri, IEnumerable<Uri> dependencyPackageUris, DeploymentOptions deploymentOptions, PackageVolume appDataVolume) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Management.Deployment.PackageVolume FindPackageVolume( string volumeName)
		{
			throw new global::System.NotImplementedException("The member PackageVolume PackageManager.FindPackageVolume(string volumeName) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IEnumerable<global::Windows.Management.Deployment.PackageVolume> FindPackageVolumes()
		{
			throw new global::System.NotImplementedException("The member IEnumerable<PackageVolume> PackageManager.FindPackageVolumes() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Management.Deployment.PackageVolume GetDefaultPackageVolume()
		{
			throw new global::System.NotImplementedException("The member PackageVolume PackageManager.GetDefaultPackageVolume() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Management.Deployment.DeploymentResult, global::Windows.Management.Deployment.DeploymentProgress> MovePackageToVolumeAsync( string packageFullName,  global::Windows.Management.Deployment.DeploymentOptions deploymentOptions,  global::Windows.Management.Deployment.PackageVolume targetVolume)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> PackageManager.MovePackageToVolumeAsync(string packageFullName, DeploymentOptions deploymentOptions, PackageVolume targetVolume) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Management.Deployment.DeploymentResult, global::Windows.Management.Deployment.DeploymentProgress> RemovePackageVolumeAsync( global::Windows.Management.Deployment.PackageVolume volume)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> PackageManager.RemovePackageVolumeAsync(PackageVolume volume) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetDefaultPackageVolume( global::Windows.Management.Deployment.PackageVolume volume)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Management.Deployment.PackageManager", "void PackageManager.SetDefaultPackageVolume(PackageVolume volume)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetPackageStatus( string packageFullName,  global::Windows.Management.Deployment.PackageStatus status)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Management.Deployment.PackageManager", "void PackageManager.SetPackageStatus(string packageFullName, PackageStatus status)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Management.Deployment.DeploymentResult, global::Windows.Management.Deployment.DeploymentProgress> SetPackageVolumeOfflineAsync( global::Windows.Management.Deployment.PackageVolume packageVolume)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> PackageManager.SetPackageVolumeOfflineAsync(PackageVolume packageVolume) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Management.Deployment.DeploymentResult, global::Windows.Management.Deployment.DeploymentProgress> SetPackageVolumeOnlineAsync( global::Windows.Management.Deployment.PackageVolume packageVolume)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> PackageManager.SetPackageVolumeOnlineAsync(PackageVolume packageVolume) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Management.Deployment.DeploymentResult, global::Windows.Management.Deployment.DeploymentProgress> StagePackageAsync( global::System.Uri packageUri,  global::System.Collections.Generic.IEnumerable<global::System.Uri> dependencyPackageUris,  global::Windows.Management.Deployment.DeploymentOptions deploymentOptions,  global::Windows.Management.Deployment.PackageVolume targetVolume)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> PackageManager.StagePackageAsync(Uri packageUri, IEnumerable<Uri> dependencyPackageUris, DeploymentOptions deploymentOptions, PackageVolume targetVolume) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Management.Deployment.DeploymentResult, global::Windows.Management.Deployment.DeploymentProgress> StageUserDataAsync( string packageFullName,  global::Windows.Management.Deployment.DeploymentOptions deploymentOptions)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> PackageManager.StageUserDataAsync(string packageFullName, DeploymentOptions deploymentOptions) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Management.Deployment.PackageVolume>> GetPackageVolumesAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<PackageVolume>> PackageManager.GetPackageVolumesAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Management.Deployment.DeploymentResult, global::Windows.Management.Deployment.DeploymentProgress> AddPackageAsync( global::System.Uri packageUri,  global::System.Collections.Generic.IEnumerable<global::System.Uri> dependencyPackageUris,  global::Windows.Management.Deployment.DeploymentOptions deploymentOptions,  global::Windows.Management.Deployment.PackageVolume targetVolume,  global::System.Collections.Generic.IEnumerable<string> optionalPackageFamilyNames,  global::System.Collections.Generic.IEnumerable<global::System.Uri> externalPackageUris)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> PackageManager.AddPackageAsync(Uri packageUri, IEnumerable<Uri> dependencyPackageUris, DeploymentOptions deploymentOptions, PackageVolume targetVolume, IEnumerable<string> optionalPackageFamilyNames, IEnumerable<Uri> externalPackageUris) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Management.Deployment.DeploymentResult, global::Windows.Management.Deployment.DeploymentProgress> StagePackageAsync( global::System.Uri packageUri,  global::System.Collections.Generic.IEnumerable<global::System.Uri> dependencyPackageUris,  global::Windows.Management.Deployment.DeploymentOptions deploymentOptions,  global::Windows.Management.Deployment.PackageVolume targetVolume,  global::System.Collections.Generic.IEnumerable<string> optionalPackageFamilyNames,  global::System.Collections.Generic.IEnumerable<global::System.Uri> externalPackageUris)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> PackageManager.StagePackageAsync(Uri packageUri, IEnumerable<Uri> dependencyPackageUris, DeploymentOptions deploymentOptions, PackageVolume targetVolume, IEnumerable<string> optionalPackageFamilyNames, IEnumerable<Uri> externalPackageUris) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Management.Deployment.DeploymentResult, global::Windows.Management.Deployment.DeploymentProgress> RegisterPackageByFamilyNameAsync( string mainPackageFamilyName,  global::System.Collections.Generic.IEnumerable<string> dependencyPackageFamilyNames,  global::Windows.Management.Deployment.DeploymentOptions deploymentOptions,  global::Windows.Management.Deployment.PackageVolume appDataVolume,  global::System.Collections.Generic.IEnumerable<string> optionalPackageFamilyNames)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> PackageManager.RegisterPackageByFamilyNameAsync(string mainPackageFamilyName, IEnumerable<string> dependencyPackageFamilyNames, DeploymentOptions deploymentOptions, PackageVolume appDataVolume, IEnumerable<string> optionalPackageFamilyNames) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Management.Deployment.PackageManager.DebugSettings.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Management.Deployment.DeploymentResult, global::Windows.Management.Deployment.DeploymentProgress> ProvisionPackageForAllUsersAsync( string packageFamilyName)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> PackageManager.ProvisionPackageForAllUsersAsync(string packageFamilyName) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Management.Deployment.DeploymentResult, global::Windows.Management.Deployment.DeploymentProgress> AddPackageByAppInstallerFileAsync( global::System.Uri appInstallerFileUri,  global::Windows.Management.Deployment.AddPackageByAppInstallerOptions options,  global::Windows.Management.Deployment.PackageVolume targetVolume)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> PackageManager.AddPackageByAppInstallerFileAsync(Uri appInstallerFileUri, AddPackageByAppInstallerOptions options, PackageVolume targetVolume) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Management.Deployment.DeploymentResult, global::Windows.Management.Deployment.DeploymentProgress> RequestAddPackageByAppInstallerFileAsync( global::System.Uri appInstallerFileUri,  global::Windows.Management.Deployment.AddPackageByAppInstallerOptions options,  global::Windows.Management.Deployment.PackageVolume targetVolume)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> PackageManager.RequestAddPackageByAppInstallerFileAsync(Uri appInstallerFileUri, AddPackageByAppInstallerOptions options, PackageVolume targetVolume) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Management.Deployment.DeploymentResult, global::Windows.Management.Deployment.DeploymentProgress> AddPackageAsync( global::System.Uri packageUri,  global::System.Collections.Generic.IEnumerable<global::System.Uri> dependencyPackageUris,  global::Windows.Management.Deployment.DeploymentOptions options,  global::Windows.Management.Deployment.PackageVolume targetVolume,  global::System.Collections.Generic.IEnumerable<string> optionalPackageFamilyNames,  global::System.Collections.Generic.IEnumerable<global::System.Uri> packageUrisToInstall,  global::System.Collections.Generic.IEnumerable<global::System.Uri> relatedPackageUris)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> PackageManager.AddPackageAsync(Uri packageUri, IEnumerable<Uri> dependencyPackageUris, DeploymentOptions options, PackageVolume targetVolume, IEnumerable<string> optionalPackageFamilyNames, IEnumerable<Uri> packageUrisToInstall, IEnumerable<Uri> relatedPackageUris) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Management.Deployment.DeploymentResult, global::Windows.Management.Deployment.DeploymentProgress> StagePackageAsync( global::System.Uri packageUri,  global::System.Collections.Generic.IEnumerable<global::System.Uri> dependencyPackageUris,  global::Windows.Management.Deployment.DeploymentOptions options,  global::Windows.Management.Deployment.PackageVolume targetVolume,  global::System.Collections.Generic.IEnumerable<string> optionalPackageFamilyNames,  global::System.Collections.Generic.IEnumerable<global::System.Uri> packageUrisToInstall,  global::System.Collections.Generic.IEnumerable<global::System.Uri> relatedPackageUris)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> PackageManager.StagePackageAsync(Uri packageUri, IEnumerable<Uri> dependencyPackageUris, DeploymentOptions options, PackageVolume targetVolume, IEnumerable<string> optionalPackageFamilyNames, IEnumerable<Uri> packageUrisToInstall, IEnumerable<Uri> relatedPackageUris) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Management.Deployment.DeploymentResult, global::Windows.Management.Deployment.DeploymentProgress> RequestAddPackageAsync( global::System.Uri packageUri,  global::System.Collections.Generic.IEnumerable<global::System.Uri> dependencyPackageUris,  global::Windows.Management.Deployment.DeploymentOptions deploymentOptions,  global::Windows.Management.Deployment.PackageVolume targetVolume,  global::System.Collections.Generic.IEnumerable<string> optionalPackageFamilyNames,  global::System.Collections.Generic.IEnumerable<global::System.Uri> relatedPackageUris)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> PackageManager.RequestAddPackageAsync(Uri packageUri, IEnumerable<Uri> dependencyPackageUris, DeploymentOptions deploymentOptions, PackageVolume targetVolume, IEnumerable<string> optionalPackageFamilyNames, IEnumerable<Uri> relatedPackageUris) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Management.Deployment.DeploymentResult, global::Windows.Management.Deployment.DeploymentProgress> RequestAddPackageAsync( global::System.Uri packageUri,  global::System.Collections.Generic.IEnumerable<global::System.Uri> dependencyPackageUris,  global::Windows.Management.Deployment.DeploymentOptions deploymentOptions,  global::Windows.Management.Deployment.PackageVolume targetVolume,  global::System.Collections.Generic.IEnumerable<string> optionalPackageFamilyNames,  global::System.Collections.Generic.IEnumerable<global::System.Uri> relatedPackageUris,  global::System.Collections.Generic.IEnumerable<global::System.Uri> packageUrisToInstall)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> PackageManager.RequestAddPackageAsync(Uri packageUri, IEnumerable<Uri> dependencyPackageUris, DeploymentOptions deploymentOptions, PackageVolume targetVolume, IEnumerable<string> optionalPackageFamilyNames, IEnumerable<Uri> relatedPackageUris, IEnumerable<Uri> packageUrisToInstall) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Management.Deployment.DeploymentResult, global::Windows.Management.Deployment.DeploymentProgress> DeprovisionPackageForAllUsersAsync( string packageFamilyName)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> PackageManager.DeprovisionPackageForAllUsersAsync(string packageFamilyName) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<global::Windows.ApplicationModel.Package> FindProvisionedPackages()
		{
			throw new global::System.NotImplementedException("The member IList<Package> PackageManager.FindProvisionedPackages() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Management.Deployment.DeploymentResult, global::Windows.Management.Deployment.DeploymentProgress> AddPackageByUriAsync( global::System.Uri packageUri,  global::Windows.Management.Deployment.AddPackageOptions options)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> PackageManager.AddPackageByUriAsync(Uri packageUri, AddPackageOptions options) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Management.Deployment.DeploymentResult, global::Windows.Management.Deployment.DeploymentProgress> StagePackageByUriAsync( global::System.Uri packageUri,  global::Windows.Management.Deployment.StagePackageOptions options)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> PackageManager.StagePackageByUriAsync(Uri packageUri, StagePackageOptions options) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Management.Deployment.DeploymentResult, global::Windows.Management.Deployment.DeploymentProgress> RegisterPackageByUriAsync( global::System.Uri manifestUri,  global::Windows.Management.Deployment.RegisterPackageOptions options)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> PackageManager.RegisterPackageByUriAsync(Uri manifestUri, RegisterPackageOptions options) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Management.Deployment.DeploymentResult, global::Windows.Management.Deployment.DeploymentProgress> RegisterPackagesByFullNameAsync( global::System.Collections.Generic.IEnumerable<string> packageFullNames,  global::Windows.Management.Deployment.RegisterPackageOptions options)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> PackageManager.RegisterPackagesByFullNameAsync(IEnumerable<string> packageFullNames, RegisterPackageOptions options) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetPackageStubPreference( string packageFamilyName,  global::Windows.Management.Deployment.PackageStubPreference useStub)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Management.Deployment.PackageManager", "void PackageManager.SetPackageStubPreference(string packageFamilyName, PackageStubPreference useStub)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Management.Deployment.PackageStubPreference GetPackageStubPreference( string packageFamilyName)
		{
			throw new global::System.NotImplementedException("The member PackageStubPreference PackageManager.GetPackageStubPreference(string packageFamilyName) is not implemented in Uno.");
		}
		#endif
	}
}
