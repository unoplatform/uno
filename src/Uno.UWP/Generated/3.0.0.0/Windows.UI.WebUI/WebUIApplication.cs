#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.WebUI
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WebUIApplication 
	{
		// Forced skipping of method Windows.UI.WebUI.WebUIApplication.NewWebUIViewCreated.add
		// Forced skipping of method Windows.UI.WebUI.WebUIApplication.NewWebUIViewCreated.remove
		// Forced skipping of method Windows.UI.WebUI.WebUIApplication.BackgroundActivated.add
		// Forced skipping of method Windows.UI.WebUI.WebUIApplication.BackgroundActivated.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Core.AppRestartFailureReason> RequestRestartAsync( string launchArguments)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<AppRestartFailureReason> WebUIApplication.RequestRestartAsync(string launchArguments) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Core.AppRestartFailureReason> RequestRestartForUserAsync( global::Windows.System.User user,  string launchArguments)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<AppRestartFailureReason> WebUIApplication.RequestRestartForUserAsync(User user, string launchArguments) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.WebUI.WebUIApplication.LeavingBackground.add
		// Forced skipping of method Windows.UI.WebUI.WebUIApplication.LeavingBackground.remove
		// Forced skipping of method Windows.UI.WebUI.WebUIApplication.EnteredBackground.add
		// Forced skipping of method Windows.UI.WebUI.WebUIApplication.EnteredBackground.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void EnablePrelaunch( bool value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIApplication", "void WebUIApplication.EnablePrelaunch(bool value)");
		}
		#endif
		// Forced skipping of method Windows.UI.WebUI.WebUIApplication.Activated.add
		// Forced skipping of method Windows.UI.WebUI.WebUIApplication.Activated.remove
		// Forced skipping of method Windows.UI.WebUI.WebUIApplication.Suspending.add
		// Forced skipping of method Windows.UI.WebUI.WebUIApplication.Suspending.remove
		// Forced skipping of method Windows.UI.WebUI.WebUIApplication.Resuming.add
		// Forced skipping of method Windows.UI.WebUI.WebUIApplication.Resuming.remove
		// Forced skipping of method Windows.UI.WebUI.WebUIApplication.Navigated.add
		// Forced skipping of method Windows.UI.WebUI.WebUIApplication.Navigated.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static event global::Windows.UI.WebUI.BackgroundActivatedEventHandler BackgroundActivated
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIApplication", "event BackgroundActivatedEventHandler WebUIApplication.BackgroundActivated");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIApplication", "event BackgroundActivatedEventHandler WebUIApplication.BackgroundActivated");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static event global::System.EventHandler<global::Windows.UI.WebUI.NewWebUIViewCreatedEventArgs> NewWebUIViewCreated
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIApplication", "event EventHandler<NewWebUIViewCreatedEventArgs> WebUIApplication.NewWebUIViewCreated");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIApplication", "event EventHandler<NewWebUIViewCreatedEventArgs> WebUIApplication.NewWebUIViewCreated");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static event global::Windows.UI.WebUI.EnteredBackgroundEventHandler EnteredBackground
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIApplication", "event EnteredBackgroundEventHandler WebUIApplication.EnteredBackground");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIApplication", "event EnteredBackgroundEventHandler WebUIApplication.EnteredBackground");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static event global::Windows.UI.WebUI.LeavingBackgroundEventHandler LeavingBackground
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIApplication", "event LeavingBackgroundEventHandler WebUIApplication.LeavingBackground");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIApplication", "event LeavingBackgroundEventHandler WebUIApplication.LeavingBackground");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static event global::Windows.UI.WebUI.ActivatedEventHandler Activated
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIApplication", "event ActivatedEventHandler WebUIApplication.Activated");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIApplication", "event ActivatedEventHandler WebUIApplication.Activated");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static event global::Windows.UI.WebUI.NavigatedEventHandler Navigated
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIApplication", "event NavigatedEventHandler WebUIApplication.Navigated");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIApplication", "event NavigatedEventHandler WebUIApplication.Navigated");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static event global::Windows.UI.WebUI.ResumingEventHandler Resuming
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIApplication", "event ResumingEventHandler WebUIApplication.Resuming");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIApplication", "event ResumingEventHandler WebUIApplication.Resuming");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static event global::Windows.UI.WebUI.SuspendingEventHandler Suspending
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIApplication", "event SuspendingEventHandler WebUIApplication.Suspending");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WebUI.WebUIApplication", "event SuspendingEventHandler WebUIApplication.Suspending");
			}
		}
		#endif
	}
}
