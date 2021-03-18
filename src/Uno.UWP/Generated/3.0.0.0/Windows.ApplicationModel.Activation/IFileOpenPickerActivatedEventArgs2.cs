#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Activation
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IFileOpenPickerActivatedEventArgs2 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string CallerPackageFamilyName
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Activation.IFileOpenPickerActivatedEventArgs2.CallerPackageFamilyName.get
	}
}
