#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel
{
	#if false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class Package 
	{
		// Skipping already declared property Dependencies
		// Skipping already declared property Id
		// Skipping already declared property InstalledLocation
		// Skipping already declared property IsFramework
		// Skipping already declared property Description
		// Skipping already declared property DisplayName
		// Skipping already declared property IsBundle
		// Skipping already declared property IsDevelopmentMode
		// Skipping already declared property IsResourcePackage
		// Skipping already declared property Logo
		// Skipping already declared property PublisherDisplayName
		// Skipping already declared property InstalledDate
		// Skipping already declared property Status
		// Skipping already declared property IsOptional
		// Skipping already declared property SignatureKind
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Storage.StorageFolder EffectiveLocation
		{
			get
			{
				throw new global::System.NotImplementedException("The member StorageFolder Package.EffectiveLocation is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Storage.StorageFolder MutableLocation
		{
			get
			{
				throw new global::System.NotImplementedException("The member StorageFolder Package.MutableLocation is not implemented in Uno.");
			}
		}
		#endif
		// Skipping already declared property InstallDate
		// Skipping already declared property Current
		// Forced skipping of method Windows.ApplicationModel.Package.Id.get
		// Forced skipping of method Windows.ApplicationModel.Package.InstalledLocation.get
		// Forced skipping of method Windows.ApplicationModel.Package.IsFramework.get
		// Forced skipping of method Windows.ApplicationModel.Package.Dependencies.get
		// Forced skipping of method Windows.ApplicationModel.Package.DisplayName.get
		// Forced skipping of method Windows.ApplicationModel.Package.PublisherDisplayName.get
		// Forced skipping of method Windows.ApplicationModel.Package.Description.get
		// Forced skipping of method Windows.ApplicationModel.Package.Logo.get
		// Forced skipping of method Windows.ApplicationModel.Package.IsResourcePackage.get
		// Forced skipping of method Windows.ApplicationModel.Package.IsBundle.get
		// Forced skipping of method Windows.ApplicationModel.Package.IsDevelopmentMode.get
		// Forced skipping of method Windows.ApplicationModel.Package.Status.get
		// Forced skipping of method Windows.ApplicationModel.Package.InstalledDate.get
		// Skipping already declared method Windows.ApplicationModel.Package.GetAppListEntriesAsync()
		// Forced skipping of method Windows.ApplicationModel.Package.InstallDate.get
		// Skipping already declared method Windows.ApplicationModel.Package.GetThumbnailToken()
		// Skipping already declared method Windows.ApplicationModel.Package.Launch(string)
		// Forced skipping of method Windows.ApplicationModel.Package.SignatureKind.get
		// Forced skipping of method Windows.ApplicationModel.Package.IsOptional.get
		// Skipping already declared method Windows.ApplicationModel.Package.VerifyContentIntegrityAsync()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IList<global::Windows.ApplicationModel.PackageContentGroup>> GetContentGroupsAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IList<PackageContentGroup>> Package.GetContentGroupsAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.PackageContentGroup> GetContentGroupAsync( string name)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<PackageContentGroup> Package.GetContentGroupAsync(string name) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IList<global::Windows.ApplicationModel.PackageContentGroup>> StageContentGroupsAsync( global::System.Collections.Generic.IEnumerable<string> names)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IList<PackageContentGroup>> Package.StageContentGroupsAsync(IEnumerable<string> names) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IList<global::Windows.ApplicationModel.PackageContentGroup>> StageContentGroupsAsync( global::System.Collections.Generic.IEnumerable<string> names,  bool moveToHeadOfQueue)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IList<PackageContentGroup>> Package.StageContentGroupsAsync(IEnumerable<string> names, bool moveToHeadOfQueue) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<bool> SetInUseAsync( bool inUse)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> Package.SetInUseAsync(bool inUse) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.ApplicationModel.AppInstallerInfo GetAppInstallerInfo()
		{
			throw new global::System.NotImplementedException("The member AppInstallerInfo Package.GetAppInstallerInfo() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.PackageUpdateAvailabilityResult> CheckUpdateAvailabilityAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<PackageUpdateAvailabilityResult> Package.CheckUpdateAvailabilityAsync() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Package.MutableLocation.get
		// Forced skipping of method Windows.ApplicationModel.Package.EffectiveLocation.get
		// Forced skipping of method Windows.ApplicationModel.Package.Current.get
	}
}
