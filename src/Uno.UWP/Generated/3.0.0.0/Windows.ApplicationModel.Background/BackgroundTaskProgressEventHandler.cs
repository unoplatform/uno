#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Background
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	public delegate void BackgroundTaskProgressEventHandler(global::Windows.ApplicationModel.Background.BackgroundTaskRegistration @sender, global::Windows.ApplicationModel.Background.BackgroundTaskProgressEventArgs @args);
	#endif
}
