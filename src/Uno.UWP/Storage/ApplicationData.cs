#if !IS_UNIT_TESTS
#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
#pragma warning disable 67 // new keyword hiding
using System;

namespace Windows.Storage
{
	public sealed partial class ApplicationData
	{
		public static ApplicationData Current { get; } = new ApplicationData();

		private ApplicationData()
		{
			PartialCtor();

			LocalFolder = new StorageFolder(GetLocalFolder());
			RoamingFolder = new StorageFolder(GetRoamingFolder());
			SharedLocalFolder = new StorageFolder(".shared", GetSharedLocalFolder());
			LocalCacheFolder = new StorageFolder(GetLocalCacheFolder());
			TemporaryFolder = new StorageFolder(GetTemporaryFolder());

			LocalSettings = new ApplicationDataContainer(this, "Local", ApplicationDataLocality.Local);
			RoamingSettings = new ApplicationDataContainer(this, "Roaming", ApplicationDataLocality.Roaming);
		}

		partial void PartialCtor();

		public StorageFolder LocalFolder { get; }

		public StorageFolder RoamingFolder { get; }

		public StorageFolder SharedLocalFolder { get; }

		public StorageFolder LocalCacheFolder { get; }

		public StorageFolder TemporaryFolder { get; }

		public ApplicationDataContainer LocalSettings { get; }

		public ApplicationDataContainer RoamingSettings { get; }

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
		public event Foundation.TypedEventHandler<ApplicationData, object> DataChanged;
	}
}
#endif
