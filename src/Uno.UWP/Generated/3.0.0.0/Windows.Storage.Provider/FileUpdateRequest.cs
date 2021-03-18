#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage.Provider
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class FileUpdateRequest 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Provider.FileUpdateStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member FileUpdateStatus FileUpdateRequest.Status is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Provider.FileUpdateRequest", "FileUpdateStatus FileUpdateRequest.Status");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string ContentId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string FileUpdateRequest.ContentId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.StorageFile File
		{
			get
			{
				throw new global::System.NotImplementedException("The member StorageFile FileUpdateRequest.File is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string UserInputNeededMessage
		{
			get
			{
				throw new global::System.NotImplementedException("The member string FileUpdateRequest.UserInputNeededMessage is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Provider.FileUpdateRequest", "string FileUpdateRequest.UserInputNeededMessage");
			}
		}
		#endif
		// Forced skipping of method Windows.Storage.Provider.FileUpdateRequest.ContentId.get
		// Forced skipping of method Windows.Storage.Provider.FileUpdateRequest.File.get
		// Forced skipping of method Windows.Storage.Provider.FileUpdateRequest.Status.get
		// Forced skipping of method Windows.Storage.Provider.FileUpdateRequest.Status.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Provider.FileUpdateRequestDeferral GetDeferral()
		{
			throw new global::System.NotImplementedException("The member FileUpdateRequestDeferral FileUpdateRequest.GetDeferral() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void UpdateLocalFile( global::Windows.Storage.IStorageFile value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Provider.FileUpdateRequest", "void FileUpdateRequest.UpdateLocalFile(IStorageFile value)");
		}
		#endif
		// Forced skipping of method Windows.Storage.Provider.FileUpdateRequest.UserInputNeededMessage.get
		// Forced skipping of method Windows.Storage.Provider.FileUpdateRequest.UserInputNeededMessage.set
	}
}
