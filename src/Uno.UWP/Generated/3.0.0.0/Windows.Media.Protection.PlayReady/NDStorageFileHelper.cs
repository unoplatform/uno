#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Protection.PlayReady
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class NDStorageFileHelper : global::Windows.Media.Protection.PlayReady.INDStorageFileHelper
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public NDStorageFileHelper() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Protection.PlayReady.NDStorageFileHelper", "NDStorageFileHelper.NDStorageFileHelper()");
		}
		#endif
		// Forced skipping of method Windows.Media.Protection.PlayReady.NDStorageFileHelper.NDStorageFileHelper()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<string> GetFileURLs( global::Windows.Storage.IStorageFile file)
		{
			throw new global::System.NotImplementedException("The member IList<string> NDStorageFileHelper.GetFileURLs(IStorageFile file) is not implemented in Uno.");
		}
		#endif
		// Processing: Windows.Media.Protection.PlayReady.INDStorageFileHelper
	}
}
