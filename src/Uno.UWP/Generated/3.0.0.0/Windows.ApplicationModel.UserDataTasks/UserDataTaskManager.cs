#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.UserDataTasks
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class UserDataTaskManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.User User
		{
			get
			{
				throw new global::System.NotImplementedException("The member User UserDataTaskManager.User is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=User%20UserDataTaskManager.User");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.UserDataTasks.UserDataTaskStore> RequestStoreAsync( global::Windows.ApplicationModel.UserDataTasks.UserDataTaskStoreAccessType accessType)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<UserDataTaskStore> UserDataTaskManager.RequestStoreAsync(UserDataTaskStoreAccessType accessType) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CUserDataTaskStore%3E%20UserDataTaskManager.RequestStoreAsync%28UserDataTaskStoreAccessType%20accessType%29");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.UserDataTasks.UserDataTaskManager.User.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.ApplicationModel.UserDataTasks.UserDataTaskManager GetDefault()
		{
			throw new global::System.NotImplementedException("The member UserDataTaskManager UserDataTaskManager.GetDefault() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=UserDataTaskManager%20UserDataTaskManager.GetDefault%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.ApplicationModel.UserDataTasks.UserDataTaskManager GetForUser( global::Windows.System.User user)
		{
			throw new global::System.NotImplementedException("The member UserDataTaskManager UserDataTaskManager.GetForUser(User user) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=UserDataTaskManager%20UserDataTaskManager.GetForUser%28User%20user%29");
		}
		#endif
	}
}
