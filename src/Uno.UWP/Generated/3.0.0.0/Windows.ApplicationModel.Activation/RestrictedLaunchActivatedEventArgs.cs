#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Activation
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class RestrictedLaunchActivatedEventArgs : global::Windows.ApplicationModel.Activation.IRestrictedLaunchActivatedEventArgs,global::Windows.ApplicationModel.Activation.IActivatedEventArgs,global::Windows.ApplicationModel.Activation.IActivatedEventArgsWithUser
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Activation.ActivationKind Kind
		{
			get
			{
				throw new global::System.NotImplementedException("The member ActivationKind RestrictedLaunchActivatedEventArgs.Kind is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=ActivationKind%20RestrictedLaunchActivatedEventArgs.Kind");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Activation.ApplicationExecutionState PreviousExecutionState
		{
			get
			{
				throw new global::System.NotImplementedException("The member ApplicationExecutionState RestrictedLaunchActivatedEventArgs.PreviousExecutionState is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=ApplicationExecutionState%20RestrictedLaunchActivatedEventArgs.PreviousExecutionState");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Activation.SplashScreen SplashScreen
		{
			get
			{
				throw new global::System.NotImplementedException("The member SplashScreen RestrictedLaunchActivatedEventArgs.SplashScreen is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=SplashScreen%20RestrictedLaunchActivatedEventArgs.SplashScreen");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.User User
		{
			get
			{
				throw new global::System.NotImplementedException("The member User RestrictedLaunchActivatedEventArgs.User is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=User%20RestrictedLaunchActivatedEventArgs.User");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  object SharedContext
		{
			get
			{
				throw new global::System.NotImplementedException("The member object RestrictedLaunchActivatedEventArgs.SharedContext is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=object%20RestrictedLaunchActivatedEventArgs.SharedContext");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Activation.RestrictedLaunchActivatedEventArgs.SharedContext.get
		// Forced skipping of method Windows.ApplicationModel.Activation.RestrictedLaunchActivatedEventArgs.Kind.get
		// Forced skipping of method Windows.ApplicationModel.Activation.RestrictedLaunchActivatedEventArgs.PreviousExecutionState.get
		// Forced skipping of method Windows.ApplicationModel.Activation.RestrictedLaunchActivatedEventArgs.SplashScreen.get
		// Forced skipping of method Windows.ApplicationModel.Activation.RestrictedLaunchActivatedEventArgs.User.get
		// Processing: Windows.ApplicationModel.Activation.IRestrictedLaunchActivatedEventArgs
		// Processing: Windows.ApplicationModel.Activation.IActivatedEventArgs
		// Processing: Windows.ApplicationModel.Activation.IActivatedEventArgsWithUser
	}
}
