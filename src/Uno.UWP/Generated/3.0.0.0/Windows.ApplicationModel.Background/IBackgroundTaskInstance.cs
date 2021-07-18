#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Background
{
	#if false || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IBackgroundTaskInstance 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.Guid InstanceId
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		uint Progress
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		uint SuspendedCount
		{
			get;
		}
		#endif
		#if false || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.ApplicationModel.Background.BackgroundTaskRegistration Task
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		object TriggerDetails
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Background.IBackgroundTaskInstance.InstanceId.get
		// Forced skipping of method Windows.ApplicationModel.Background.IBackgroundTaskInstance.Task.get
		// Forced skipping of method Windows.ApplicationModel.Background.IBackgroundTaskInstance.Progress.get
		// Forced skipping of method Windows.ApplicationModel.Background.IBackgroundTaskInstance.Progress.set
		// Forced skipping of method Windows.ApplicationModel.Background.IBackgroundTaskInstance.TriggerDetails.get
		// Forced skipping of method Windows.ApplicationModel.Background.IBackgroundTaskInstance.Canceled.add
		// Forced skipping of method Windows.ApplicationModel.Background.IBackgroundTaskInstance.Canceled.remove
		// Forced skipping of method Windows.ApplicationModel.Background.IBackgroundTaskInstance.SuspendedCount.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.ApplicationModel.Background.BackgroundTaskDeferral GetDeferral();
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		 event global::Windows.ApplicationModel.Background.BackgroundTaskCanceledEventHandler Canceled;
		#endif
	}
}
