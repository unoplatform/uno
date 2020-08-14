#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Activation
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface ILaunchActivatedEventArgs2 : global::Windows.ApplicationModel.Activation.ILaunchActivatedEventArgs,global::Windows.ApplicationModel.Activation.IActivatedEventArgs
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.ApplicationModel.Activation.TileActivatedInfo TileActivatedInfo
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Activation.ILaunchActivatedEventArgs2.TileActivatedInfo.get
	}
}
