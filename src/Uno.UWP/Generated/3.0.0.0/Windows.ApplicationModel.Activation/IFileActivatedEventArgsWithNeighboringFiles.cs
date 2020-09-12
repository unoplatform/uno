#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Activation
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IFileActivatedEventArgsWithNeighboringFiles : global::Windows.ApplicationModel.Activation.IFileActivatedEventArgs,global::Windows.ApplicationModel.Activation.IActivatedEventArgs
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Storage.Search.StorageFileQueryResult NeighboringFilesQuery
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Activation.IFileActivatedEventArgsWithNeighboringFiles.NeighboringFilesQuery.get
	}
}
