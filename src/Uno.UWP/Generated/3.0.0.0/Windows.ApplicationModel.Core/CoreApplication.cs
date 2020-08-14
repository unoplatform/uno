#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Core
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class CoreApplication 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static string Id
		{
			get
			{
				throw new global::System.NotImplementedException("The member string CoreApplication.Id is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.Collections.IPropertySet Properties
		{
			get
			{
				throw new global::System.NotImplementedException("The member IPropertySet CoreApplication.Properties is not implemented in Uno.");
			}
		}
		#endif
		// Skipping already declared property MainView
		// Skipping already declared property Views
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.ApplicationModel.Core.CoreApplicationView CreateNewView( global::Windows.ApplicationModel.Core.IFrameworkViewSource viewSource)
		{
			throw new global::System.NotImplementedException("The member CoreApplicationView CoreApplication.CreateNewView(IFrameworkViewSource viewSource) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.ApplicationModel.Core.CoreApplicationView CreateNewView()
		{
			throw new global::System.NotImplementedException("The member CoreApplicationView CoreApplication.CreateNewView() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Core.CoreApplication.UnhandledErrorDetected.add
		// Forced skipping of method Windows.ApplicationModel.Core.CoreApplication.UnhandledErrorDetected.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void IncrementApplicationUseCount()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Core.CoreApplication", "void CoreApplication.IncrementApplicationUseCount()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void DecrementApplicationUseCount()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Core.CoreApplication", "void CoreApplication.DecrementApplicationUseCount()");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Core.CoreApplication.Views.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.ApplicationModel.Core.CoreApplicationView CreateNewView( string runtimeType,  string entryPoint)
		{
			throw new global::System.NotImplementedException("The member CoreApplicationView CoreApplication.CreateNewView(string runtimeType, string entryPoint) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Core.CoreApplication.MainView.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void Exit()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Core.CoreApplication", "void CoreApplication.Exit()");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Core.CoreApplication.Exiting.add
		// Forced skipping of method Windows.ApplicationModel.Core.CoreApplication.Exiting.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Core.AppRestartFailureReason> RequestRestartAsync( string launchArguments)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<AppRestartFailureReason> CoreApplication.RequestRestartAsync(string launchArguments) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Core.AppRestartFailureReason> RequestRestartForUserAsync( global::Windows.System.User user,  string launchArguments)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<AppRestartFailureReason> CoreApplication.RequestRestartForUserAsync(User user, string launchArguments) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Core.CoreApplication.BackgroundActivated.add
		// Forced skipping of method Windows.ApplicationModel.Core.CoreApplication.BackgroundActivated.remove
		// Forced skipping of method Windows.ApplicationModel.Core.CoreApplication.LeavingBackground.add
		// Forced skipping of method Windows.ApplicationModel.Core.CoreApplication.LeavingBackground.remove
		// Forced skipping of method Windows.ApplicationModel.Core.CoreApplication.EnteredBackground.add
		// Forced skipping of method Windows.ApplicationModel.Core.CoreApplication.EnteredBackground.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void EnablePrelaunch( bool value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Core.CoreApplication", "void CoreApplication.EnablePrelaunch(bool value)");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Core.CoreApplication.Id.get
		// Forced skipping of method Windows.ApplicationModel.Core.CoreApplication.Suspending.add
		// Forced skipping of method Windows.ApplicationModel.Core.CoreApplication.Suspending.remove
		// Forced skipping of method Windows.ApplicationModel.Core.CoreApplication.Resuming.add
		// Forced skipping of method Windows.ApplicationModel.Core.CoreApplication.Resuming.remove
		// Forced skipping of method Windows.ApplicationModel.Core.CoreApplication.Properties.get
		// Skipping already declared method Windows.ApplicationModel.Core.CoreApplication.GetCurrentView()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void Run( global::Windows.ApplicationModel.Core.IFrameworkViewSource viewSource)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Core.CoreApplication", "void CoreApplication.Run(IFrameworkViewSource viewSource)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void RunWithActivationFactories( global::Windows.Foundation.IGetActivationFactory activationFactoryCallback)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Core.CoreApplication", "void CoreApplication.RunWithActivationFactories(IGetActivationFactory activationFactoryCallback)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static event global::System.EventHandler<global::Windows.ApplicationModel.Core.UnhandledErrorDetectedEventArgs> UnhandledErrorDetected
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Core.CoreApplication", "event EventHandler<UnhandledErrorDetectedEventArgs> CoreApplication.UnhandledErrorDetected");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Core.CoreApplication", "event EventHandler<UnhandledErrorDetectedEventArgs> CoreApplication.UnhandledErrorDetected");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static event global::System.EventHandler<object> Exiting
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Core.CoreApplication", "event EventHandler<object> CoreApplication.Exiting");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Core.CoreApplication", "event EventHandler<object> CoreApplication.Exiting");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static event global::System.EventHandler<global::Windows.ApplicationModel.Activation.BackgroundActivatedEventArgs> BackgroundActivated
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Core.CoreApplication", "event EventHandler<BackgroundActivatedEventArgs> CoreApplication.BackgroundActivated");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Core.CoreApplication", "event EventHandler<BackgroundActivatedEventArgs> CoreApplication.BackgroundActivated");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static event global::System.EventHandler<global::Windows.ApplicationModel.EnteredBackgroundEventArgs> EnteredBackground
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Core.CoreApplication", "event EventHandler<EnteredBackgroundEventArgs> CoreApplication.EnteredBackground");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Core.CoreApplication", "event EventHandler<EnteredBackgroundEventArgs> CoreApplication.EnteredBackground");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static event global::System.EventHandler<global::Windows.ApplicationModel.LeavingBackgroundEventArgs> LeavingBackground
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Core.CoreApplication", "event EventHandler<LeavingBackgroundEventArgs> CoreApplication.LeavingBackground");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Core.CoreApplication", "event EventHandler<LeavingBackgroundEventArgs> CoreApplication.LeavingBackground");
			}
		}
		#endif
		// Skipping already declared event Windows.ApplicationModel.Core.CoreApplication.Resuming
		// Skipping already declared event Windows.ApplicationModel.Core.CoreApplication.Suspending
	}
}
