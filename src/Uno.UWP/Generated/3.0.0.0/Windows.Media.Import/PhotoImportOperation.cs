#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Import
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PhotoImportOperation 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Media.Import.PhotoImportDeleteImportedItemsFromSourceResult, double> ContinueDeletingImportedItemsFromSourceAsync
		{
			get
			{
				throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<PhotoImportDeleteImportedItemsFromSourceResult, double> PhotoImportOperation.ContinueDeletingImportedItemsFromSourceAsync is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Media.Import.PhotoImportFindItemsResult, uint> ContinueFindingItemsAsync
		{
			get
			{
				throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<PhotoImportFindItemsResult, uint> PhotoImportOperation.ContinueFindingItemsAsync is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Media.Import.PhotoImportImportItemsResult, global::Windows.Media.Import.PhotoImportProgress> ContinueImportingItemsAsync
		{
			get
			{
				throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<PhotoImportImportItemsResult, PhotoImportProgress> PhotoImportOperation.ContinueImportingItemsAsync is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Import.PhotoImportSession Session
		{
			get
			{
				throw new global::System.NotImplementedException("The member PhotoImportSession PhotoImportOperation.Session is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Import.PhotoImportStage Stage
		{
			get
			{
				throw new global::System.NotImplementedException("The member PhotoImportStage PhotoImportOperation.Stage is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Import.PhotoImportOperation.Stage.get
		// Forced skipping of method Windows.Media.Import.PhotoImportOperation.Session.get
		// Forced skipping of method Windows.Media.Import.PhotoImportOperation.ContinueFindingItemsAsync.get
		// Forced skipping of method Windows.Media.Import.PhotoImportOperation.ContinueImportingItemsAsync.get
		// Forced skipping of method Windows.Media.Import.PhotoImportOperation.ContinueDeletingImportedItemsFromSourceAsync.get
	}
}
