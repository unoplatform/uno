#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class StorageLibraryLastChangeId 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static ulong Unknown
		{
			get
			{
				throw new global::System.NotImplementedException("The member ulong StorageLibraryLastChangeId.Unknown is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=ulong%20StorageLibraryLastChangeId.Unknown");
			}
		}
		#endif
		// Forced skipping of method Windows.Storage.StorageLibraryLastChangeId.Unknown.get
	}
}
