#if !NET461
#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
#pragma warning disable 67 // new keyword hiding
using System;

namespace Windows.Storage
{
	public  partial class ApplicationData 
	{
		private readonly static ApplicationData _instance ;

		static ApplicationData()
		{
			_instance = new ApplicationData();
			_instance.Initialize();
		}

		private ApplicationData()
		{
		}

		private void Initialize()
		{
			LocalFolder = new StorageFolder(GetLocalFolder());
			RoamingFolder = new StorageFolder(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
			LocalSettings = new ApplicationDataContainer("Local", ApplicationDataLocality.Local);
			RoamingSettings = new ApplicationDataContainer("Roaming", ApplicationDataLocality.Roaming);
		}

		public global::Windows.Storage.StorageFolder LocalFolder { get; private set; }

		public global::Windows.Storage.ApplicationDataContainer LocalSettings { get; private set; }

		public global::Windows.Storage.StorageFolder RoamingFolder { get; private set; }

		public global::Windows.Storage.ApplicationDataContainer RoamingSettings { get; private set; }

		[Uno.NotImplemented]
		public ulong RoamingStorageQuota => 0;
		
		public  global::Windows.Storage.StorageFolder TemporaryFolder 
			=> new StorageFolder(GetTemporaryFolder());

		[Uno.NotImplemented]
		public uint Version => 0;
		
		public  global::Windows.Storage.StorageFolder LocalCacheFolder => new StorageFolder(GetLocalCacheFolder());

		public  global::Windows.Storage.StorageFolder SharedLocalFolder 
			=> new StorageFolder(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ".shared");

		public static global::Windows.Storage.ApplicationData Current => _instance;
		
		public  global::Windows.Foundation.IAsyncAction SetVersionAsync( uint desiredVersion,  global::Windows.Storage.ApplicationDataSetVersionHandler handler)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction ApplicationData.SetVersionAsync(uint desiredVersion, ApplicationDataSetVersionHandler handler) is not implemented in Uno.");
		}
		
		public  global::Windows.Foundation.IAsyncAction ClearAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction ApplicationData.ClearAsync() is not implemented in Uno.");
		}
		
		public  global::Windows.Foundation.IAsyncAction ClearAsync( global::Windows.Storage.ApplicationDataLocality locality)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction ApplicationData.ClearAsync(ApplicationDataLocality locality) is not implemented in Uno.");
		}
		
		public  void SignalDataChanged()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.ApplicationData", "void ApplicationData.SignalDataChanged()");
		}
		
		public  global::Windows.Storage.StorageFolder GetPublisherCacheFolder( string folderName)
		{
			throw new global::System.NotImplementedException("The member StorageFolder ApplicationData.GetPublisherCacheFolder(string folderName) is not implemented in Uno.");
		}
		
		public  global::Windows.Foundation.IAsyncAction ClearPublisherCacheFolderAsync( string folderName)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction ApplicationData.ClearPublisherCacheFolderAsync(string folderName) is not implemented in Uno.");
		}
		
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.ApplicationData> GetForUserAsync( global::Windows.System.User user)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<ApplicationData> ApplicationData.GetForUserAsync(User user) is not implemented in Uno.");
		}

		public event global::Windows.Foundation.TypedEventHandler<global::Windows.Storage.ApplicationData, object> DataChanged;
	}
}
#endif