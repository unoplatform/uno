#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class UserChangedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.User User
		{
			get
			{
				throw new global::System.NotImplementedException("The member User UserChangedEventArgs.User is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=User%20UserChangedEventArgs.User");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.System.UserWatcherUpdateKind> ChangedPropertyKinds
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<UserWatcherUpdateKind> UserChangedEventArgs.ChangedPropertyKinds is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3CUserWatcherUpdateKind%3E%20UserChangedEventArgs.ChangedPropertyKinds");
			}
		}
		#endif
		// Forced skipping of method Windows.System.UserChangedEventArgs.User.get
		// Forced skipping of method Windows.System.UserChangedEventArgs.ChangedPropertyKinds.get
	}
}
