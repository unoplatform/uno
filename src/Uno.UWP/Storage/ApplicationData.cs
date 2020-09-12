#if !NET461
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
			LocalSettings = new ApplicationDataContainer(this, "Local", ApplicationDataLocality.Local);
			RoamingSettings = new ApplicationDataContainer(this, "Roaming", ApplicationDataLocality.Roaming);

			PartialCtor();
		}

		partial void PartialCtor();

		public StorageFolder LocalFolder { get; } = new StorageFolder(GetLocalFolder());

		public StorageFolder RoamingFolder { get; } = new StorageFolder(GetRoamingFolder());

		public StorageFolder SharedLocalFolder { get; } = new StorageFolder(".shared", GetSharedLocalFolder());

		public StorageFolder LocalCacheFolder { get; } = new StorageFolder(GetLocalCacheFolder());

		public StorageFolder TemporaryFolder { get; } = new StorageFolder(GetTemporaryFolder());

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
		public  StorageFolder GetPublisherCacheFolder( string folderName)
		{
			throw new NotImplementedException("The member StorageFolder ApplicationData.GetPublisherCacheFolder(string folderName) is not implemented in Uno.");
		}

		[Uno.NotImplemented]
		public Foundation.IAsyncAction ClearPublisherCacheFolderAsync( string folderName)
		{
			throw new NotImplementedException("The member IAsyncAction ApplicationData.ClearPublisherCacheFolderAsync(string folderName) is not implemented in Uno.");
		}

		[Uno.NotImplemented]
		public static Foundation.IAsyncOperation<ApplicationData> GetForUserAsync( System.User user)
		{
			throw new NotImplementedException("The member IAsyncOperation<ApplicationData> ApplicationData.GetForUserAsync(User user) is not implemented in Uno.");
		}

		[Uno.NotImplemented]
		public event Foundation.TypedEventHandler<ApplicationData, object> DataChanged;
	}
}
#endif
