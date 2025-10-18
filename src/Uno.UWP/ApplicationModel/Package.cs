using System;
using System.Collections.Generic;
using Uno.Diagnostics.Eventing;
using Windows.Storage;

namespace Windows.ApplicationModel
{
	public partial class Package
	{
		internal Package()
		{
		}

		/// <summary>
		/// Gets the package for the current app.
		/// </summary>
		public static Package Current { get; } = new Package();

		/// <summary>
		/// Gets the packages on which the current package depends.
		/// </summary>
		[Uno.NotImplemented]
		public IReadOnlyList<Package> Dependencies => new List<Package>();

		/// <summary>
		/// Gets the description of the package.
		/// </summary>
		[Uno.NotImplemented]
		public string Description => "";

		/// <summary>
		/// Gets the display name of the package.
		/// </summary>
		public string DisplayName => GetDisplayName();

		/// <summary>
		/// Gets the location of the machine-wide or per-user external folder specified
		/// in the package manifest for the current package, depending on how the app is installed.
		/// </summary>
		public StorageFolder EffectiveExternalLocation => null;

		/// <summary>
		/// Gets the path of the machine-wide or per-user external folder specified
		/// in the package manifest for the current package, depending on how the app is installed.
		/// </summary>
		public string EffectiveExternalPath => "";

		/// <summary>
		/// Gets either the location of the installed folder or the mutable folder
		/// for the installed package, depending on whether the app is declared
		/// to be mutable in its package manifest.
		/// </summary>
		public StorageFolder EffectiveLocation => MutableLocation ?? InstalledLocation;

		/// <summary>
		/// Gets either the path of the installed folder or the mutable folder
		/// for the installed package, depending on whether the app is declared
		/// to be mutable in its package manifest.
		/// </summary>
		public string EffectivePath => EffectiveLocation?.Path ?? "";

		/// <summary>
		/// Gets the package identity of the current package.
		/// </summary>
		public PackageId Id => new PackageId();

		/// <summary>
		/// Same as InstalledDate, gets the date the application package was installed on the user's phone.
		/// </summary>
		public DateTimeOffset InstallDate => InstalledDate;

		/// <summary>
		/// Gets the date on which the application package was installed or last updated.
		/// </summary>
		/// <remarks>
		/// On platforms where this value is not available 
		/// this returns 1st January 2000.
		/// </remarks>
		public DateTimeOffset InstalledDate => GetInstallDate();

		/// <summary>
		/// Gets the current package's location in the original install folder for the current package.
		/// </summary>
		public StorageFolder InstalledLocation => new StorageFolder(GetInstalledLocation());

		/// <summary>
		/// Gets the current package's path in the original install folder for the current package.
		/// </summary>
		public string InstalledPath => InstalledLocation?.Path ?? "";

		/// <summary>
		/// Indicates whether the package is a bundle package.
		/// </summary>
		public bool IsBundle => false;

		/// <summary>
		/// Indicates whether the package is installed in development mode.
		/// </summary>
		public bool IsDevelopmentMode => GetIsDevelopmentMode();

		/// <summary>
		/// Indicates whether other packages can declare a dependency on this package.
		/// </summary>
		public bool IsFramework => false;

		/// <summary>
		/// Indicates whether the package is optional.
		/// </summary>
		public bool IsOptional => false;

		/// <summary>
		/// Indicates whether the package is a resource package.
		/// </summary>
		public bool IsResourcePackage => false;

		/// <summary>
		/// Gets a value that indicates whether the application in the current package is a stub application.
		/// </summary>
		public bool IsStub => false;

		/// <summary>
		/// Gets the logo of the package.
		/// </summary>
#if (__IOS__ || __ANDROID__ || __MACOS__)
		[global::Uno.NotImplemented]
		public global::System.Uri Logo => new Uri("http://example.com");
#else
		public Uri Logo => GetLogo();
#endif

		/// <summary>
		/// Gets the location of the machine-wide external folder specified
		/// in the package manifest for the current package.
		/// </summary>
		public StorageFolder MachineExternalLocation => null;

		/// <summary>
		/// Gets the path of the machine-wide external folder specified
		/// in the package manifest for the current package.
		/// </summary>
		public string MachineExternalPath => MachineExternalLocation?.Path ?? "";

		/// <summary>
		/// Gets the current package's location in the mutable folder for the installed package,
		/// if the app is declared to be mutable in its package manifest.
		/// </summary>
		public StorageFolder MutableLocation => null;

		/// <summary>
		/// Gets the current package's path in the mutable folder for the installed package,
		/// if the app is declared to be mutable in its package manifest.
		/// </summary>
		public string MutablePath => MutableLocation?.Path ?? "";

		/// <summary>
		/// Gets the publisher display name of the package.
		/// </summary>
		[Uno.NotImplemented]
		public string PublisherDisplayName => "";

		/// <summary>
		/// How the app package is signed.
		/// </summary>
		[Uno.NotImplemented]
		public PackageSignatureKind SignatureKind => PackageSignatureKind.None;

		/// <summary>
		/// Get the current status of the package for the user.
		/// </summary>
		[Uno.NotImplemented]
		public PackageStatus Status => new PackageStatus();

		/// <summary>
		/// Gets the location of the per-user external folder specified in the package manifest for the current package.
		/// </summary>
		public StorageFolder UserExternalLocation => null;

		/// <summary>
		/// Gets the path of the per-user external folder specified in the package manifest for the current package.
		/// </summary>
		public string UserExternalPath => UserExternalLocation?.Path ?? "";

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
