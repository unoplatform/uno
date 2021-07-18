#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Background
{
	#if false || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IBackgroundTaskRegistration 
	{
		#if false || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string Name
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.Guid TaskId
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Background.IBackgroundTaskRegistration.TaskId.get
		// Forced skipping of method Windows.ApplicationModel.Background.IBackgroundTaskRegistration.Name.get
		// Forced skipping of method Windows.ApplicationModel.Background.IBackgroundTaskRegistration.Progress.add
		// Forced skipping of method Windows.ApplicationModel.Background.IBackgroundTaskRegistration.Progress.remove
		// Forced skipping of method Windows.ApplicationModel.Background.IBackgroundTaskRegistration.Completed.add
		// Forced skipping of method Windows.ApplicationModel.Background.IBackgroundTaskRegistration.Completed.remove
		#if false || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void Unregister( bool cancelTask);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		 event global::Windows.ApplicationModel.Background.BackgroundTaskCompletedEventHandler Completed;
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		 event global::Windows.ApplicationModel.Background.BackgroundTaskProgressEventHandler Progress;
		#endif
	}
}
