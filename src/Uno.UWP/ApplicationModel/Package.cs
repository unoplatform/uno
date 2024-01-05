using System;
using System.Collections.Generic;
using Windows.Storage;

namespace Windows.ApplicationModel
{
	public partial class Package
	{
		private StorageFolder? _installedLocation;

		internal Package() => InitializePlatform();

		partial void InitializePlatform();

		public bool IsDevelopmentMode => GetInnerIsDevelopmentMode();

		public PackageId Id { get; } = new();

		public DateTimeOffset InstallDate => GetInstallDate();

		public DateTimeOffset InstalledDate => GetInstallDate();

		public static Package Current { get; } = new Package();

		[Uno.NotImplemented]
		public IReadOnlyList<Package> Dependencies => new List<Package>();

		/// <summary>
		/// Gets the current package's location in the original install folder for the current package.
		/// </summary>
#if __WASM__
		[Uno.NotImplemented("__WASM__")]
#endif
		public StorageFolder InstalledLocation => _installedLocation ??= new StorageFolder(GetInstalledPath());

		/// <summary>
		/// Gets the current package's path in the original install folder for the current package.
		/// </summary>
#if __WASM__
		[Uno.NotImplemented("__WASM__")]
#endif
		public string InstalledPath => GetInstalledPath();

		[Uno.NotImplemented]
		public bool IsFramework => false;

#if !__SKIA__
		[Uno.NotImplemented]
		public string Description => "";
#endif

		[Uno.NotImplemented]
		public bool IsBundle => false;

		[Uno.NotImplemented]
		public bool IsResourcePackage => false;

#if (__IOS__ || __ANDROID__ || __MACOS__)
		[global::Uno.NotImplemented]
		public global::System.Uri Logo => default!;
#endif

#if !__SKIA__
		[Uno.NotImplemented]
		public string PublisherDisplayName => "";
#endif

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
