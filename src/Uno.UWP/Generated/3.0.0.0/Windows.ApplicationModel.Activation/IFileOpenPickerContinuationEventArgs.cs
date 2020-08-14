#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Activation
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IFileOpenPickerContinuationEventArgs : global::Windows.ApplicationModel.Activation.IContinuationActivatedEventArgs,global::Windows.ApplicationModel.Activation.IActivatedEventArgs
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.Collections.Generic.IReadOnlyList<global::Windows.Storage.StorageFile> Files
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Activation.IFileOpenPickerContinuationEventArgs.Files.get
	}
}
