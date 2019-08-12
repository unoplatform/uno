#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage.Provider
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CachedFileUpdater 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static void SetUpdateInformation( global::Windows.Storage.IStorageFile file,  string contentId,  global::Windows.Storage.Provider.ReadActivationMode readMode,  global::Windows.Storage.Provider.WriteActivationMode writeMode,  global::Windows.Storage.Provider.CachedFileOptions options)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Provider.CachedFileUpdater", "void CachedFileUpdater.SetUpdateInformation(IStorageFile file, string contentId, ReadActivationMode readMode, WriteActivationMode writeMode, CachedFileOptions options)");
		}
		#endif
	}
}
