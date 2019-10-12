using System;
using System.Collections.Generic;
using Windows.Storage;

namespace Windows.ApplicationModel
{
	public partial class Package
	{
		private static readonly Package _instance = new Package();

		public bool IsDevelopmentMode => GetInnerIsDevelopmentMode();

		public global::Windows.ApplicationModel.PackageId Id => new PackageId();

		public global::System.DateTimeOffset InstallDate => GetInstallDate();

		public global::System.DateTimeOffset InstalledDate => GetInstallDate();

		public static global::Windows.ApplicationModel.Package Current => _instance;

		[global::Uno.NotImplemented]
		public global::System.Collections.Generic.IReadOnlyList<global::Windows.ApplicationModel.Package> Dependencies => new List<Package>();

		public global::Windows.Storage.StorageFolder InstalledLocation
			=> new StorageFolder(GetInstalledLocation());

		[global::Uno.NotImplemented]
		public bool IsFramework => false;

		[global::Uno.NotImplemented]
		public string Description => "";

#if !__ANDROID__ && !__IOS__
		[global::Uno.NotImplemented]
		public string DisplayName => "";
#endif

		[global::Uno.NotImplemented]
		public bool IsBundle => false;

		[global::Uno.NotImplemented]
		public bool IsResourcePackage => false;

		[global::Uno.NotImplemented]
		public global::System.Uri Logo => new Uri("http://example.com");

		[global::Uno.NotImplemented]
		public string PublisherDisplayName => "";

		[global::Uno.NotImplemented]
		public global::Windows.ApplicationModel.PackageStatus Status => new PackageStatus();

		[global::Uno.NotImplemented]
		public bool IsOptional => false;

		[global::Uno.NotImplemented]
		public global::Windows.ApplicationModel.PackageSignatureKind SignatureKind => PackageSignatureKind.None;

		[global::Uno.NotImplemented]
		public global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.ApplicationModel.Core.AppListEntry>> GetAppListEntriesAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<AppListEntry>> Package.GetAppListEntriesAsync() is not implemented in Uno.");
		}

		[global::Uno.NotImplemented]
		public string GetThumbnailToken()
		{
			throw new global::System.NotImplementedException("The member string Package.GetThumbnailToken() is not implemented in Uno.");
		}

		[global::Uno.NotImplemented]
		public void Launch(string parameters)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Package", "void Package.Launch(string parameters)");
		}

		[global::Uno.NotImplemented]
		public global::Windows.Foundation.IAsyncOperation<bool> VerifyContentIntegrityAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> Package.VerifyContentIntegrityAsync() is not implemented in Uno.");
		}
	}
}
