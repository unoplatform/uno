#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class StorageLibraryChangeTrackerOptions 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool TrackChangeDetails
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool StorageLibraryChangeTrackerOptions.TrackChangeDetails is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20StorageLibraryChangeTrackerOptions.TrackChangeDetails");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.StorageLibraryChangeTrackerOptions", "bool StorageLibraryChangeTrackerOptions.TrackChangeDetails");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public StorageLibraryChangeTrackerOptions() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.StorageLibraryChangeTrackerOptions", "StorageLibraryChangeTrackerOptions.StorageLibraryChangeTrackerOptions()");
		}
		#endif
		// Forced skipping of method Windows.Storage.StorageLibraryChangeTrackerOptions.StorageLibraryChangeTrackerOptions()
		// Forced skipping of method Windows.Storage.StorageLibraryChangeTrackerOptions.TrackChangeDetails.get
		// Forced skipping of method Windows.Storage.StorageLibraryChangeTrackerOptions.TrackChangeDetails.set
	}
}
