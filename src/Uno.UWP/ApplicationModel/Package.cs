using System;
using System.Collections.Generic;
using Windows.Storage;

namespace Windows.ApplicationModel
{
	public partial class Package
	{
		internal Package()
		{
		}

		public bool IsDevelopmentMode => GetInnerIsDevelopmentMode();

		public PackageId Id => new PackageId();

		public DateTimeOffset InstallDate => GetInstallDate();

		public DateTimeOffset InstalledDate => GetInstallDate();

		public static Package Current { get; } = new Package();

		[Uno.NotImplemented]
		public IReadOnlyList<Package> Dependencies => new List<Package>();

		public StorageFolder InstalledLocation
			=> new StorageFolder(GetInstalledLocation());

		[Uno.NotImplemented]
		public bool IsFramework => false;

		[Uno.NotImplemented]
		public string Description => "";

		[Uno.NotImplemented]
		public bool IsBundle => false;

		[Uno.NotImplemented]
		public bool IsResourcePackage => false;


#if (__IOS__ || __ANDROID__ || __MACOS__)
		[global::Uno.NotImplemented]
		public global::System.Uri Logo => new Uri("http://example.com");
#endif

		[Uno.NotImplemented]
		public string PublisherDisplayName => "";

		[Uno.NotImplemented]
		public PackageStatus Status => new PackageStatus();

		[Uno.NotImplemented]
		public bool IsOptional => false;

		[Uno.NotImplemented]
		public PackageSignatureKind SignatureKind => PackageSignatureKind.None;

		[Uno.NotImplemented]
		public Foundation.IAsyncOperation<IReadOnlyList<Core.AppListEntry>> GetAppListEntriesAsync()
		{
			throw new NotImplementedException("The member IAsyncOperation<IReadOnlyList<AppListEntry>> Package.GetAppListEntriesAsync() is not implemented in Uno.");
		}

		[Uno.NotImplemented]
		public string GetThumbnailToken()
		{
			throw new NotImplementedException("The member string Package.GetThumbnailToken() is not implemented in Uno.");
		}

		[Uno.NotImplemented]
		public void Launch(string parameters)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Package", "void Package.Launch(string parameters)");
		}

		[Uno.NotImplemented]
		public Foundation.IAsyncOperation<bool> VerifyContentIntegrityAsync()
		{
			throw new NotImplementedException("The member IAsyncOperation<bool> Package.VerifyContentIntegrityAsync() is not implemented in Uno.");
		}
	}
}
