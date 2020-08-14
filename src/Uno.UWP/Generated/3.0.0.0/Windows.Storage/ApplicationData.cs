#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class ApplicationData 
	{
		#if false || false || NET461 || false || false || false || false
		[global::Uno.NotImplemented("NET461")]
		public  global::Windows.Storage.StorageFolder LocalFolder
		{
			get
			{
				throw new global::System.NotImplementedException("The member StorageFolder ApplicationData.LocalFolder is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || NET461 || false || false || false || false
		[global::Uno.NotImplemented("NET461")]
		public  global::Windows.Storage.ApplicationDataContainer LocalSettings
		{
			get
			{
				throw new global::System.NotImplementedException("The member ApplicationDataContainer ApplicationData.LocalSettings is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || NET461 || false || false || false || false
		[global::Uno.NotImplemented("NET461")]
		public  global::Windows.Storage.StorageFolder RoamingFolder
		{
			get
			{
				throw new global::System.NotImplementedException("The member StorageFolder ApplicationData.RoamingFolder is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || NET461 || false || false || false || false
		[global::Uno.NotImplemented("NET461")]
		public  global::Windows.Storage.ApplicationDataContainer RoamingSettings
		{
			get
			{
				throw new global::System.NotImplementedException("The member ApplicationDataContainer ApplicationData.RoamingSettings is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || NET461 || false || false || false || false
		[global::Uno.NotImplemented("NET461")]
		public  ulong RoamingStorageQuota
		{
			get
			{
				throw new global::System.NotImplementedException("The member ulong ApplicationData.RoamingStorageQuota is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || NET461 || false || false || false || false
		[global::Uno.NotImplemented("NET461")]
		public  global::Windows.Storage.StorageFolder TemporaryFolder
		{
			get
			{
				throw new global::System.NotImplementedException("The member StorageFolder ApplicationData.TemporaryFolder is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || NET461 || false || false || false || false
		[global::Uno.NotImplemented("NET461")]
		public  uint Version
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint ApplicationData.Version is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || NET461 || false || false || false || false
		[global::Uno.NotImplemented("NET461")]
		public  global::Windows.Storage.StorageFolder LocalCacheFolder
		{
			get
			{
				throw new global::System.NotImplementedException("The member StorageFolder ApplicationData.LocalCacheFolder is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || NET461 || false || false || false || false
		[global::Uno.NotImplemented("NET461")]
		public  global::Windows.Storage.StorageFolder SharedLocalFolder
		{
			get
			{
				throw new global::System.NotImplementedException("The member StorageFolder ApplicationData.SharedLocalFolder is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || NET461 || false || false || false || false
		[global::Uno.NotImplemented("NET461")]
		public static global::Windows.Storage.ApplicationData Current
		{
			get
			{
				throw new global::System.NotImplementedException("The member ApplicationData ApplicationData.Current is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Storage.ApplicationData.Version.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction SetVersionAsync( uint desiredVersion,  global::Windows.Storage.ApplicationDataSetVersionHandler handler)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction ApplicationData.SetVersionAsync(uint desiredVersion, ApplicationDataSetVersionHandler handler) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ClearAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction ApplicationData.ClearAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ClearAsync( global::Windows.Storage.ApplicationDataLocality locality)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction ApplicationData.ClearAsync(ApplicationDataLocality locality) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Storage.ApplicationData.LocalSettings.get
		// Forced skipping of method Windows.Storage.ApplicationData.RoamingSettings.get
		// Forced skipping of method Windows.Storage.ApplicationData.LocalFolder.get
		// Forced skipping of method Windows.Storage.ApplicationData.RoamingFolder.get
		// Forced skipping of method Windows.Storage.ApplicationData.TemporaryFolder.get
		// Forced skipping of method Windows.Storage.ApplicationData.DataChanged.add
		// Forced skipping of method Windows.Storage.ApplicationData.DataChanged.remove
		#if false || false || NET461 || false || false || false || false
		[global::Uno.NotImplemented("NET461")]
		public  void SignalDataChanged()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.ApplicationData", "void ApplicationData.SignalDataChanged()");
		}
		#endif
		// Forced skipping of method Windows.Storage.ApplicationData.RoamingStorageQuota.get
		// Forced skipping of method Windows.Storage.ApplicationData.LocalCacheFolder.get
		#if false || false || NET461 || false || false || false || false
		[global::Uno.NotImplemented("NET461")]
		public  global::Windows.Storage.StorageFolder GetPublisherCacheFolder( string folderName)
		{
			throw new global::System.NotImplementedException("The member StorageFolder ApplicationData.GetPublisherCacheFolder(string folderName) is not implemented in Uno.");
		}
		#endif
		#if false || false || NET461 || false || false || false || false
		[global::Uno.NotImplemented("NET461")]
		public  global::Windows.Foundation.IAsyncAction ClearPublisherCacheFolderAsync( string folderName)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction ApplicationData.ClearPublisherCacheFolderAsync(string folderName) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Storage.ApplicationData.SharedLocalFolder.get
		#if false || false || NET461 || false || false || false || false
		[global::Uno.NotImplemented("NET461")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.ApplicationData> GetForUserAsync( global::Windows.System.User user)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<ApplicationData> ApplicationData.GetForUserAsync(User user) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Storage.ApplicationData.Current.get
		#if false || false || NET461 || false || false || false || false
		[global::Uno.NotImplemented("NET461")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Storage.ApplicationData, object> DataChanged
		{
			[global::Uno.NotImplemented("NET461")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.ApplicationData", "event TypedEventHandler<ApplicationData, object> ApplicationData.DataChanged");
			}
			[global::Uno.NotImplemented("NET461")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.ApplicationData", "event TypedEventHandler<ApplicationData, object> ApplicationData.DataChanged");
			}
		}
		#endif
	}
}
