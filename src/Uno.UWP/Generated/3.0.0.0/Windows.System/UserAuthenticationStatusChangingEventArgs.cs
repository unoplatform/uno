#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class UserAuthenticationStatusChangingEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.UserAuthenticationStatus CurrentStatus
		{
			get
			{
				throw new global::System.NotImplementedException("The member UserAuthenticationStatus UserAuthenticationStatusChangingEventArgs.CurrentStatus is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=UserAuthenticationStatus%20UserAuthenticationStatusChangingEventArgs.CurrentStatus");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.UserAuthenticationStatus NewStatus
		{
			get
			{
				throw new global::System.NotImplementedException("The member UserAuthenticationStatus UserAuthenticationStatusChangingEventArgs.NewStatus is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=UserAuthenticationStatus%20UserAuthenticationStatusChangingEventArgs.NewStatus");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.User User
		{
			get
			{
				throw new global::System.NotImplementedException("The member User UserAuthenticationStatusChangingEventArgs.User is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=User%20UserAuthenticationStatusChangingEventArgs.User");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.UserAuthenticationStatusChangeDeferral GetDeferral()
		{
			throw new global::System.NotImplementedException("The member UserAuthenticationStatusChangeDeferral UserAuthenticationStatusChangingEventArgs.GetDeferral() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=UserAuthenticationStatusChangeDeferral%20UserAuthenticationStatusChangingEventArgs.GetDeferral%28%29");
		}
		#endif
		// Forced skipping of method Windows.System.UserAuthenticationStatusChangingEventArgs.User.get
		// Forced skipping of method Windows.System.UserAuthenticationStatusChangingEventArgs.NewStatus.get
		// Forced skipping of method Windows.System.UserAuthenticationStatusChangingEventArgs.CurrentStatus.get
	}
}
