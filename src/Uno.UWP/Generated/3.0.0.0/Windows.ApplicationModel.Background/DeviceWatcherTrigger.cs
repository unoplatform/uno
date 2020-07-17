#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Background
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DeviceWatcherTrigger : global::Windows.ApplicationModel.Background.IBackgroundTrigger
	{
		// Processing: Windows.ApplicationModel.Background.IBackgroundTrigger
	}
}
