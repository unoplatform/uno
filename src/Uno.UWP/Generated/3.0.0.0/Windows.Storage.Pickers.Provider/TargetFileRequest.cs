#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage.Pickers.Provider
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class TargetFileRequest 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.IStorageFile TargetFile
		{
			get
			{
				throw new global::System.NotImplementedException("The member IStorageFile TargetFileRequest.TargetFile is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Pickers.Provider.TargetFileRequest", "IStorageFile TargetFileRequest.TargetFile");
			}
		}
		#endif
		// Forced skipping of method Windows.Storage.Pickers.Provider.TargetFileRequest.TargetFile.get
		// Forced skipping of method Windows.Storage.Pickers.Provider.TargetFileRequest.TargetFile.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Pickers.Provider.TargetFileRequestDeferral GetDeferral()
		{
			throw new global::System.NotImplementedException("The member TargetFileRequestDeferral TargetFileRequest.GetDeferral() is not implemented in Uno.");
		}
		#endif
	}
}
