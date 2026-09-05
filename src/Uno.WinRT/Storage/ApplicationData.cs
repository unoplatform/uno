#nullable enable

#if !IS_UNIT_TESTS
#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
#pragma warning disable 67 // new keyword hiding
using System;
using Uno;

namespace Windows.Storage;

/// <summary>
/// Provides access to the application data store. Application data consists of files 
/// and settings that are either local, roaming, or temporary.
/// </summary>
public sealed partial class ApplicationData
{
	private readonly Lazy<StorageFolder> _localFolderLazy;
	private readonly Lazy<StorageFolder> _roamingFolderLazy;
	private readonly Lazy<StorageFolder?> _sharedLocalFolderLazy;
	private readonly Lazy<StorageFolder> _localCacheFolderLazy;
	private readonly Lazy<StorageFolder> _temporaryFolderLazy;
	private readonly Lazy<ApplicationDataContainer> _localSettingsLazy;
	private readonly Lazy<ApplicationDataContainer> _roamingSettingsLazy;

	private ApplicationData()
	{
		_localFolderLazy = new(() => CreateStorageFolder(GetLocalFolder()));
		_roamingFolderLazy = new(() => CreateStorageFolder(GetRoamingFolder()));
#if !__SKIA__ // The concept of Shared Local folder is not implemented for Skia.
		_sharedLocalFolderLazy = new(() => CreateStorageFolder(".shared", GetSharedLocalFolder()));
#else
		_sharedLocalFolderLazy = new((StorageFolder?)null);
#endif
		_localCacheFolderLazy = new(() => CreateStorageFolder(GetLocalCacheFolder()));
		_temporaryFolderLazy = new(() => CreateStorageFolder(GetTemporaryFolder()));

		_localSettingsLazy = new(() => new ApplicationDataContainer(this, "Local", ApplicationDataLocality.Local));
		_roamingSettingsLazy = new(() => new ApplicationDataContainer(this, "Roaming", ApplicationDataLocality.Roaming));

		InitializePartial();
	}

	partial void InitializePartial();

	/// <summary>
	/// Provides access to the app data store associated with the app's app package.
	/// </summary>
	public static ApplicationData Current { get; } = new();

	/// <summary>
	/// Gets the root folder in the local app data store. This folder is backed up to the cloud.
	/// </summary>
	public StorageFolder LocalFolder => _localFolderLazy.Value;

	/// <summary>
	/// Gets the root folder in the roaming app data store.
	/// </summary>
	public StorageFolder RoamingFolder => _roamingFolderLazy.Value;

	/// <summary>
	/// Gets the root folder in the shared app data store.
	/// </summary>
	public StorageFolder? SharedLocalFolder => _sharedLocalFolderLazy.Value;

	/// <summary>
	/// Gets the folder in the local app data store where you can save files that are not included in backup and restore.
	/// </summary>
	public StorageFolder LocalCacheFolder => _localCacheFolderLazy.Value;

	/// <summary>
	/// Gets the root folder in the temporary app data store.
	/// </summary>
	public StorageFolder TemporaryFolder => _temporaryFolderLazy.Value;

	/// <summary>
	/// Gets the application settings container in the local app data store.
	/// </summary>
	public ApplicationDataContainer LocalSettings => _localSettingsLazy.Value;

	/// <summary>
	/// Gets the root folder in the roaming app data store.
	/// </summary>
	public ApplicationDataContainer RoamingSettings => _roamingSettingsLazy.Value;

	[Uno.NotImplemented]
	public ulong RoamingStorageQuota => 0;

	[Uno.NotImplemented]
	public uint Version => 0;

	[Uno.NotImplemented]
	public void SignalDataChanged()
	{
		Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.ApplicationData", "void ApplicationData.SignalDataChanged()");
	}

	[Uno.NotImplemented]
	public StorageFolder GetPublisherCacheFolder(string folderName)
	{
		throw new NotImplementedException("The member StorageFolder ApplicationData.GetPublisherCacheFolder(string folderName) is not implemented in Uno.");
	}

	[Uno.NotImplemented]
	public Foundation.IAsyncAction ClearPublisherCacheFolderAsync(string folderName)
	{
		throw new NotImplementedException("The member IAsyncAction ApplicationData.ClearPublisherCacheFolderAsync(string folderName) is not implemented in Uno.");
	}

	[Uno.NotImplemented]
	public static Foundation.IAsyncOperation<ApplicationData> GetForUserAsync(System.User user)
	{
		throw new NotImplementedException("The member IAsyncOperation<ApplicationData> ApplicationData.GetForUserAsync(User user) is not implemented in Uno.");
	}

	[Uno.NotImplemented]
	public event Foundation.TypedEventHandler<ApplicationData, object>? DataChanged;

	private static StorageFolder CreateStorageFolder(string folder)
	{
		try
		{
			return new StorageFolder(folder);
		}
		catch (Exception e)
		{
			throw new InvalidOperationException(
				$"The creation of the StorageFolder \'{folder}\' failed. It may have been initialized too early, see for more information: https://aka.platform.uno/application-data",
				e);
		}
	}

#if !__SKIA__
	private static StorageFolder CreateStorageFolder(string name, string folder)
	{
		try
		{
			return new StorageFolder(name, folder);
		}
		catch (Exception e)
		{
			throw new InvalidOperationException(
				$"The creation of the StorageFolder \'{folder}\' failed. It may have been initialized too early, see for more information: https://aka.platform.uno/application-data",
				e);
		}
	}
#endif
}
#endif
