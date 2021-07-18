#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Background
{
	#if false || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IBackgroundTaskRegistration3 : global::Windows.ApplicationModel.Background.IBackgroundTaskRegistration
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.ApplicationModel.Background.BackgroundTaskRegistrationGroup TaskGroup
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Background.IBackgroundTaskRegistration3.TaskGroup.get
	}
}
