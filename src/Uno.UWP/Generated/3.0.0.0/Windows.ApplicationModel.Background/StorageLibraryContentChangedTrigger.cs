#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Background
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class StorageLibraryContentChangedTrigger : global::Windows.ApplicationModel.Background.IBackgroundTrigger
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.ApplicationModel.Background.StorageLibraryContentChangedTrigger Create( global::Windows.Storage.StorageLibrary storageLibrary)
		{
			throw new global::System.NotImplementedException("The member StorageLibraryContentChangedTrigger StorageLibraryContentChangedTrigger.Create(StorageLibrary storageLibrary) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.ApplicationModel.Background.StorageLibraryContentChangedTrigger CreateFromLibraries( global::System.Collections.Generic.IEnumerable<global::Windows.Storage.StorageLibrary> storageLibraries)
		{
			throw new global::System.NotImplementedException("The member StorageLibraryContentChangedTrigger StorageLibraryContentChangedTrigger.CreateFromLibraries(IEnumerable<StorageLibrary> storageLibraries) is not implemented in Uno.");
		}
		#endif
		// Processing: Windows.ApplicationModel.Background.IBackgroundTrigger
	}
}
