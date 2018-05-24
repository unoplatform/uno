#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class Package 
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.ApplicationModel.Package> Dependencies
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<Package> Package.Dependencies is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.ApplicationModel.PackageId Id
		{
			get
			{
				throw new global::System.NotImplementedException("The member PackageId Package.Id is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.Storage.StorageFolder InstalledLocation
		{
			get
			{
				throw new global::System.NotImplementedException("The member StorageFolder Package.InstalledLocation is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool IsFramework
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool Package.IsFramework is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  string Description
		{
			get
			{
				throw new global::System.NotImplementedException("The member string Package.Description is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  string DisplayName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string Package.DisplayName is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool IsBundle
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool Package.IsBundle is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool IsDevelopmentMode
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool Package.IsDevelopmentMode is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool IsResourcePackage
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool Package.IsResourcePackage is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::System.Uri Logo
		{
			get
			{
				throw new global::System.NotImplementedException("The member Uri Package.Logo is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  string PublisherDisplayName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string Package.PublisherDisplayName is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::System.DateTimeOffset InstalledDate
		{
			get
			{
				throw new global::System.NotImplementedException("The member DateTimeOffset Package.InstalledDate is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.ApplicationModel.PackageStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member PackageStatus Package.Status is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool IsOptional
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool Package.IsOptional is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.ApplicationModel.PackageSignatureKind SignatureKind
		{
			get
			{
				throw new global::System.NotImplementedException("The member PackageSignatureKind Package.SignatureKind is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::System.DateTimeOffset InstallDate
		{
			get
			{
				throw new global::System.NotImplementedException("The member DateTimeOffset Package.InstallDate is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.ApplicationModel.Package Current
		{
			get
			{
				throw new global::System.NotImplementedException("The member Package Package.Current is not implemented in Uno.");
			}
		}
		#endif
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
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.ApplicationModel.Core.AppListEntry>> GetAppListEntriesAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<AppListEntry>> Package.GetAppListEntriesAsync() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Package.InstallDate.get
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  string GetThumbnailToken()
		{
			throw new global::System.NotImplementedException("The member string Package.GetThumbnailToken() is not implemented in Uno.");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  void Launch( string parameters)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Package", "void Package.Launch(string parameters)");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Package.SignatureKind.get
		// Forced skipping of method Windows.ApplicationModel.Package.IsOptional.get
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<bool> VerifyContentIntegrityAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> Package.VerifyContentIntegrityAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IList<global::Windows.ApplicationModel.PackageContentGroup>> GetContentGroupsAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IList<PackageContentGroup>> Package.GetContentGroupsAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.PackageContentGroup> GetContentGroupAsync( string name)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<PackageContentGroup> Package.GetContentGroupAsync(string name) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IList<global::Windows.ApplicationModel.PackageContentGroup>> StageContentGroupsAsync( global::System.Collections.Generic.IEnumerable<string> names)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IList<PackageContentGroup>> Package.StageContentGroupsAsync(IEnumerable<string> names) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IList<global::Windows.ApplicationModel.PackageContentGroup>> StageContentGroupsAsync( global::System.Collections.Generic.IEnumerable<string> names,  bool moveToHeadOfQueue)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IList<PackageContentGroup>> Package.StageContentGroupsAsync(IEnumerable<string> names, bool moveToHeadOfQueue) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<bool> SetInUseAsync( bool inUse)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> Package.SetInUseAsync(bool inUse) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Package.Current.get
	}
}
