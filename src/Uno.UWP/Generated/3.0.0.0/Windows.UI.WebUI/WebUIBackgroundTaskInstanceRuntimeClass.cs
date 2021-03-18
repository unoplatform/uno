#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.WebUI
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WebUIBackgroundTaskInstanceRuntimeClass : global::Windows.UI.WebUI.IWebUIBackgroundTaskInstance,global::Windows.ApplicationModel.Background.IBackgroundTaskInstance
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint Progress
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint WebUIBackgroundTaskInstanceRuntimeClass.Progress is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIBackgroundTaskInstanceRuntimeClass", "uint WebUIBackgroundTaskInstanceRuntimeClass.Progress");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Guid InstanceId
		{
			get
			{
				throw new global::System.NotImplementedException("The member Guid WebUIBackgroundTaskInstanceRuntimeClass.InstanceId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint SuspendedCount
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint WebUIBackgroundTaskInstanceRuntimeClass.SuspendedCount is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Background.BackgroundTaskRegistration Task
		{
			get
			{
				throw new global::System.NotImplementedException("The member BackgroundTaskRegistration WebUIBackgroundTaskInstanceRuntimeClass.Task is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  object TriggerDetails
		{
			get
			{
				throw new global::System.NotImplementedException("The member object WebUIBackgroundTaskInstanceRuntimeClass.TriggerDetails is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Succeeded
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool WebUIBackgroundTaskInstanceRuntimeClass.Succeeded is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIBackgroundTaskInstanceRuntimeClass", "bool WebUIBackgroundTaskInstanceRuntimeClass.Succeeded");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.WebUI.WebUIBackgroundTaskInstanceRuntimeClass.Succeeded.get
		// Forced skipping of method Windows.UI.WebUI.WebUIBackgroundTaskInstanceRuntimeClass.Succeeded.set
		// Forced skipping of method Windows.UI.WebUI.WebUIBackgroundTaskInstanceRuntimeClass.InstanceId.get
		// Forced skipping of method Windows.UI.WebUI.WebUIBackgroundTaskInstanceRuntimeClass.Task.get
		// Forced skipping of method Windows.UI.WebUI.WebUIBackgroundTaskInstanceRuntimeClass.Progress.get
		// Forced skipping of method Windows.UI.WebUI.WebUIBackgroundTaskInstanceRuntimeClass.Progress.set
		// Forced skipping of method Windows.UI.WebUI.WebUIBackgroundTaskInstanceRuntimeClass.TriggerDetails.get
		// Forced skipping of method Windows.UI.WebUI.WebUIBackgroundTaskInstanceRuntimeClass.Canceled.add
		// Forced skipping of method Windows.UI.WebUI.WebUIBackgroundTaskInstanceRuntimeClass.Canceled.remove
		// Forced skipping of method Windows.UI.WebUI.WebUIBackgroundTaskInstanceRuntimeClass.SuspendedCount.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Background.BackgroundTaskDeferral GetDeferral()
		{
			throw new global::System.NotImplementedException("The member BackgroundTaskDeferral WebUIBackgroundTaskInstanceRuntimeClass.GetDeferral() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.ApplicationModel.Background.BackgroundTaskCanceledEventHandler Canceled
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIBackgroundTaskInstanceRuntimeClass", "event BackgroundTaskCanceledEventHandler WebUIBackgroundTaskInstanceRuntimeClass.Canceled");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIBackgroundTaskInstanceRuntimeClass", "event BackgroundTaskCanceledEventHandler WebUIBackgroundTaskInstanceRuntimeClass.Canceled");
			}
		}
		#endif
		// Processing: Windows.UI.WebUI.IWebUIBackgroundTaskInstance
		// Processing: Windows.ApplicationModel.Background.IBackgroundTaskInstance
	}
}
